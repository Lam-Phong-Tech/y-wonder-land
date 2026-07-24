using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Managers;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Đổi Vé đào mỏ SERVER-AUTHORITATIVE: client KHÔNG tự cộng lượt. Gọi /player/mining/redeem-ticket,
    /// server tự trừ 1 mine_ticket_01 + +1 lượt daily-limit "mining" (đúng bên mà đào realtime kiểm),
    /// rồi trả về số lượt đào còn lại + snapshot kho. Chỉ dùng khi ĐÃ đăng nhập; offline thì
    /// FarmInteractionController tự cộng lượt local (demo).
    /// </summary>
    public static class MiningService
    {
        [Serializable]
        private class RedeemRequest
        {
            public string idempotency_key;
        }

        [Serializable]
        private class RedeemResponse
        {
            public bool ok;
            public int miningTurnsRemaining;
            public int ticketRemaining;
            public PlayerBootstrapService.InventoryPayload inventory;
            public string error;
        }

        public struct Result
        {
            public bool ok;
            public string errorCode;
            public long status;
            public int miningTurnsRemaining;
            public int ticketRemaining;
        }

        public static async Awaitable<Result> RedeemTicketAsync()
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrWhiteSpace(auth.Token))
                return new Result { ok = false, errorCode = "NO_AUTH", status = 401 };

            // Đảm bảo delta kho đang chờ đã lên server để kho nhất quán trước khi đổi vé.
            await GameplayMutationSync.FlushAsync();

            var request = new RedeemRequest { idempotency_key = Guid.NewGuid().ToString("N") };
            var res = await ApiClient.PostAsync<RedeemResponse>("/player/mining/redeem-ticket", request, auth.Token);

            // Chỉ retry khi mất kết nối / lỗi server (dùng LẠI key nên không trừ 2 vé).
            if (!res.ok && (res.status == 0 || res.status >= 500))
            {
                await Awaitable.NextFrameAsync();
                res = await ApiClient.PostAsync<RedeemResponse>("/player/mining/redeem-ticket", request, auth.Token);
            }

            if (!res.ok || res.data == null || !res.data.ok)
            {
                string code = res.data != null && !string.IsNullOrWhiteSpace(res.data.error)
                    ? res.data.error
                    : (!string.IsNullOrWhiteSpace(res.errorCode) ? res.errorCode : "MINE_TICKET_FAILED");
                return new Result { ok = false, errorCode = code, status = res.status };
            }

            // Áp snapshot kho authoritative (đã trừ 1 vé).
            if (res.data.inventory != null)
                InventoryManager.Instance?.ApplyServerState(
                    res.data.inventory.maxSlots, ToManagerSlots(res.data.inventory.slots));

            return new Result
            {
                ok = true,
                miningTurnsRemaining = res.data.miningTurnsRemaining,
                ticketRemaining = res.data.ticketRemaining,
            };
        }

        private static List<InventorySlot> ToManagerSlots(List<PlayerBootstrapService.InventorySlotPayload> slots)
        {
            var result = new List<InventorySlot>();
            if (slots == null) return result;
            foreach (var slot in slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.itemId) || slot.quantity <= 0) continue;
                result.Add(new InventorySlot(slot.itemId, slot.quantity));
            }
            return result;
        }
    }
}
