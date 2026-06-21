using UnityEngine;
using YWonderLand.Data;

/// <summary>
/// ShopZoneTrigger: gắn lên NHÀ của NPC (hoặc 1 vùng quanh quầy). Khi nhân vật BƯỚC VÀO
/// vùng trigger -> tự mở popup mua/bán của shop. Người chơi đi RA thì popup VẪN MỞ
/// (đóng bằng nút X) theo yêu cầu sếp.
///
/// Dùng chung cho thành phố lẫn đảo riêng: chỉ cần đặt nhà + collider + kéo ShopDefinition.
///
/// YÊU CẦU SETUP:
///  - GameObject này có Collider "Is Trigger" = TRUE (Reset tự bật).
///  - Nhân vật có tag "Player" + CharacterController.
///  - Scene nền có sẵn ShopPopupController / WorkshopPopupController / PiggyBankPopupController.
///  - Với Service = Shop: kéo 1 ShopDefinition.asset vào ô Shop Data.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopZoneTrigger : MonoBehaviour
{
    public enum ServiceKind { Shop, Workshop, PiggyBank }

    [Header("Loại dịch vụ khi chạm nhà")]
    [SerializeField] private ServiceKind service = ServiceKind.Shop;

    [Tooltip("CHỈ dùng khi Service = Shop. Catalog mua/bán riêng của NPC này.")]
    [SerializeField] private ShopDefinition shopData;

    [Tooltip("Chỉ mở khi đang ở trạng thái Gameplay (không mở lúc cutscene/menu).")]
    [SerializeField] private bool onlyDuringGameplay = true;

    [Header("Đang phát triển (Phase 2)")]
    [Tooltip("BẬT cho NPC tính năng CHƯA làm (VIP/Maid/Pet/Game Center/Gift...). Chạm vào → hiện thông báo 'đang phát triển' thay vì mở dịch vụ trống.")]
    [SerializeField] private bool comingSoon = false;

    [Header("Bảng tên nổi (tên shop)")]
    [Tooltip("Kéo NPC vào đây để hiện tên shop TRÊN ĐẦU NPC (dễ nhìn). Để TRỐNG = hiện trên chính cái nhà/vùng này.")]
    [SerializeField] private GameObject nameTagTarget;

    [Tooltip("Chữ nổi cho người chơi biết đây là shop gì. Để trống = lấy theo tên shop/dịch vụ.")]
    [SerializeField] private string shopLabel = "";

    [Tooltip("Độ cao bảng tên so với gốc (m). NPC thì ~2.5-3.5; nhà cao thì tăng số này.")]
    [SerializeField] private float nameTagHeight = 3.0f;

    private void Reset()
    {
        if (TryGetComponent<Collider>(out var col)) col.isTrigger = true;
    }

    private void Start()
    {
        // Hiện tên shop trên đầu NPC (nếu gán) cho dễ nhìn; không gán thì hiện trên chính cái nhà/vùng.
        GameObject target = nameTagTarget != null ? nameTagTarget : gameObject;
        if (target.GetComponent<FloatingNameTag>() == null)
        {
            FloatingNameTag tag = target.AddComponent<FloatingNameTag>();
            tag.displayName = ResolveLabel();
            tag.nameColor = FloatingNameTag.COLOR_ACHIEVEMENT; // vàng gold — đồng bộ NPC quầy dịch vụ
            tag.heightOffset = nameTagHeight;
            tag.tmpFontSize = 3.5f;
        }
    }

    private string ResolveLabel()
    {
        if (!string.IsNullOrEmpty(shopLabel)) return shopLabel;
        if (service == ServiceKind.Shop && shopData != null && !string.IsNullOrEmpty(shopData.shopName))
            return shopData.shopName;
        switch (service)
        {
            case ServiceKind.Workshop:  return "Xưởng Nâng Cấp";
            case ServiceKind.PiggyBank: return "Heo Đất";
            default:                    return "Cửa Hàng";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Chỉ mở lúc Gameplay. KHÔNG dùng cờ sticky chống-lặp (teleport tắt CharacterController
        // có thể nuốt OnTriggerExit -> cờ kẹt). IsVisible() của popup là chống-lặp chính xác.
        if (onlyDuringGameplay && GameManager.Instance != null
            && GameManager.Instance.currentState != GameManager.GameState.Gameplay)
            return;

        // NPC tính năng chưa làm → báo "đang phát triển", KHÔNG mở dịch vụ trống.
        if (comingSoon) { ShowComingSoon(); return; }

        switch (service)
        {
            case ServiceKind.Shop:      OpenShop();      break;
            case ServiceKind.Workshop:  OpenWorkshop();  break;
            case ServiceKind.PiggyBank: OpenPiggyBank(); break;
        }
    }

    // Người chơi đi RA: KHÔNG đóng popup (giữ mở, đóng bằng nút X) theo yêu cầu.

    private float _lastComingSoonAt = -10f;

    // Thông báo "tính năng đang phát triển" cho NPC chưa làm xong (Phase 2). Chống spam bằng cooldown 2s.
    private void ShowComingSoon()
    {
        if (Time.time - _lastComingSoonAt < 2f) return;
        _lastComingSoonAt = Time.time;
        YWonderLand.Environment.ScreenToast.ShowInfo($"🚧 {ResolveLabel()}: tính năng đang phát triển — sắp ra mắt!");
    }

    private void OpenShop()
    {
        var shop = Object.FindFirstObjectByType<ShopPopupController>(FindObjectsInactive.Include);
        if (shop == null) { Debug.LogWarning("[ShopZone] Không tìm thấy ShopPopupController!"); return; }
        if (shopData == null) { ShowComingSoon(); return; } // chưa gán shop → coi như đang phát triển (hết warning)
        if (!shop.IsVisible())
        {
            shop.Show(shopData);
            Debug.Log($"[ShopZone] Chạm nhà -> mở shop '{shopData.shopName}'.");
        }
    }

    private void OpenWorkshop()
    {
        var ws = WorkshopPopupController.Instance != null
            ? WorkshopPopupController.Instance
            : Object.FindFirstObjectByType<WorkshopPopupController>(FindObjectsInactive.Include);
        if (ws != null) ws.Show();
        else Debug.LogWarning("[ShopZone] Không tìm thấy WorkshopPopupController!");
    }

    private void OpenPiggyBank()
    {
        var piggy = Object.FindFirstObjectByType<PiggyBankPopupController>(FindObjectsInactive.Include);
        if (piggy != null) piggy.Show();
        else Debug.LogWarning("[ShopZone] Không tìm thấy PiggyBankPopupController!");
    }
}
