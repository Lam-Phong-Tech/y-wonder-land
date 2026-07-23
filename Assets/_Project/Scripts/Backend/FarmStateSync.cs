using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using YWonderLand.Environment;
using YWonderLand.Managers;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Keeps the four legacy farm PlayerPrefs snapshots as a local cache while
    /// mirroring one authoritative snapshot through /player/farm-state.
    /// </summary>
    public static class FarmStateSync
    {
        public const string BuildStateKey = "YW_BuildState";
        public const string PlacedTilesKey = "YW_PlacedTiles";
        public const string FarmTilesKey = "YW_FarmState";
        public const string AnimalStateKey = "YW_AnimalState";

        private const int SnapshotSchema = 1;
        private const int MaxRequestBytes = 480 * 1024;
        private const string OutboxKey = "YW_FarmSyncOutbox";
        private const string EmptyBuildState = "{\"items\":[],\"animals\":[]}";
        private const string EmptyPlacedTiles = "{\"tiles\":[]}";
        private const string EmptyFarmTiles = "{\"tiles\":[]}";
        private const string EmptyAnimalState = "{\"animals\":[]}";

        private sealed class PendingUpload
        {
            public string token;
            public string scopeId;
            public int expectedVersion;
            public PlayerBootstrapService.FarmStatePayload payload;
        }

        [Serializable]
        private sealed class PersistedOutbox
        {
            public string scopeId;
            public int expectedVersion;
            public PlayerBootstrapService.FarmStatePayload payload;
        }

        [Serializable]
        private sealed class FarmStateRequest
        {
            [JsonProperty("expected_version")]
            public int expectedVersion;

            public PlayerBootstrapService.FarmStatePayload farm_state;
        }

        [Serializable]
        private sealed class FarmStateResponse
        {
            public bool ok;
            public PlayerBootstrapService.FarmStatePayload farm_state;
        }

        [Serializable]
        private sealed class AnimalPlacementRequest
        {
            [JsonProperty("item_id")]
            public string itemId;

            [JsonProperty("cell_keys")]
            public List<string> cellKeys;

            [JsonProperty("expected_version")]
            public int expectedVersion;

            [JsonProperty("idempotency_key")]
            public string idempotencyKey;
        }

        [Serializable]
        private sealed class AnimalPlacementResponse
        {
            public bool ok;
            public bool duplicate;
            public PlayerBootstrapService.InventoryPayload inventory;
            public PlayerBootstrapService.FarmStatePayload farm_state;
        }

        public sealed class AnimalPlacementResult
        {
            public bool ok;
            public bool duplicate;
            public long status;
            public string errorCode;
            public int remainingQuantity;
        }

        private static PendingUpload pending;
        private static bool processing;
        private static bool blockedByTransientFailure;
        private static bool applyingServerSnapshot;
        private static string reconciledScopeId = "";
        private static int serverVersion = 1;

        public static event Action AuthoritativeSnapshotApplied;

        public static int PendingCount => pending != null || HasPersistedOutbox() ? 1 : 0;
        public static string LastError { get; private set; } = "";
        public static bool IsCurrentPlayerReconciled =>
            !string.IsNullOrEmpty(reconciledScopeId)
            && string.Equals(reconciledScopeId, PlayerScopedPrefs.CurrentScopeId, StringComparison.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            pending = null;
            processing = false;
            blockedByTransientFailure = false;
            applyingServerSnapshot = false;
            reconciledScopeId = "";
            serverVersion = 1;
            LastError = "";
            AuthoritativeSnapshotApplied = null;
        }

        /// <summary>Prevents a newly authenticated device from uploading its cache before bootstrap.</summary>
        public static void MarkIdentityNeedsBootstrap()
        {
            reconciledScopeId = "";
            serverVersion = 1;
            pending = null;
            blockedByTransientFailure = false;
            LastError = "";
        }

        /// <summary>
        /// Applies a server snapshot, or uploads this device's legacy local save once
        /// when the server has no snapshot yet.
        /// </summary>
        public static async Awaitable<bool> ReconcileBootstrapAsync(PlayerBootstrapService.FarmStatePayload serverState)
        {
            var auth = AuthService.Instance;
            string scopeId = PlayerScopedPrefs.CurrentScopeId;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrEmpty(auth.Token) || string.IsNullOrEmpty(scopeId))
            {
                LastError = "NO_AUTH_TOKEN";
                return false;
            }

            serverVersion = Mathf.Max(1, serverState != null ? serverState.version : 1);
            reconciledScopeId = scopeId;

            if (serverState != null && serverState.snapshotSchema >= SnapshotSchema)
            {
                int localLegacyScore = CurrentContentScore();
                if (serverState.legacyMigration && localLegacyScore > serverState.legacyContentScore)
                {
                    QueueCurrentSnapshotInternal(auth.Token, scopeId, true, true);
                    bool upgraded = await FlushAsync();
                    if (!upgraded)
                    {
                        LastError = string.IsNullOrEmpty(LastError) ? "FARM_MIGRATION_UPGRADE_FAILED" : LastError;
                        return false;
                    }

                    Debug.Log($"[FarmStateSync] Upgraded legacy farm snapshot from score {serverState.legacyContentScore} to {localLegacyScore}.");
                    return true;
                }

                ApplyServerSnapshot(serverState);
                LastError = "";
                Debug.Log($"[FarmStateSync] Applied authoritative farm snapshot v{serverVersion} for '{scopeId}'.");
                return true;
            }

            // One-time migration for accounts created before farm snapshots were
            // server-backed. The first updated device supplies its existing save.
            QueueCurrentSnapshotInternal(auth.Token, scopeId, true, true);
            bool uploaded = await FlushAsync();
            if (uploaded)
                Debug.Log($"[FarmStateSync] Migrated legacy local farm snapshot for '{scopeId}'.");
            else
                Debug.LogWarning($"[FarmStateSync] Legacy farm snapshot migration deferred: {LastError}");
            return uploaded;
        }

        /// <summary>Called after one of the four local persistence owners saves.</summary>
        public static void NotifyLocalStateSaved()
        {
            if (applyingServerSnapshot || !IsCurrentPlayerReconciled) return;

            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrEmpty(auth.Token)) return;
            QueueCurrentSnapshotInternal(auth.Token, reconciledScopeId, false, false);
        }

        /// <summary>Persists the runtime farm immediately before logout/suspend.</summary>
        public static void SaveRuntimeState()
        {
            SaveBuildState();
            TilePlacementSystem.Instance?.SaveTiles();
            FarmManager.Instance?.SaveFarmState();
            AnimalManager.Instance?.SaveAnimalState();
        }

        /// <summary>Persists buildings, pens, crops on build plots, and pen animals.</summary>
        public static void SaveBuildState()
        {
            var buildPersistence = UnityEngine.Object.FindFirstObjectByType<BuildPersistence>(FindObjectsInactive.Include);
            buildPersistence?.SaveBuildings();
        }

        /// <summary>Persists whichever legacy snapshot owns this FarmTile.</summary>
        public static void SaveTileState(FarmTile tile)
        {
            if (tile == null) return;

            if (tile.GetComponentInParent<TilePlacementSystem>() != null)
            {
                TilePlacementSystem.Instance?.SaveTiles();
                return;
            }

            if (tile.GetComponentInParent<PlacedBuilding>() != null)
            {
                var buildPersistence = UnityEngine.Object.FindFirstObjectByType<BuildPersistence>(FindObjectsInactive.Include);
                buildPersistence?.SaveBuildings();
                return;
            }

            FarmManager.Instance?.SaveFarmState();
        }

        public static async Awaitable<bool> FlushAsync(float timeoutSeconds = 12f)
        {
            RestorePendingFromOutbox();
            if (pending == null && !processing) return string.IsNullOrEmpty(LastError);

            blockedByTransientFailure = false;
            StartProcessing();
            float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, timeoutSeconds);
            while ((processing || pending != null) && Time.realtimeSinceStartup < deadline)
                await Awaitable.NextFrameAsync();

            bool completed = pending == null && !processing;
            if (!completed && string.IsNullOrEmpty(LastError)) LastError = "FARM_SYNC_TIMEOUT";
            return completed && string.IsNullOrEmpty(LastError);
        }

        /// <summary>
        /// Atomically consumes one animal item and appends the animal to the
        /// authoritative build snapshot. There is intentionally no offline fallback.
        /// </summary>
        public static async Awaitable<AnimalPlacementResult> PlaceAnimalAsync(
            string itemId,
            List<string> cellKeys)
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrEmpty(auth.Token))
                return PlacementFailure(0, "NO_AUTH_TOKEN");
            if (!IsCurrentPlayerReconciled)
                return PlacementFailure(409, "FARM_STATE_NOT_RECONCILED");
            if (string.IsNullOrWhiteSpace(itemId) || cellKeys == null || cellKeys.Count == 0)
                return PlacementFailure(400, "INVALID_ANIMAL_PLACEMENT");

            // The pen may have been built immediately before selecting the animal.
            // Flush it first so the server can validate every requested pen cell.
            SaveBuildState();
            if (!await FlushAsync())
                return PlacementFailure(0, string.IsNullOrEmpty(LastError) ? "FARM_SYNC_FAILED" : LastError);

            var request = new AnimalPlacementRequest
            {
                itemId = itemId.Trim(),
                cellKeys = new List<string>(cellKeys),
                expectedVersion = Mathf.Max(1, serverVersion),
                idempotencyKey = Guid.NewGuid().ToString("N")
            };

            ApiResult<AnimalPlacementResponse> response = await SendAnimalPlacementAsync(request, auth.Token);
            if (!response.ok && IsTransient(response.status))
            {
                await Awaitable.NextFrameAsync();
                response = await SendAnimalPlacementAsync(request, auth.Token);
            }

            if (response.ok && response.data != null
                && response.data.inventory != null
                && response.data.farm_state != null)
            {
                int remainingQuantity = GetInventoryQuantity(response.data.inventory, itemId);
                serverVersion = Mathf.Max(serverVersion, response.data.farm_state.version);
                ApplyInventorySnapshot(response.data.inventory);
                ApplyServerSnapshot(response.data.farm_state);
                LastError = "";
                return new AnimalPlacementResult
                {
                    ok = true,
                    duplicate = response.data.duplicate,
                    status = response.status,
                    errorCode = "",
                    remainingQuantity = remainingQuantity
                };
            }

            if (response.data != null)
            {
                if (response.data.inventory != null)
                    ApplyInventorySnapshot(response.data.inventory);
                if (response.data.farm_state != null)
                {
                    serverVersion = Mathf.Max(1, response.data.farm_state.version);
                    ApplyServerSnapshot(response.data.farm_state);
                }
            }

            string errorCode = !string.IsNullOrWhiteSpace(response.errorCode)
                ? response.errorCode
                : (!string.IsNullOrWhiteSpace(response.error) ? response.error : "ANIMAL_PLACEMENT_FAILED");
            LastError = errorCode;
            return PlacementFailure(response.status, errorCode);
        }

        public static string CellKey(Vector3 position)
        {
            return $"{Mathf.RoundToInt(position.x * 10f)}_{Mathf.RoundToInt(position.z * 10f)}";
        }

        private static void ApplyServerSnapshot(PlayerBootstrapService.FarmStatePayload state)
        {
            applyingServerSnapshot = true;
            try
            {
                PlayerScopedPrefs.SetString(BuildStateKey, ValidOrDefault(state.buildStateJson, EmptyBuildState));
                PlayerScopedPrefs.SetString(PlacedTilesKey, ValidOrDefault(state.placedTilesJson, EmptyPlacedTiles));
                PlayerScopedPrefs.SetString(FarmTilesKey, ValidOrDefault(state.farmTilesJson, EmptyFarmTiles));
                PlayerScopedPrefs.SetString(AnimalStateKey, ValidOrDefault(state.animalStateJson, EmptyAnimalState));
                PlayerScopedPrefs.Save();
            }
            finally
            {
                applyingServerSnapshot = false;
            }

            InvokeSnapshotAppliedHandlers();
        }

        private static void QueueCurrentSnapshotInternal(
            string token,
            string scopeId,
            bool force,
            bool legacyMigration)
        {
            if (applyingServerSnapshot || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(scopeId)) return;
            if (!force && !string.Equals(scopeId, reconciledScopeId, StringComparison.Ordinal)) return;

            var payload = new PlayerBootstrapService.FarmStatePayload
            {
                version = Mathf.Max(2, serverVersion + 1),
                snapshotSchema = SnapshotSchema,
                buildStateJson = PlayerScopedPrefs.GetString(BuildStateKey, EmptyBuildState),
                placedTilesJson = PlayerScopedPrefs.GetString(PlacedTilesKey, EmptyPlacedTiles),
                farmTilesJson = PlayerScopedPrefs.GetString(FarmTilesKey, EmptyFarmTiles),
                animalStateJson = PlayerScopedPrefs.GetString(AnimalStateKey, EmptyAnimalState),
                legacyMigration = legacyMigration,
                clientSavedAt = DateTime.UtcNow.ToString("o")
            };
            payload.legacyContentScore = ContentScore(payload);

            int requestBytes = Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(
                new FarmStateRequest { farm_state = payload }));
            if (requestBytes > MaxRequestBytes)
            {
                LastError = "FARM_SNAPSHOT_TOO_LARGE";
                Debug.LogError($"[FarmStateSync] Refusing oversized farm snapshot ({requestBytes} bytes).");
                return;
            }

            pending = new PendingUpload
            {
                token = token,
                scopeId = scopeId,
                expectedVersion = Mathf.Max(1, serverVersion),
                payload = payload
            };
            PersistOutbox(pending);
            LastError = "";
            StartProcessing();
        }

        private static void StartProcessing()
        {
            if (processing || pending == null) return;
            blockedByTransientFailure = false;
            _ = ProcessQueueAsync();
        }

        private static async Awaitable ProcessQueueAsync()
        {
            if (processing) return;
            processing = true;

            try
            {
                while (pending != null)
                {
                    PendingUpload upload = pending;
                    pending = null;
                    upload.expectedVersion = Mathf.Max(upload.expectedVersion, serverVersion);
                    upload.payload.version = upload.expectedVersion + 1;
                    PersistOutbox(upload);

                    ApiResult<FarmStateResponse> result = await SendAsync(upload);
                    if (!result.ok && IsTransient(result.status))
                    {
                        await Awaitable.NextFrameAsync();
                        result = await SendAsync(upload);
                    }

                    if (result.ok)
                    {
                        if (result.data != null && result.data.farm_state != null)
                            serverVersion = Mathf.Max(serverVersion, result.data.farm_state.version);
                        ClearOutboxIfMatches(upload);
                        LastError = "";
                        continue;
                    }

                    if (result.status == 409
                        && string.Equals(result.errorCode, "FARM_STATE_CONFLICT", StringComparison.OrdinalIgnoreCase)
                        && result.data != null
                        && result.data.farm_state != null)
                    {
                        serverVersion = Mathf.Max(1, result.data.farm_state.version);
                        ApplyServerSnapshot(result.data.farm_state);
                        ClearOutboxIfMatches(upload);
                        LastError = "";
                        Debug.LogWarning($"[FarmStateSync] Rejected stale snapshot for '{upload.scopeId}' and restored server revision {serverVersion}.");
                        continue;
                    }

                    LastError = !string.IsNullOrWhiteSpace(result.errorCode)
                        ? result.errorCode
                        : (!string.IsNullOrWhiteSpace(result.error) ? result.error : "FARM_STATE_SYNC_FAILED");

                    if (IsTransient(result.status))
                    {
                        // Preserve a newer snapshot queued while this request was in flight.
                        if (pending == null || !string.Equals(pending.scopeId, upload.scopeId, StringComparison.Ordinal))
                            pending = upload;
                        blockedByTransientFailure = true;
                        Debug.LogWarning($"[FarmStateSync] Upload deferred for '{upload.scopeId}': {LastError}");
                        break;
                    }

                    Debug.LogError($"[FarmStateSync] Upload rejected for '{upload.scopeId}': {LastError}");
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

            if (pending != null && !blockedByTransientFailure)
                StartProcessing();
        }

        private static Awaitable<ApiResult<FarmStateResponse>> SendAsync(PendingUpload upload)
        {
            return ApiClient.PutAsync<FarmStateResponse>(
                "/player/farm-state",
                new FarmStateRequest
                {
                    expectedVersion = upload.expectedVersion,
                    farm_state = upload.payload
                },
                upload.token);
        }

        private static Awaitable<ApiResult<AnimalPlacementResponse>> SendAnimalPlacementAsync(
            AnimalPlacementRequest request,
            string token)
        {
            return ApiClient.PostAsync<AnimalPlacementResponse>(
                "/player/farm/animals/place",
                request,
                token);
        }

        private static AnimalPlacementResult PlacementFailure(long status, string errorCode)
        {
            return new AnimalPlacementResult
            {
                ok = false,
                duplicate = false,
                status = status,
                errorCode = errorCode
            };
        }

        private static void ApplyInventorySnapshot(PlayerBootstrapService.InventoryPayload inventory)
        {
            if (inventory == null || InventoryManager.Instance == null) return;

            var slots = new List<InventorySlot>();
            if (inventory.slots != null)
            {
                foreach (var slot in inventory.slots)
                {
                    if (slot == null || string.IsNullOrWhiteSpace(slot.itemId) || slot.quantity <= 0)
                        continue;
                    slots.Add(new InventorySlot(slot.itemId, slot.quantity));
                }
            }

            InventoryManager.Instance.ApplyServerState(inventory.maxSlots, slots);
        }

        private static int GetInventoryQuantity(
            PlayerBootstrapService.InventoryPayload inventory,
            string itemId)
        {
            if (inventory?.slots == null || string.IsNullOrWhiteSpace(itemId)) return 0;

            foreach (var slot in inventory.slots)
            {
                if (slot != null && string.Equals(slot.itemId, itemId, StringComparison.Ordinal))
                    return Mathf.Max(0, slot.quantity);
            }

            return 0;
        }

        private static bool HasPersistedOutbox()
        {
            return PlayerScopedPrefs.HasPlayerScope && PlayerScopedPrefs.HasKey(OutboxKey);
        }

        private static void PersistOutbox(PendingUpload upload)
        {
            if (upload == null || upload.payload == null) return;
            if (!string.Equals(upload.scopeId, PlayerScopedPrefs.CurrentScopeId, StringComparison.Ordinal)) return;

            var record = new PersistedOutbox
            {
                scopeId = upload.scopeId,
                expectedVersion = Mathf.Max(1, upload.expectedVersion),
                payload = upload.payload
            };
            PlayerScopedPrefs.SetString(OutboxKey, JsonConvert.SerializeObject(record));
            PlayerScopedPrefs.Save();
        }

        private static void RestorePendingFromOutbox()
        {
            if (pending != null || processing || !HasPersistedOutbox()) return;

            var auth = AuthService.Instance;
            string scopeId = PlayerScopedPrefs.CurrentScopeId;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrEmpty(auth.Token) || string.IsNullOrEmpty(scopeId))
                return;

            try
            {
                string json = PlayerScopedPrefs.GetString(OutboxKey, "");
                var record = JsonConvert.DeserializeObject<PersistedOutbox>(json);
                if (record == null || record.payload == null ||
                    !string.Equals(record.scopeId, scopeId, StringComparison.Ordinal))
                    throw new InvalidOperationException("FARM_OUTBOX_INVALID");

                int fallbackExpected = Mathf.Max(1, record.payload.version - 1);
                pending = new PendingUpload
                {
                    token = auth.Token,
                    scopeId = scopeId,
                    expectedVersion = Mathf.Max(fallbackExpected, record.expectedVersion),
                    payload = record.payload
                };
                LastError = "";
                Debug.Log($"[FarmStateSync] Restored pending farm upload for '{scopeId}'.");
            }
            catch (Exception exception)
            {
                PlayerScopedPrefs.DeleteKey(OutboxKey);
                PlayerScopedPrefs.Save();
                LastError = "FARM_OUTBOX_INVALID";
                Debug.LogError($"[FarmStateSync] Removed invalid farm outbox: {exception.Message}");
            }
        }

        private static void ClearOutboxIfMatches(PendingUpload upload)
        {
            if (upload == null || !string.Equals(upload.scopeId, PlayerScopedPrefs.CurrentScopeId, StringComparison.Ordinal))
                return;
            if (!PlayerScopedPrefs.HasKey(OutboxKey)) return;

            try
            {
                var current = JsonConvert.DeserializeObject<PersistedOutbox>(
                    PlayerScopedPrefs.GetString(OutboxKey, ""));
                string currentSavedAt = current?.payload?.clientSavedAt ?? "";
                string uploadedSavedAt = upload.payload?.clientSavedAt ?? "";
                if (!string.Equals(currentSavedAt, uploadedSavedAt, StringComparison.Ordinal)) return;
            }
            catch
            {
                return;
            }

            PlayerScopedPrefs.DeleteKey(OutboxKey);
            PlayerScopedPrefs.Save();
        }

        private static bool IsTransient(long status) => status == 0 || status == 429 || status >= 500;

        private static string ValidOrDefault(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static int CurrentContentScore()
        {
            return ContentScore(new PlayerBootstrapService.FarmStatePayload
            {
                buildStateJson = PlayerScopedPrefs.GetString(BuildStateKey, EmptyBuildState),
                placedTilesJson = PlayerScopedPrefs.GetString(PlacedTilesKey, EmptyPlacedTiles),
                farmTilesJson = PlayerScopedPrefs.GetString(FarmTilesKey, EmptyFarmTiles),
                animalStateJson = PlayerScopedPrefs.GetString(AnimalStateKey, EmptyAnimalState)
            });
        }

        private static int ContentScore(PlayerBootstrapService.FarmStatePayload payload)
        {
            if (payload == null) return 0;
            return CountArray(payload.buildStateJson, "items")
                + CountArray(payload.buildStateJson, "animals")
                + CountArray(payload.placedTilesJson, "tiles")
                + CountArray(payload.farmTilesJson, "tiles")
                + CountArray(payload.animalStateJson, "animals");
        }

        private static int CountArray(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0;
            try
            {
                return JObject.Parse(json)[propertyName] is JArray values ? values.Count : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static void InvokeSnapshotAppliedHandlers()
        {
            Delegate[] handlers = AuthoritativeSnapshotApplied?.GetInvocationList();
            if (handlers == null) return;

            foreach (Delegate handler in handlers)
            {
                try { ((Action)handler).Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}
