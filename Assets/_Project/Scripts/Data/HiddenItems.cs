using System.Collections.Generic;

namespace YWonderLand.Data
{
    /// <summary>
    /// DANH SÁCH VẬT PHẨM ẨN TẠM khỏi shop + túi đồ + danh sách gieo hạt.
    ///
    /// Lý do: 8 cây LÂU NĂM khách CHƯA chốt số liệu (giá giống / thời gian / sản lượng / giá bán) —
    /// theo SoLieu_CanKhachChot.md mục 2. Ẩn cả HẠT GIỐNG lẫn SẢN PHẨM để người chơi không mua/bán/trồng
    /// nhầm khi số liệu còn tạm. 3 cây đã chốt (Sa Chi / Sầu Riêng / Chanh dây) KHÔNG ẩn.
    ///
    /// KHÔNG xoá data/asset — chỉ lọc ở lớp hiển thị. Khi khách chốt số liệu 1 cây,
    /// chỉ cần BỎ 2 ID (hạt + sản phẩm) của cây đó khỏi danh sách này là hiện lại ngay.
    /// Muốn tắt hẳn cơ chế ẩn: xoá hết ID hoặc để <see cref="Ids"/> rỗng.
    /// </summary>
    public static class HiddenItems
    {
        // Cặp (hạt giống, sản phẩm) của từng cây lâu năm chưa chốt.
        public static readonly HashSet<string> Ids = new HashSet<string>
        {
            "banana_seed_01",       "banana_01",        // Chuối
            "coconut_seed_01",      "coconut_01",       // Dừa
            "areca_seed_01",        "areca_01",         // Cau
            "date_seed_01",         "date_01",          // Chà là
            "tea_seed_01",          "tea_01",           // Trà
            "asparagus_seed_01",    "asparagus_01",     // Măng tây
            "red_ginseng_seed_01",  "red_ginseng_01",   // Hồng sâm
            "royal_ginseng_seed_01","royal_ginseng_01", // Sâm tiến vua

            "fertilizer_01",                            // Phân bón — ẩn tạm khỏi shop/túi theo yêu cầu
        };

        /// <summary>True nếu vật phẩm đang bị ẩn tạm (chưa chốt số liệu).</summary>
        public static bool IsHidden(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && Ids.Contains(itemId);
        }
    }
}
