using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Định nghĩa một loại cây trồng trong game.
    /// Mỗi loại hạt giống tương ứng với một CropDefinition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCrop", menuName = "YWonderLand/Crop Definition")]
    public class CropDefinition : ScriptableObject
    {
        [Header("Liên kết Item")]
        [Tooltip("ID hạt giống trong ItemDatabase (VD: carrot_seed_01)")]
        public string seedItemId;

        [Tooltip("ID sản phẩm thu hoạch trong ItemDatabase (VD: carrot_01)")]
        public string harvestItemId;

        [Header("Thời gian (giây)")]
        [Tooltip("Thời gian trưởng thành. Demo: 30-60s, Production: 86400 (24h)")]
        public float growthTimeSec = 45f;

        [Tooltip("Tần suất cần tưới nước. Demo: 15-30s, Production: 36000 (10h)")]
        public float waterIntervalSec = 20f;

        [Header("Phần thưởng")]
        [Tooltip("Số lượng sản phẩm thu hoạch mỗi vụ")]
        public int harvestYield = 3;

        [Tooltip("EXP nhận được khi thu hoạch")]
        public int expReward = 20;

        [Tooltip("POS nhận được khi thu hoạch")]
        public int posReward = 50;

        [Header("Visual")]
        [Tooltip("Số giai đoạn phát triển (mầm → lớn → chín)")]
        public int growthStages = 3;

        [Tooltip("Màu đặc trưng của cây (dùng cho primitive fallback)")]
        public Color cropColor = Color.green;

        [Tooltip("Emoji hiển thị trên UI")]
        public string iconEmoji = "\ud83c\udf31";
    }
}
