using UnityEngine;

namespace YWonderLand.Data
{
    /// <summary>
    /// Định nghĩa một loại công trình có thể xây trong Build Mode.
    /// Gồm: Đường đi (1 đá), Đèn (8 quặng), Chuồng (4 gỗ).
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "YWonderLand/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        public string buildingId;
        public string buildingName;

        [Tooltip("Danh mục: path / lamp / pen")]
        public string category = "path";

        [Header("Kích thước Grid")]
        [Tooltip("Chiếm bao nhiêu ô trên grid (VD: 1x1, 2x2)")]
        public Vector2Int gridSize = Vector2Int.one;

        [Header("Chi phí tài nguyên")]
        [Tooltip("Số gỗ cần thiết (wood_01)")]
        public int woodCost = 0;

        [Tooltip("Số đá cần thiết (stone_01)")]
        public int stoneCost = 0;

        [Tooltip("Số quặng/sắt cần thiết (iron_01)")]
        public int oreCost = 0;

        [Tooltip("Số Point cần thiết")]
        public int posCost = 0;

        [Header("Visual (Primitive Fallback)")]
        [Tooltip("Màu công trình")]
        public Color buildingColor = Color.gray;

        [Tooltip("Kích thước scale")]
        public Vector3 buildingScale = Vector3.one;

        [Tooltip("Emoji hiển thị trên UI")]
        public string iconEmoji = "\ud83c\udfe0";

        /// <summary>
        /// Kiểm tra người chơi có đủ tài nguyên để xây không.
        /// </summary>
        public bool CanAfford(
            int playerWood, int playerStone, int playerOre, long playerPOS)
        {
            return playerWood >= woodCost
                && playerStone >= stoneCost
                && playerOre >= oreCost
                && playerPOS >= posCost;
        }
    }
}
