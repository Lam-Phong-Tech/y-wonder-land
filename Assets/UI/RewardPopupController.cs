using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Reusable Reward Popup — hiển thị phần thưởng sau quest, event, heo đất, level up...
/// Gọi Show() với danh sách RewardItemData để hiển thị.
/// </summary>
public class RewardPopupController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement overlay;
    private Label titleLabel;
    private VisualElement rewardGrid;
    private Label emptyLabel;
    private Button btnClaim;
    private Button btnClose;

    private Action onClaimCallback;

    private void Awake()
    {
        if (uiDocument == null)
        {
            if (!TryGetComponent(out uiDocument))
            {
                Debug.LogError("[RewardPopup] UIDocument không tìm thấy!");
                return;
            }
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        overlay = root.Q<VisualElement>("reward-overlay");
        titleLabel = root.Q<Label>("reward-title");
        rewardGrid = root.Q<VisualElement>("reward-grid");
        emptyLabel = root.Q<Label>("reward-empty");
        btnClaim = root.Q<Button>("btn-claim");
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
        if (btnClaim != null)
        {
            btnClaim.RegisterCallback<ClickEvent>(OnClaimClicked);
        }

        if (btnClose != null)
        {
            btnClose.RegisterCallback<ClickEvent>(OnCloseClicked);
        }

        if (overlay != null)
        {
            overlay.RegisterCallback<ClickEvent>(OnOverlayClicked);
        }
    }

    private void UnregisterCallbacks()
    {
        if (btnClaim != null)
        {
            btnClaim.UnregisterCallback<ClickEvent>(OnClaimClicked);
        }

        if (btnClose != null)
        {
            btnClose.UnregisterCallback<ClickEvent>(OnCloseClicked);
        }

        if (overlay != null)
        {
            overlay.UnregisterCallback<ClickEvent>(OnOverlayClicked);
        }
    }

    /// <summary>
    /// Hiển thị Reward Popup với danh sách phần thưởng.
    /// </summary>
    /// <param name="title">Tiêu đề (VD: "PHẦN THƯỞNG", "HOÀN THÀNH NHIỆM VỤ")</param>
    /// <param name="rewards">Danh sách vật phẩm thưởng</param>
    /// <param name="buttonText">Chữ trên nút (VD: "Nhận thưởng", "Đóng")</param>
    /// <param name="onClaim">Callback khi bấm nhận thưởng</param>
    public void Show(
        string title,
        List<RewardItemData> rewards,
        string buttonText = "Nhận thưởng",
        Action onClaim = null)
    {
        onClaimCallback = onClaim;

        if (titleLabel != null)
        {
            titleLabel.text = title;
        }

        if (btnClaim != null)
        {
            btnClaim.text = buttonText;
        }

        PopulateRewardGrid(rewards);

        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Ẩn Reward Popup.
    /// </summary>
    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
        }

        onClaimCallback = null;
    }

    private void PopulateRewardGrid(List<RewardItemData> rewards)
    {
        if (rewardGrid == null)
        {
            return;
        }

        // Xóa các reward item cũ (giữ lại emptyLabel)
        List<VisualElement> toRemove = new List<VisualElement>();
        foreach (VisualElement child in rewardGrid.Children())
        {
            if (child != emptyLabel)
            {
                toRemove.Add(child);
            }
        }

        foreach (VisualElement item in toRemove)
        {
            rewardGrid.Remove(item);
        }

        // Hiển thị empty state hoặc reward items
        if (rewards == null || rewards.Count == 0)
        {
            if (emptyLabel != null)
            {
                emptyLabel.style.display = DisplayStyle.Flex;
            }
            return;
        }

        if (emptyLabel != null)
        {
            emptyLabel.style.display = DisplayStyle.None;
        }

        // Tạo reward items
        foreach (RewardItemData reward in rewards)
        {
            VisualElement rewardItem = CreateRewardItem(reward);
            rewardGrid.Add(rewardItem);
        }
    }

    private VisualElement CreateRewardItem(RewardItemData reward)
    {
        // Container cho 1 reward item
        VisualElement itemContainer = new VisualElement();
        itemContainer.AddToClassList("reward-item");

        // Slot (khung trắng bo góc)
        VisualElement slot = new VisualElement();
        slot.AddToClassList("reward-item-slot");

        // Icon (emoji hoặc ký tự)
        Label iconLabel = new Label();
        iconLabel.AddToClassList("reward-item-icon");
        iconLabel.text = reward.Icon;
        slot.Add(iconLabel);

        itemContainer.Add(slot);

        // Tên vật phẩm
        Label nameLabel = new Label();
        nameLabel.AddToClassList("reward-item-name");
        nameLabel.text = reward.Name;
        itemContainer.Add(nameLabel);

        // Số lượng
        if (reward.Quantity > 0)
        {
            Label quantityLabel = new Label();
            quantityLabel.AddToClassList("reward-item-quantity");
            quantityLabel.text = $"x{reward.Quantity}";
            itemContainer.Add(quantityLabel);
        }

        return itemContainer;
    }

    // === EVENT HANDLERS ===

    private void OnClaimClicked(ClickEvent evt)
    {
        evt.StopPropagation();
        Action callback = onClaimCallback;
        Hide();
        callback?.Invoke();
    }

    private void OnCloseClicked(ClickEvent evt)
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
/// Dữ liệu 1 vật phẩm thưởng.
/// </summary>
[Serializable]
public struct RewardItemData
{
    /// <summary>Icon hiển thị (emoji hoặc ký tự Unicode, VD: "🥕", "💰", "⭐")</summary>
    public string Icon;

    /// <summary>Tên vật phẩm (VD: "Cà rốt", "100 POS")</summary>
    public string Name;

    /// <summary>Số lượng (0 = không hiển thị số lượng)</summary>
    public int Quantity;

    public RewardItemData(string icon, string name, int quantity)
    {
        Icon = icon;
        Name = name;
        Quantity = quantity;
    }
}
