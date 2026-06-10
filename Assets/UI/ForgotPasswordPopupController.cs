using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Forgot Password Popup.
/// Shows a simple email input + submit flow (mockup — no backend).
/// Usage: Drag ForgotPasswordPopup.uxml onto a UIDocument,
///        reference this controller, call Show() from LoginScreenController.
/// </summary>
public class ForgotPasswordPopupController : MonoBehaviour
{
    private UIDocument uiDocument;

    // Elements
    private VisualElement overlay;
    private VisualElement emailGroup;
    private TextField emailField;
    private Button btnClose;
    private Button btnSubmit;
    private Label statusLabel;

    // USS class names (khớp popup cũ)
    private const string FOCUS_CLASS = "forgot-input-group-focus";
    private const string STATUS_SUCCESS = "status-success";
    private const string STATUS_ERROR = "status-error";

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[ForgotPassword] UIDocument component not found!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        SetupPlaceholder();
    }

    void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("forgot-overlay");
        emailGroup = root.Q<VisualElement>("EmailGroup");
        emailField = root.Q<TextField>("EmailField");
        btnClose = root.Q<Button>("btn-close");
        btnSubmit = root.Q<Button>("btn-submit");
        statusLabel = root.Q<Label>("ForgotStatus");
    }

    private void RegisterCallbacks()
    {
        btnClose?.RegisterCallback<ClickEvent>(OnCloseClicked);
        btnSubmit?.RegisterCallback<ClickEvent>(OnSubmitClicked);

        // Email real-time validation
        emailField?.RegisterValueChangedCallback(OnEmailChanged);

        // Input focus styling
        if (emailField != null && emailGroup != null)
        {
            emailField.RegisterCallback<FocusInEvent>(evt => emailGroup.AddToClassList(FOCUS_CLASS));
            emailField.RegisterCallback<FocusOutEvent>(evt => emailGroup.RemoveFromClassList(FOCUS_CLASS));
        }

        // Overlay click-to-dismiss
        overlay?.RegisterCallback<ClickEvent>(OnOverlayClicked);
    }

    private void UnregisterCallbacks()
    {
        btnClose?.UnregisterCallback<ClickEvent>(OnCloseClicked);
        btnSubmit?.UnregisterCallback<ClickEvent>(OnSubmitClicked);
        overlay?.UnregisterCallback<ClickEvent>(OnOverlayClicked);
    }

    private void SetupPlaceholder()
    {
        if (emailField != null)
        {
            emailField.textEdition.placeholder = "Nhập email đã đăng ký...";
        }
    }

    // ── Public API ──

    /// <summary>
    /// Show the Forgot Password popup.
    /// </summary>
    public void Show()
    {
        if (overlay == null) return;

        // Reset state
        if (emailField != null) emailField.value = "";
        ClearStatus();

        overlay.style.display = DisplayStyle.Flex;
        Debug.Log("[ForgotPassword] Popup shown");
    }

    /// <summary>
    /// Hide the Forgot Password popup.
    /// </summary>
    public void Hide()
    {
        if (overlay == null) return;

        overlay.style.display = DisplayStyle.None;
        Debug.Log("[ForgotPassword] Popup hidden");
    }

    // ── Callbacks ──

    private void OnCloseClicked(ClickEvent evt)
    {
        evt.StopPropagation();
        Hide();
    }

    private void OnOverlayClicked(ClickEvent evt)
    {
        // Only dismiss if clicking overlay itself (not panel)
        if (evt.target == overlay)
        {
            Hide();
        }
    }

    private void OnEmailChanged(ChangeEvent<string> evt)
    {
        // Clear previous status when user edits
        string email = evt.newValue ?? "";
        if (!string.IsNullOrEmpty(email))
        {
            ClearStatus();
        }
    }

    private void OnSubmitClicked(ClickEvent evt)
    {
        evt.StopPropagation();

        string email = emailField?.value ?? "";

        if (!ValidateEmail(email))
        {
            ShowStatus("Email không đúng định dạng", false);
            return;
        }

        // Mockup: always succeed
        ShowStatus("Đã gửi mã xác nhận đến " + email + "! Vui lòng kiểm tra hộp thư.", true);
        Debug.Log($"[ForgotPassword] Sent reset code to: {email} (mockup)");
    }

    // ── Validation ──

    private bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(
            email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }

    // ── Status Helpers ──

    private void ShowStatus(string message, bool isSuccess)
    {
        if (statusLabel == null) return;

        statusLabel.text = message;
        statusLabel.RemoveFromClassList(STATUS_SUCCESS);
        statusLabel.RemoveFromClassList(STATUS_ERROR);
        statusLabel.AddToClassList(isSuccess ? STATUS_SUCCESS : STATUS_ERROR);
        statusLabel.style.display = DisplayStyle.Flex;
    }

    private void ClearStatus()
    {
        if (statusLabel == null) return;

        statusLabel.text = "";
        statusLabel.RemoveFromClassList(STATUS_SUCCESS);
        statusLabel.RemoveFromClassList(STATUS_ERROR);
        statusLabel.style.display = DisplayStyle.None;
    }
}
