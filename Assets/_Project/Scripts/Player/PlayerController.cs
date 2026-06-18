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
    private bool isSprintUIHeld = false;
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

        Vector2 inputVec = (!isTyping && moveAction != null) ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool jumpPressed = !isTyping && jumpAction != null && jumpAction.triggered;
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

        Vector3 inputDir = new Vector3(inputVec.x, 0, inputVec.y).normalized;
        Vector3 moveDirection = Vector3.zero;

        // Ensure we always have the active main camera transform if the camera changed states
        if (mainCameraTransform == null || !mainCameraTransform.gameObject.activeInHierarchy)
        {
            DiscoverMainCamera();
        }

        // 3. Move Relative to Camera Direction
        if (inputVec.magnitude >= 0.1f)
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

            // Khi GIỮ Free-Look: nhân vật KHÔNG quay theo camera; chỉ xoay theo hướng đang chạy.
            // (Trường hợp mặc định — quay theo camera — xử lý mỗi frame ở dưới, kể cả khi đứng yên.)
            if (IsFreeLook() && moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Check Sprint
            bool isSprinting = isSprintUIHeld || (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
            // Tốc độ bơi bị giảm đi 30% so với đi bộ
            float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
            if (isSwimming) currentSpeed *= 0.7f;

            // Move the character controller
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);
        }

        // MẶC ĐỊNH (không Free-Look): nhân vật LUÔN quay lưng về người chơi — mặt hướng theo camera.
        // Chạy MỖI FRAME kể cả khi đứng yên -> xoay camera là nhân vật xoay theo (kiểu PUBG/Free Fire mobile).
        if (!IsFreeLook() && mainCameraTransform != null)
        {
            ThirdPersonCamera tpc = mainCameraTransform.GetComponent<ThirdPersonCamera>();
            if (tpc != null)
            {
                Quaternion targetRotation = Quaternion.Euler(0, tpc.Yaw, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2.5f * Time.deltaTime);
            }
        }

        // 4. Handle Jumping (Không cho nhảy khi đang bơi)
        if (jumpPressed && isGrounded && !isSwimming)
        {
            // Physics formula for jump velocity: v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            CrossFadeAnim(animJump, 0.1f);
            Debug.Log("[PlayerController] Jump triggered!");
        }

        // Apply gravity & Buoyancy
        if (!isSwimming)
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
        if (isSwimming)
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
            bool isSprinting = isSprintUIHeld || (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
            if (isSprinting)
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
            isSwimming = true;
            waterSurfaceY = other.bounds.max.y;
            Debug.Log($"[PlayerController] Xuống nước. Cao độ mặt nước: {waterSurfaceY}");
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
            isSwimming = false;
            Debug.Log("[PlayerController] Lên bờ -> Chuyển về đi bộ.");
        }
    }
}
