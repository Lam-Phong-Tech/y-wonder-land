using UnityEngine;

/// <summary>
/// GTA V-style third-person camera.
/// - Camera orbits around the player independently via mouse input.
/// - Player moves relative to camera direction (handled by PlayerController).
/// - Smooth position damping prevents jitter when the character turns.
/// - Raycast collision prevents clipping through terrain/walls.
/// - Scroll wheel zoom in/out.
/// - Cursor is locked ONLY when this camera is active (not during menus/cutscenes).
/// </summary>
public class GameplayCamera : MonoBehaviour
{
    private Transform target;

    [Header("Orbit Settings")]
    public float mouseSensitivity = 2.5f;
    public float minPitch = -20f;   // How far you can look down
    public float maxPitch = 60f;    // How far you can look up

    [Header("Distance & Zoom")]
    public float defaultDistance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 14f;
    public float zoomSmoothSpeed = 8f;
    public float scrollSensitivity = 3f;

    [Header("Follow Smoothing")]
    public float positionSmoothTime = 0.1f;  // Lower = snappier, higher = more cinematic lag
    public float lookAtHeightOffset = 1.4f;  // Focus point on character (chest/shoulder height)

    [Header("Collision")]
    public float collisionPadding = 0.3f;    // Pushes camera forward slightly to avoid wall clipping
    public LayerMask collisionLayers = ~0;   // Which layers the camera collides with

    // Internal state
    private float yaw;
    private float pitch = 12f;
    private float currentDistance;
    private float targetDistance;
    private Vector3 followVelocity = Vector3.zero;
    private bool isInitialized = false;

    void Start()
    {
        currentDistance = defaultDistance;
        targetDistance = defaultDistance;
        yaw = transform.eulerAngles.y;
        // Do NOT lock cursor here — let OnEnable handle it when GameManager activates us
    }

    /// <summary>
    /// Called every time this camera GameObject is enabled (Gameplay state).
    /// </summary>
    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[GameplayCamera] Enabled — cursor locked for gameplay.");
    }

    /// <summary>
    /// Called every time this camera GameObject is disabled (Menu/Cutscene states).
    /// </summary>
    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[GameplayCamera] Disabled — cursor unlocked.");
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            // Start behind the character
            yaw = target.eulerAngles.y;
            isInitialized = true;
            Debug.Log("[GameplayCamera] Target set: " + target.name);
        }
    }

    void LateUpdate()
    {
        // Auto-discover target if missing
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                yaw = target.eulerAngles.y;
                isInitialized = true;
            }
            else
            {
                return;
            }
        }

        // --- 1. Read Mouse Input (New Input System) ---
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw   += delta.x * mouseSensitivity * 0.1f;  // Scale down raw pixel delta
            pitch -= delta.y * mouseSensitivity * 0.1f;
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // --- 2. Scroll Wheel Zoom ---
        if (mouse != null)
        {
            float scrollValue = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollValue) > 0.01f)
            {
                targetDistance -= scrollValue * scrollSensitivity * 0.01f;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSmoothSpeed * Time.deltaTime);

        // --- 3. Calculate Orbit Position ---
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 lookAtPoint = target.position + Vector3.up * lookAtHeightOffset;
        Vector3 desiredPosition = lookAtPoint - (orbitRotation * Vector3.forward * currentDistance);

        // --- 4. Collision Detection (prevent clipping through walls/terrain) ---
        Vector3 directionToCamera = (desiredPosition - lookAtPoint).normalized;
        float desiredDist = Vector3.Distance(lookAtPoint, desiredPosition);

        if (Physics.SphereCast(lookAtPoint, collisionPadding, directionToCamera, out RaycastHit hit, desiredDist, collisionLayers))
        {
            float clampedDist = hit.distance - collisionPadding;
            clampedDist = Mathf.Max(clampedDist, 0.5f);
            desiredPosition = lookAtPoint + directionToCamera * clampedDist;
        }

        // --- 5. Smooth Follow ---
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, positionSmoothTime);

        // --- 6. Always Look At Character ---
        transform.LookAt(lookAtPoint);

        // --- 7. Toggle Cursor Lock with Escape ---
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
