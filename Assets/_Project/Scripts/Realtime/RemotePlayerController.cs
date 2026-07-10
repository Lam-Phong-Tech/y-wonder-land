using UnityEngine;

namespace YWonderLand.Realtime
{
    /// <summary>
    /// Lightweight visual controller for another player in shared islands.
    /// It interpolates transform and mirrors the local player's visual animation/tool state.
    /// </summary>
    public class RemotePlayerController : MonoBehaviour
    {
        [Header("Remote Identity")]
        [SerializeField] private string playerId;
        [SerializeField] private string displayName = "Player";

        [Header("Interpolation")]
        [SerializeField] private float positionLerp = 10f;
        [SerializeField] private float rotationLerp = 12f;

        private Animator animator;
        private YWonderLand.Player.EquipmentManager equipment;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private string currentAnimation = "";
        private YWonderLand.Player.ToolType currentTool = YWonderLand.Player.ToolType.None;
        private float emoteTimer = 0f;

        public string PlayerId => playerId;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            equipment = GetComponentInChildren<YWonderLand.Player.EquipmentManager>(true);
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        public void Initialize(string id, string name)
        {
            playerId = id;
            displayName = string.IsNullOrWhiteSpace(name) ? "Player" : name;

            var tag = GetComponent<FloatingNameTag>();
            if (tag == null) tag = gameObject.AddComponent<FloatingNameTag>();
            tag.displayName = displayName;
            tag.heightOffset = 2.2f;
            tag.tmpFontSize = 2.3f;
        }

        public void ApplySnapshot(Vector3 position, float yaw, string animationName, float animationSpeed, string toolName)
        {
            targetPosition = position;
            targetRotation = Quaternion.Euler(0f, yaw, 0f);
            if (animator != null) animator.speed = Mathf.Clamp(animationSpeed, 0.1f, 4f);
            ApplyTool(toolName);

            if (emoteTimer <= 0f)
            {
                CrossFade(animationName);
            }
        }

        public void PlayEmote(string animationName, float duration)
        {
            if (string.IsNullOrWhiteSpace(animationName)) return;
            emoteTimer = Mathf.Max(0.5f, duration);
            CrossFade(animationName);
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerp * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerp * Time.deltaTime);

            if (emoteTimer > 0f)
            {
                emoteTimer -= Time.deltaTime;
            }
        }

        private void CrossFade(string animationName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(animationName) || currentAnimation == animationName) return;

            const int baseLayer = 0;
            int stateHash = Animator.StringToHash(animationName);
            if (animator.layerCount <= baseLayer || !animator.HasState(baseLayer, stateHash))
                return;

            animator.CrossFadeInFixedTime(stateHash, 0.15f, baseLayer);
            currentAnimation = animationName;
        }

        private void ApplyTool(string toolName)
        {
            if (!System.Enum.TryParse(toolName, true, out YWonderLand.Player.ToolType requestedTool))
                requestedTool = YWonderLand.Player.ToolType.None;
            if (requestedTool == currentTool) return;

            currentTool = requestedTool;
            if (equipment == null) return;

            if (currentTool == YWonderLand.Player.ToolType.None)
                equipment.HideAllTools();
            else
                equipment.ShowTool(currentTool);
        }
    }
}
