using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-Down camera controller for Build Mode.
/// Switches from ThirdPersonCamera to a bird's-eye view.
/// Supports pan (WASD/drag) and zoom (scroll wheel).
/// </summary>
public class BuildCameraController : MonoBehaviour
{
    public static BuildCameraController Instance { get; private set; }

    [Header("Top-Down Settings")]
    [SerializeField] private float defaultHeight = 15f;
    [SerializeField] private float minHeight = 8f;
    [SerializeField] private float maxHeight = 30f;
    [SerializeField] private float panSpeed = 12f;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float transitionDuration = 0.5f;

    [Header("Camera Angle")]
    [SerializeField] private float topDownAngle = 75f; // Not fully 90 to keep some depth perception

    // State
    private bool isActive = false;
    private Camera cam;
    private ThirdPersonCamera thirdPersonCam;

    // Saved state for restoring
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private bool savedThirdPersonEnabled;

    // Transition
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private Vector3 transitionStartPos;
    private Quaternion transitionStartRot;
    private Vector3 transitionTargetPos;
    private Quaternion transitionTargetRot;

    // Current top-down position
    private Vector3 topDownFocusPoint;
    private float currentHeight;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        cam = Camera.main;
        thirdPersonCam = FindFirstObjectByType<ThirdPersonCamera>();
    }

    void Update()
    {
        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        if (!isActive) return;

        HandlePan();
        HandleZoom();
    }

    // ── Activate / Deactivate ──

    /// <summary>
    /// Switch to top-down build camera.
    /// </summary>
    public void Activate()
    {
        if (cam == null) cam = Camera.main;
        if (thirdPersonCam == null) thirdPersonCam = FindFirstObjectByType<ThirdPersonCamera>();

        // Save current camera state
        savedPosition = cam.transform.position;
        savedRotation = cam.transform.rotation;

        // Disable ThirdPersonCamera
        if (thirdPersonCam != null)
        {
            savedThirdPersonEnabled = thirdPersonCam.enabled;
            thirdPersonCam.enabled = false;
        }

        // Unlock cursor for build mode
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Calculate target position (above current look point)
        currentHeight = defaultHeight;
        topDownFocusPoint = cam.transform.position + cam.transform.forward * 10f;
        topDownFocusPoint.y = 0f;

        // Start transition
        Vector3 targetPos = topDownFocusPoint + Vector3.up * currentHeight;
        Quaternion targetRot = Quaternion.Euler(topDownAngle, 0f, 0f);

        StartTransition(cam.transform.position, cam.transform.rotation, targetPos, targetRot);

        isActive = true;
        Debug.Log("[BuildCamera] Activated — Top-Down view");
    }

    /// <summary>
    /// Return to third-person camera.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;

        if (cam != null)
        {
            // Transition back to saved position
            StartTransition(cam.transform.position, cam.transform.rotation, savedPosition, savedRotation);
        }

        Debug.Log("[BuildCamera] Deactivating — returning to Third-Person");
    }

    private void OnTransitionComplete()
    {
        if (!isActive)
        {
            // Restore ThirdPersonCamera
            if (thirdPersonCam != null)
            {
                thirdPersonCam.enabled = savedThirdPersonEnabled;
            }
        }
    }

    // ── Transition ──

    private void StartTransition(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot)
    {
        transitionStartPos = fromPos;
        transitionStartRot = fromRot;
        transitionTargetPos = toPos;
        transitionTargetRot = toRot;
        transitionTimer = 0f;
        isTransitioning = true;
    }

    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / transitionDuration);

        // Smooth ease-in-out
        float easedT = t * t * (3f - 2f * t);

        if (cam != null)
        {
            cam.transform.position = Vector3.Lerp(transitionStartPos, transitionTargetPos, easedT);
            cam.transform.rotation = Quaternion.Slerp(transitionStartRot, transitionTargetRot, easedT);
        }

        if (t >= 1f)
        {
            isTransitioning = false;
            OnTransitionComplete();
        }
    }

    // ── Pan (WASD or Middle Mouse Drag) ──

    private void HandlePan()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector3 move = Vector3.zero;

        if (keyboard.wKey.isPressed) move.z += 1f;
        if (keyboard.sKey.isPressed) move.z -= 1f;
        if (keyboard.aKey.isPressed) move.x -= 1f;
        if (keyboard.dKey.isPressed) move.x += 1f;

        if (move.sqrMagnitude > 0.01f)
        {
            move.Normalize();
            topDownFocusPoint += move * panSpeed * Time.deltaTime;

            // Update camera position
            if (cam != null)
            {
                Vector3 newPos = topDownFocusPoint + Vector3.up * currentHeight;
                cam.transform.position = newPos;
            }
        }
    }

    // ── Zoom (Scroll Wheel) ──

    private void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentHeight -= scroll * zoomSpeed * 0.01f; // 0.01f to normalize scroll value (typically ±120)
            currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);

            if (cam != null)
            {
                Vector3 newPos = topDownFocusPoint + Vector3.up * currentHeight;
                cam.transform.position = newPos;
            }
        }
    }
}
