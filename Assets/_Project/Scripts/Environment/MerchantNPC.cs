using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Gắn vào NPC Thương Nhân (hoặc sạp hàng). 
    /// Khi click vào sẽ tự động mở ShopPopupController.
    /// </summary>
    public class MerchantNPC : MonoBehaviour
    {
        public void Interact()
        {
            var shopPopup = Object.FindFirstObjectByType<ShopPopupController>();
            if (shopPopup != null)
            {
                shopPopup.Show();
                Debug.Log("[MerchantNPC] Tương tác với Thương Nhân -> Mở Shop UI thành công!");
            }
            else
            {
                Debug.LogWarning("[MerchantNPC] Lỗi: Không tìm thấy ShopPopupController trong scene!");
            }
        }
    }
}
