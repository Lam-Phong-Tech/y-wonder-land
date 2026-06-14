using UnityEngine;
using UnityEngine.Events;

namespace YWonderLand.Tutorial
{
    /// <summary>
    /// Đại diện cho 1 Trạm dừng chân trong chuỗi Hướng dẫn.
    /// Kịch bản: NPC chạy đến Node này -> Chờ người chơi tới -> Giao việc -> Chờ làm xong -> Chuyển Node tiếp theo.
    /// </summary>
    public class TutorialNode : MonoBehaviour
    {
        [Header("NPC Dialogues")]
        [TextArea]
        [Tooltip("Các câu thoại ngẫu nhiên khi Lâm đang chạy đến Trạm này")]
        public string[] walkDialogues = new string[] { "Theo tôi đi nào cậu ơi!", "Tôi đi trước, cậu bước theo sau nhé!" };
        
        [TextArea]
        [Tooltip("Các câu thoại khi Lâm đợi bé chạy tới hoặc AFK")]
        public string[] waitPlayerDialogues = new string[] { 
            "Tôi đợi cậu ở đây nãy giờ nè, nhanh chân lên nào!", 
            "Alo alo, Trái đất gọi phi hành gia, mạng lag hả cậu? 🐢",
            "Cậu vừa đi vừa ngắm cảnh à? Nhanh lên tôi chờ tới rễ mọc trên đầu rồi đây này!"
        };

        [TextArea]
        [Tooltip("Các câu thoại giao việc chi tiết")]
        public string[] actionDialogues = new string[] { "Cậu làm theo tôi nhé!" };

        [TextArea]
        [Tooltip("Các câu thoại chọc ghẹo khi bé đứng im không biết làm gì (Làm việc quá lâu)")]
        public string[] idleWarningDialogues = new string[] { 
            "Cậu không biết làm hả? Nhìn lên màn hình có hướng dẫn chi tiết đó!",
            "Cậu làm gì mà lâu thế? Cần tôi xắn tay áo vào làm dùm luôn không? 😂",
            "Đứng nhìn ô đất thì nó không tự nảy mầm đâu, bắt tay vào việc đi sếp!"
        };

        [Header("Events")]
        [Tooltip("Được gọi khi người chơi đã chạy tới gần NPC tại Trạm này. Dùng để bật UI, Highlight vật thể...")]
        public UnityEvent OnPlayerArrivedAtNode;

        [Tooltip("Được gọi khi người chơi hoàn thành nhiệm vụ tại Trạm này (Ví dụ: Trồng xong cây). Gọi bằng Code.")]
        public UnityEvent OnTaskCompleted;

        [HideInInspector]
        public bool isTaskCompleted = false;

        private void OnDrawGizmos()
        {
            // Vẽ cục Sphere màu xanh lá để dễ nhìn trên Editor
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.5f);
            
            // Vẽ đường Line hướng mặt
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
        }

        /// <summary>
        /// Gọi hàm này từ bên ngoài (ví dụ: Event trồng cây xong) để báo hiệu đã hoàn thành Trạm.
        /// </summary>
        public void CompleteNodeTask()
        {
            if (isTaskCompleted) return;
            
            isTaskCompleted = true;
            OnTaskCompleted?.Invoke();
            Debug.Log($"[TutorialNode] Đã hoàn thành Task tại trạm: {gameObject.name}");
        }
    }
}
