using UnityEngine;
using UnityEngine.InputSystem;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Script chữa cháy: Gắn vào con thú (đảm bảo con thú có 1 Collider đánh dấu IsTrigger).
    /// Khi Player lại gần sẽ hiện chữ, bấm phím P để phát animation "Pet".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PetInteraction : MonoBehaviour
    {
        private bool canPet = false;
        private Animator playerAnimator;

        // Mã Hash của parameter "Pet" trong Animator
        private int petHash;

        void Start()
        {
            // Tối ưu hóa hiệu suất bằng cách hash chuỗi string
            petHash = Animator.StringToHash("Pet");
            
            // Đảm bảo collider được set là trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        void Update()
        {
            // Bấm phím P để vuốt ve (Sử dụng Input System mới)
            if (canPet && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                if (playerAnimator != null)
                {
                    Debug.Log("[PetInteraction] Đang vuốt ve thú cưng!");
                    playerAnimator.SetTrigger(petHash);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                canPet = true;
                playerAnimator = other.GetComponent<Animator>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                canPet = false;
                playerAnimator = null;
            }
        }

        // Dùng OnGUI để vẽ chữ lên màn hình không cần Canvas (chữa cháy cực nhanh)
        void OnGUI()
        {
            if (canPet)
            {
                // Tùy chỉnh font chữ to và màu sắc dễ nhìn
                GUIStyle style = new GUIStyle();
                style.fontSize = 24;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.yellow;
                style.alignment = TextAnchor.MiddleCenter;

                // Vẽ dòng chữ ngay giữa màn hình, lệch xuống dưới một chút
                float width = 400f;
                float height = 50f;
                float x = (Screen.width - width) / 2f;
                float y = (Screen.height / 2f) + 150f;

                GUI.Label(new Rect(x, y, width, height), "Bấm [P] để vuốt ve thú cưng", style);
            }
        }
    }
}
