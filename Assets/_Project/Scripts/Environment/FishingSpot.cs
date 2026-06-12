using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Place on a 3D object near water to create a fishing interaction point.
/// When player is within range and clicks, opens the FishingOverlay UI.
/// </summary>
public class FishingSpot : MonoBehaviour
{
    [Header("Settings")]
    public float interactionRange = 3f;
    public string promptText = "Nh\u1ea5p F \u0111\u1ec3 c\u00e2u c\u00e1"; // "Nhấp F để câu cá"
    
    // Tương tác giờ được quản lý tập trung bên FarmInteractionController.cs
    // Class này chỉ còn đóng vai trò làm điểm đánh dấu (Tag component).

    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
