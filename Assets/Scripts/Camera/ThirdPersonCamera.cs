using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GTA-style Third Person Camera Controller.
/// Uses rigid orbit calculation (no position/rotation smoothing) to prevent wobble.
/// Only mouse input is smoothed for a premium feel.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform the camera orbits around. Set at runtime by GameManager.")]
    public Transform target;

    [Header("Distance")]
    [Tooltip("Fixed distance from the target.")]
    public float distance = 5.0f;

    [Header("Shoulder Offset (GTA-style)")]
    [Tooltip("Horizontal offset: positive = camera shifts right, character appears left on screen.")]
    public float shoulderOffsetX = 0.6f;
    [Tooltip("Vertical offset from the target pivot (look at chest/head height).")]
    public float heightOffset = 1.6f;

    [Header("Mouse Sensitivity")]
    [Tooltip("Horizontal rotation speed.")]
    public float horizontalSensitivity = 2.0f;
    [Tooltip("Vertical rotation speed.")]
    public float verticalSensitivity = 1.5f;

    [Header("Input Smoothing")]
    [Tooltip("How much the mouse input is smoothed. 0 = no smoothing (raw), higher = smoother but more laggy.")]
    [Range(0f, 0.95f)]
    public float inputSmoothing = 0.5f;

    [Header("Vertical Angle Limits")]
    [Tooltip("Minimum pitch angle (negative = look slightly downward).")]
    public float minVerticalAngle = -10f;
    [Tooltip("Maximum pitch angle (positive = look upward).")]
    public float maxVerticalAngle = 50f;

    [Header("Ground Collision")]
    [Tooltip("Minimum height above ground to prevent camera going underground.")]
    public float minHeightAboveGround = 0.5f;
    [Tooltip("Layer mask for ground collision check.")]
    public LayerMask groundLayerMask = ~0;

    // Internal state
    private float yaw = 0f;
    private float pitch = 15f;
    private Vector2 smoothedInput = Vector2.zero;
    private bool cursorLocked = true;

    // Input System
    private InputAction lookAction;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw from current camera direction
        yaw = transform.eulerAngles.y;

        // Find Look action from any PlayerInput in scene
        PlayerInput playerInput = null;
        if (target != null)
        {
            playerInput = target.GetComponent<PlayerInput>();
        }
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }
        if (playerInput != null)
        {
            lookAction = playerInput.actions["Look"];
        }
    }

    void Update()
    {
        // Toggle cursor lock with ESC
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            cursorLocked = !cursorLocked;
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !cursorLocked;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Read raw mouse input (skip rotation when cursor is unlocked)
        Vector2 rawInput = Vector2.zero;
        if (cursorLocked)
        {
            if (lookAction != null)
            {
                rawInput = lookAction.ReadValue<Vector2>();
            }
            else
            {
                rawInput.x = Input.GetAxis("Mouse X");
                rawInput.y = Input.GetAxis("Mouse Y");
            }
        }

        // 2. Smooth the input (prevents jitter from raw mouse delta spikes)
        smoothedInput = Vector2.Lerp(rawInput, smoothedInput, inputSmoothing);

        // 3. Apply smoothed input to yaw/pitch
        yaw += smoothedInput.x * horizontalSensitivity;
        pitch -= smoothedInput.y * verticalSensitivity;

        // 4. Clamp vertical angle
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        // 5. Calculate orbit position (rigid, no smoothing = no wobble)
        Vector3 pivotPoint = target.position + Vector3.up * heightOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);

        // Camera sits behind the pivot at the given distance
        Vector3 cameraPos = pivotPoint - orbitRotation * Vector3.forward * distance;

        // Apply shoulder offset
        cameraPos += orbitRotation * Vector3.right * shoulderOffsetX;

        // 6. Prevent going underground
        cameraPos = ClampAboveGround(cameraPos);

        // 7. Set camera transform directly (rigid = rock solid, no wobble)
        transform.position = cameraPos;
        transform.rotation = orbitRotation;
    }

    /// <summary>
    /// Ensures the camera position stays above the ground surface.
    /// </summary>
    private Vector3 ClampAboveGround(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundLayerMask))
        {
            float minY = hit.point.y + minHeightAboveGround;
            if (pos.y < minY)
            {
                pos.y = minY;
            }
        }
        return pos;
    }

    void OnDisable()
    {
        // Unlock cursor when camera is disabled (e.g., menu/login state)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Set the target transform at runtime (called by GameManager when player spawns).
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // Re-acquire Look action from new target's PlayerInput
        if (newTarget != null)
        {
            PlayerInput pi = newTarget.GetComponent<PlayerInput>();
            if (pi != null)
            {
                lookAction = pi.actions["Look"];
            }
        }

        Debug.Log($"[ThirdPersonCamera] Target set to: {(newTarget != null ? newTarget.name : "NULL")}");
    }
}
