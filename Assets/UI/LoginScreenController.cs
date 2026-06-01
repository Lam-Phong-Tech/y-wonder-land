using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Login/Register UI screen.
/// Handles tab switching, password toggle, and navigation to Character Selection.
/// Mockup only — no backend authentication logic.
/// </summary>
public class LoginScreenController : MonoBehaviour
{
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

        // Forgot password
        forgotPassword?.RegisterCallback<ClickEvent>(evt => OnForgotPasswordClicked());

        // Input focus styling
        RegisterInputFocusEvents(usernameField, "UsernameGroup");
        RegisterInputFocusEvents(passwordField, "PasswordGroup");
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

    private void OnLoginClicked()
    {
        string username = usernameField?.value ?? "";
        string password = passwordField?.value ?? "";

        Debug.Log($"[LoginScreen] Login clicked — Username: '{username}'");

        // Validation
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowStatus(loginStatus, "Vui lòng nhập tên đăng nhập", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowStatus(loginStatus, "Vui lòng nhập mật khẩu", false);
            return;
        }

        // Mockup: always succeed
        ShowStatus(loginStatus, "Đăng nhập thành công!", true);
        Debug.Log("[LoginScreen] Login successful (mockup). Transitioning to Character Selection...");

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

    private void OnRegisterClicked()
    {
        string username = regUsernameField?.value ?? "";
        string email = regEmailField?.value ?? "";
        string password = regPasswordField?.value ?? "";
        string confirm = regConfirmField?.value ?? "";

        Debug.Log($"[LoginScreen] Register clicked — Username: '{username}', Email: '{email}'");

        // Validation
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowStatus(registerStatus, "Vui lòng nhập tên đăng nhập", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowStatus(registerStatus, "Vui lòng nhập email", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowStatus(registerStatus, "Vui lòng nhập mật khẩu", false);
            return;
        }

        if (password != confirm)
        {
            ShowStatus(registerStatus, "Mật khẩu xác nhận không khớp", false);
            return;
        }

        // Mockup: always succeed
        ShowStatus(registerStatus, "Đăng ký thành công! Chuyển sang Đăng Nhập...", true);
        Debug.Log("[LoginScreen] Registration successful (mockup). Switching to Login tab...");

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
        Debug.Log("[LoginScreen] Forgot password clicked (mockup)");
        ShowStatus(loginStatus, "Chức năng đang phát triển...", false);
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
}
