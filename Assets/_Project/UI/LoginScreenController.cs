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
    private Label loginStatus;

    // Register form elements
    private TextField regUsernameField;
    private TextField regEmailField;
    private TextField regPasswordField;
    private TextField regConfirmField;
    private Button btnToggleRegPassword;
    private Button btnRegister;
    private Label registerStatus;

    // State
    private bool isPasswordVisible = false;
    private bool isRegPasswordVisible = false;
    private bool isAuthRequestInProgress = false;

    // USS class names
    private const string TAB_ACTIVE_CLASS = "login-tab-active";
    private const string HIDDEN_CLASS = "hidden";
    private const string INPUT_FOCUS_CLASS = "login-input-group-focus";
    private const string STATUS_SUCCESS = "status-success";
    private const string STATUS_ERROR = "status-error";

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[LoginScreen] UIDocument component not found!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        SetupPlaceholders();
        
        // Disable register button by default until validated
        if (btnRegister != null)
        {
            btnRegister.SetEnabled(false);
        }

        // Default: show Login tab
        ShowLoginTab();
    }

    private void QueryElements(VisualElement root)
    {
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
        loginStatus = root.Q<Label>("LoginStatus");

        // Register elements
        regUsernameField = root.Q<TextField>("RegUsernameField");
        regEmailField = root.Q<TextField>("RegEmailField");
        regPasswordField = root.Q<TextField>("RegPasswordField");
        regConfirmField = root.Q<TextField>("RegConfirmField");
        btnToggleRegPassword = root.Q<Button>("BtnToggleRegPassword");
        btnRegister = root.Q<Button>("BtnRegister");
        registerStatus = root.Q<Label>("RegisterStatus");
    }

    private void RegisterCallbacks()
    {
        // Tab switching
        tabLogin?.RegisterCallback<ClickEvent>(evt => ShowLoginTab());
        tabRegister?.RegisterCallback<ClickEvent>(evt => ShowRegisterTab());

        // Login actions
        btnLogin?.RegisterCallback<ClickEvent>(evt => OnLoginClicked());
        btnTogglePassword?.RegisterCallback<ClickEvent>(evt => TogglePasswordVisibility());

        // Register actions
        btnRegister?.RegisterCallback<ClickEvent>(evt => OnRegisterClicked());
        btnToggleRegPassword?.RegisterCallback<ClickEvent>(evt => ToggleRegPasswordVisibility());

        // Register real-time validation callbacks
        regUsernameField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());
        regEmailField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());
        regPasswordField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());
        regConfirmField?.RegisterValueChangedCallback(evt => OnRegisterFieldsChanged());

        // Forgot password
        forgotPassword?.RegisterCallback<ClickEvent>(evt => OnForgotPasswordClicked());

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

        field.RegisterCallback<FocusInEvent>(evt => group.AddToClassList(INPUT_FOCUS_CLASS));
        field.RegisterCallback<FocusOutEvent>(evt => group.RemoveFromClassList(INPUT_FOCUS_CLASS));
    }

    private void SetupPlaceholders()
    {
        // Set placeholder text via textEdition (Unity 2022.2+)
        SetPlaceholder(usernameField, "Tên đăng nhập");
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

    // ── Tab Switching ──

    private void ShowLoginTab()
    {
        tabLogin?.AddToClassList(TAB_ACTIVE_CLASS);
        tabRegister?.RemoveFromClassList(TAB_ACTIVE_CLASS);

        loginForm?.RemoveFromClassList(HIDDEN_CLASS);
        registerForm?.AddToClassList(HIDDEN_CLASS);

        ClearStatus(loginStatus);
        Debug.Log("[LoginScreen] Switched to Login tab");
    }

    private void ShowRegisterTab()
    {
        tabRegister?.AddToClassList(TAB_ACTIVE_CLASS);
        tabLogin?.RemoveFromClassList(TAB_ACTIVE_CLASS);

        registerForm?.RemoveFromClassList(HIDDEN_CLASS);
        loginForm?.AddToClassList(HIDDEN_CLASS);

        ClearStatus(registerStatus);
        Debug.Log("[LoginScreen] Switched to Register tab");
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

    private async void OnLoginClicked()
    {
        if (isAuthRequestInProgress) return;

        string username = (usernameField?.value ?? "").Trim();
        string password = passwordField?.value ?? "";

        Debug.Log($"[LoginScreen] Login clicked — Username: '{username}'");

        // Validation
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowStatus(loginStatus, "Vui lòng nhập tên đăng nhập", false);
            return;
        }
        if (username.Length > 20)
        {
            ShowStatus(loginStatus, "Tên đăng nhập không được quá 20 ký tự", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowStatus(loginStatus, "Vui lòng nhập mật khẩu", false);
            return;
        }
        if (password.Length > 20)
        {
            ShowStatus(loginStatus, "Mật khẩu không được quá 20 ký tự", false);
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
        ShowStatus(loginStatus, "Đang đăng nhập...", true);
        bool success = await auth.LoginAsync(username, password);
        SetAuthControlsEnabled(true);

        if (!success)
        {
            ShowStatus(loginStatus, "Đăng nhập thất bại. Kiểm tra tài khoản/mật khẩu.", false);
            return;
        }

        var profile = PlayerProfileService.Instance;
        if (profile != null)
        {
            ShowStatus(loginStatus, "Đang nạp hồ sơ...", true);
            await profile.LoadProfileAsync();
        }

        ShowStatus(loginStatus, "Đăng nhập thành công!", true);
        Debug.Log("[LoginScreen] Login successful. Transitioning to Character Selection...");

        // Transition to Character Selection after a short delay
        Invoke(nameof(TransitionToCharacterSelect), 0.8f);
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

        // Show real-time error helper only if at least one field has been edited
        bool anyFieldFilled = !string.IsNullOrEmpty(regUsernameField?.value) ||
                              !string.IsNullOrEmpty(regEmailField?.value) ||
                              !string.IsNullOrEmpty(regPasswordField?.value) ||
                              !string.IsNullOrEmpty(regConfirmField?.value);

        if (!isValid)
        {
            if (anyFieldFilled)
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
        string username = regUsernameField?.value ?? "";
        string email = regEmailField?.value ?? "";
        string password = regPasswordField?.value ?? "";
        string confirm = regConfirmField?.value ?? "";

        // 1. Check empty
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
        {
            errorMessage = "Vui lòng điền đầy đủ các thông tin đăng ký";
            return false;
        }

        // 2. Validate Username (> 8 chars, alphanumeric & underscore only)
        if (username.Length <= 8)
        {
            errorMessage = "Tên đăng nhập phải dài hơn 8 ký tự";
            return false;
        }
        if (username.Length > 20)
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

        // 4. Validate Password (> 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char)
        if (password.Length <= 8)
        {
            errorMessage = "Mật khẩu phải dài hơn 8 ký tự";
            return false;
        }
        if (password.Length > 20)
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
        string password = regPasswordField?.value ?? "";

        SetAuthControlsEnabled(false);
        ShowStatus(registerStatus, "Đang tạo tài khoản...", true);
        bool success = await auth.RegisterAsync(username, password);
        SetAuthControlsEnabled(true);

        if (!success)
        {
            ShowStatus(registerStatus, "Đăng ký thất bại. Tài khoản có thể đã tồn tại hoặc mất kết nối.", false);
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

        regUsernameField?.SetEnabled(enabled);
        regEmailField?.SetEnabled(enabled);
        regPasswordField?.SetEnabled(enabled);
        regConfirmField?.SetEnabled(enabled);
        btnToggleRegPassword?.SetEnabled(enabled);

        if (btnRegister != null)
        {
            btnRegister.SetEnabled(enabled && ValidateRegisterForm(out _));
        }
    }
}
