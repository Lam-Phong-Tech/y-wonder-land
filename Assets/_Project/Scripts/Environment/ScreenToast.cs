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
        private Texture2D _iconTexture;
        private Sprite _iconSprite;
        private string _iconFallbackText;
        private float _iconStartAt;
        private float _iconDuration;

        public static void Show(string message, float seconds = 2.5f)
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenToast");
                _instance = go.AddComponent<ScreenToast>();
            }
            _instance._message = message;
            _instance._hideAt = Time.unscaledTime + seconds;
            _instance._iconTexture = null;
            _instance._iconSprite = null;
            _instance._iconFallbackText = null;
        }

        public static void ShowInfo(string message, float seconds = 2f)
        {
            Show(message, seconds);
            if (_instance != null) _instance._color = new Color(0.18f, 0.31f, 0.50f, 0.95f); // xanh Deep Blue
        }

        public static void ShowInfoWithIcon(
            string message,
            Texture2D iconTexture,
            Sprite iconSprite = null,
            string fallbackText = "!",
            float seconds = 2f)
        {
            ShowInfo(message, seconds);
            if (_instance == null) return;

            _instance._iconTexture = iconTexture;
            _instance._iconSprite = iconSprite;
            _instance._iconFallbackText = fallbackText;
            _instance._iconStartAt = Time.unscaledTime;
            _instance._iconDuration = seconds;
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

            DrawFloatingIcon(x + (w * 0.5f), y);
        }

        private void DrawFloatingIcon(float centerX, float toastY)
        {
            if (_iconTexture == null && _iconSprite == null && string.IsNullOrEmpty(_iconFallbackText)) return;

            float progress = Mathf.Clamp01((Time.unscaledTime - _iconStartAt) / Mathf.Max(0.1f, _iconDuration));
            float fadeT = Mathf.Clamp01((progress - 0.62f) / 0.38f);
            float alpha = 1f - (fadeT * fadeT * (3f - 2f * fadeT));
            float lift = Mathf.SmoothStep(0f, 36f, progress);
            const float size = 72f;
            Rect bgRect = new Rect(centerX - size * 0.5f, toastY - 78f - lift, size, size);
            Rect iconRect = new Rect(bgRect.x + 8f, bgRect.y + 8f, bgRect.width - 16f, bgRect.height - 16f);

            Color prev = GUI.color;
            GUI.color = new Color(0.18f, 0.31f, 0.50f, 0.82f * alpha);
            GUI.Box(bgRect, GUIContent.none);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            if (_iconTexture != null)
            {
                GUI.DrawTexture(iconRect, _iconTexture, ScaleMode.ScaleToFit, true);
            }
            else if (_iconSprite != null)
            {
                GUI.DrawTexture(iconRect, _iconSprite.texture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                var iconStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 34,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                iconStyle.normal.textColor = Color.white;
                GUI.Label(bgRect, _iconFallbackText, iconStyle);
            }

            GUI.color = prev;
        }
    }
}
