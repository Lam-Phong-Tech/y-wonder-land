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

    [Tooltip("Chỉ bật cho UIDocument mà phần tử gốc UXML neo ĐỦ 4 cạnh (top/left/right/bottom), " +
             "ví dụ .hud-root. Khi bật, safe inset được đặt thẳng vào left/top/right/bottom của " +
             "phần tử đó thay vì đệm padding — vì phần tử absolute bỏ qua padding của cha, khiến " +
             "cụm nút trái bị Dynamic Island che trên iPhone 14 trở lên. TUYỆT ĐỐI không bật cho " +
             "popup căn giữa (.chat-root dùng left: 50%) hay overlay không neo cạnh nào.")]
    [SerializeField] private bool offsetAbsoluteRoot = false;

    /// <summary>Cho installer bật riêng cho HUD (xem tooltip của <c>offsetAbsoluteRoot</c>).</summary>
    public bool OffsetAbsoluteRoot
    {
        get => offsetAbsoluteRoot;
        set { offsetAbsoluteRoot = value; lastSafe = new Rect(-1f, -1f, -1f, -1f); } // ép tính lại
    }

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
        // Tắt xem trước: trả UI về nguyên trạng, gồm cả offset đã đặt vào phần tử gốc absolute
        // (nếu chỉ xoá padding thì offset cũ còn sót lại, UI lệch mãi trong Editor).
        if (!applyInEditor) { ClearPadding(root); TryApplyToAbsoluteRootChild(root, 0f, 0f, 0f, 0f); return; }
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

        // Chọn MỘT trong hai cách, không dùng cả hai — nếu vừa đệm padding vừa đặt offset thì
        // tuỳ phiên bản Yoga có thể cộng dồn, đẩy UI vào sâu gấp đôi.
        if (TryApplyToAbsoluteRootChild(root, left, top, right, bottom))
        {
            ClearPadding(root); // offset đã lo hết, padding chỉ tổ cộng dồn
            return;
        }

        root.style.paddingLeft = left;
        root.style.paddingRight = right;
        root.style.paddingTop = top;
        root.style.paddingBottom = bottom;
    }

    /// <summary>
    /// Padding ở trên CHỈ đẩy được các con xếp theo flex. Phần tử gốc của UXML (vd .hud-root)
    /// dùng `position: absolute; top/left/right/bottom: 0`, mà phần tử absolute neo theo
    /// padding-box của cha nên nó BỎ QUA padding vừa đặt — kéo theo mọi con absolute bên trong
    /// (.hud-top-left, .hud-bottom-left...) vẫn nằm sát mép và bị Dynamic Island / bo góc che.
    ///
    /// Khách báo 14/08: từ iPhone 14 trở lên, cụm nút trái (bảng xếp hạng, lịch, thư, bạn bè)
    /// bị che khi xoay ngang. Nên với phần tử gốc absolute, ta đặt thẳng left/top/right/bottom
    /// bằng đúng safe inset — cả cây con dịch theo, không cần sửa từng vị trí trong USS.
    /// </summary>
    /// <returns>true nếu đã xử lý bằng offset (khi đó KHÔNG đặt padding nữa).</returns>
    private bool TryApplyToAbsoluteRootChild(VisualElement root, float left, float top, float right, float bottom)
    {
        // CHỈ làm khi được bật rõ ràng. Đè left/top/right/bottom lên một phần tử absolute
        // KHÔNG neo đủ 4 cạnh sẽ phá bố cục của nó. Trong dự án này có thật:
        //   .chat-root       { position: absolute; bottom: 16px; left: 50%; }  ← căn giữa
        //   .build-overlay / .charselect-screen / .fish-root                   ← không neo gì
        //   .levelup-overlay / .login-screen / .reward-overlay / .splash-screen ← chỉ top+left
        // Đè lên mấy cái đó thì khung chat văng sang trái, overlay nhảy lung tung.
        // Chỉ .hud-root mới neo đủ top/left/right/bottom = 0 nên an toàn.
        if (!offsetAbsoluteRoot) return false;

        if (root.childCount == 0) return false;

        VisualElement content = root[0];
        if (content == null) return false;
        if (content.resolvedStyle.position != Position.Absolute) return false; // bố cục flex: padding đã đủ

        content.style.left = left;
        content.style.top = top;
        content.style.right = right;
        content.style.bottom = bottom;
        return true;
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
