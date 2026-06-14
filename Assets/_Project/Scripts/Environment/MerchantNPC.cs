using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Gắn vào NPC quầy dịch vụ (Mua / Bán / Nâng cấp). Khi click vào sẽ mở popup tương ứng.
    /// Đặt <see cref="serviceType"/> trong Inspector để 1 script dùng chung cho cả 3 loại NPC.
    ///
    /// LƯU Ý: script PHẢI nằm cùng GameObject với Collider thì click mới nhận
    /// (FarmInteractionController.HandleClick chỉ dò trên chính collider, không dò cha).
    /// </summary>
    public class MerchantNPC : MonoBehaviour
    {
        public enum ServiceType
        {
            ShopBuy,   // Mở Shop ở tab MUA
            ShopSell,  // Mở Shop ở tab BÁN
            Workshop,  // Mở popup NÂNG CẤP
            PiggyBank  // Mở popup HEO ĐẤT (gửi tiết kiệm)
        }

        [Header("Loại dịch vụ")]
        [Tooltip("NPC này cung cấp dịch vụ gì khi click vào")]
        public ServiceType serviceType = ServiceType.ShopBuy;

        [Tooltip("Nhãn nút gợi ý khi rê tâm ngắm (để trống = tự đặt theo loại dịch vụ)")]
        public string interactionLabel = "";

        [Header("Tên trên đầu NPC")]
        [Tooltip("Tên riêng hiện trên đầu NPC (để trống = tự đặt theo loại dịch vụ: Quầy Mua/Quầy Bán/Tiệm Rèn)")]
        public string npcName = "";

        // Tự gắn bảng tên nổi trên đầu NPC (giống GuideNPC) để người chơi dễ nhận biết.
        private void Start()
        {
            if (GetComponent<FloatingNameTag>() == null)
            {
                FloatingNameTag tag = gameObject.AddComponent<FloatingNameTag>();
                tag.displayName = !string.IsNullOrEmpty(npcName) ? npcName : GetDefaultName();
                tag.nameColor = FloatingNameTag.COLOR_ACHIEVEMENT; // vàng gold — hợp NPC quầy dịch vụ
                tag.heightOffset = 3.0f;
                tag.tmpFontSize = 3.5f;
            }
        }

        private string GetDefaultName()
        {
            switch (serviceType)
            {
                case ServiceType.ShopBuy:  return "Quầy Mua";
                case ServiceType.ShopSell: return "Quầy Bán";
                case ServiceType.Workshop: return "Tiệm Rèn";
                case ServiceType.PiggyBank: return "Heo Đất";
                default:                   return "Thương Nhân";
            }
        }

        /// <summary>Nhãn nút hover, tuỳ loại dịch vụ.</summary>
        public string GetInteractionLabel()
        {
            if (!string.IsNullOrEmpty(interactionLabel)) return interactionLabel;
            switch (serviceType)
            {
                case ServiceType.ShopBuy:  return "Mua hàng";
                case ServiceType.ShopSell: return "Bán đồ";
                case ServiceType.Workshop: return "Nâng cấp";
                case ServiceType.PiggyBank: return "Gửi tiết kiệm";
                default:                   return "Giao thương";
            }
        }

        public void Interact()
        {
            switch (serviceType)
            {
                case ServiceType.ShopBuy:  OpenShop(ShopPopupController.ShopAccessMode.BuyOnly);  break;
                case ServiceType.ShopSell: OpenShop(ShopPopupController.ShopAccessMode.SellOnly); break;
                case ServiceType.Workshop: OpenWorkshop(); break;
                case ServiceType.PiggyBank: OpenPiggyBank(); break;
            }
        }

        private void OpenShop(ShopPopupController.ShopAccessMode mode)
        {
            var shop = Object.FindFirstObjectByType<ShopPopupController>(FindObjectsInactive.Include);
            if (shop != null)
            {
                shop.Show(mode);
                Debug.Log($"[MerchantNPC] Mở Shop chế độ {mode} thành công!");
            }
            else
            {
                Debug.LogWarning("[MerchantNPC] Lỗi: Không tìm thấy ShopPopupController trong scene!");
            }
        }

        private void OpenWorkshop()
        {
            var ws = WorkshopPopupController.Instance != null
                ? WorkshopPopupController.Instance
                : Object.FindFirstObjectByType<WorkshopPopupController>(FindObjectsInactive.Include);
            if (ws != null)
            {
                ws.Show();
                Debug.Log("[MerchantNPC] Mở popup Nâng cấp (Workshop) thành công!");
            }
            else
            {
                Debug.LogWarning("[MerchantNPC] Lỗi: Không tìm thấy WorkshopPopupController trong scene!");
            }
        }

        private void OpenPiggyBank()
        {
            var piggy = Object.FindFirstObjectByType<PiggyBankPopupController>(FindObjectsInactive.Include);
            if (piggy != null)
            {
                piggy.Show();
                Debug.Log("[MerchantNPC] Mở popup Heo Đất (gửi tiết kiệm) thành công!");
            }
            else
            {
                Debug.LogWarning("[MerchantNPC] Lỗi: Không tìm thấy PiggyBankPopupController trong scene!");
            }
        }
    }
}
