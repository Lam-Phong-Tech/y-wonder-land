using System;
using System.Collections.Generic;
using UnityEngine;
using YWonderLand.Managers;

namespace YWonderLand.Backend
{
    /// <summary>
    /// Điểm danh 15 ngày tân thủ, SERVER-AUTHORITATIVE hoàn toàn: server chấm ngày (giờ vận
    /// hành, không phải đồng hồ máy), giữ chuỗi, tự trao thưởng rồi trả kho + ví về.
    ///
    /// Trước đây sổ nằm trong PlayerPrefs của thiết bị nên vặn đồng hồ hoặc cài lại game là
    /// quay vòng 15 ngày lại từ đầu, mà phần thưởng thì vẫn đồng bộ lên server thật.
    /// Chỉ dùng khi ĐÃ đăng nhập; chưa đăng nhập thì popup tự chạy đường local (bản demo).
    /// </summary>
    public static class AttendanceService
    {
        [Serializable]
        private class ClaimRequest { public string idempotency_key; }

        [Serializable]
        public class AttendancePayload
        {
            public int claimedDays;
            public int maxRewardedDay;
            public string lastClaimDate;
            public int visibleDays;     // số ô sáng trên lưới — đã trừ luật mất chuỗi
            public int totalDays = 15;
            public bool claimedToday;
            public int nextDay;         // bấm bây giờ thì vào ngày mấy (0 = đã xong 15 ngày)
            public bool canClaim;
            public bool finished;
            public string today;
        }

        [Serializable]
        public class RewardPayload
        {
            public int day;
            public int point;
            public string itemId;
            public int qty;
            public bool isNothing;
        }

        [Serializable]
        private class AttendanceResponse
        {
            public bool ok;
            public AttendancePayload attendance;
            public RewardPayload reward;
            public bool rewardPaid;
            public bool streakReset;
            public PlayerBootstrapService.EconomyPayload economy;
            public PlayerBootstrapService.InventoryPayload inventory;
            public string error;
        }

        public struct ClaimResult
        {
            public bool ok;
            public string errorCode;
            public long status;
            public AttendancePayload attendance;
            public RewardPayload reward;
            public bool rewardPaid;   // false = ngày này đã lĩnh quà từ lượt chuỗi trước
            public bool streakReset;  // true = vừa bị mất chuỗi, quay về ngày 1
        }

        public static bool IsOnline()
        {
            var auth = AuthService.Instance;
            return auth != null && auth.IsSignedIn && !string.IsNullOrWhiteSpace(auth.Token);
        }

        /// <summary>Trạng thái điểm danh theo server. Trả null nếu không lấy được.</summary>
        public static async Awaitable<AttendancePayload> GetStateAsync()
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrWhiteSpace(auth.Token))
                return null;

            var res = await ApiClient.GetAsync<AttendanceResponse>("/player/attendance", auth.Token);
            if (!res.ok || res.data == null) return null;
            return res.data.attendance;
        }

        public static async Awaitable<ClaimResult> ClaimAsync()
        {
            var auth = AuthService.Instance;
            if (auth == null || !auth.IsSignedIn || string.IsNullOrWhiteSpace(auth.Token))
                return new ClaimResult { ok = false, errorCode = "NO_AUTH", status = 401 };

            // Đẩy hết delta đang chờ trước đã, kẻo server trả kho về rồi delta cũ ghi đè lên.
            await GameplayMutationSync.FlushAsync();

            var request = new ClaimRequest { idempotency_key = Guid.NewGuid().ToString("N") };
            var res = await ApiClient.PostAsync<AttendanceResponse>("/player/attendance/claim", request, auth.Token);

            // Mất kết nối / lỗi server thì gửi lại ĐÚNG key cũ nên không điểm danh hai lần.
            if (!res.ok && (res.status == 0 || res.status >= 500))
            {
                await Awaitable.NextFrameAsync();
                res = await ApiClient.PostAsync<AttendanceResponse>("/player/attendance/claim", request, auth.Token);
            }

            if (!res.ok || res.data == null || !res.data.ok)
            {
                string code = res.data != null && !string.IsNullOrWhiteSpace(res.data.error)
                    ? res.data.error
                    : (!string.IsNullOrWhiteSpace(res.errorCode) ? res.errorCode : "ATTENDANCE_FAILED");
                return new ClaimResult
                {
                    ok = false,
                    errorCode = code,
                    status = res.status,
                    // 409 vẫn kèm trạng thái mới nhất -> dùng luôn để vẽ lại lưới cho đúng.
                    attendance = res.data != null ? res.data.attendance : null,
                };
            }

            ApplyServerState(res.data);

            return new ClaimResult
            {
                ok = true,
                attendance = res.data.attendance,
                reward = res.data.reward,
                rewardPaid = res.data.rewardPaid,
                streakReset = res.data.streakReset,
            };
        }

        private static void ApplyServerState(AttendanceResponse data)
        {
            if (data.economy != null)
                EconomyManager.Instance?.ApplyServerState(data.economy.pos);

            if (data.inventory != null)
                InventoryManager.Instance?.ApplyServerState(
                    data.inventory.maxSlots, ToManagerSlots(data.inventory.slots));
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
