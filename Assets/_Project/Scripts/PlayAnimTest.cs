using UnityEngine;
using UnityEngine.InputSystem;

namespace YWonderLand
{
    public class PlayAnimTest : MonoBehaviour
    {
        private Animator anim;

        void Start()
        {
            anim = GetComponent<Animator>();
        }

        void Update()
        {
            // Bấm phím L để ép múa
            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            {
                if (anim != null)
                {
                    // Lệnh Play sẽ bỏ qua toàn bộ dây nhợ và Trigger, ép Animator phát thẳng State "Planting"
                    anim.Play("Planting");
                    Debug.Log("[PlayAnimTest] Đã ép play State Planting!");
                }
            }
        }
    }
}
