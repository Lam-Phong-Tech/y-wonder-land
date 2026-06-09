using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4.0f;
    public float rotationSpeed = 15.0f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

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

    // Animator parameter hash IDs for better performance
    private int speedHash;
    private int isMovingHash;
    private int jumpHash;
    private int sitHash;
    private int feedHash;
    private int plantHash;
    private int chopHash;
    private int petHash;

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

        // Cache Animator parameters
        speedHash = Animator.StringToHash("Speed");
        isMovingHash = Animator.StringToHash("IsMoving");
        jumpHash = Animator.StringToHash("Jump");
        sitHash = Animator.StringToHash("Sit");
        feedHash = Animator.StringToHash("Feed");
        plantHash = Animator.StringToHash("Plant");
        chopHash = Animator.StringToHash("Chop");
        petHash = Animator.StringToHash("Pet");
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
        Vector2 inputVec = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool jumpPressed = jumpAction != null && jumpAction.triggered;
        bool interactPressed = interactAction != null && interactAction.triggered;
        bool attackPressed = attackAction != null && attackAction.triggered;

        // Process Interaction / Actions
        if (interactPressed)
        {
            Debug.Log("[PlayerController] Interact button pressed!");
            // TODO: Bỏ hành động Trồng Cây ra khỏi nút Interact như đã trao đổi
        }

        if (attackPressed)
        {
            Debug.Log("[PlayerController] Attack button pressed!");
            if (animator != null && HasParameter(animator, "Chop"))
            {
                animator.SetTrigger(chopHash);
            }
        }

        if (attackPressed)
        {
            Debug.Log("[PlayerController] Attack button pressed!");
            if (animator != null && HasParameter(animator, "Chop"))
            {
                animator.SetTrigger(chopHash);
            }
        }

        // Test Nút Câu Cá (Phím F)
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("[PlayerController] Nút Câu Cá (F) được bấm!");
            if (animator != null && HasParameter(animator, "Feed"))
            {
                animator.SetTrigger(feedHash); // Feed đang được nối với Fishing
            }
        }

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

            // Rotate character towards movement direction smoothly
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Move the character controller
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

        // 4. Handle Jumping
        if (jumpPressed && isGrounded)
        {
            // Physics formula for jump velocity: v = sqrt(h * -2 * g)
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            
            if (animator != null && HasParameter(animator, "Jump"))
            {
                animator.SetTrigger(jumpHash);
            }
            
            Debug.Log("[PlayerController] Jump triggered!");
        }

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // 5. Update Animator Parameters (for Character animations)
        if (animator != null)
        {
            float currentSpeed = moveDirection.magnitude * moveSpeed;
            if (HasParameter(animator, "Speed"))
            {
                animator.SetFloat(speedHash, currentSpeed);
            }
            if (HasParameter(animator, "IsMoving"))
            {
                animator.SetBool(isMovingHash, moveDirection.magnitude > 0.1f);
            }
        }
    }

    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}
