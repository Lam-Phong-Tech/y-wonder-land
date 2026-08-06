using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Cấu hình CÔNG CỤ CỨU vật nuôi & cây trồng (khách chốt 04/08).
    /// Chi phí cứu = phần người chơi phải trả tính trên GIÁ MUA:
    ///   - Công ty hỗ trợ 40% → người chơi trả 60%.
    ///   - Admin "tạm đóng hỗ trợ" (tắt cờ) → người chơi trả 100%.
    /// Đây là CỜ CLIENT (anh chốt: wire server sau). Admin đổi bằng cách set
    /// <see cref="CompanySupport40On"/> = false (từ UI admin/cấu hình build/tương lai là server).
    /// </summary>
    public static class RescueConfig
    {
        /// <summary>Công ty có đang hỗ trợ 40% không. Bật = người chơi trả 60%; tắt = trả 100%.</summary>
        public static bool CompanySupport40On = true;

        /// <summary>Phần người chơi trả khi CÓ hỗ trợ (60%).</summary>
        public const float PlayerShareSupported = 0.6f;

        /// <summary>Tỷ lệ người chơi phải trả hiện tại (0.6 nếu có hỗ trợ, 1.0 nếu admin đã tạm đóng).</summary>
        public static float PlayerShare => CompanySupport40On ? PlayerShareSupported : 1f;

        /// <summary>Số Point người chơi phải trả để cứu, tính từ giá mua. Tối thiểu 1.</summary>
        public static int RescueCost(int buyPrice)
        {
            if (buyPrice <= 0) return 0;
            return Mathf.Max(1, Mathf.CeilToInt(buyPrice * PlayerShare));
        }
    }
}
