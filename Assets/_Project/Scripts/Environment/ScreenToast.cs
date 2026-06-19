using UnityEngine;

namespace YWonderLand.Environment
{
    /// <summary>
    /// Thông báo ngắn giữa-trên màn hình (toast). Tự tạo khi gọi ScreenToast.Show(...).
    /// Dùng cho báo lỗi tức thì (vd "Chuồng không đủ chỗ"). Tạm dùng OnGUI cho gọn,
    /// có thể nâng lên UI Toolkit sau.
    /// </summary>
    public class ScreenToast : MonoBehaviour
    {
        private static ScreenToast _instance;
        private string _message;
        private float _hideAt;
        private Color _color = new Color(0.90f, 0.35f, 0.30f, 0.95f); // đỏ Palia

        public static void Show(string message, float seconds = 2.5f)
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenToast");
                _instance = go.AddComponent<ScreenToast>();
            }
            _instance._message = message;
            _instance._hideAt = Time.unscaledTime + seconds;
        }

        public static void ShowInfo(string message, float seconds = 2f)
        {
            Show(message, seconds);
            if (_instance != null) _instance._color = new Color(0.18f, 0.31f, 0.50f, 0.95f); // xanh Deep Blue
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(_message) || Time.unscaledTime > _hideAt) return;

            const float w = 540f, h = 56f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.26f;

            var prev = GUI.color;
            GUI.color = _color;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none);
            GUI.color = prev;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, w, h), _message, style);
        }
    }
}
