using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Cho phép tương tác NPC dịch vụ bằng cách BƯỚC VÀO VÙNG (không cần bấm).
    /// Tạo 1 vùng trigger quanh NPC; người chơi vào -> tự gọi MerchantNPC.Interact().
    /// Script độc lập, tự gắn vào mọi MerchantNPC lúc chạy (không sửa file QC).
    /// </summary>
    [RequireComponent(typeof(MerchantNPC))]
    public class NpcProximityInteract : MonoBehaviour
    {
        [Tooltip("Bán kính vùng tới gần để tự mở tương tác (mét).")]
        public float interactRadius = 2.5f;

        private MerchantNPC _merchant;
        private bool _opened;

        private void Start()
        {
            _merchant = GetComponent<MerchantNPC>();

            // Vùng cảm ứng riêng (trigger) — KHÔNG ảnh hưởng collider click sẵn có của NPC.
            var zone = gameObject.AddComponent<SphereCollider>();
            zone.isTrigger = true;
            zone.radius = interactRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other) || _opened) return;
            if (UIPopupTracker.AnyOpen) return; // đang mở popup khác -> không tự mở chồng
            _opened = true;
            if (_merchant != null) _merchant.Interact();
        }

        private void OnTriggerExit(Collider other)
        {
            // Rời vùng -> cho phép lần sau vào lại tự mở lại.
            if (IsPlayer(other)) _opened = false;
        }

        private bool IsPlayer(Collider c)
            => c.CompareTag("Player") || c.GetComponentInParent<PlayerController>() != null;
    }

    /// <summary>Tự gắn NpcProximityInteract vào mọi MerchantNPC lúc chạy (khỏi kéo thả).</summary>
    public class NpcProximityScanner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("[NpcProximityScanner]");
            DontDestroyOnLoad(go);
            go.AddComponent<NpcProximityScanner>();
        }

        private void Start() => InvokeRepeating(nameof(Scan), 1f, 2f);

        private void Scan()
        {
            var npcs = FindObjectsByType<MerchantNPC>(FindObjectsSortMode.None);
            foreach (var n in npcs)
                if (n.GetComponent<NpcProximityInteract>() == null)
                    n.gameObject.AddComponent<NpcProximityInteract>();
        }
    }
}
