using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Định nghĩa một loại vật nuôi trong game.
    /// Demo: 3 loại (Gà, Bò, Heo) dùng primitive shapes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimal", menuName = "YWonderLand/Animal Definition")]
    public class AnimalDefinition : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        public string animalId;
        public string animalName;

        [Tooltip("Giá mua tại shop (Point)")]
        public int buyPrice = 500;

        [Header("Thông tin chăn nuôi (hiển thị)")]
        [Tooltip("Số ô chuồng con vật chiếm (1 hoặc 9)")]
        public int penSlots = 1;

        [Tooltip("Thức ăn chính + số lượng")]
        public string foodMainName;
        public int foodMainAmount;

        [Tooltip("Thức ăn phụ (thay thế) + số lượng. Để trống nếu không có.")]
        public string foodAltName;
        public int foodAltAmount;

        [Tooltip("Sản phẩm chính + số lượng (hiển thị)")]
        public string productMainName;
        public int productMainAmount;

        [Tooltip("Sản phẩm phụ + số lượng (hiển thị)")]
        public string productAltName;
        public int productAltAmount;

        [Header("Sản xuất")]
        [Tooltip("ID sản phẩm trong ItemDatabase (VD: egg_01)")]
        public string produceItemId;

        [Tooltip("Số lượng sản phẩm mỗi vụ")]
        public int produceAmount = 1;

        [Tooltip("Chu kỳ ra sản phẩm (giây). Demo: 30-45s, Production: 14400-43200 (4-12h)")]
        public float produceCycleTimeSec = 35f;

        [Tooltip("Số vụ thu hoạch trước khi hết tuổi. 0 = vô hạn")]
        public int maxHarvests = 0;

        [Tooltip("EXP nhan o lan thu hoach cuoi (theo VatNuoi2).")]
        public int expReward = 0;

        [Header("Vụ cuối (làm thịt)")]
        [Tooltip("ID sản phẩm THỊT thu ở VỤ CUỐI (Pro2 trong VatNuoi). Thu xong con vật biến mất. Để trống nếu không có.")]
        public string meatItemId;

        [Tooltip("Số lượng thịt ở vụ cuối")]
        public int meatAmount;

        [Header("Chăm sóc")]
        [Tooltip("Tần suất cho ăn (giây). Demo: 30s, Production: 28800 (8h)")]
        public float feedIntervalSec = 30f;

        [Tooltip("THỜI GIAN SỐNG khi THẢ mà CHƯA cho ăn lần nào (giây) = đồng hồ chết đói ban đầu. " +
                 "Khách chốt: đa số thú 24h game, rùa 5 ngày. 0 = không chết đói.")]
        public float noFeedDeathSec = 0f;

        [Tooltip("THỜI GIAN SỐNG sau MỖI lần cho ăn (giây) = sức chứa thanh máu. " +
                 "Khách chốt: đa số thú 48h game, rùa 10 ngày. Cho ăn = đổ đầy về mốc này; cạn = chết. 0 = không chết đói.")]
        public float fedLifeSec = 0f;

        [Tooltip("Có thể bị bệnh nếu không tiêm vaccine")]
        public bool canGetSick = true;

        [Header("Bệnh / Vắc-xin (số theo VatNuoi2 + CachTinh)")]
        [Tooltip("Tổng SỐ NGÀY NUÔI cả vòng đời (cột 'Số ngày nuôi' của VatNuoi2). " +
                 "Là gốc để quy ra mốc phát bệnh và lịch tiêm.")]
        public float raisingDays = 0f;

        [Tooltip("HỆ SỐ thời điểm phát bệnh (cột 'Thời điểm phát bệnh', 0.15–0.5). " +
                 "Mốc phát bệnh = hệ số × số ngày nuôi. VD bò 0.3 × 270 = ngày thứ 81.")]
        [Range(0f, 1f)] public float sicknessOnsetRatio = 0f;

        [Tooltip("XÁC SUẤT phát bệnh khi tới mốc (cột 'Tỉ lệ phát bệnh', 0.3–0.6). " +
                 "Gieo MỘT lần cho cả vòng nuôi — trượt thì con đó nuôi hết vòng không bệnh.")]
        [Range(0f, 1f)] public float sicknessChance = 0f;

        [Tooltip("Số MŨI vắc-xin cần trong cả vòng nuôi (cột 'Số lượng Vắc-xin cần'). " +
                 "Một mũi phòng được = số ngày nuôi ÷ số mũi. VD bò 270 ÷ 4 = 67,5 ngày/mũi.")]
        public int vaccineDosesPerCycle = 0;

        [Tooltip("Số LIỀU thuốc tốn để chữa khỏi (cột 'Số lượng thuốc trị bệnh cần'). " +
                 "VD bò 10 liều, vịt 2 liều. Theo CachTinh thì đây là chi phí thuốc CẢ vòng nuôi.")]
        public int medicineDosesPerCure = 1;

        [Header("Visual (Primitive Fallback)")]
        [Tooltip("Loại primitive shape: 0=Capsule (gà), 1=Cube (bò), 2=Sphere (heo)")]
        public int primitiveType = 0;

        [Tooltip("Màu đặc trưng")]
        public Color animalColor = Color.white;

        [Tooltip("Kích thước scale")]
        public Vector3 animalScale = new Vector3(0.5f, 0.5f, 0.5f);

        [Tooltip("Emoji hiển thị trên UI")]
        public string iconEmoji = "\ud83d\udc14";
    }
}
