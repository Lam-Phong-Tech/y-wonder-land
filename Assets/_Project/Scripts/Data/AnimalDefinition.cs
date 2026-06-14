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

        [Tooltip("Giá mua tại shop (POS)")]
        public int buyPrice = 500;

        [Header("Sản xuất")]
        [Tooltip("ID sản phẩm trong ItemDatabase (VD: egg_01)")]
        public string produceItemId;

        [Tooltip("Số lượng sản phẩm mỗi vụ")]
        public int produceAmount = 1;

        [Tooltip("Chu kỳ ra sản phẩm (giây). Demo: 30-45s, Production: 14400-43200 (4-12h)")]
        public float produceCycleTimeSec = 35f;

        [Tooltip("Số vụ thu hoạch trước khi hết tuổi. 0 = vô hạn")]
        public int maxHarvests = 0;

        [Header("Chăm sóc")]
        [Tooltip("Tần suất cho ăn (giây). Demo: 30s, Production: 28800 (8h)")]
        public float feedIntervalSec = 30f;

        [Tooltip("Có thể bị bệnh nếu không tiêm vaccine")]
        public bool canGetSick = true;

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
