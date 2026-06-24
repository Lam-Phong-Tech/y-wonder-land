using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4.0f;
    public float sprintMultiplier = 1.6f;
    public float rotationSpeed = 15.0f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    public static PlayerController Instance { get; private set; }

    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private Vector3 playerVelocity;
    private bool isGrounded;

    // Input System Actions
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private InputAction attackAction;

    // Animation State
    private string currentAnimState = "";
    [Header("State")]
    public bool isSwimming = false;
    private float waterSurfaceY = 0f;
    // Cửa sổ "bật khỏi mặt nước" sau khi bấm Nhảy lúc đang bơi: tạm ngắt lực đẩy để vọt lên trèo bờ.
    private float swimLeapTimer = 0f;
    // Số khối nước (trigger tag "Water") nhân vật đang nằm trong. Cho phép GHÉP NHIỀU BOX để
    // ướm hồ hình dạng lạ — chỉ tắt bơi khi rời HẾT (đếm về 0), không bị giật giữa các box.
    private int waterVolumeCount = 0;
    private bool isSprintUIHeld = false;
    private bool isStickAutoSprintLatched = false;
    private bool stickSprintDirectionLocked = false;
    private Vector3 stickSprintLockDirection = Vector3.zero;
    private bool autoRunForward = false; // AUTO-RUN: nút chạy nhanh -> tự tiến thẳng, vuốt màn hình để lái
    [Header("Mobile Sprint")]
    [SerializeField] private bool enableStickAutoSprint = true;
    [SerializeField, Range(0.6f, 1f)] private float stickAutoSprintThreshold = 0.88f;
    // Runtime cap để đảm bảo ngưỡng sprint bằng joystick không quá gắt trên mobile
    private const float MobileSprintThresholdCap = 0.62f;
    private float actionLockTimer = 0f;
    private float _actionSpeed = 1f;

    [Header("Animation State Names")]
    public string animIdle = "Idle";
    public string animWalk = "Walk";
    public string animRun = "Run";
    public string animJump = "Jump";
    public string animSwim = "Swimming";

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        // Cache actions
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            interactAction = playerInput.actions["Interact"];
            attackAction = playerInput.actions["Attack"];
        }

        DiscoverMainCamera();

        // No need to cache Animator parameters in Code-Driven Animation
    }

    private void DiscoverMainCamera()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            Debug.Log("[PlayerController] Main Camera found and bound: " + mainCameraTransform.name);
        }
        else
        {
            // Fallback: look for any active camera in the scene
            Camera activeCam = GameObject.FindFirstObjectByType<Camera>();
            if (activeCam != null)
            {
                mainCameraTransform = activeCam.transform;
                Debug.Log("[PlayerController] Main Camera not tagged, falling back to active camera: " + activeCam.name);
            }
        }
    }

    void Update()
    {
        // Ensure components are cached
        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponent<Animator>();

        // 1. Gravity and Ground Check
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Soft clamp to keep grounded
        }

        // 2. Read Inputs via Input System Action Map
        bool isTyping = ChatPanelController.Instance != null && ChatPanelController.Instance.IsTyping();

        Vector2 keyboardVec = (!isTyping && moveAction != null) ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        // Gộp bàn phím (PC) + joystick ảo (mobile): joystick được ưu tiên khi đang kéo.
        Vector2 inputVec = (!isTyping) ? Vector2.ClampMagnitude(keyboardVec + virtualMoveInput, 1f) : Vector2.zero;

        // AUTO-RUN: nút chạy nhanh bật -> tự tiến THẲNG theo hướng camera (vuốt màn hình để lái).
        // Đang kéo joystick thì joystick ưu tiên; thả ra lại tự tiến.
        if (autoRunForward && !isTyping && inputVec.magnitude < 0.1f)
            inputVec = new Vector2(0f, 1f);

        bool jumpPressed = (!isTyping && jumpAction != null && jumpAction.triggered) || jumpQueuedFromUI;
        jumpQueuedFromUI = false; // tiêu thụ cờ nhảy từ nút HUD (mỗi lần bấm = 1 lần nhảy)
        bool interactPressed = !isTyping && interactAction != null && interactAction.triggered;
        
        // Kiểm tra xem chuột có đang nằm trên UI không để chặn click xuyên UI
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool attackPressed = !isTyping && attackAction != null && attackAction.triggered && !isPointerOverUI;

        // Process Interaction / Actions (Attack, Fish, etc.)
        // Bây giờ nếu ấn, chúng ta gọi thẳng PlayActionAnimation
        if (interactPressed)
        {
            Debug.Log("[PlayerController] Interact button pressed!");
        }

        if (attackPressed && actionLockTimer <= 0)
        {
            // Bỏ hành động chặt cây gán cứng ở đây, nhường quyền điều khiển cho FarmInteractionController
            Debug.Log("[PlayerController] Attack button pressed! (No hardcoded action)");
        }

        // --- KIỂM TRA ACTION LOCK ---
        // Nếu nhân vật đang làm việc (chặt cây, câu cá...), khóa không cho chạy nhảy nhưng vẫn áp dụng trọng lực
        if (actionLockTimer > 0)
        {
            actionLockTimer -= Time.deltaTime;
            
            // Nếu vừa hết giờ múa (làm việc xong): trả tốc độ animation về bình thường + cất đồ nghề
            if (actionLockTimer <= 0)
            {
                if (animator != null) animator.speed = 1f;
                if (YWonderLand.Player.EquipmentManager.Instance != null)
                {
                    YWonderLand.Player.EquipmentManager.Instance.HideAllTools();
                }
            }

            // Apply gravity
            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
            return; // Ngừng xử lý di chuyển
        }
        // ------------------------------

        Vector3 inputDir = new Vector3(inputVec.x, 0, inputVec.y);
        float inputMagnitude = Mathf.Clamp01(inputVec.magnitude);
        bool hasMoveInput = inputMagnitude >= 0.05f;
        Vector3 moveDirection = Vector3.zero;

        // Ensure we always have the active main camera transform if the camera changed states
        if (mainCameraTransform == null || !mainCameraTransform.gameObject.activeInHierarchy)
        {
            DiscoverMainCamera();
        }

        // 3. Move Relative to Camera Direction
        if (hasMoveInput)
        {
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;

            if (mainCameraTransform != null)
            {
                camForward = mainCameraTransform.forward;
                camRight = mainCameraTransform.right;
            }

            camForward.y = 0; // Flatten so player doesn't move vertically based on cam angle
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = camForward * inputDir.z + camRight * inputDir.x;
            Vector3 moveDirectionNormalized = moveDirection.normalized;
            Vector3 preRotationForward = transform.forward;

            // Sprint latch từ joystick: khóa hướng tại thời điểm kích hoạt.
            // Khi người chơi đổi hướng rõ rệt thì tự hủy latch (trả về điều khiển thường).
            if (isStickAutoSprintLatched)
            {
                if (!stickSprintDirectionLocked && moveDirectionNormalized.sqrMagnitude > 0.0001f)
                {
                    stickSprintLockDirection = moveDirectionNormalized;
                    stickSprintDirectionLocked = true;
                }
                else if (stickSprintDirectionLocked)
                {
                    float dirDot = Vector3.Dot(stickSprintLockDirection, moveDirectionNormalized);
                    if (dirDot < 0.92f)
                    {
                        SetStickAutoSprint(false);
                    }
                }
            }

            // Có input di chuyển: luôn quay theo hướng chạy để tránh "chạy lùi" khi kéo joystick xuống.
            // GIỮ free-look thì quay chậm hơn để vẫn giữ cảm giác nhìn quanh.
            if (moveDirectionNormalized.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirectionNormalized);
                float rotationMultiplier = IsFreeLook() ? 1.2f : 3.0f;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * rotationMultiplier * Time.deltaTime);
            }

            bool sprintActive = IsSprintActive();
            bool autoRunMode = autoRunForward || isStickAutoSprintLatched;
            float preDot = Vector3.Dot(preRotationForward, moveDirectionNormalized);
            bool isBackwardInput = !autoRunMode && preDot < -0.15f;
            if (isBackwardInput)
            {
                sprintActive = false;
            }

            // Tốc độ bơi bị giảm đi 30% so với đi bộ
            float currentSpeed = moveSpeed * (sprintActive ? sprintMultiplier : 1f);
            if (isBackwardInput)
            {
                currentSpeed *= 0.7f;
            }
            if (isSwimming) currentSpeed *= 0.7f;

            // Move the character controller
            controller.Move(moveDirectionNormalized * (currentSpeed * inputMagnitude) * Time.deltaTime);
        }

        // Không có input di chuyển: mới quay follow camera (tránh cảm giác bị "kéo lùi" khi đang lái joystick).
        if (!IsFreeLook() && !hasMoveInput && mainCameraTransform != null)
        {
            ThirdPersonCamera tpc = mainCameraTransform.GetComponent<ThirdPersonCamera>();
            if (tpc != null)
            {
                Quaternion targetRotation = Quaternion.Euler(0, tpc.Yaw, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 1.4f * Time.deltaTime);
            }
        }

        // 4. Handle Jumping — nhảy trên cạn HOẶC bật lên khỏi mặt nước để trèo lên bờ.
        if (jumpPressed && (isGrounded || isSwimming))
        {
            // Physics formula for jump velocity: v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            CrossFadeAnim(animJump, 0.1f);
            // Nhảy DƯỚI NƯỚC: mở cửa sổ ngắt lực đẩy để gravity + lực nhảy đưa nhân vật
            // vọt lên TRÊN mặt nước (giữ hướng tiến tới bờ là leo lên được).
            if (isSwimming) swimLeapTimer = 0.6f;
            Debug.Log("[PlayerController] Jump triggered!");
        }

        if (swimLeapTimer > 0f) swimLeapTimer -= Time.deltaTime;

        // Apply gravity & Buoyancy. Trong lúc "bật khỏi nước" (swimLeapTimer) -> dùng gravity, KHÔNG ghim lực đẩy.
        bool buoyancyActive = isSwimming && swimLeapTimer <= 0f;
        if (!buoyancyActive)
        {
            playerVelocity.y += gravity * Time.deltaTime;
        }
        else
        {
            // Lực đẩy Archimes: Nổi trên mặt nước
            // Giả sử tâm nhân vật (chân) cần nằm dưới mặt nước khoảng 1.2 mét để ngực vừa ngang mặt nước
            float targetFeetY = waterSurfaceY - 1.2f;
            float depth = targetFeetY - transform.position.y;

            // Lò xo nước: chìm càng sâu đẩy lên càng mạnh, cao quá thì rơi xuống nhẹ
            playerVelocity.y = depth * 5f;
            playerVelocity.y = Mathf.Clamp(playerVelocity.y, -10f, 5f);
        }
        controller.Move(playerVelocity * Time.deltaTime);

        // Cập nhật lại trạng thái chạm đất sau khi đã di chuyển
        isGrounded = controller.isGrounded;

        // 5. Update Locomotion Animations
        if (isSwimming && swimLeapTimer <= 0f)
        {
            // Nếu có di chuyển thì bơi lội, không thì bơi đứng (tạm dùng chung animation bơi)
            CrossFadeAnim(animSwim, 0.2f);
        }
        else if (!isGrounded || playerVelocity.y > 0)
        {
            // Giữ trạng thái "Jump"
            CrossFadeAnim(animJump, 0.2f);
        }
        else if (moveDirection.magnitude > 0.1f)
        {
            if (IsSprintActive())
            {
                CrossFadeAnim(animRun, 0.2f);
            }
            else
            {
                CrossFadeAnim(animWalk, 0.2f);
            }
        }
        else
        {
            CrossFadeAnim(animIdle, 0.2f);
        }
    }

    public void SetSprintUI(bool isPressed)
    {
        isSprintUIHeld = isPressed;
    }

    /// <summary>
    /// HUD báo trạng thái "đã giữ joystick đủ lâu để vào sprint" (kiểu FF/PUBG mobile).
    /// </summary>
    public void SetStickAutoSprint(bool enabled)
    {
        isStickAutoSprintLatched = enableStickAutoSprint && enabled;
        autoRunForward = isStickAutoSprintLatched;
        if (!isStickAutoSprintLatched)
        {
            stickSprintDirectionLocked = false;
            stickSprintLockDirection = Vector3.zero;
        }
    }

    /// <summary>Nút Sprint trên HUD bấm 1 lần = bật/tắt chạy nhanh. Trả về trạng thái mới (true = đang chạy nhanh).</summary>
    public bool ToggleSprintUI()
    {
        isSprintUIHeld = !isSprintUIHeld;
        return isSprintUIHeld;
    }

    /// <summary>Trạng thái chạy nhanh đang bật từ nút HUD (để HUD đồng bộ hiệu ứng nút).</summary>
    public bool IsSprintUIOn => isSprintUIHeld;
    public bool IsStickAutoSprintOn => isStickAutoSprintLatched;
    public float StickAutoSprintThreshold => Mathf.Min(stickAutoSprintThreshold, MobileSprintThresholdCap);

    /// <summary>Nút "chạy nhanh" trên HUD: bật/tắt AUTO-RUN — nhân vật tự tiến thẳng theo hướng camera,
    /// người chơi chỉ vuốt màn hình để đổi hướng (khỏi phải kéo joystick). Trả về trạng thái mới.</summary>
    public bool ToggleAutoRun()
    {
        autoRunForward = !autoRunForward;
        if (autoRunForward)
        {
            isSprintUIHeld = false;
        }
        return autoRunForward;
    }
    /// <summary>Auto-run đang bật (để HUD đồng bộ hiệu ứng nút).</summary>
    public bool IsAutoRunOn => autoRunForward;

    public bool IsSprintActive()
    {
        return isSprintUIHeld ||
               isStickAutoSprintLatched ||
               autoRunForward ||
               (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
    }

    // ── Joystick ảo (mobile) ──
    private Vector2 virtualMoveInput = Vector2.zero;
    /// <summary>Joystick ảo trên HUD gọi mỗi frame khi kéo (x,y trong [-1,1]); thả tay gọi Vector2.zero.</summary>
    public void SetMoveInput(Vector2 move)
    {
        virtualMoveInput = Vector2.ClampMagnitude(move, 1f);
    }

    /// <summary>
    /// Bản mở rộng cho mobile: truyền thêm độ kéo thô của joystick (trước response curve)
    /// để sprint theo lực kéo thật, không bị hụt ngưỡng do curve.
    /// </summary>
    public void SetMoveInput(Vector2 move, float rawMagnitude)
    {
        virtualMoveInput = Vector2.ClampMagnitude(move, 1f);
    }

    // ── Nút Nhảy ảo (mobile) ──
    private bool jumpQueuedFromUI = false;
    /// <summary>Nút Nhảy trên HUD gọi: đặt cờ, được tiêu thụ ở Update kế tiếp (giống jumpAction.triggered).</summary>
    public void TriggerJump() => jumpQueuedFromUI = true;

    // ── Hủy hoạt ảnh (nút X trên HUD) ──
    /// <summary>Nhân vật đang khóa trong một hoạt ảnh hành động (chặt/đào/cuốc/tưới/câu...).</summary>
    public bool IsBusy => actionLockTimer > 0f;
    /// <summary>Nút X trên HUD gọi: ngắt hoạt ảnh đang chạy, cất đồ nghề, ẩn thanh tiến trình, về Idle.</summary>
    public void CancelAction()
    {
        if (actionLockTimer <= 0f) return;
        actionLockTimer = 0f;
        if (animator != null)
        {
            animator.speed = 1f;
            CrossFadeAnim(animIdle, 0.1f);
        }
        if (YWonderLand.Player.EquipmentManager.Instance != null)
            YWonderLand.Player.EquipmentManager.Instance.HideAllTools();
        // Ẩn thanh tiến trình nếu đang chặt cây/đào khoáng.
        if (YWonderLand.UI.ResourceInteractionUIController.Instance != null)
            YWonderLand.UI.ResourceInteractionUIController.Instance.Hide();
    }

    // ── Free-Look (kiểu PUBG/Free Fire) ──
    private bool externalFreeLook = false;
    /// <summary>Nút Free-Look trên HUD mobile gọi: giữ = ngó quanh (nhân vật đứng im), thả = về bình thường.</summary>
    public void SetFreeLook(bool held) => externalFreeLook = held;
    private bool IsFreeLook() => externalFreeLook || (Keyboard.current != null && Keyboard.current.altKey.isPressed);

    /// <summary>
    /// Xoay nhân vật mặt thẳng về một điểm trong thế giới (chỉ trục Y).
    /// Dùng khi cuốc/gieo/tưới để nhân vật quay đúng về ô đất (camera lệch vai GTA nên
    /// hướng camera KHÔNG trùng hướng tới ô đất — xoay tay cho khớp, tránh cuốc lệch).
    /// </summary>
    public void FaceTowards(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    /// <summary>
    /// Gọi hàm này từ các script khác (Farm, Pet) để bắt nhân vật múa kỹ năng
    /// </summary>
    public float PlayActionAnimation(string animName, float duration, YWonderLand.Player.ToolType tool = YWonderLand.Player.ToolType.None, float speed = 1f)
    {
        if (animator == null) return 0f;

        speed = Mathf.Max(0.1f, speed);
        _actionSpeed = speed;
        animator.speed = speed;        // tăng/giảm tốc phát animation (2 = nhanh gấp đôi)

        CrossFadeAnim(animName, 0.1f); // phát animation trước

        // duration <= 0  ->  tự đo ĐÚNG độ dài clip (khỏi chỉnh tay theo artist)
        if (duration <= 0f)
        {
            duration = GetClipLength(animName); // thử khớp theo TÊN clip (nhanh)
            if (duration <= 0f)
            {
                // Tên clip không khớp tên state -> đọc clip THỰC TẾ đang phát sau 1 frame
                duration = speed; // tạm: actionLockTimer = 1f, coroutine sẽ chỉnh đúng
                StartCoroutine(SyncLockToPlayingClip());
            }
        }
        actionLockTimer = duration / speed; // phát nhanh thì khóa ngắn lại

        // Hiện công cụ tương ứng lên tay
        if (YWonderLand.Player.EquipmentManager.Instance != null && tool != YWonderLand.Player.ToolType.None)
        {
            YWonderLand.Player.EquipmentManager.Instance.ShowTool(tool);
        }

        Debug.Log($"[PlayerController] Hành động: {animName} (x{speed}) khóa {actionLockTimer:F2}s");
        return actionLockTimer;
    }

    // Lấy độ dài (giây) của clip animation theo tên — để khóa hành động đúng bằng thời lượng clip.
    private float GetClipLength(string animName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == animName) return clip.length;
        }
        return 0f;
    }

    // Đọc clip ĐANG PHÁT (sau CrossFade) và khóa hành động đúng bằng độ dài clip đó.
    // Dùng khi tên clip khác tên state nên không khớp được theo tên.
    private System.Collections.IEnumerator SyncLockToPlayingClip()
    {
        yield return null; // đợi 1 frame cho crossfade vào cuộc
        if (animator == null) yield break;

        AnimationClip clip = null;
        if (animator.IsInTransition(0))
        {
            var n = animator.GetNextAnimatorClipInfo(0);
            if (n.Length > 0) clip = n[0].clip;
        }
        else
        {
            var c = animator.GetCurrentAnimatorClipInfo(0);
            if (c.Length > 0) clip = c[0].clip;
        }

        if (clip != null)
        {
            actionLockTimer = clip.length / _actionSpeed;
            Debug.Log($"[PlayerController] Khóa theo clip đang phát: {clip.name} = {clip.length:F2}s / x{_actionSpeed}");
        }
    }

    private void CrossFadeAnim(string animName, float transitionDuration = 0.2f)
    {
        if (animator != null && currentAnimState != animName)
        {
            animator.CrossFadeInFixedTime(animName, transitionDuration);
            currentAnimState = animName;
        }
    }

    // ── Xử lý chạm mặt nước ──
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            waterVolumeCount++;
            isSwimming = true;
            waterSurfaceY = other.bounds.max.y;
            Debug.Log($"[PlayerController] Xuống nước (khối #{waterVolumeCount}). Cao độ mặt nước: {waterSurfaceY}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            waterSurfaceY = other.bounds.max.y;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            waterVolumeCount = Mathf.Max(0, waterVolumeCount - 1);
            // Chỉ thật sự "lên bờ" khi đã rời HẾT mọi khối nước (đếm về 0).
            if (waterVolumeCount == 0)
            {
                isSwimming = false;
                Debug.Log("[PlayerController] Lên bờ -> Chuyển về đi bộ.");
            }
        }
    }

    /// <summary>
    /// Đặt lại sạch trạng thái bơi — gọi khi teleport/đổi đảo. Cần vì tắt collider nước
    /// (ẩn biển lúc đổi đảo) có thể KHÔNG bắn OnTriggerExit -> waterVolumeCount kẹt > 0.
    /// </summary>
    public void ResetSwimState()
    {
        isSwimming = false;
        waterVolumeCount = 0;
        swimLeapTimer = 0f;
    }
}
