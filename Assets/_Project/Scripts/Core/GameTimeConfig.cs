namespace YWonderLand.Core
{
    /// <summary>
    /// Quy đổi thời gian GAME ↔ THỰC. Khách chốt (21/06): 1 NGÀY GAME = 24H THỰC.
    ///
    /// ⭐ ĐỔI DUY NHẤT hằng số <see cref="SecondsPerGameDay"/> để chuyển:
    ///   • DEMO  = 60f   → 1 ngày game = 60s thực (test nhanh).
    ///   • THẬT  = 86400f → 1 ngày game = 24h thực (bản phát hành).
    ///
    /// ⚠️ Khi đổi sang 86400 (bản thật): PHẢI lưu MỐC DateTime ra đĩa/server để cây/thú
    /// lớn BÙ đúng khi đóng app vài ngày (Time.timeAsDouble chỉ chạy lúc app mở). Đó là
    /// "Bước persistence" còn nợ — xem task.md.
    ///
    /// Số liệu thời gian trong generator được khai theo NGÀY/GIỜ game (Days()/Hours()),
    /// rồi nhân hằng số này ra GIÂY THỰC — nên đổi 1 chỗ là cả cây lẫn thú đổi theo.
    /// </summary>
    public static class GameTimeConfig
    {
        /// <summary>Bao nhiêu GIÂY THỰC = 1 NGÀY GAME. Demo 60f · Thật 86400f.</summary>
        public const float SecondsPerGameDay = 60f;

        /// <summary>Đổi số NGÀY game → giây thực.</summary>
        public static float Days(float gameDays) => gameDays * SecondsPerGameDay;

        /// <summary>Đổi số GIỜ game → giây thực (24 giờ = 1 ngày).</summary>
        public static float Hours(float gameHours) => gameHours * (SecondsPerGameDay / 24f);
    }
}
