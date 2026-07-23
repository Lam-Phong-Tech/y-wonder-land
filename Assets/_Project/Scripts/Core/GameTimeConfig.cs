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
        /// <summary>Bao nhiêu GIÂY THỰC = 1 NGÀY GAME. Demo 60f · THẬT 86400f.
        /// ⭐ ĐANG BẬT THỜI GIAN THỰC (86400). Muốn test nhanh thì đổi tạm về 60f rồi đổi lại.</summary>
        public const float SecondsPerGameDay = 86400f;

        /// <summary>Đổi số NGÀY game → giây thực.</summary>
        public static float Days(float gameDays) => gameDays * SecondsPerGameDay;

        /// <summary>Đổi số GIỜ game → giây thực (24 giờ = 1 ngày).</summary>
        public static float Hours(float gameHours) => gameHours * (SecondsPerGameDay / 24f);

        /// <summary>
        /// Đổi GIÂY còn lại → chuỗi dễ đọc cho người chơi: "3 ngày 4 giờ", "5 giờ 12 phút",
        /// "2 phút 30 giây", "45 giây". Chỉ hiện 2 mốc lớn nhất cho gọn — vì chạy thời gian thực
        /// thì thời gian còn lại có thể lên tới vài chục ngày, ghi đủ 4 mốc sẽ quá dài.
        /// </summary>
        public static string FormatDuration(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = (int)System.Math.Ceiling((double)seconds);

            int days = total / 86400;
            int hours = (total % 86400) / 3600;
            int minutes = (total % 3600) / 60;
            int secs = total % 60;

            if (days > 0) return hours > 0 ? $"{days} ngày {hours} giờ" : $"{days} ngày";
            if (hours > 0) return minutes > 0 ? $"{hours} giờ {minutes} phút" : $"{hours} giờ";
            if (minutes > 0) return secs > 0 ? $"{minutes} phút {secs} giây" : $"{minutes} phút";
            return $"{secs} giây";
        }
    }
}
