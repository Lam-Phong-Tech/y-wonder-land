using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Managers;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Câu cá SERVER-AUTHORITATIVE: client KHÔNG tự bốc cá. Gọi /player/fishing/catch,
    /// server tự quyết cá + tự quản 10 lượt free/ngày + trừ mồi, rồi trả snapshot kho về.
    /// Chống client hack "tự cho cá hiếm". Chỉ dùng khi ĐÃ đăng nhập; offline thì FishingOverlay
    /// tự rơi về luồng local (demo).
    /// </summary>
    public static class FishingService
    {
        [Serializable]
        private class CatchRequest
        {
            public string idempotency_key;
        }

        [Serializable]
        private class CatchResponse
        {
            public bool ok;
            public FishPayload fish;
            public PlayerBootstrapService.InventoryPayload inventory;
            public LimitPayload limit;
            public bool usedBait;
            public int baitRemaining;
            public string error;
        }

        [Serializable]
        private class FishPayload
        {
            public string itemId;
            public int pointValue;
        }

        [Serializable]
        private class LimitPayload
        {
            public int used;
            public int maxCount;
            public int remaining;
        }

        public struct Result
        {
            public bool ok;
            public string errorCode;
            public long status;
            public string fishItemId;
            public int fishPointValue;
            public int freeRemaining;   // lượt free còn lại (server)
            public int baitRemaining;
            public bool usedBait;
        }

        public static async Awaitable<Result> CatchAsync()
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrWhiteSpace(auth.Token))
                return new Result { ok = false, errorCode = "NO_AUTH", status = 401 };

            // Đảm bảo mọi delta kho đang chờ đã lên server để kho nhất quán trước khi câu.
            await GameplayMutationSync.FlushAsync();

            var request = new CatchRequest { idempotency_key = Guid.NewGuid().ToString("N") };
            var res = await ApiClient.PostAsync<CatchResponse>("/player/fishing/catch", request, auth.Token);

            // Chỉ retry khi mất kết nối / lỗi server (dùng LẠI key nên không câu 2 lần).
            if (!res.ok && (res.status == 0 || res.status >= 500))
            {
                await Awaitable.NextFrameAsync();
                res = await ApiClient.PostAsync<CatchResponse>("/player/fishing/catch", request, auth.Token);
            }

            if (!res.ok || res.data == null || !res.data.ok)
            {
                string code = res.data != null && !string.IsNullOrWhiteSpace(res.data.error)
                    ? res.data.error
                    : (!string.IsNullOrWhiteSpace(res.errorCode) ? res.errorCode : "FISHING_FAILED");
                return new Result { ok = false, errorCode = code, status = res.status };
            }

            // Áp snapshot kho authoritative từ server (đã có con cá vừa câu + trừ mồi).
            if (res.data.inventory != null)
                InventoryManager.Instance?.ApplyServerState(
                    res.data.inventory.maxSlots, ToManagerSlots(res.data.inventory.slots));

            return new Result
            {
                ok = true,
                fishItemId = res.data.fish != null ? res.data.fish.itemId : "",
                fishPointValue = res.data.fish != null ? res.data.fish.pointValue : 0,
                freeRemaining = res.data.limit != null ? res.data.limit.remaining : 0,
                baitRemaining = res.data.baitRemaining,
                usedBait = res.data.usedBait,
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
