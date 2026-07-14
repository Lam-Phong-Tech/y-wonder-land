using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Serializes gameplay economy/inventory deltas so local interactions remain
    /// responsive while the same changes are persisted by the authenticated server.
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
            public string token;
            public string scopeId;
            public string itemId;
            public int quantityDelta;
            public long posDelta;
            public long uposDelta;
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
            public long delta_upos;
            public string type;
            public string idempotency_key;
        }

        [Serializable]
        private sealed class MutationResponse
        {
            public bool ok;
            public bool duplicate;
        }

        private static readonly Queue<PendingMutation> Pending = new Queue<PendingMutation>();
        private static bool processing;
        private static bool blockedByTransientFailure;
        private static bool reconciliationRequired;

        public static int PendingCount => Pending.Count;
        public static bool RequiresAuthoritativeSnapshot => reconciliationRequired;
        public static string LastError { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Pending.Clear();
            processing = false;
            blockedByTransientFailure = false;
            reconciliationRequired = false;
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
            if (!TryCaptureSession(out string token, out string scopeId)) return;

            Pending.Enqueue(new PendingMutation
            {
                kind = MutationKind.Inventory,
                token = token,
                scopeId = scopeId,
                itemId = itemId,
                quantityDelta = quantityDelta,
                reason = NormalizeReason(reason, "gameplay_inventory"),
                idempotencyKey = Guid.NewGuid().ToString("N")
            });
            StartProcessing();
        }

        public static void QueueEconomyDelta(long posDelta, long uposDelta, string reason)
        {
            if (posDelta == 0 && uposDelta == 0) return;
            if (!TryCaptureSession(out string token, out string scopeId)) return;

            Pending.Enqueue(new PendingMutation
            {
                kind = MutationKind.Economy,
                token = token,
                scopeId = scopeId,
                posDelta = posDelta,
                uposDelta = uposDelta,
                reason = NormalizeReason(reason, "gameplay_economy"),
                idempotencyKey = Guid.NewGuid().ToString("N")
            });
            StartProcessing();
        }

        /// <summary>
        /// Waits for mutations already queued by gameplay. Call before bootstrap,
        /// shop transactions, and sign-out so a later server snapshot cannot restore
        /// stale quantities.
        /// </summary>
        public static async Awaitable<bool> FlushAsync(float timeoutSeconds = 12f)
        {
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

        private static bool TryCaptureSession(out string token, out string scopeId)
        {
            var auth = AuthService.Instance;
            token = auth != null ? auth.Token : "";
            scopeId = AuthService.GetCurrentPlayerScopeId();
            return auth != null
                   && auth.IsSignedIn
                   && !string.IsNullOrWhiteSpace(token)
                   && !string.IsNullOrWhiteSpace(scopeId);
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
                    PendingMutation mutation = Pending.Peek();
                    ApiResult<MutationResponse> result = await SendAsync(mutation);

                    if (!result.ok && IsTransient(result.status))
                    {
                        // Retry once with the same key. If the first request committed
                        // but its response was lost, the server returns the same result.
                        await Awaitable.NextFrameAsync();
                        result = await SendAsync(mutation);
                    }

                    if (result.ok)
                    {
                        Pending.Dequeue();
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

        private static Awaitable<ApiResult<MutationResponse>> SendAsync(PendingMutation mutation)
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
                    mutation.token);
            }

            return ApiClient.PostAsync<MutationResponse>(
                "/player/economy/apply",
                new EconomyAdjustRequest
                {
                    delta_pos = mutation.posDelta,
                    delta_upos = mutation.uposDelta,
                    type = mutation.reason,
                    idempotency_key = mutation.idempotencyKey
                },
                mutation.token);
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
            return $"economy Point {mutation.posDelta:+#;-#;0}, UPoint {mutation.uposDelta:+#;-#;0}";
        }
    }
}
