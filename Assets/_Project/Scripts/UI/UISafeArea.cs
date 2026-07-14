using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Đệm (padding) root của 1 UIDocument theo <see cref="Screen.safeArea"/> để UI không bị
/// TAI THỎ / bo góc / thanh điều hướng che trên điện thoại. Tự cập nhật khi xoay máy hoặc
/// đổi safe area. Gắn vào GameObject có UIDocument (vd GameHUD); muốn áp cho popup khác thì
/// gắn thêm vào UIDocument đó.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UISafeArea : MonoBehaviour
{
    [Tooltip("Bật để áp safe area NGAY trong Editor (giả lập tai thỏ). Build mobile luôn áp dù tắt cờ này.")]
    [SerializeField] private bool applyInEditor = false;
    [SerializeField, Min(0f)] private float minimumMobileInset = 18f;
    [SerializeField] private bool previewMinimumInsetInEditor = false;

    private UIDocument doc;
    private Rect lastSafe = new Rect(-1f, -1f, -1f, -1f);
    private Vector2 lastScreen = Vector2.zero;

    /// <summary>Cho installer bật xem trước safe area trong Editor/Device Simulator (build luôn áp).</summary>
    public bool ApplyInEditor
    {
        get => applyInEditor;
        set { applyInEditor = value; lastSafe = new Rect(-1f, -1f, -1f, -1f); } // ép tính lại
    }

    void OnEnable()
    {
        doc = GetComponent<UIDocument>();
        lastSafe = new Rect(-1f, -1f, -1f, -1f); // ép áp lại lần kế
    }

    void Update()
    {
        if (doc == null) return;
        VisualElement root = doc.rootVisualElement;
        if (root == null || root.resolvedStyle.width <= 0f) return; // chưa layout xong

        Rect safe = Screen.safeArea;
        Vector2 screen = new Vector2(Screen.width, Screen.height);
        if (safe == lastSafe && screen == lastScreen) return; // không đổi -> bỏ qua
        lastSafe = safe;
        lastScreen = screen;

#if UNITY_EDITOR
        if (!applyInEditor) { ClearPadding(root); return; }
#endif

        // Quy đổi pixel màn hình -> đơn vị panel (PanelSettings scale theo reference resolution).
        float sx = root.resolvedStyle.width / Mathf.Max(1f, screen.x);
        float sy = root.resolvedStyle.height / Mathf.Max(1f, screen.y);

        // Screen.safeArea gốc TRÁI-DƯỚI; quy ra đệm 4 cạnh.
        float left = safe.xMin * sx;
        float right = (screen.x - safe.xMax) * sx;
        float top = (screen.y - safe.yMax) * sy;
        float bottom = safe.yMin * sy;

        if (ShouldUseMinimumInset())
        {
            left = Mathf.Max(left, minimumMobileInset);
            right = Mathf.Max(right, minimumMobileInset);
            top = Mathf.Max(top, minimumMobileInset);
            bottom = Mathf.Max(bottom, minimumMobileInset);
        }

        root.style.paddingLeft = left;
        root.style.paddingRight = right;
        root.style.paddingTop = top;
        root.style.paddingBottom = bottom;
    }

    private bool ShouldUseMinimumInset()
    {
        if (minimumMobileInset <= 0f) return false;
#if UNITY_EDITOR
        return previewMinimumInsetInEditor;
#else
        return Application.isMobilePlatform;
#endif
    }

    private static void ClearPadding(VisualElement root)
    {
        root.style.paddingLeft = 0f;
        root.style.paddingRight = 0f;
        root.style.paddingTop = 0f;
        root.style.paddingBottom = 0f;
    }
}
