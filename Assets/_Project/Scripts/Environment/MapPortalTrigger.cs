using UnityEngine;

/// <summary>
/// MapPortalTrigger: Gắn lên một "cổng dịch chuyển" (portal) đặt trong đảo.
/// Khi nhân vật BƯỚC VÀO vùng trigger -> tự mở Bản đồ (MapPopupController) để
/// người chơi chọn đảo muốn tới. Việc dịch chuyển thật do IslandTravelManager lo.
///
/// YÊU CẦU SETUP:
///  - GameObject này phải có Collider với "Is Trigger" = TRUE (Reset tự bật giúp).
///  - Nhân vật có tag "Player" (đã có sẵn) + CharacterController.
///  - Trong Scene nền (Scene 0) có sẵn MapPopupController.
///  - QUAN TRỌNG: đặt điểm spawn của mỗi đảo CÁCH XA cổng vài mét, để khi vừa
///    dịch chuyển tới không vô tình đứng đè lên cổng -> mở lại Bản đồ ngay.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MapPortalTrigger : MonoBehaviour
{
    [Header("Tùy chọn")]
    [Tooltip("Kéo MapPopupController vào đây. Để trống thì script tự tìm trong scene.")]
    [SerializeField] private MapPopupController mapPopup;

    [Tooltip("Chỉ mở Bản đồ khi đang ở trạng thái Gameplay (không mở lúc cutscene/menu)")]
    [SerializeField] private bool onlyDuringGameplay = true;

    [Header("Bảng tên trên đầu cổng")]
    [Tooltip("Chữ nổi trên cổng cho người chơi dễ nhận biết")]
    [SerializeField] private string portalLabel = "Cổng Dịch Chuyển";

    [Tooltip("Độ cao bảng tên so với gốc cổng (m). Cổng cao thì tăng số này.")]
    [SerializeField] private float nameTagHeight = 3.0f;

    private void Reset()
    {
        // Tự bật Is Trigger cho tiện khi mới gắn component
        if (TryGetComponent<Collider>(out var col)) col.isTrigger = true;
    }

    private void Start()
    {
        // Tự gắn bảng tên nổi trên đầu cổng (giống NPC) để người chơi dễ thấy đây là cổng đi đảo.
        if (GetComponent<FloatingNameTag>() == null)
        {
            FloatingNameTag tag = gameObject.AddComponent<FloatingNameTag>();
            tag.displayName = portalLabel;
            tag.nameColor = FloatingNameTag.COLOR_CONFIRM; // xanh dương — phân biệt NPC (vàng) & Guide (tím)
            tag.heightOffset = nameTagHeight;
            tag.tmpFontSize = 3.5f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Chỉ mở Bản đồ khi đang ở Gameplay (không mở lúc cutscene/menu).
        // LƯU Ý: KHÔNG dùng cờ "_playerInside" làm chống-mở-lặp nữa — vì khi
        // IslandTravelManager teleport, nó TẮT CharacterController nên Unity có thể
        // KHÔNG bao giờ bắn OnTriggerExit -> cờ sẽ kẹt true và cổng "chết" sau lần
        // đầu. OnTriggerEnter vốn chỉ bắn 1 lần mỗi lần bước vào, và IsVisible() đã
        // chặn việc mở lại khi bản đồ đang hiện -> đó là nguồn chống-lặp luôn chính xác.
        if (onlyDuringGameplay && GameManager.Instance != null
            && GameManager.Instance.currentState != GameManager.GameState.Gameplay)
            return;

        if (mapPopup == null)
            mapPopup = FindFirstObjectByType<MapPopupController>(FindObjectsInactive.Include);

        if (mapPopup != null && !mapPopup.IsVisible())
        {
            mapPopup.Show();
            Debug.Log("[MapPortal] Nhân vật bước vào cổng -> mở Bản đồ.");
        }
    }
}
