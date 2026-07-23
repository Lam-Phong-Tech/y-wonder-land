using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Managers;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Executes one server-authoritative shop transaction. Economy and inventory
    /// are applied together only after the backend accepts the request.
    /// </summary>
    public static class ShopTransactionService
    {
        public struct Result
        {
            public bool ok;
            public bool duplicate;
            public long status;
            public string errorCode;
            public long totalPrice;
        }

        [Serializable]
        private class ShopTransactionRequest
        {
            public string shop_id;
            public string mode;
            public string item_id;
            public int quantity;
            public string idempotency_key;
        }

        [Serializable]
        private class ShopTransactionResponse
        {
            public bool ok;
            public bool duplicate;
            public PlayerBootstrapService.EconomyPayload economy;
            public PlayerBootstrapService.InventoryPayload inventory;
            public TransactionPayload transaction;
        }

        [Serializable]
        private class TransactionPayload
        {
            public long totalPrice;
        }

        public static async Awaitable<Result> TransactAsync(
            string shopId,
            bool sellMode,
            string itemId,
            int quantity)
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrWhiteSpace(auth.Token))
            {
                return Failure(401, "NO_AUTH_TOKEN");
            }

            if (string.IsNullOrWhiteSpace(shopId) || string.IsNullOrWhiteSpace(itemId) || quantity < 1)
            {
                return Failure(400, "INVALID_SHOP_REQUEST");
            }

            // Keep shop's atomic economy+inventory snapshot ordered after any
            // gameplay deltas (planting, water, harvest, build, rewards).
            bool mutationsFlushed = await GameplayMutationSync.FlushAsync();
            if (!mutationsFlushed)
            {
                if (GameplayMutationSync.PendingCount > 0)
                    return Failure(503, "PENDING_STATE_SYNC_FAILED");

                // A permanent 4xx drops the rejected local delta and requires an
                // authoritative snapshot before another economy operation. Reuse
                // bootstrap's guarded reconciliation instead of blocking shops for
                // the rest of the session.
                var bootstrap = PlayerBootstrapService.Instance;
                if (bootstrap == null || !await bootstrap.LoadBootstrapAsync())
                {
                    if (bootstrap != null && bootstrap.LastStatus == 401)
                        return Failure(401, "AUTH_EXPIRED");
                    return Failure(503, "STATE_RECONCILIATION_FAILED");
                }
            }

            var request = new ShopTransactionRequest
            {
                shop_id = shopId,
                mode = sellMode ? "sell" : "buy",
                item_id = itemId,
                quantity = quantity,
                idempotency_key = Guid.NewGuid().ToString("N")
            };

            var response = await SendAsync(request, auth.Token);
            if (!response.ok && (response.status == 0 || response.status >= 500))
            {
                // Reuse the same key so a committed request whose response was lost
                // cannot be charged/applied twice.
                await Awaitable.NextFrameAsync();
                response = await SendAsync(request, auth.Token);
            }

            if (!response.ok || response.data == null)
            {
                if (response.status == 401)
                    return Failure(401, "AUTH_EXPIRED");
                return Failure(response.status,
                    !string.IsNullOrWhiteSpace(response.errorCode) ? response.errorCode : "SHOP_REQUEST_FAILED");
            }

            if (response.data.economy == null || response.data.inventory == null)
            {
                return Failure(response.status, "INCOMPLETE_SHOP_RESPONSE");
            }

            EconomyManager.Instance?.ApplyServerState(response.data.economy.pos);
            InventoryManager.Instance?.ApplyServerState(
                response.data.inventory.maxSlots,
                ToManagerSlots(response.data.inventory.slots));

            return new Result
            {
                ok = true,
                duplicate = response.data.duplicate,
                status = response.status,
                errorCode = "",
                totalPrice = response.data.transaction != null ? response.data.transaction.totalPrice : 0
            };
        }

        private static Awaitable<ApiResult<ShopTransactionResponse>> SendAsync(
            ShopTransactionRequest request,
            string token)
        {
            return ApiClient.PostAsync<ShopTransactionResponse>(
                "/player/shop/transaction",
                request,
                token);
        }

        private static Result Failure(long status, string errorCode)
        {
            return new Result
            {
                ok = false,
                status = status,
                errorCode = errorCode ?? "SHOP_REQUEST_FAILED",
                totalPrice = 0
            };
        }

        private static List<InventorySlot> ToManagerSlots(
            List<PlayerBootstrapService.InventorySlotPayload> slots)
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
