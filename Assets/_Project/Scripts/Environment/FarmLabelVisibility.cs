using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Cờ TOÀN CỤC bật/tắt CHỮ nổi trên cây &amp; con vật. Khách chốt 30/07: mặc định ẨN — cách xem
    /// CHÍNH THỨC là bấm vào cây/con vật. Ai máy khoẻ muốn xem ngay trên đầu thì tự bật ở
    /// Cài đặt &gt; Đồ hoạ &gt; "Chữ nổi trên cây/thú" (có kèm cảnh báo rối mắt + dễ giật).
    ///
    /// CHỈ ẩn CHỮ — thanh nước của cây và thanh đói của con vật VẪN GIỮ, để liếc phát là biết
    /// cây khát hay thú đói mà không phải bật nhãn lên.
    ///
    /// FarmTile/FarmAnimal đọc cờ này trong LateUpdate mỗi frame nên bật/tắt thấy ngay,
    /// không cần event. Đây là cài đặt của MÁY (như âm lượng) nên dùng PlayerPrefs thường,
    /// không gắn theo tài khoản.
    /// </summary>
    public static class FarmLabelVisibility
    {
        private const string PrefKey = "YW_ShowFarmLabels";

        private static bool loaded;
        private static bool show;

        /// <summary>Đang hiện chữ nổi hay không. Mặc định TẮT.</summary>
        public static bool Show
        {
            get
            {
                if (!loaded)
                {
                    show = PlayerPrefs.GetInt(PrefKey, 0) == 1;
                    loaded = true;
                }
                return show;
            }
            set
            {
                show = value;
                loaded = true;
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Đảo trạng thái, trả về giá trị MỚI (để nút HUD tô sáng theo).</summary>
        public static bool Toggle() => Show = !Show;
    }
}
