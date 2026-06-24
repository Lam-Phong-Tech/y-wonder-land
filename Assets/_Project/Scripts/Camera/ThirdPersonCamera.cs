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
    public float shoulderOffsetX = 0.45f;
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
    public float inputSmoothing = 0.1f;

    [Header("Vertical Angle Limits")]
    [Tooltip("Minimum pitch angle. Đặt >= 0 để tránh góc nhìn không phù hợp kiểm duyệt.")]
    public float minVerticalAngle = 6f;
    [Tooltip("Maximum pitch angle (positive = look upward).")]
    public float maxVerticalAngle = 30f;

    [Header("Camera Collision")]
    [Tooltip("Layer mask for obstacles (walls, ground) that should block the camera.")]
    public LayerMask obstacleLayerMask = ~0;
    [Tooltip("Radius of the camera sphere cast to prevent clipping through walls.")]
    public float cameraCollisionRadius = 0.2f;
    [Tooltip("Minimum distance the camera can get to the player when blocked.")]
    public float minDistance = 1.0f;

    [Header("Touch Look (Mobile)")]
    [Tooltip("Độ nhạy xoay NGANG khi kéo 1 ngón trên màn hình (mobile). Tách riêng với chuột.")]
    public float touchHorizontalSensitivity = 0.15f;
    [Tooltip("Độ nhạy xoay DỌC khi kéo 1 ngón (mobile).")]
    public float touchVerticalSensitivity = 0.12f;
    [Tooltip("Làm mượt riêng cho touch look. 0 = raw, cao hơn = êm hơn nhưng trễ hơn.")]
    [Range(0f, 0.95f)]
    public float touchInputSmoothing = 0.08f;

    // Internal state
    private float yaw = 0f;
    private float pitch = 15f;
    private Vector2 smoothedInput = Vector2.zero;
    private bool cursorLocked = true;
    private Vector2 _touchLookDelta; // cộng dồn delta kéo ngón trong frame
    private Vector2 _smoothedTouchLook = Vector2.zero;
    // Base "1x" độ nhạy (chốt sau Start clamp) — để SetUserSensitivity scale cả PC + mobile theo 1 slider.
    private float baseH, baseV, baseTH, baseTV;
    private bool baseCaptured = false;

    // Singleton để GameHUDController (vùng nhìn mobile) đẩy delta vào.
    public static ThirdPersonCamera Instance { get; private set; }

    // Properties for PlayerController
    public float Yaw => yaw;

    // Input System
    private InputAction lookAction;
    // Safety floor cho kiểm duyệt: không cho camera hạ xuống dưới ngưỡng này.
    private const float ComplianceMinVerticalAngle = 6f;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Mobile: cộng delta kéo 1 ngón (đơn vị panel UI) để xoay camera. Gọi từ GameHUDController.</summary>
    public void AddTouchLook(Vector2 delta)
    {
        _touchLookDelta += delta;
    }

    /// <summary>Đặt độ nhạy camera từ Settings (1 slider 0..1, scale CẢ chuột PC + chạm mobile). 0.5 = mức gốc (1x).</summary>
    public void SetUserSensitivity(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        if (!baseCaptured) return; // chưa chốt base (chưa Start) → áp sau khi Start
        float mult = Mathf.Lerp(0.4f, 1.6f, t01); // t=0.5 → 1x (giữ cảm giác gốc)
        horizontalSensitivity = baseH * mult;
        verticalSensitivity = baseV * mult;
        touchHorizontalSensitivity = baseTH * mult;
        touchVerticalSensitivity = baseTV * mult;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw from current camera direction
        yaw = transform.eulerAngles.y;

        // Chống chóng mặt: ép input mượt KHÔNG quán tính + giảm độ nhạy về mức êm
        // (áp dụng kể cả khi component trong scene còn lưu giá trị cũ cao).
        inputSmoothing = Mathf.Clamp(inputSmoothing, 0f, 0.12f);
        touchInputSmoothing = Mathf.Clamp(touchInputSmoothing, 0f, 0.16f);
        horizontalSensitivity = Mathf.Min(horizontalSensitivity, 0.8f);
        verticalSensitivity = Mathf.Min(verticalSensitivity, 0.6f);
        minVerticalAngle = Mathf.Max(minVerticalAngle, ComplianceMinVerticalAngle);
        maxVerticalAngle = Mathf.Clamp(maxVerticalAngle, minVerticalAngle + 5f, 30f);
        if (Mathf.Abs(shoulderOffsetX) > 0.45f)
        {
            shoulderOffsetX = Mathf.Sign(shoulderOffsetX) * 0.45f;
        }

        // Chốt mức "1x" (base) sau khi clamp, rồi áp độ nhạy người chơi đã lưu (Settings — 1 slider).
        baseH = horizontalSensitivity; baseV = verticalSensitivity;
        baseTH = touchHorizontalSensitivity; baseTV = touchVerticalSensitivity;
        baseCaptured = true;
        SetUserSensitivity(PlayerPrefs.GetFloat("YW_CamSensitivity", 0.5f));

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
        }

        // Mỗi frame áp dụng trạng thái chuột THỰC TẾ: tự MỞ chuột khi có popup
        // đang mở (UIPopupTracker.AnyOpen) hoặc khi người chơi nhấn ESC.
        ApplyCursorState();
    }

    // Chuột chỉ thực sự khóa khi: người chơi muốn khóa VÀ không có popup nào đang mở.
    private bool IsEffectivelyLocked()
    {
        return cursorLocked && !UIPopupTracker.AnyOpen;
    }

    private void ApplyCursorState()
    {
        bool locked = IsEffectivelyLocked();
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void LateUpdate()
    {
        if (target == null) 
        {
            // Debug.LogWarning("[ThirdPersonCamera] No target assigned!");
            return;
        }

        // 1. Read raw mouse input (skip rotation when cursor is unlocked)
        Vector2 rawInput = Vector2.zero;
        if (IsEffectivelyLocked())
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

        // 2. Mượt nhẹ, framerate-independent, KHÔNG quán tính trôi (dừng tay là camera dừng ngay).
        float follow = 1f - Mathf.Pow(inputSmoothing, Time.deltaTime * 60f);
        smoothedInput = Vector2.Lerp(smoothedInput, rawInput, follow);

        // 3. Apply smoothed input to yaw/pitch
        yaw += smoothedInput.x * horizontalSensitivity;
        pitch -= smoothedInput.y * verticalSensitivity;

        // 3b. Kéo chạm mobile: làm mượt RIÊNG cho touch để đỡ gắt trên điện thoại.
        float touchFollow = 1f - Mathf.Pow(touchInputSmoothing, Time.deltaTime * 60f);
        _smoothedTouchLook = Vector2.Lerp(_smoothedTouchLook, _touchLookDelta, touchFollow);
        if (_smoothedTouchLook.sqrMagnitude < 0.0001f && _touchLookDelta.sqrMagnitude < 0.0001f)
        {
            _smoothedTouchLook = Vector2.zero;
        }

        // Trục Y panel hướng XUỐNG -> kéo NGÓN lên (delta.y âm) thì pitch tăng = nhìn lên.
        if (_smoothedTouchLook != Vector2.zero)
        {
            yaw += _smoothedTouchLook.x * touchHorizontalSensitivity;
            pitch -= _smoothedTouchLook.y * touchVerticalSensitivity;
        }
        _touchLookDelta = Vector2.zero;

        // 4. Clamp vertical angle
        float effectiveMinVertical = Mathf.Max(minVerticalAngle, ComplianceMinVerticalAngle);
        pitch = Mathf.Clamp(pitch, effectiveMinVertical, maxVerticalAngle);

        // 5. Calculate orbit position (rigid, no smoothing = no wobble)
        Vector3 pivotPoint = target.position + Vector3.up * heightOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);

        // Camera sits behind the pivot at the given distance
        Vector3 cameraPos = pivotPoint - orbitRotation * Vector3.forward * distance;

        // Apply shoulder offset
        cameraPos += orbitRotation * Vector3.right * shoulderOffsetX;

        // 6. Camera Collision (Ngăn xuyên tường và đất)
        cameraPos = HandleCameraCollision(pivotPoint, cameraPos);

        // 7. Set camera transform directly (rigid = rock solid, no wobble)
        transform.position = cameraPos;
        transform.rotation = orbitRotation;
        
        // Debug.Log($"[ThirdPersonCamera] Orbiting at {transform.position} around {target.name}");
    }

    /// <summary>
    /// Bắn SphereCast từ vai/ngực nhân vật về phía Camera. Nếu đụng tường/đất thì kéo Camera lại gần nhân vật.
    /// Tự động bỏ qua các Collider thuộc về Player và các vùng Trigger vô hình.
    /// </summary>
    private Vector3 HandleCameraCollision(Vector3 pivot, Vector3 targetCameraPos)
    {
        Vector3 direction = targetCameraPos - pivot;
        float dist = direction.magnitude;
        direction.Normalize();

        RaycastHit[] hits = Physics.SphereCastAll(pivot, cameraCollisionRadius, direction, dist, obstacleLayerMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Bỏ qua nếu tia quét đụng trúng chính nhân vật (target) hoặc các mảnh con của nhân vật
            if (target != null && (hit.collider.transform == target || hit.collider.transform.IsChildOf(target)))
                continue;

            // Bỏ qua các vùng Trigger (như mặt nước, vùng sáng) không có cản trở vật lý
            if (hit.collider.isTrigger)
                continue;

            // Đặt camera tại điểm đụng
            float hitDistance = Mathf.Max(hit.distance, minDistance);
            return pivot + direction * hitDistance;
        }

        return targetCameraPos;
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
