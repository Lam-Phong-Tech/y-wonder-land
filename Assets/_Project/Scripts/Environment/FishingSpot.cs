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
    
    private Transform playerTransform;
    private bool isPlayerNearby = false;
    private FishingOverlayController fishingUI;
    
    // Prompt UI (simple TextMeshPro 3D above the spot)
    private TMPro.TextMeshPro promptLabel;
    private GameObject promptObject;
    
    void Start()
    {
        // Find player
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null) playerTransform = playerGo.transform;
        
        // Find fishing UI
        fishingUI = FindFirstObjectByType<FishingOverlayController>();
        
        // Create floating prompt
        CreatePrompt();
    }
    
    void Update()
    {
        if (playerTransform == null)
        {
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null) playerTransform = playerGo.transform;
            return;
        }
        
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = dist <= interactionRange;
        
        // Show/hide prompt
        if (promptObject != null)
        {
            promptObject.SetActive(isPlayerNearby);
            if (isPlayerNearby)
            {
                // Billboard face camera
                if (Camera.main != null)
                    promptObject.transform.rotation = Camera.main.transform.rotation;
            }
        }
        
        // Check for interaction key (E or left click)
        if (isPlayerNearby)
        {
            bool pressed = false;
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                pressed = true;
            
            if (pressed && fishingUI != null)
            {
                fishingUI.Show();
                Debug.Log("[FishingSpot] Opened fishing UI");
            }
        }
    }
    
    private void CreatePrompt()
    {
        promptObject = new GameObject("FishingPrompt");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = new Vector3(0, 2.5f, 0);
        
        promptLabel = promptObject.AddComponent<TMPro.TextMeshPro>();
        promptLabel.text = promptText;
        promptLabel.fontSize = 3f;
        promptLabel.fontStyle = TMPro.FontStyles.Bold;
        promptLabel.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold
        promptLabel.alignment = TMPro.TextAlignmentOptions.Center;
        promptLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        promptLabel.overflowMode = TMPro.TextOverflowModes.Overflow;
        promptLabel.outlineColor = new Color32(45, 52, 54, 255); // #2D3436
        promptLabel.outlineWidth = 0.3f;
        promptLabel.sortingOrder = 100;
        
        var rt = promptLabel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(15f, 3f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        
        var mr = promptLabel.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.allowOcclusionWhenDynamic = false;
        
        promptLabel.ForceMeshUpdate();
        promptObject.SetActive(false);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
