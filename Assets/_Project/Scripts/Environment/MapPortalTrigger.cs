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

    private bool _playerInside = false;

    private void Reset()
    {
        // Tự bật Is Trigger cho tiện khi mới gắn component
        if (TryGetComponent<Collider>(out var col)) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_playerInside) return;          // tránh kích hoạt lặp khi vẫn đứng trong cổng
        _playerInside = true;

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

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) _playerInside = false;
    }
}
