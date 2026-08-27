using UnityEngine;
using UnityEngine.UIElements;
using YWonderLand.Backend;

/// <summary>
/// Controller for the Login/Register UI screen.
/// Handles tab switching, password toggle, backend auth, and navigation to Character Selection.
/// </summary>
public class LoginScreenController : MonoBehaviour
{
    [Header("Popup References")]
    [SerializeField] private ForgotPasswordPopupController forgotPasswordPopup;

    private UIDocument uiDocument;
    private VisualElement rootElement;
    private VisualElement loginShadow;

    // Tab buttons
    private Button tabLogin;
    private Button tabRegister;

    // Forms
    private VisualElement loginForm;
    private VisualElement registerForm;

    // Login form elements
    private TextField usernameField;
    private TextField passwordField;
    private Button btnTogglePassword;
    private Toggle rememberToggle;
    private Label forgotPassword;
    private Button btnLogin;
    private Button btnLoginWeb;
    private Button btnQuitApp;
    private Label loginStatus;
    private Label demoLoginLink;
    private bool demoFormRevealed;
    private int browserAuthUiAttempt;
    private bool isBrowserAuthWaiting;

    // Register form elements
    private TextField regUsernameField;
    private TextField regEmailField;
    private TextField regPasswordField;
    private TextField regConfirmField;
    private Button btnToggleRegPassword;
    private Button btnRegister;
    private Button btnOpenWebRegistration;
    private Label registerStatus;

    // State
    private bool isPasswordVisible = false;
    private bool isRegPasswordVisible = false;
    private bool isAuthRequestInProgress = false;
    private bool isQuitRequestSent = false;
    private bool shouldSkipCharacterSelectAfterLogin = false;
    private VisualElement callbacksRoot;
    private TextField focusedKeyboardField;
    private float keyboardUpShift;

    // USS class names
    private const string TAB_ACTIVE_CLASS = "login-tab-active";
    private const string HIDDEN_CLASS = "hidden";
    private const string INPUT_FOCUS_CLASS = "login-input-group-focus";
    private const string STATUS_SUCCESS = "status-success";
    private const string STATUS_ERROR = "status-error";
    private const int LOGIN_IDENTITY_MAX_LENGTH = 254;
    private const int LOGIN_PASSWORD_MAX_LENGTH = 128;
    private const int REGISTER_MIN_LENGTH = 9;
    private const int REGISTER_MAX_LENGTH = 20;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[LoginScreen] UIDocument component not found!");
            return;
        }

        rootElement = uiDocument.rootVisualElement;
        QueryElements(rootElement);
        RegisterCallbacks();
        SetupPlaceholders();
        ApplyAuthModeLayout();

        // Disable register button by default until validated
        if (btnRegister != null)
        {
            btnRegister.SetEnabled(false);
        }

        // Default: show Login tab
        ShowLoginTab();
    }

    void Update()
    {
        UpdateKeyboardAvoidance();
    }

    void OnDisable()
    {
        ResetKeyboardAvoidance();
    }

    private void QueryElements(VisualElement root)
    {
        rootElement = root;
        loginShadow = root.Q<VisualElement>(className: "login-shadow");

        // Tabs
        tabLogin = root.Q<Button>("TabLogin");
        tabRegister = root.Q<Button>("TabRegister");

        // Forms
        loginForm = root.Q<VisualElement>("LoginForm");
        registerForm = root.Q<VisualElement>("RegisterForm");

        // Login elements
        usernameField = root.Q<TextField>("UsernameField");
        passwordField = root.Q<TextField>("PasswordField");
        btnTogglePassword = root.Q<Button>("BtnTogglePassword");
        rememberToggle = root.Q<Toggle>("RememberToggle");
        forgotPassword = root.Q<Label>("ForgotPassword");
        btnLogin = root.Q<Button>("BtnLogin");
        btnLoginWeb = root.Q<Button>("BtnLoginWeb");
        btnQuitApp = root.Q<Button>("BtnQuitApp");
        loginStatus = root.Q<Label>("LoginStatus");
        demoLoginLink = root.Q<Label>("DemoLoginLink");

        // Register elements
        regUsernameField = root.Q<TextField>("RegUsernameField");
        regEmailField = root.Q<TextField>("RegEmailField");
        regPasswordField = root.Q<TextField>("RegPasswordField");
        regConfirmField = root.Q<TextField>("RegConfirmField");
        btnToggleRegPassword = root.Q<Button>("BtnToggleRegPassword");
        btnRegister = root.Q<Button>("BtnRegister");
        btnOpenWebRegistration = root.Q<Button>("BtnOpenWebRegistration");
        registerStatus = root.Q<Label>("RegisterStatus");
    }

    private void RegisterCallbacks()
    {
        if (callbacksRoot == uiDocument.rootVisualElement) return;
        callbacksRoot = uiDocument.rootVisualElement;

        // Tab switching
        tabLogin?.RegisterCallback<ClickEvent>(evt => ShowLoginTab());
        tabRegister?.RegisterCallback<ClickEvent>(evt => ShowRegisterTab());

        // Login actions
        btnLogin?.RegisterCallback<ClickEvent>(evt => OnLoginClicked(false));
        btnLoginWeb?.RegisterCallback<ClickEvent>(evt => OnWebsiteAuthClicked("login"));
        if (btnQuitApp != null)
        {
            btnQuitApp.pickingMode = PickingMode.Position;
            btnQuitApp.BringToFront();
            btnQuitApp.clicked += OnQuitAppClicked;
            btnQuitApp.RegisterCallback<PointerDownEvent>(OnQuitAppPointerDown, TrickleDown.TrickleDown);
        }
        btnTogglePassword?.RegisterCallback<ClickEvent>(evt => TogglePasswordVisibility());

        // Register actions
        btnRegister?.RegisterCallback<ClickEvent>(evt => OnRegisterClicked());
        btnOpenWebRegistration?.RegisterCallback<ClickEvent>(evt => OnWebsiteAuthClicked("register"));
        btnToggleRegPassword?.RegisterCallback<ClickEvent>(evt => ToggleRegPasswordVisibility());

        // Register real-time validation callbacks
        regUsernameField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());
        regEmailField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());
        regPasswordField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());
        regConfirmField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());

        // Forgot password
        forgotPassword?.RegisterCallback<ClickEvent>(evt => OnForgotPasswordClicked());

        // Lối vào tài khoản trình diễn
        demoLoginLink?.RegisterCallback<ClickEvent>(evt => ShowDemoLoginForm());

        // Input focus styling
        RegisterInputFocusEvents(usernameField, "UsernameGroup");
        RegisterInputFocusEvents(passwordField, "PasswordGroup");
        RegisterInputFocusEvents(regUsernameField, "RegUsernameGroup");
        RegisterInputFocusEvents(regEmailField, "RegEmailGroup");
        RegisterInputFocusEvents(regPasswordField, "RegPasswordGroup");
        RegisterInputFocusEvents(regConfirmField, "RegConfirmGroup");
    }

    private void RegisterInputFocusEvents(TextField field, string groupName)
    {
        if (field == null) return;

        var root = uiDocument.rootVisualElement;
        var group = root.Q<VisualElement>(groupName);
        if (group == null) return;

        field.RegisterCallback<FocusInEvent>(evt =>
        {
            group.AddToClassList(INPUT_FOCUS_CLASS);
            focusedKeyboardField = field;
            UpdateKeyboardAvoidance();
        });

        field.RegisterCallback<FocusOutEvent>(evt =>
        {
            group.RemoveFromClassList(INPUT_FOCUS_CLASS);
            if (focusedKeyboardField == field)
                ResetKeyboardAvoidance();
        });
    }

    private void SetupPlaceholders()
    {
        // Set placeholder text via textEdition (Unity 2022.2+)
        SetPlaceholder(usernameField, "Email / SĐT / ID đăng nhập");
        SetPlaceholder(passwordField, "Mật khẩu");
        SetPlaceholder(regUsernameField, "Tên đăng nhập");
        SetPlaceholder(regEmailField, "Email");
        SetPlaceholder(regPasswordField, "Mật khẩu");
        SetPlaceholder(regConfirmField, "Xác nhận mật khẩu");
    }

    private void SetPlaceholder(TextField field, string placeholder)
    {
        if (field == null) return;
        // Unity UI Toolkit uses the 'viewDataKey' or we set via textEdition
        // For broad compatibility, use the label or a manual approach
        field.textEdition.placeholder = placeholder;
    }

    /// <summary>
    /// Dựng lại màn Login theo BackendConfig.localAuthEnabled.
    /// TẮT (mặc định) = mọi tài khoản đăng nhập qua website: giấu tab, giấu ô nhập
    /// tài khoản/mật khẩu và 2 nút "trong game", chỉ chừa ĐĂNG NHẬP + ĐĂNG KÝ mở web.
    /// Lý do giấu cả form: khi browserAuthEnabled bật, luồng web KHÔNG đọc chữ người
    /// chơi gõ trong game — để ô đó lại chỉ khiến họ gõ thừa rồi bị trình duyệt hỏi lại.
    /// BẬT lại cờ là mọi thứ trở về như cũ, không cần sửa code.
    /// </summary>
    private void ApplyAuthModeLayout()
    {
        var config = BackendConfig.Active;
        if (config != null && config.localAuthEnabled)
        {
            // Apple từ chối 16/08/2026 theo Guideline 4: đẩy người chơi ra trình duyệt
            // mặc định để đăng nhập/đăng ký là trải nghiệm kém.
            //
            // Khi đã có form NGAY TRONG GAME và KHÔNG bật browser SSO thì giấu hẳn 2 nút
            // mở website. Nút "ĐĂNG NHẬP WEBSITE" tuy đã tự rơi về form trong game (xem
            // OnWebsiteAuthClicked), nhưng nút "ĐĂNG KÝ TRONG WEBSITE" vẫn gọi thẳng
            // Application.OpenURL — để lại là còn nguyên đường vi phạm.
            if (!config.browserAuthEnabled)
            {
                SetHidden(btnLoginWeb, true);
                SetHidden(btnOpenWebRegistration, true);
                if (rootElement != null)
                    SetHidden(rootElement.Q<Label>("WebOnlyHint"), true);
            }
            return;
        }
        if (rootElement == null) return;

        demoFormRevealed = false; // OnEnable có thể chạy lại -> phải mở lại được form demo

        SetHidden(rootElement.Q<VisualElement>("LoginTabBar"), true);
        SetHidden(rootElement.Q<VisualElement>("UsernameGroup"), true);
        SetHidden(rootElement.Q<VisualElement>("PasswordGroup"), true);
        SetHidden(rootElement.Q<VisualElement>("LoginOptionsRow"), true);
        SetHidden(rootElement.Q<VisualElement>("RegUsernameGroup"), true);
        SetHidden(rootElement.Q<VisualElement>("RegEmailGroup"), true);
        SetHidden(rootElement.Q<VisualElement>("RegPasswordGroup"), true);
        SetHidden(rootElement.Q<VisualElement>("RegConfirmGroup"), true);
        SetHidden(btnLogin, true);
        SetHidden(btnRegister, true);
        SetHidden(rootElement.Q<Label>("WebOnlyHint"), false);

        if (btnLoginWeb != null)
        {
            btnLoginWeb.text = "ĐĂNG NHẬP";
            btnLoginWeb.RemoveFromClassList("login-action-web");
            btnLoginWeb.AddToClassList("login-action-local"); // giờ nó là nút TRÁI
            btnLoginWeb.AddToClassList("login-web-solo");
            btnLoginWeb.AddToClassList("login-web-primary");
        }

        // Kéo nút đăng ký sang cùng hàng với nút đăng nhập; form Đăng Ký không còn dùng tới.
        var actionsRow = rootElement.Q<VisualElement>("LoginActionsRow");
        if (btnOpenWebRegistration != null && actionsRow != null)
        {
            btnOpenWebRegistration.text = "ĐĂNG KÝ";
            btnOpenWebRegistration.RemoveFromClassList("register-action-web");
            btnOpenWebRegistration.AddToClassList("login-action-btn");
            btnOpenWebRegistration.AddToClassList("login-action-web");
            btnOpenWebRegistration.AddToClassList("login-web-solo");
            actionsRow.Add(btnOpenWebRegistration);
        }

        // Lối vào demo mặc định ẨN: R1..R5 nằm ở DB game-server, chưa seed lên production
        // nên người chơi thường bấm vào chỉ gặp "sai tài khoản". Bật lại bằng
        // BackendConfig.demoLoginLinkEnabled khi cần trình diễn.
        SetHidden(demoLoginLink, config == null || !config.demoLoginLinkEnabled);
    }

    /// <summary>
    /// Mở form gõ tay cho các tài khoản trình diễn R1..R5. Chúng nằm ở DB game-server
    /// (không phải DB website) nên phải đi đường /auth/login, không đi qua trình duyệt.
    /// </summary>
    private void ShowDemoLoginForm()
    {
        if (demoFormRevealed) return;
        demoFormRevealed = true;

        // Đang chờ trình duyệt mà quay sang đăng nhập demo -> huỷ lượt chờ, kẻo nút
        // VÀO GAME DEMO trông bấm được nhưng bị isAuthRequestInProgress chặn im lặng.
        if (isBrowserAuthWaiting)
        {
            browserAuthUiAttempt++;
            AuthService.Instance?.CancelBrowserLogin();
            isBrowserAuthWaiting = false;
            SetAuthControlsEnabled(true);
            ClearStatus(loginStatus);
        }

        SetHidden(rootElement.Q<VisualElement>("UsernameGroup"), false);
        SetHidden(rootElement.Q<VisualElement>("PasswordGroup"), false);
        SetHidden(btnLogin, false);
        SetHidden(demoLoginLink, true);
        SetHidden(rootElement.Q<Label>("WebOnlyHint"), true);

        if (btnLogin != null)
        {
            btnLogin.text = "VÀO GAME DEMO";
            btnLogin.SetEnabled(true);
        }

        SetPlaceholder(usernameField, "Tên tài khoản demo");
        usernameField?.Focus();
    }

    private void SetHidden(VisualElement element, bool hidden)
    {
        if (element == null) return;
        if (hidden) element.AddToClassList(HIDDEN_CLASS);
        else element.RemoveFromClassList(HIDDEN_CLASS);
    }

    // ── Tab Switching ──

    private void ShowLoginTab()
    {
        ResetKeyboardAvoidance();
        tabLogin?.AddToClassList(TAB_ACTIVE_CLASS);
        tabRegister?.RemoveFromClassList(TAB_ACTIVE_CLASS);

        loginForm?.RemoveFromClassList(HIDDEN_CLASS);
        registerForm?.AddToClassList(HIDDEN_CLASS);

        ClearStatus(loginStatus);
        Debug.Log("[LoginScreen] Switched to Login tab");
    }

    private void ShowRegisterTab()
    {
        ResetKeyboardAvoidance();
        tabRegister?.AddToClassList(TAB_ACTIVE_CLASS);
        tabLogin?.RemoveFromClassList(TAB_ACTIVE_CLASS);

        registerForm?.RemoveFromClassList(HIDDEN_CLASS);
        loginForm?.AddToClassList(HIDDEN_CLASS);

        ClearStatus(registerStatus);
        Debug.Log("[LoginScreen] Switched to Register tab");
    }

    private void OpenRegistrationPage()
    {
        ResetKeyboardAvoidance();
        ShowLoginTab();

        var config = BackendConfig.Active;

        // CHỐT CHẶN (Apple từ chối lần 2 ngày 18/08/2026, Guideline 4): đăng ký được NGAY
        // TRONG GAME thì tuyệt đối không đẩy người chơi ra trình duyệt mặc định.
        // Giấu nút thôi chưa đủ — hàm này trước đây không có rào nào, nút hiện lại vì bất kỳ
        // lý do gì (đổi cấu hình, đường gọi khác, layout iPad khác máy thử) là vi phạm ngay.
        // Rào ở ĐÂY mới chặn được tận gốc; nút chỉ là lớp ngoài.
        if (config != null && config.localAuthEnabled && !config.browserAuthEnabled)
        {
            ShowRegisterTab();
            Debug.Log("[LoginScreen] Đăng ký trong game — không mở trình duyệt (Guideline 4).");
            return;
        }

        string configuredUrl = config != null ? config.registrationUrl?.Trim() : "";
        if (!System.Uri.TryCreate(configuredUrl, System.UriKind.Absolute, out var registrationUri) ||
            !string.Equals(registrationUri.Scheme, System.Uri.UriSchemeHttps, System.StringComparison.OrdinalIgnoreCase))
        {
            ShowStatus(loginStatus, "Trang đăng ký chưa được cấu hình. Vui lòng thử lại sau.", false);
            Debug.LogWarning("[LoginScreen] Registration URL is missing or is not HTTPS.");
            return;
        }

        ShowStatus(loginStatus,
            "Đã mở trang đăng ký. Hoàn tất trên web rồi quay lại game để đăng nhập.",
            true);
        Application.OpenURL(registrationUri.AbsoluteUri);
    }

    // ── Password Toggle ──

    private void TogglePasswordVisibility()
    {
        if (passwordField == null) return;

        isPasswordVisible = !isPasswordVisible;
        passwordField.isPasswordField = !isPasswordVisible;

        if (btnTogglePassword != null)
        {
            btnTogglePassword.text = isPasswordVisible ? "◎" : "◉";
        }

        Debug.Log($"[LoginScreen] Password visibility: {(isPasswordVisible ? "SHOWN" : "HIDDEN")}");
    }

    private void ToggleRegPasswordVisibility()
    {
        if (regPasswordField == null) return;

        isRegPasswordVisible = !isRegPasswordVisible;
        regPasswordField.isPasswordField = !isRegPasswordVisible;

        if (btnToggleRegPassword != null)
        {
            btnToggleRegPassword.text = isRegPasswordVisible ? "◎" : "◉";
        }
    }

    // ── Login ──

    private async void OnLoginClicked(bool useWebAccount)
    {
        if (isAuthRequestInProgress) return;

        string username = (usernameField?.value ?? "").Trim();
        string password = passwordField?.value ?? "";

        Debug.Log($"[LoginScreen] {(useWebAccount ? "Web" : "Local")} login clicked.");

        // Validation
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowStatus(loginStatus,
                demoFormRevealed ? "Vui lòng nhập tên tài khoản demo" : "Vui lòng nhập Email, SĐT hoặc ID đăng nhập",
                false);
            return;
        }
        if (username.Length > LOGIN_IDENTITY_MAX_LENGTH)
        {
            ShowStatus(loginStatus, "Email, SĐT hoặc ID đăng nhập quá dài", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowStatus(loginStatus, "Vui lòng nhập mật khẩu", false);
            return;
        }
        if (password.Length > LOGIN_PASSWORD_MAX_LENGTH)
        {
            ShowStatus(loginStatus, "Mật khẩu không được quá 128 ký tự", false);
            return;
        }

        var auth = AuthService.Instance;
        if (auth == null)
        {
            ShowStatus(loginStatus, "Hệ thống đăng nhập chưa sẵn sàng. Thử lại sau.", false);
            Debug.LogWarning("[LoginScreen] AuthService.Instance is null. Cannot login.");
            return;
        }

        SetAuthControlsEnabled(false);
        ShowStatus(loginStatus,
            useWebAccount ? "Đang đăng nhập tài khoản website..." : "Đang đăng nhập tài khoản trong game...",
            true);
        bool success = useWebAccount
            ? await auth.LoginWebAsync(username, password)
            // LoginAsync chu KHONG phai LoginLocalAsync: no thu /auth/login truoc, gap 404 thi
            // rơi sang /auth/web-login. Truoc day chi goi /auth/login nen tai khoan tao tren
            // ywonder.net khong dang nhap duoc trong game — nguoi duyet cua Apple ma duoc cap
            // tai khoan website la ket ngay man hinh dau, khong xem duoc gi de duyet.
            : await auth.LoginAsync(username, password);

        if (!success)
        {
            SetAuthControlsEnabled(true);
            ShowStatus(loginStatus, ResolveLoginFailureMessage(auth), false);
            return;
        }

        await CompleteAuthenticatedLoginAsync(auth.Username);
    }

    private async void OnWebsiteAuthClicked(string intent)
    {
        // Đang chờ trình duyệt thì VẪN cho bấm lại: người chơi hay quên mất là đã bấm,
        // hoặc lỡ đóng tab web. Bấm lại = mở tab mới. Lượt cũ bị AuthService tự huỷ
        // (LoginWithBrowserAsync tăng browserAuthAttempt ngay đầu hàm) nên không chồng chéo.
        // Chỉ chặn khi đang có request đăng nhập TRONG GAME chạy dở.
        if (isAuthRequestInProgress && !isBrowserAuthWaiting) return;

        var config = BackendConfig.Active;
        if (config == null || !config.browserAuthEnabled)
        {
            // Không có form trong game thì không có gì để gửi đi -> chỉ mở được trang web.
            bool hasLocalForm = config != null && config.localAuthEnabled;
            if (!hasLocalForm || string.Equals(intent, "register", System.StringComparison.OrdinalIgnoreCase))
                OpenRegistrationPage();
            else
                OnLoginClicked(true);
            return;
        }

        var auth = AuthService.Instance;
        if (auth == null)
        {
            ShowStatus(loginStatus, "Hệ thống đăng nhập chưa sẵn sàng. Thử lại sau.", false);
            return;
        }

        int myAttempt = ++browserAuthUiAttempt;
        isBrowserAuthWaiting = true;

        SetAuthControlsEnabled(false);
        // ...nhưng chừa lại 2 nút web để còn mở lại trang được.
        btnLoginWeb?.SetEnabled(true);
        btnOpenWebRegistration?.SetEnabled(true);

        ShowStatus(loginStatus, "Đang tạo phiên xác thực website...", true);
        bool success = await auth.LoginWithBrowserAsync(intent, message =>
        {
            if (myAttempt != browserAuthUiAttempt) return; // lượt cũ, đừng ghi đè trạng thái lượt mới
            if (loginStatus != null)
                ShowStatus(loginStatus, message + "\nLỡ đóng tab? Bấm lại nút bên dưới để mở lại trang.", true);
        });

        // Người chơi đã bấm lại -> lượt này hết hiệu lực, bỏ qua kết quả của nó.
        if (myAttempt != browserAuthUiAttempt) return;

        isBrowserAuthWaiting = false;
        if (!success)
        {
            SetAuthControlsEnabled(true);
            ShowStatus(loginStatus, ResolveBrowserAuthFailureMessage(auth), false);
            return;
        }

        await CompleteAuthenticatedLoginAsync(auth.Username);
    }

    private async Awaitable CompleteAuthenticatedLoginAsync(string username)
    {
        var auth = AuthService.Instance;
        shouldSkipCharacterSelectAfterLogin = false;
        bool bootstrapLoaded = false;
        var bootstrap = PlayerBootstrapService.Instance;
        if (bootstrap != null)
        {
            ShowStatus(loginStatus, "Đang nạp dữ liệu nhân vật...", true);
            bootstrapLoaded = await bootstrap.LoadBootstrapAsync();
        }

        if (!bootstrapLoaded && IsOnlineOnly())
        {
            string bootstrapError = ResolveBootstrapFailureMessage(bootstrap);
            auth.SignOut();
            SetAuthControlsEnabled(true);
            ShowStatus(loginStatus, bootstrapError, false);
            return;
        }

        var profile = PlayerProfileService.Instance;
        if (profile != null)
        {
            if (!bootstrapLoaded)
            {
                ShowStatus(loginStatus, "Đang nạp hồ sơ...", true);
                await profile.LoadProfileAsync();
            }

            if (GameManager.IsRichDemoAccountName(username) && !profile.HasCharacterCreated)
                profile.ApplyCharacterInfo(username, profile.Profile != null ? profile.Profile.gender : "male");

            shouldSkipCharacterSelectAfterLogin = profile.HasCharacterCreated;
        }

        if (GameManager.IsRichDemoAccountName(username))
            shouldSkipCharacterSelectAfterLogin = true;

        SetAuthControlsEnabled(true);
        ShowStatus(loginStatus,
            shouldSkipCharacterSelectAfterLogin
                ? "\u0110\u0103ng nh\u1eadp th\u00e0nh c\u00f4ng! \u0110ang v\u00e0o game..."
                : "\u0110\u0103ng nh\u1eadp th\u00e0nh c\u00f4ng! H\u00e3y t\u1ea1o nh\u00e2n v\u1eadt.",
            true);
        Debug.Log($"[LoginScreen] Login successful. Skip character select: {shouldSkipCharacterSelectAfterLogin}");

        Invoke(nameof(TransitionAfterLogin), 0.8f);
    }

    private void TransitionAfterLogin()
    {
        if (shouldSkipCharacterSelectAfterLogin)
            TransitionToExistingCharacter();
        else
            TransitionToCharacterSelect();
    }

    private void TransitionToExistingCharacter()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGameFromProfile();
            Debug.Log("[LoginScreen] Existing character profile found. Starting game directly.");
        }
        else
        {
            Debug.LogWarning("[LoginScreen] GameManager.Instance is null! Cannot start existing character.");
        }
    }

    private void TransitionToCharacterSelect()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.Menu);
            Debug.Log("[LoginScreen] Transitioned to Menu (Character Selection) state.");
        }
        else
        {
            Debug.LogWarning("[LoginScreen] GameManager.Instance is null! Cannot transition.");
        }
    }

    // ── Register ──

    private void OnRegisterFieldsChanged()
    {
        string errMsg;
        bool isValid = ValidateRegisterForm(out errMsg);

        if (btnRegister != null)
        {
            btnRegister.SetEnabled(isValid);
        }

        // Trên mobile, không hiện lỗi độ dài liên tục khi người chơi vẫn đang gõ.
        bool allFieldsFilled = !string.IsNullOrWhiteSpace(regUsernameField?.value) &&
                               !string.IsNullOrWhiteSpace(regEmailField?.value) &&
                               !string.IsNullOrEmpty(regPasswordField?.value) &&
                               !string.IsNullOrEmpty(regConfirmField?.value);

        if (!isValid)
        {
            if (allFieldsFilled)
            {
                ShowStatus(registerStatus, errMsg, false);
            }
            else
            {
                ClearStatus(registerStatus);
            }
        }
        else
        {
            ShowStatus(registerStatus, "Thông tin hợp lệ, sẵn sàng đăng ký!", true);
        }
    }

    private bool ValidateRegisterForm(out string errorMessage)
    {
        errorMessage = "";
        string username = (regUsernameField?.value ?? "").Trim();
        string email = (regEmailField?.value ?? "").Trim();
        string password = regPasswordField?.value ?? "";
        string confirm = regConfirmField?.value ?? "";

        // 1. Check empty
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
        {
            errorMessage = "Vui lòng điền đầy đủ các thông tin đăng ký";
            return false;
        }

        // 2. Validate Username (9-20 chars, alphanumeric & underscore only)
        if (username.Length < REGISTER_MIN_LENGTH)
        {
            errorMessage = "Tên đăng nhập mới cần ít nhất 9 ký tự";
            return false;
        }
        if (username.Length > REGISTER_MAX_LENGTH)
        {
            errorMessage = "Tên đăng nhập không được quá 20 ký tự";
            return false;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            errorMessage = "Tên đăng nhập chỉ được chứa chữ, số và dấu gạch dưới (_)";
            return false;
        }

        // 3. Validate Email (standard email regex)
        if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            errorMessage = "Email không đúng định dạng";
            return false;
        }

        // 4. Validate Password (9-20 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char)
        if (password.Length < REGISTER_MIN_LENGTH)
        {
            errorMessage = "Mật khẩu mới cần ít nhất 9 ký tự";
            return false;
        }
        if (password.Length > REGISTER_MAX_LENGTH)
        {
            errorMessage = "Mật khẩu không được quá 20 ký tự";
            return false;
        }

        bool hasUpper = false;
        bool hasLower = false;
        bool hasDigit = false;
        bool hasSpecial = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else if (!char.IsLetterOrDigit(c)) hasSpecial = true;
        }

        if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
        {
            errorMessage = "Mật khẩu cần ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt";
            return false;
        }

        // 5. Validate Password Confirm
        if (password != confirm)
        {
            errorMessage = "Mật khẩu xác nhận không khớp";
            return false;
        }

        return true;
    }

    private async void OnRegisterClicked()
    {
        if (isAuthRequestInProgress) return;

        string errMsg;
        if (!ValidateRegisterForm(out errMsg))
        {
            ShowStatus(registerStatus, errMsg, false);
            return;
        }

        var auth = AuthService.Instance;
        if (auth == null)
        {
            ShowStatus(registerStatus, "Hệ thống đăng ký chưa sẵn sàng. Thử lại sau.", false);
            Debug.LogWarning("[LoginScreen] AuthService.Instance is null. Cannot register.");
            return;
        }

        string username = (regUsernameField?.value ?? "").Trim();
        string email = (regEmailField?.value ?? "").Trim();
        string password = regPasswordField?.value ?? "";

        SetAuthControlsEnabled(false);
        ShowStatus(registerStatus, "Đang tạo tài khoản...", true);
        bool success = await auth.RegisterAsync(username, password, email);
        SetAuthControlsEnabled(true);

        if (!success)
        {
            ShowStatus(registerStatus, ResolveRegisterFailureMessage(auth), false);
            return;
        }

        auth.SignOut();

        ShowStatus(registerStatus, "Đăng ký thành công! Chuyển sang Đăng Nhập...", true);
        Debug.Log("[LoginScreen] Registration successful. Switching to Login tab...");

        Invoke(nameof(SwitchToLoginAfterRegister), 1.2f);
    }

    private void SwitchToLoginAfterRegister()
    {
        ShowLoginTab();
        // Pre-fill username
        if (usernameField != null && regUsernameField != null)
        {
            usernameField.value = regUsernameField.value;
        }
    }

    // ── Forgot Password ──

    private void OnForgotPasswordClicked()
    {
        Debug.Log("[LoginScreen] Forgot password clicked");

        if (forgotPasswordPopup != null)
        {
            forgotPasswordPopup.Show();
        }
        else
        {
            ShowStatus(loginStatus, "Chưa gắn ForgotPasswordPopupController!", false);
            Debug.LogWarning("[LoginScreen] forgotPasswordPopup is null! Drag the reference in Inspector.");
        }
    }

    // ── Status Message Helpers ──

    private static bool IsOnlineOnly()
    {
        return BackendConfig.Active != null && !BackendConfig.Active.useOfflineFallback;
    }

    private static string ResolveLoginFailureMessage(AuthService auth)
    {
        long status = auth != null ? auth.LastStatus : 0;
        string errorCode = auth != null ? auth.LastErrorCode : "";
        if (status == 0)
            return "Không thể kết nối máy chủ. Kiểm tra mạng hoặc chờ server được bật.";
        if (status == 401 || status == 404)
            return "Sai tên tài khoản hoặc mật khẩu.";
        if (status == 403 &&
            (string.Equals(errorCode, "WEB_ACCOUNT_LOCKED", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(errorCode, "WEB_ACCOUNT_DELETED", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(errorCode, "WEB_ACCOUNT_INACTIVE", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(errorCode, "WEB_AUTH_FORBIDDEN", System.StringComparison.OrdinalIgnoreCase)))
            return "Tài khoản đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ hỗ trợ.";
        if (status == 429)
            return "Bạn đã thử đăng nhập quá nhiều lần. Vui lòng chờ rồi thử lại.";
        if (status >= 500)
            return "Máy chủ đăng nhập đang tạm ngừng. Vui lòng thử lại sau.";
        return "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.";
    }

    private static string ResolveBrowserAuthFailureMessage(AuthService auth)
    {
        string errorCode = auth != null ? auth.LastErrorCode : "";
        if (string.Equals(errorCode, "BROWSER_AUTH_CANCELLED", System.StringComparison.OrdinalIgnoreCase))
            return "Đã hủy đăng nhập website.";
        if (string.Equals(errorCode, "BROWSER_AUTH_EXPIRED", System.StringComparison.OrdinalIgnoreCase))
            return "Phiên đăng nhập website đã hết hạn. Vui lòng thử lại.";
        if (string.Equals(errorCode, "BROWSER_AUTH_DISABLED", System.StringComparison.OrdinalIgnoreCase))
            return "Đăng nhập website chưa được bật trên phiên bản này.";
        if (string.Equals(errorCode, "WEB_ACCOUNT_LOCKED", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, "WEB_ACCOUNT_DELETED", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(errorCode, "WEB_ACCOUNT_INACTIVE", System.StringComparison.OrdinalIgnoreCase))
            return "Tài khoản website đã bị khóa hoặc ngừng hoạt động.";
        return ResolveLoginFailureMessage(auth);
    }

    private static string ResolveRegisterFailureMessage(AuthService auth)
    {
        long status = auth != null ? auth.LastStatus : 0;
        if (status == 0)
            return "Không thể kết nối máy chủ nên chưa thể tạo tài khoản.";
        if (status == 409)
            return "Tên tài khoản hoặc email này đã được sử dụng.";
        if (status >= 500)
            return "Máy chủ đăng ký đang tạm ngừng. Vui lòng thử lại sau.";
        return "Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.";
    }

    private static string ResolveBootstrapFailureMessage(PlayerBootstrapService bootstrap)
    {
        long status = bootstrap != null ? bootstrap.LastStatus : 0;
        if (status == 401)
            return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
        if (status == 0)
            return "Không thể tải dữ liệu nhân vật vì mất kết nối máy chủ.";
        return "Máy chủ chưa tải được dữ liệu nhân vật. Vui lòng thử lại.";
    }

    private void ShowStatus(Label label, string message, bool isSuccess)
    {
        if (label == null) return;

        label.text = message;
        label.RemoveFromClassList(STATUS_SUCCESS);
        label.RemoveFromClassList(STATUS_ERROR);
        label.AddToClassList(isSuccess ? STATUS_SUCCESS : STATUS_ERROR);
        label.style.display = DisplayStyle.Flex;
    }

    private void ClearStatus(Label label)
    {
        if (label == null) return;

        label.text = "";
        label.RemoveFromClassList(STATUS_SUCCESS);
        label.RemoveFromClassList(STATUS_ERROR);
        label.style.display = DisplayStyle.None;
    }

    private void SetAuthControlsEnabled(bool enabled)
    {
        isAuthRequestInProgress = !enabled;

        tabLogin?.SetEnabled(enabled);
        tabRegister?.SetEnabled(enabled);

        usernameField?.SetEnabled(enabled);
        passwordField?.SetEnabled(enabled);
        btnTogglePassword?.SetEnabled(enabled);
        rememberToggle?.SetEnabled(enabled);
        forgotPassword?.SetEnabled(enabled);
        btnLogin?.SetEnabled(enabled);
        btnLoginWeb?.SetEnabled(enabled);
        regUsernameField?.SetEnabled(enabled);
        regEmailField?.SetEnabled(enabled);
        regPasswordField?.SetEnabled(enabled);
        regConfirmField?.SetEnabled(enabled);
        btnToggleRegPassword?.SetEnabled(enabled);
        btnOpenWebRegistration?.SetEnabled(enabled);

        if (btnRegister != null)
        {
            btnRegister.SetEnabled(enabled && ValidateRegisterForm(out _));
        }
    }

    private void UpdateKeyboardAvoidance()
    {
        if (rootElement == null || loginShadow == null) return;

        if (!MobileKeyboardAvoidance.ShouldAvoidKeyboard(focusedKeyboardField))
        {
            if (keyboardUpShift > 0f)
                ResetKeyboardAvoidance();
            return;
        }

        float newShift = MobileKeyboardAvoidance.CalculateRequiredUpShift(
            rootElement,
            focusedKeyboardField,
            keyboardUpShift,
            marginAboveKeyboard: 24f);

        if (Mathf.Abs(newShift - keyboardUpShift) < 0.5f) return;

        keyboardUpShift = newShift;
        loginShadow.style.translate = new Translate(0, -keyboardUpShift, 0);
    }

    private void ResetKeyboardAvoidance()
    {
        focusedKeyboardField = null;
        keyboardUpShift = 0f;
        if (loginShadow != null)
            loginShadow.style.translate = new Translate(0, 0, 0);
    }

    private void OnQuitAppClicked()
    {
        if (isQuitRequestSent) return;
        isQuitRequestSent = true;

        Debug.Log("[LoginScreen] Quit app clicked.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnQuitAppPointerDown(PointerDownEvent evt)
    {
        evt.StopImmediatePropagation();
        OnQuitAppClicked();
    }
}
