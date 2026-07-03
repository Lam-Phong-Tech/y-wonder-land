using UnityEngine;

namespace YWonderLand.Realtime
{
    /// <summary>
    /// Lightweight visual controller for another player in shared islands.
    /// It only interpolates transform and plays simple locomotion/emote animations.
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
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private string currentAnimation = "";
        private float emoteTimer = 0f;

        public string PlayerId => playerId;

        private void Awake()
        {
            animator = GetComponent<Animator>();
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

        public void ApplySnapshot(Vector3 position, float yaw, string animationName)
        {
            targetPosition = position;
            targetRotation = Quaternion.Euler(0f, yaw, 0f);

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
    }
}
