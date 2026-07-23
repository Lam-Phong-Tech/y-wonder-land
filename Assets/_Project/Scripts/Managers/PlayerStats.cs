using UnityEngine;
using YWonderLand.Backend;

namespace YWonderLand.Managers
{
    /// <summary>
    /// Đếm THẬT các cột mốc hiển thị trong popup Hồ sơ (trước đây là số Random).
    /// Lưu theo từng tài khoản qua <see cref="PlayerScopedPrefs"/>, cộng dồn qua các phiên chơi.
    /// Cố tình để nhẹ và tĩnh: gọi được từ FarmTile / Shop mà không cần kéo tham chiếu.
    /// </summary>
    public static class PlayerStats
    {
        private const string K_Planted = "YW_StatPlanted";   // số hạt đã gieo
        private const string K_Sold = "YW_StatSold";         // số món đã bán ở cửa hàng
        private const string K_JoinedUnix = "YW_JoinedUnix"; // lần đầu vào game (giây UTC)

        public static int Planted => PlayerScopedPrefs.GetInt(K_Planted, 0);
        public static int Sold => PlayerScopedPrefs.GetInt(K_Sold, 0);

        public static void AddPlanted(int amount = 1) => Bump(K_Planted, amount);
        public static void AddSold(int amount = 1) => Bump(K_Sold, amount);

        private static void Bump(string key, int amount)
        {
            if (amount <= 0) return;
            PlayerScopedPrefs.SetInt(key, PlayerScopedPrefs.GetInt(key, 0) + amount);
            PlayerScopedPrefs.Save();
        }

        /// <summary>Ngày tham gia — ghi lần đầu được hỏi tới, sau đó giữ nguyên vĩnh viễn.</summary>
        public static System.DateTime JoinedDate
        {
            get
            {
                long unix = (long)PlayerScopedPrefs.GetFloat(K_JoinedUnix, 0f);
                if (unix <= 0L)
                {
                    unix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    PlayerScopedPrefs.SetFloat(K_JoinedUnix, unix);
                    PlayerScopedPrefs.Save();
                }
                return System.DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().DateTime;
            }
        }

        /// <summary>Tiện TEST: xoá sạch thống kê của tài khoản đang đăng nhập.</summary>
        public static void ClearAll()
        {
            PlayerScopedPrefs.DeleteKey(K_Planted);
            PlayerScopedPrefs.DeleteKey(K_Sold);
            PlayerScopedPrefs.DeleteKey(K_JoinedUnix);
            PlayerScopedPrefs.Save();
            Debug.Log("[PlayerStats] Đã xoá thống kê hồ sơ.");
        }
    }
}
