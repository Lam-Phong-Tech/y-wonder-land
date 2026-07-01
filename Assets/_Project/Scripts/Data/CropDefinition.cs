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

        [Tooltip("Tần suất cần tưới nước. Demo: 15-30s, Production: 36000 (10h). (CŨ — không còn dùng để tính chết.)")]
        public float waterIntervalSec = 20f;

        [Tooltip("THỜI GIAN SỐNG khi gieo mà CHƯA tưới lần nào (giây) = đồng hồ chết ban đầu. " +
                 "Cây ngắn ngày (khách chốt) = 8h game. Thanh nước/máu lúc mới gieo đầy tới mốc này; cạn = cây chết. " +
                 "0 = không chết khi chưa tưới. Khách cho số riêng từng cây thì điền vào đây.")]
        public float noWaterDeathSec = 0f;

        [Tooltip("THỜI GIAN SỐNG sau MỖI lần tưới (giây) = sức chứa thanh nước/máu. " +
                 "Cây ngắn ngày (khách chốt) = 20h game. Tưới = đổ đầy về mốc này; cạn = cây chết. " +
                 "0 = không chết vì thiếu nước. Phải < thời gian lớn để buộc tưới ≥2 lần mới kịp thu.")]
        public float wateredLifeSec = 0f;

        [Header("Phần thưởng")]
        [Tooltip("Số lượng sản phẩm thu hoạch mỗi vụ")]
        public int harvestYield = 3;

        [Tooltip("EXP nhận được khi thu hoạch")]
        public int expReward = 20;

        [Tooltip("Point nhận được khi thu hoạch")]
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

        [Header("C\u00e2y L\u00c2U N\u0102M / nhi\u1ec1u \u00f4 (theo b\u1ea3ng CayTrongLauNam)")]
        [Tooltip("S\u1ed1 \u00f4 \u0111\u1ea5t c\u00e2y chi\u1ebfm khi tr\u1ed3ng. 1 = b\u00ecnh th\u01b0\u1eddng; Chanh d\u00e2y = 20 (c\u00e2y leo gi\u00e0n).")]
        public int plotSlots = 1;

        [Tooltip("S\u1ed1 L\u1ea6N THU tr\u01b0\u1edbc khi c\u00e2y h\u1ebft \u0111\u1eddi. 1 = c\u00e2y ng\u1eafn ng\u00e0y (thu 1 l\u1ea7n r\u1ed3i m\u1ea5t). " +
                 "C\u00e2y l\u00e2u n\u0103m: Sa Chi 9, S\u1ea7u Ri\u00eang 12, Chanh d\u00e2y 2.")]
        public int maxHarvests = 1;

        [Tooltip("Chu k\u1ef3 RA QU\u1ea2 gi\u1eefa c\u00e1c l\u1ea7n thu (gi\u00e2y). Ch\u1ec9 d\u00f9ng khi maxHarvests>1. " +
                 "Sa Chi/S\u1ea7u Ri\u00eang 28 ng\u00e0y, Chanh d\u00e2y 90 ng\u00e0y. Demo quy \u0111\u1ed5i qua GameTimeConfig.Days().")]
        public float reHarvestCycleSec = 0f;

        [Tooltip("ID s\u1ea3n ph\u1ea9m V\u1ee4 CU\u1ed0I (Product 2) \u2014 thu th\u00eam khi c\u00e2y h\u1ebft \u0111\u1eddi r\u1ed3i bi\u1ebfn m\u1ea5t. \u0110\u1ec3 tr\u1ed1ng n\u1ebfu kh\u00f4ng c\u00f3.")]
        public string finalProductItemId;

        [Tooltip("S\u1ed1 l\u01b0\u1ee3ng s\u1ea3n ph\u1ea9m v\u1ee5 cu\u1ed1i.")]
        public int finalProductAmount;
    }
}
