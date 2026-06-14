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

        [Header("Model 3D th\u1eadt (t\u00f9y ch\u1ecdn)")]
        [Tooltip("Prefab model 3D c\u1ee7a c\u00e2y (carrot/cabbage/corn...). G\u00e1n v\u00e0o \u0111\u00e2y \u0111\u1ec3 FarmTile (b\u1eadt Use Custom Crop Models) hi\u1ec7n model th\u1eadt v\u00e0 ph\u00f3ng to d\u1ea7n khi l\u1edbn.")]
        public GameObject cropPrefab;

        [Tooltip("N\u00e2ng/h\u1ea1 model so v\u1edbi m\u1eb7t \u0111\u1ea5t (m\u00e9t) \u0111\u1ec3 kh\u1edbp m\u1eb7t m\u1ea3nh \u0111\u1ea5t. Carrot 'n\u1eeda c\u1ee7' ch\u1ec9nh \u1edf \u0111\u00e2y.")]
        public float modelGroundOffset = 0f;

        [Tooltip("T\u1ec9 l\u1ec7 k\u00edch th\u01b0\u1edbc l\u00fac m\u1edbi gieo so v\u1edbi l\u00fac ch\u00edn (0.25 = 25%).")]
        [Range(0.05f, 1f)]
        public float seedlingScale = 0.25f;
    }
}
