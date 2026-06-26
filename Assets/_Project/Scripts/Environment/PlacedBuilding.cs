using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Đánh dấu 1 GameObject được đặt qua Build Mode (Ruộng/Chuồng/Đường...).
    /// Lưu đủ thông tin để BuildPersistence dựng lại ĐÚNG prefab + kích thước + vật liệu khi mở lại game.
    /// Gắn tự động ở GhostPlacementController.ConfirmPlacement / PlaceFromSave.
    /// </summary>
    public class PlacedBuilding : MonoBehaviour
    {
        public string itemName = "";   // tên item Build (để tra BuildPrefabLibrary, vd "Ruộng")
        public int footprintX = 1;     // số ô theo X (cho stretch)
        public int footprintY = 1;     // số ô theo Y
        public string materialId = ""; // vật liệu đã tốn ("" = miễn phí, vd ruộng)
        public int cost = 0;           // số lượng vật liệu (để hoàn khi phá)
    }
}
