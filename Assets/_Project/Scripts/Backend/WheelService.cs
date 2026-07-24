using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Managers;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Vòng quay may mắn SERVER-AUTHORITATIVE cho phần ĐẾM LƯỢT: server nắm 3 lượt free/ngày
    /// (daily-limit "spin", theo NGÀY SERVER nên bền qua đăng nhập lại) + trừ spin_ticket_01.
    /// Server KHÔNG bốc quà — client tự bốc + áp (đồng bộ qua kho). Chỉ dùng khi ĐÃ đăng nhập.
    /// </summary>
    public static class WheelService
    {
        [Serializable]
        private class SpinRequest { public string idempotency_key; }

        [Serializable]
        private class SpinResponse
        {
            public bool ok;
            public bool usedTicket;
            public int spinsRemaining;
            public int ticketRemaining;
            public PlayerBootstrapService.InventoryPayload inventory;
            public string error;
        }

        private class DailyLimitsResponse { public DailyLimitsPayload daily_limits; }
        private class DailyLimitsPayload { public Dictionary<string, LimitInfo> limits; }
        private class LimitInfo { public int used; public int maxCount; public int remaining; }

        public struct SpinResult
        {
            public bool ok;
            public string errorCode;
            public long status;
            public bool usedTicket;
            public int spinsRemaining;
            public int ticketRemaining;
        }

        public static bool IsOnline()
        {
            var auth = AuthService.Instance;
            return auth != null && auth.IsSignedIn && !string.IsNullOrWhiteSpace(auth.Token);
        }

        public static async Awaitable<SpinResult> SpinAsync()
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrWhiteSpace(auth.Token))
                return new SpinResult { ok = false, errorCode = "NO_AUTH", status = 401 };

            await GameplayMutationSync.FlushAsync();

            var request = new SpinRequest { idempotency_key = Guid.NewGuid().ToString("N") };
            var res = await ApiClient.PostAsync<SpinResponse>("/player/wheel/spin", request, auth.Token);

            // Retry khi mất kết nối / lỗi server (dùng LẠI key nên không quay 2 lần).
            if (!res.ok && (res.status == 0 || res.status >= 500))
            {
                await Awaitable.NextFrameAsync();
                res = await ApiClient.PostAsync<SpinResponse>("/player/wheel/spin", request, auth.Token);
            }

            if (!res.ok || res.data == null || !res.data.ok)
            {
                string code = res.data != null && !string.IsNullOrWhiteSpace(res.data.error)
                    ? res.data.error
                    : (!string.IsNullOrWhiteSpace(res.errorCode) ? res.errorCode : "SPIN_FAILED");
                return new SpinResult { ok = false, errorCode = code, status = res.status };
            }

            if (res.data.inventory != null)
                InventoryManager.Instance?.ApplyServerState(
                    res.data.inventory.maxSlots, ToManagerSlots(res.data.inventory.slots));

            return new SpinResult
            {
                ok = true,
                usedTicket = res.data.usedTicket,
                spinsRemaining = res.data.spinsRemaining,
                ticketRemaining = res.data.ticketRemaining,
            };
        }

        // Số lượt free còn lại theo server (khi mở vòng quay). Trả -1 nếu không lấy được.
        public static async Awaitable<int> GetFreeSpinsRemainingAsync(int defaultMax)
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrWhiteSpace(auth.Token))
                return -1;

            var res = await ApiClient.GetAsync<DailyLimitsResponse>("/player/daily-limits", auth.Token);
            if (!res.ok || res.data == null || res.data.daily_limits == null || res.data.daily_limits.limits == null)
                return -1;

            if (res.data.daily_limits.limits.TryGetValue("spin", out var info) && info != null)
                return Mathf.Max(0, info.maxCount - info.used);

            // Chưa có bản ghi "spin" hôm nay -> còn nguyên free.
            return Mathf.Max(0, defaultMax);
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
