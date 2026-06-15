using UnityEngine;

namespace YWonderLand.Player
{
    /// <summary>
    /// Cầu nối cho Animation Event. GẮN LÊN CÙNG GameObject với Animator (model nhân vật),
    /// vì Animation Event chỉ gọi được hàm trên đúng object chứa Animator.
    ///
    /// Dùng để ẩn dụng cụ GIỮA CHỪNG clip — vd: trồng cây xong, tại frame cắm cây
    /// xuống đất thì ẩn cây giống đi để khi đứng lên tay không còn cầm cây.
    ///
    /// SETUP: mở clip "Planting" -> thêm Animation Event tại frame mong muốn (vd 123)
    /// -> Function = HideHeldTool.
    /// </summary>
    public class AnimEventToolHider : MonoBehaviour
    {
        /// <summary>Animation Event gọi tên hàm này để ẩn dụng cụ đang cầm.</summary>
        public void HideHeldTool()
        {
            if (EquipmentManager.Instance != null)
                EquipmentManager.Instance.HideAllTools();
        }

        /// <summary>Animation Event gọi tên hàm này (frame vung cần) để bắn dây câu ra.</summary>
        public void CastFishingLine()
        {
            Debug.Log("[Fishing] 🚩 Animation Event 'CastFishingLine' đã chạy (frame đúng).");
            if (YWonderLand.Environment.FishingLineController.Instance != null)
                YWonderLand.Environment.FishingLineController.Instance.FireCast();
            else
                Debug.LogWarning("[Fishing] ⚠️ Không tìm thấy FishingLineController.Instance.");
        }
    }
}
