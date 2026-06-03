using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4.0f;
    public float rotationSpeed = 15.0f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private Vector3 playerVelocity;
    private bool isGrounded;

    // Animator parameter hash IDs for better performance
    private int speedHash;
    private int isMovingHash;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        DiscoverMainCamera();

        // Cache Animator parameters
        speedHash = Animator.StringToHash("Speed");
        isMovingHash = Animator.StringToHash("IsMoving");
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
        // Ensure character controller is enabled
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        // 1. Gravity and Ground Check
        isGrounded = controller.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Soft clamp to keep grounded
        }

        // 2. Read Keyboard/Gamepad Inputs (New Input System API)
        float horizontal = 0f;
        float vertical = 0f;

        // Poll Keyboard
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical = 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical = -1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal = -1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal = 1f;
        }

        // Poll Gamepad (Optional but nice)
        var gamepad = UnityEngine.InputSystem.Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude > 0.1f)
            {
                horizontal = stick.x;
                vertical = stick.y;
            }
        }

        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 moveDirection = Vector3.zero;

        // Ensure we always have the active main camera transform if the camera changed states
        if (mainCameraTransform == null || !mainCameraTransform.gameObject.activeInHierarchy)
        {
            DiscoverMainCamera();
        }

        // 3. Move Relative to Camera Direction
        if (inputDir.magnitude >= 0.1f)
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
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move the character controller
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // 4. Update Animator Parameters (for Character animations)
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
