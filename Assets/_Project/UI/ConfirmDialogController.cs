using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Reusable Confirm Dialog — dùng chung cho toàn bộ game.
/// Hỗ trợ 3 loại: Warning (vàng), Danger (đỏ), Info (xanh).
/// Gọi Show() để hiển thị, truyền callback xác nhận.
/// </summary>
public class ConfirmDialogController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float visibleSortingOrder = 5000f;

    private VisualElement overlay;
    private VisualElement iconContainer;
    private Label iconLabel;
    private Label titleLabel;
    private Label messageLabel;
    private Button btnConfirm;
    private Button btnCancel;
    private Button btnClose;

    private Action onConfirmCallback;
    private float baseSortingOrder;

    private void Awake()
    {
        if (uiDocument == null)
        {
            if (!TryGetComponent(out uiDocument))
            {
                Debug.LogError("[ConfirmDialog] UIDocument không tìm thấy!");
                return;
            }
        }

        baseSortingOrder = uiDocument.sortingOrder;
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        overlay = root.Q<VisualElement>("confirm-overlay");
        iconContainer = root.Q<VisualElement>("confirm-icon-container");
        iconLabel = root.Q<Label>("confirm-icon");
        titleLabel = root.Q<Label>("confirm-title");
        messageLabel = root.Q<Label>("confirm-message");
        btnConfirm = root.Q<Button>("btn-confirm");
        btnCancel = root.Q<Button>("btn-cancel");
        btnClose = root.Q<Button>("btn-close");

        RegisterCallbacks();
        Hide();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        if (btnConfirm != null)
        {
            btnConfirm.RegisterCallback<ClickEvent>(OnConfirmClicked);
        }

        if (btnCancel != null)
        {
            btnCancel.RegisterCallback<ClickEvent>(OnCancelClicked);
        }

        if (btnClose != null)
        {
            btnClose.RegisterCallback<ClickEvent>(OnCancelClicked);
        }

        if (overlay != null)
        {
            overlay.RegisterCallback<ClickEvent>(OnOverlayClicked);
        }
    }

    private void UnregisterCallbacks()
    {
        if (btnConfirm != null)
        {
            btnConfirm.UnregisterCallback<ClickEvent>(OnConfirmClicked);
        }

        if (btnCancel != null)
        {
            btnCancel.UnregisterCallback<ClickEvent>(OnCancelClicked);
        }

        if (btnClose != null)
        {
            btnClose.UnregisterCallback<ClickEvent>(OnCancelClicked);
        }

        if (overlay != null)
        {
            overlay.UnregisterCallback<ClickEvent>(OnOverlayClicked);
        }
    }

    /// <summary>
    /// Hiển thị Confirm Dialog.
    /// </summary>
    /// <param name="title">Tiêu đề (VD: "XÁC NHẬN", "CẢNH BÁO")</param>
    /// <param name="message">Nội dung cảnh báo</param>
    /// <param name="confirmText">Chữ trên nút xác nhận (VD: "Đồng ý")</param>
    /// <param name="cancelText">Chữ trên nút hủy (VD: "Hủy bỏ")</param>
    /// <param name="onConfirm">Callback khi bấm xác nhận</param>
    /// <param name="dialogType">Loại dialog: Warning / Danger / Info</param>
    public void Show(
        string title,
        string message,
        string confirmText,
        string cancelText,
        Action onConfirm,
        ConfirmDialogType dialogType = ConfirmDialogType.Warning)
    {
        onConfirmCallback = onConfirm;

        if (titleLabel != null)
        {
            titleLabel.text = title;
        }

        if (messageLabel != null)
        {
            messageLabel.text = message;
        }

        if (btnConfirm != null)
        {
            btnConfirm.text = confirmText;
            if (string.IsNullOrEmpty(cancelText)) {
                btnConfirm.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            } else {
                btnConfirm.style.width = new StyleLength(new Length(46, LengthUnit.Percent));
            }
        }

        if (btnCancel != null)
        {
            btnCancel.text = cancelText;
            btnCancel.style.display = string.IsNullOrEmpty(cancelText) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        ApplyDialogType(dialogType);

        BringDialogToFront();

        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Ẩn Confirm Dialog.
    /// </summary>
    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
        }

        if (uiDocument != null)
        {
            uiDocument.sortingOrder = baseSortingOrder;
        }

        onConfirmCallback = null;
    }

    private void BringDialogToFront()
    {
        transform.SetAsLastSibling();

        if (uiDocument != null)
        {
            uiDocument.sortingOrder = Mathf.Max(uiDocument.sortingOrder, visibleSortingOrder);
            uiDocument.rootVisualElement?.BringToFront();
        }

        overlay?.BringToFront();
    }

    private void ApplyDialogType(ConfirmDialogType dialogType)
    {
        // Reset tất cả class type trên nút Confirm
        if (btnConfirm != null)
        {
            btnConfirm.RemoveFromClassList("btn-confirm-warning");
            btnConfirm.RemoveFromClassList("btn-confirm-danger");
            btnConfirm.RemoveFromClassList("btn-confirm-info");
        }

        // Reset tất cả class type trên icon container
        if (iconContainer != null)
        {
            iconContainer.RemoveFromClassList("icon-container-warning");
            iconContainer.RemoveFromClassList("icon-container-danger");
            iconContainer.RemoveFromClassList("icon-container-info");
        }

        // Reset tất cả class type trên icon label
        if (iconLabel != null)
        {
            iconLabel.RemoveFromClassList("icon-warning");
            iconLabel.RemoveFromClassList("icon-danger");
            iconLabel.RemoveFromClassList("icon-info");
        }

        switch (dialogType)
        {
            case ConfirmDialogType.Warning:
                if (iconLabel != null)
                {
                    iconLabel.text = "⚠";
                    iconLabel.AddToClassList("icon-warning");
                }
                if (iconContainer != null)
                {
                    iconContainer.AddToClassList("icon-container-warning");
                }
                if (btnConfirm != null)
                {
                    btnConfirm.AddToClassList("btn-confirm-warning");
                }
                break;

            case ConfirmDialogType.Danger:
                if (iconLabel != null)
                {
                    iconLabel.text = "✕";
                    iconLabel.AddToClassList("icon-danger");
                }
                if (iconContainer != null)
                {
                    iconContainer.AddToClassList("icon-container-danger");
                }
                if (btnConfirm != null)
                {
                    btnConfirm.AddToClassList("btn-confirm-danger");
                }
                break;

            case ConfirmDialogType.Info:
                if (iconLabel != null)
                {
                    iconLabel.text = "i";
                    iconLabel.AddToClassList("icon-info");
                }
                if (iconContainer != null)
                {
                    iconContainer.AddToClassList("icon-container-info");
                }
                if (btnConfirm != null)
                {
                    btnConfirm.AddToClassList("btn-confirm-info");
                }
                break;
        }
    }

    // === EVENT HANDLERS ===

    private void OnConfirmClicked(ClickEvent evt)
    {
        evt.StopPropagation();
        Action callback = onConfirmCallback;
        Hide();
        callback?.Invoke();
    }

    private void OnCancelClicked(ClickEvent evt)
    {
        evt.StopPropagation();
        Hide();
    }

    private void OnOverlayClicked(ClickEvent evt)
    {
        // Chỉ đóng khi click đúng overlay, không phải click vào panel
        if (evt.target == overlay)
        {
            Hide();
        }
    }
}

/// <summary>
/// Loại Confirm Dialog — quyết định màu sắc icon và nút xác nhận.
/// </summary>
public enum ConfirmDialogType
{
    /// <summary>Cảnh báo hành động không hoàn tác (vàng #FFC107)</summary>
    Warning,

    /// <summary>Hành động nguy hiểm — xóa tài khoản, xóa bạn (đỏ #FF4B4B)</summary>
    Danger,

    /// <summary>Xác nhận bình thường — mua hàng, gửi heo đất (xanh #2D7BFF)</summary>
    Info
}
