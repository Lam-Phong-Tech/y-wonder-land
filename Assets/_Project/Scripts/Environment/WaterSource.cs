using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Đánh dấu VÙNG NƯỚC MÚC ĐƯỢC để tưới cây (ao/hồ trên đảo) — KHÁC nước biển (không múc được).
    ///
    /// Cách gắn (Editor): thêm 1 Collider (BẬT Is Trigger) ốp lên bề mặt ao giữa đảo,
    /// rồi gắn component này lên CHÍNH GameObject đó. Ngắm vào ao + bấm "Múc nước" → +xô nước vào túi.
    /// (Không cần animation — khách không yêu cầu.)
    /// </summary>
    public class WaterSource : MonoBehaviour
    {
        [Tooltip("Số xô nước nhận mỗi lần múc.")]
        public int amountPerScoop = 5;

        [Tooltip("Tầm với tối đa để múc nước (m).")]
        public float interactRange = 6f;
    }
}
