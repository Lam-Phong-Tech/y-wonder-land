using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Managers;

namespace YWonderLand.Backend
{
    public class PlayerBootstrapService : MonoBehaviour
    {
        public static PlayerBootstrapService Instance { get; private set; }

        public bool HasServerBootstrap { get; private set; }
        public PlayerBootstrapPayload LastBootstrap { get; private set; }
        public long LastStatus { get; private set; }
        public string LastError { get; private set; }

        [Serializable]
        public class PlayerBootstrapPayload
        {
            public PlayerProfile player_profile;
            public EconomyPayload economy;
            public InventoryPayload inventory;
            public FarmStatePayload farm_state;
            public DailyLimitsPayload daily_limits;
        }

        [Serializable]
        public class EconomyPayload
        {
            public int version = 1;
            public long pos;
            public long upos;
            public string updatedAt;
        }

        [Serializable]
        public class InventoryPayload
        {
            public int version = 1;
            public int maxSlots = 50;
            public List<InventorySlotPayload> slots = new List<InventorySlotPayload>();
            public string updatedAt;
        }

        [Serializable]
        public class InventorySlotPayload
        {
            public string itemId;
            public int quantity;
        }

        [Serializable]
        public class FarmStatePayload
        {
            public int version = 1;
            public string updatedAt;
        }

        [Serializable]
        public class DailyLimitsPayload
        {
            public int version = 1;
            public string updatedAt;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public async Awaitable<bool> LoadBootstrapAsync()
        {
            var auth = AuthService.Instance;
            if (auth == null || string.IsNullOrEmpty(auth.Token))
            {
                HasServerBootstrap = false;
                LastStatus = 401;
                LastError = "NO_AUTH_TOKEN";
                Debug.LogWarning("[Bootstrap] Cannot load player bootstrap: no auth token.");
                return false;
            }

            // A previous gameplay action may still be writing a seed, water,
            // reward, or currency delta. Flush first so bootstrap cannot restore
            // the older server snapshot over that local action.
            bool mutationsFlushed = await GameplayMutationSync.FlushAsync();
            if (!mutationsFlushed)
            {
                if (GameplayMutationSync.PendingCount > 0)
                {
                    HasServerBootstrap = false;
                    LastStatus = 503;
                    LastError = !string.IsNullOrWhiteSpace(GameplayMutationSync.LastError)
                        ? GameplayMutationSync.LastError
                        : "PENDING_STATE_SYNC_FAILED";
                    Debug.LogWarning("[Bootstrap] Refusing to load a stale snapshot while gameplay mutations are still pending.");
                    return false;
                }

                Debug.LogWarning("[Bootstrap] A gameplay mutation was rejected; loading the authoritative snapshot for reconciliation.");
            }

            var res = await ApiClient.GetAsync<PlayerBootstrapPayload>("/player/bootstrap", auth.Token);
            LastStatus = res.status;
            LastError = res.error ?? "";
            if (!res.ok || res.data == null)
            {
                HasServerBootstrap = false;
                Debug.LogWarning("[Bootstrap] Server bootstrap failed; keeping local fallback. " + res.error);
                return false;
            }

            ApplyBootstrap(res.data);
            GameplayMutationSync.MarkAuthoritativeSnapshotApplied();
            LastBootstrap = res.data;
            HasServerBootstrap = true;
            LastError = "";
            Debug.Log("[Bootstrap] Applied server profile, economy, and inventory.");
            return true;
        }

        public void ResetRuntimeState()
        {
            HasServerBootstrap = false;
            LastBootstrap = null;
            LastStatus = 0;
            LastError = "";
        }

        private static void ApplyBootstrap(PlayerBootstrapPayload payload)
        {
            if (payload == null) return;

            if (payload.player_profile != null)
                PlayerProfileService.Instance?.AcceptServerProfile(payload.player_profile);

            if (payload.economy != null)
                EconomyManager.Instance?.ApplyServerState(payload.economy.pos, payload.economy.upos);

            if (payload.inventory != null)
                InventoryManager.Instance?.ApplyServerState(payload.inventory.maxSlots, ToManagerSlots(payload.inventory.slots));
        }

        private static List<InventorySlot> ToManagerSlots(List<InventorySlotPayload> slots)
        {
            var result = new List<InventorySlot>();
            if (slots == null) return result;

            foreach (var slot in slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.itemId) || slot.quantity <= 0)
                    continue;

                result.Add(new InventorySlot(slot.itemId, slot.quantity));
            }

            return result;
        }
    }
}
