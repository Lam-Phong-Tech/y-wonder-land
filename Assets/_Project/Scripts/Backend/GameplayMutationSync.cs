using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Serializes gameplay economy/inventory deltas so local interactions remain
    /// responsive while the same changes are persisted by the authenticated server.
    ///
    /// Hàng đợi delta được LƯU XUỐNG ĐĨA (per-player, KHÔNG lưu token) nên nếu app
    /// đóng/mất kết nối lúc còn delta chưa gửi thì KHÔNG mất: lần chạy sau (khi có phiên
    /// đăng nhập) sẽ nạp lại và flush TRƯỚC khi bootstrap kéo snapshot server về. Server
    /// dedup theo idempotency_key nên gửi lại delta cũ không bị nhân đôi.
    /// </summary>
    public static class GameplayMutationSync
    {
        private enum MutationKind
        {
            Inventory,
            Economy
        }

        private sealed class PendingMutation
        {
            public MutationKind kind;
            public string scopeId;
            public string itemId;
            public int quantityDelta;
            public long posDelta;
            public string reason;
            public string idempotencyKey;
        }

        [Serializable]
        private sealed class InventoryAdjustRequest
        {
            public string item_id;
            public int quantity_delta;
            public string type;
            public string idempotency_key;
        }

        [Serializable]
        private sealed class EconomyAdjustRequest
        {
            public long delta_pos;
            public string type;
            public string idempotency_key;
        }

        [Serializable]
        private sealed class MutationResponse
        {
            public bool ok;
            public bool duplicate;
        }

        // ── Lưu đĩa (per-player qua PlayerScopedPrefs). KHÔNG bao giờ ghi token vào đây. ──
        private const string PersistKey = "PendingMutationQueue_v1";

        [Serializable]
        private sealed class PersistedMutation
        {
            public int kind;
            public string scopeId;
            public string itemId;
            public int quantityDelta;
            public long posDelta;
            public string reason;
            public string idempotencyKey;
        }

        [Serializable]
        private sealed class PersistedQueue
        {
            public List<PersistedMutation> items = new List<PersistedMutation>();
        }

        private static readonly Queue<PendingMutation> Pending = new Queue<PendingMutation>();
        private static bool processing;
        private static bool blockedByTransientFailure;
        private static bool reconciliationRequired;
        // scope mà hàng đợi in-memory đang thuộc về; null = chưa nạp từ đĩa lần nào.
        private static string restoredScopeId;

        public static int PendingCount => Pending.Count;
        public static bool RequiresAuthoritativeSnapshot => reconciliationRequired;
        public static string LastError { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            // Chỉ xoá TRẠNG THÁI RAM; KHÔNG đụng bản lưu đĩa (đó là chỗ để khôi phục sau restart).
            Pending.Clear();
            processing = false;
            blockedByTransientFailure = false;
            reconciliationRequired = false;
            restoredScopeId = null;
            LastError = "";
        }

        public static void MarkAuthoritativeSnapshotApplied()
        {
            reconciliationRequired = false;
            if (Pending.Count == 0) LastError = "";
        }

        public static void QueueInventoryDelta(string itemId, int quantityDelta, string reason)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantityDelta == 0) return;
            if (!TryCaptureScope(out string scopeId)) return;

            EnsureRestoredForCurrentScope();
            Pending.Enqueue(new PendingMutation
            {
                kind = MutationKind.Inventory,
                scopeId = scopeId,
                itemId = itemId,
                quantityDelta = quantityDelta,
                reason = NormalizeReason(reason, "gameplay_inventory"),
                idempotencyKey = Guid.NewGuid().ToString("N")
            });
            Persist();
            StartProcessing();
        }

        public static void QueueEconomyDelta(long posDelta, string reason)
        {
            if (posDelta == 0) return;
            if (!TryCaptureScope(out string scopeId)) return;

            EnsureRestoredForCurrentScope();
            Pending.Enqueue(new PendingMutation
            {
                kind = MutationKind.Economy,
                scopeId = scopeId,
                posDelta = posDelta,
                reason = NormalizeReason(reason, "gameplay_economy"),
                idempotencyKey = Guid.NewGuid().ToString("N")
            });
            Persist();
            StartProcessing();
        }

        /// <summary>
        /// Waits for mutations already queued by gameplay. Call before bootstrap,
        /// shop transactions, and sign-out so a later server snapshot cannot restore
        /// stale quantities. Nạp lại delta đã lưu đĩa TRƯỚC khi flush.
        /// </summary>
        public static async Awaitable<bool> FlushAsync(float timeoutSeconds = 12f)
        {
            EnsureRestoredForCurrentScope();
            if (Pending.Count == 0 && !processing) return !reconciliationRequired;

            blockedByTransientFailure = false;
            StartProcessing();
            float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, timeoutSeconds);

            while (processing && Time.realtimeSinceStartup < deadline)
                await Awaitable.NextFrameAsync();

            bool completed = Pending.Count == 0 && !reconciliationRequired;
            if (!completed && string.IsNullOrEmpty(LastError))
                LastError = "SYNC_TIMEOUT";
            return completed;
        }

        // Chỉ cần có phiên (đăng nhập + scope) để ghi delta. Token lấy TƯƠI lúc gửi (không lưu).
        private static bool TryCaptureScope(out string scopeId)
        {
            var auth = AuthService.Instance;
            scopeId = AuthService.GetCurrentPlayerScopeId();
            return auth != null && auth.IsSignedIn && !string.IsNullOrWhiteSpace(scopeId);
        }

        // Nạp lại hàng đợi từ đĩa cho scope hiện tại (1 lần/scope). Đổi scope -> nạp lại đúng người.
        private static void EnsureRestoredForCurrentScope()
        {
            if (processing) return; // đừng đụng hàng đợi khi đang gửi dở

            string scope = PlayerScopedPrefs.CurrentScopeId ?? "";
            if (scope == restoredScopeId) return;

            // Delta của scope cũ đã nằm ở đĩa (theo key scope cũ) nên xoá RAM an toàn, rồi nạp scope mới.
            Pending.Clear();
            restoredScopeId = scope;

            string json = PlayerScopedPrefs.GetString(PersistKey, "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var dto = JsonUtility.FromJson<PersistedQueue>(json);
                if (dto?.items == null) return;
                foreach (var p in dto.items)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.idempotencyKey)) continue;
                    Pending.Enqueue(new PendingMutation
                    {
                        kind = (MutationKind)p.kind,
                        scopeId = p.scopeId,
                        itemId = p.itemId,
                        quantityDelta = p.quantityDelta,
                        posDelta = p.posDelta,
                        reason = p.reason,
                        idempotencyKey = p.idempotencyKey
                    });
                }
                if (Pending.Count > 0)
                    Debug.Log($"[StateSync] Khôi phục {Pending.Count} delta chưa gửi từ phiên trước cho '{scope}'.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StateSync] Không đọc được hàng đợi đã lưu: {ex.Message}");
            }
        }

        // Ghi toàn bộ hàng đợi hiện tại xuống đĩa (theo scope hiện tại). KHÔNG kèm token.
        private static void Persist()
        {
            var dto = new PersistedQueue();
            foreach (var m in Pending)
            {
                dto.items.Add(new PersistedMutation
                {
                    kind = (int)m.kind,
                    scopeId = m.scopeId,
                    itemId = m.itemId,
                    quantityDelta = m.quantityDelta,
                    posDelta = m.posDelta,
                    reason = m.reason,
                    idempotencyKey = m.idempotencyKey
                });
            }
            PlayerScopedPrefs.SetString(PersistKey, JsonUtility.ToJson(dto));
            PlayerScopedPrefs.Save();
        }

        private static void StartProcessing()
        {
            if (processing || Pending.Count == 0) return;
            blockedByTransientFailure = false;
            _ = ProcessQueueAsync();
        }

        private static async Awaitable ProcessQueueAsync()
        {
            if (processing) return;
            processing = true;
            LastError = "";

            try
            {
                while (Pending.Count > 0)
                {
                    // Token lấy TƯƠI mỗi lần (không dùng token đã lưu). Chưa có phiên -> giữ hàng đợi, thử lại sau.
                    string token = AuthService.Instance != null ? AuthService.Instance.Token : "";
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        blockedByTransientFailure = true;
                        LastError = "NO_SESSION";
                        break;
                    }

                    PendingMutation mutation = Pending.Peek();
                    ApiResult<MutationResponse> result = await SendAsync(mutation, token);

                    if (!result.ok && IsTransient(result.status))
                    {
                        // Retry once with the same key. If the first request committed
                        // but its response was lost, the server returns the same result.
                        await Awaitable.NextFrameAsync();
                        result = await SendAsync(mutation, token);
                    }

                    if (result.ok)
                    {
                        Pending.Dequeue();
                        Persist();
                        continue;
                    }

                    LastError = !string.IsNullOrWhiteSpace(result.errorCode)
                        ? result.errorCode
                        : (!string.IsNullOrWhiteSpace(result.error) ? result.error : "STATE_SYNC_FAILED");

                    if (IsTransient(result.status))
                    {
                        blockedByTransientFailure = true;
                        Debug.LogWarning($"[StateSync] Deferred {Describe(mutation)} for '{mutation.scopeId}': {LastError}");
                        break;
                    }

                    // A permanent 4xx cannot succeed on retry. Drop only that delta so
                    // later valid changes are not blocked; the next bootstrap reconciles
                    // the local cache with the authoritative server snapshot.
                    Pending.Dequeue();
                    Persist();
                    reconciliationRequired = true;
                    Debug.LogError($"[StateSync] Rejected {Describe(mutation)} for '{mutation.scopeId}': {LastError}");
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                blockedByTransientFailure = true;
                Debug.LogException(ex);
            }
            finally
            {
                processing = false;
            }

            if (Pending.Count > 0 && !blockedByTransientFailure)
                StartProcessing();
        }

        private static Awaitable<ApiResult<MutationResponse>> SendAsync(PendingMutation mutation, string token)
        {
            if (mutation.kind == MutationKind.Inventory)
            {
                return ApiClient.PostAsync<MutationResponse>(
                    "/player/inventory/adjust",
                    new InventoryAdjustRequest
                    {
                        item_id = mutation.itemId,
                        quantity_delta = mutation.quantityDelta,
                        type = mutation.reason,
                        idempotency_key = mutation.idempotencyKey
                    },
                    token);
            }

            return ApiClient.PostAsync<MutationResponse>(
                "/player/economy/apply",
                new EconomyAdjustRequest
                {
                    delta_pos = mutation.posDelta,
                    type = mutation.reason,
                    idempotency_key = mutation.idempotencyKey
                },
                token);
        }

        private static bool IsTransient(long status) => status == 0 || status >= 500;

        private static string NormalizeReason(string reason, string fallback)
        {
            return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
        }

        private static string Describe(PendingMutation mutation)
        {
            if (mutation.kind == MutationKind.Inventory)
                return $"inventory {mutation.itemId} {mutation.quantityDelta:+#;-#;0}";
            return $"economy Point {mutation.posDelta:+#;-#;0}";
        }
    }
}
