using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Inventory Popup.
/// Manages category tabs and item grid display.
/// </summary>
public class InventoryPopupController : MonoBehaviour
{
    /// <summary>
    /// Fired when player clicks the action button on an item.
    /// Parameter is the item name (e.g. "H\u1ea1t c\u00e0 r\u1ed1t").
    /// </summary>
    public event System.Action<string> OnItemUsed;
    [Header("References")]
    [SerializeField] private UIDocument inventoryDocument;

    private VisualElement overlay;
    private Button btnClose;
    private VisualElement gridContainer;
    private Label lblItemCount;
    private Label lblCategory;

    // Tab buttons
    private Button tabTools;
    private Button tabMaterials;
    private Button tabSeeds;
    private Button tabFood;
    private Button tabOutfit;
    private Button tabAnimals;
    private Button tabSpecial;

    private Button activeTab;
    private string activeCategory = "tools";

    // Detail panel references
    private VisualElement detailEmptyState;
    private VisualElement detailContent;
    private Label lblDetailIcon;
    private Label lblDetailName;
    private Label lblDetailQty;
    private Label lblDetailDesc;
    private Button btnDetailAction;
    private Button btnDetailDiscard;

    private VisualElement selectedItemCard;
    private InventoryItem? selectedItem;

    // Max inventory slots
    private const int MAX_SLOTS = 50;

    // Max inventory slots from Manager
    private int maxSlots => YWonderLand.Managers.InventoryManager.Instance?.GetMaxSlots() ?? 50;

    // Database
    private YWonderLand.Data.ItemDatabase itemDatabase;

    private struct InventoryItem
    {
        public string id;
        public string icon;
        public string name;
        public int quantity;
        public string description;
        public string actionText;
    }

    private void Awake()
    {
        if (inventoryDocument == null)
        {
            inventoryDocument = GetComponent<UIDocument>();
        }

        if (inventoryDocument != null)
        {
            inventoryDocument.sortingOrder = 100;
        }

        itemDatabase = Resources.Load<YWonderLand.Data.ItemDatabase>("ItemDatabase");
        if (itemDatabase == null) Debug.LogError("[Inventory] ItemDatabase not found in Resources!");
    }

    private void OnEnable()
    {
        var root = inventoryDocument.rootVisualElement;

        // Query elements
        overlay = root.Q<VisualElement>("InventoryOverlay");
        btnClose = root.Q<Button>("BtnCloseInventory");
        gridContainer = root.Q<VisualElement>("InventoryGrid");
        lblItemCount = root.Q<Label>("LblItemCount");
        lblCategory = root.Q<Label>("LblCategory");

        // Query tabs
        tabTools = root.Q<Button>("TabTools");
        tabMaterials = root.Q<Button>("TabMaterials");
        tabSeeds = root.Q<Button>("TabSeeds");
        tabFood = root.Q<Button>("TabFood");
        tabOutfit = root.Q<Button>("TabOutfit");
        tabAnimals = root.Q<Button>("TabAnimals");
        tabSpecial = root.Q<Button>("TabSpecial");

        // Query detail panel elements
        detailEmptyState = root.Q<VisualElement>("InventoryDetailEmptyState");
        detailContent = root.Q<VisualElement>("InventoryDetailContent");
        lblDetailIcon = root.Q<Label>("LblDetailIcon");
        lblDetailName = root.Q<Label>("LblDetailName");
        lblDetailQty = root.Q<Label>("LblDetailQty");
        lblDetailDesc = root.Q<Label>("LblDetailDesc");
        btnDetailAction = root.Q<Button>("BtnDetailAction");
        btnDetailDiscard = root.Q<Button>("BtnDetailDiscard");

        // Register callbacks
        RegisterCallbacks();

        // Set default tab
        activeTab = tabTools;
        SetActiveTab(tabTools, "tools", "Dụng cụ");

        // Start hidden
        Hide();

        if (YWonderLand.Managers.InventoryManager.Instance != null)
        {
            YWonderLand.Managers.InventoryManager.Instance.OnInventoryChanged += RefreshGrid;
        }
    }

    private void OnDisable()
    {
        if (YWonderLand.Managers.InventoryManager.Instance != null)
        {
            YWonderLand.Managers.InventoryManager.Instance.OnInventoryChanged -= RefreshGrid;
        }
        // Gỡ khỏi UIPopupTracker phòng khi bị tắt/destroy lúc đang mở (vd đổi đảo)
        // mà chưa kịp Hide() -> tránh chuột kẹt + tương tác thế giới chết.
        UIPopupTracker.SetOpen(this, false);
    }

    private void RegisterCallbacks()
    {
        // Close
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay) Hide();
        });

        // Tab clicks
        tabTools?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabTools, "tools", "Dụng cụ"));
        tabMaterials?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabMaterials, "materials", "Nguyên liệu"));
        tabSeeds?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabSeeds, "seeds", "Hạt giống"));
        tabFood?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabFood, "food", "Thực phẩm"));
        tabOutfit?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabOutfit, "outfit", "Trang phục"));
        tabAnimals?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabAnimals, "animals", "Thú nuôi"));
        tabSpecial?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabSpecial, "special", "Đặc biệt"));

        // Detail Action clicks
        btnDetailAction?.RegisterCallback<ClickEvent>(evt =>
        {
            if (selectedItem.HasValue)
            {
                Debug.Log($"[Inventory] Executed action '{selectedItem.Value.actionText}' on item: {selectedItem.Value.name} ({selectedItem.Value.id})");
                OnItemUsed?.Invoke(selectedItem.Value.id); // Send ID instead of name
            }
        });

        btnDetailDiscard?.RegisterCallback<ClickEvent>(evt =>
        {
            if (selectedItem.HasValue)
            {
                Debug.Log($"[Inventory] Discarded item: {selectedItem.Value.name}");
            }
        });
    }

    private void SetActiveTab(Button tab, string category, string categoryName)
    {
        // Update tab styles
        if (activeTab != null)
        {
            activeTab.RemoveFromClassList("inventory-tab--active");
        }
        activeTab = tab;
        activeTab?.AddToClassList("inventory-tab--active");

        activeCategory = category;

        // Update info bar
        if (lblCategory != null) lblCategory.text = categoryName;

        // Refresh grid
        RefreshGrid();

        Debug.Log($"[Inventory] Tab changed: {categoryName}");
    }
    private void RefreshGrid()
    {
        if (gridContainer == null) return;

        // Clear existing items
        gridContainer.Clear();

        if (itemDatabase == null || YWonderLand.Managers.InventoryManager.Instance == null)
        {
            ShowEmptyDetails();
            return;
        }

        var allSlots = YWonderLand.Managers.InventoryManager.Instance.GetAllSlots();
        var categoryItems = new List<InventoryItem>();

        foreach (var slot in allSlots)
        {
            var def = itemDatabase.GetItem(slot.itemId);
            if (def != null && def.category == activeCategory)
            {
                categoryItems.Add(new InventoryItem
                {
                    id = def.id,
                    icon = !string.IsNullOrEmpty(def.iconEmoji) ? def.iconEmoji : "📦",
                    name = def.itemName,
                    quantity = slot.quantity,
                    description = def.description,
                    actionText = "Sử dụng"
                });
            }
        }

        if (categoryItems.Count > 0)
        {
            VisualElement firstCard = null;

            for (int i = 0; i < categoryItems.Count; i++)
            {
                var item = categoryItems[i];
                var itemElement = CreateItemElement(item, out var card);
                gridContainer.Add(itemElement);

                if (i == 0)
                {
                    firstCard = card;
                }
            }

            // Update count
            if (lblItemCount != null)
                lblItemCount.text = $"{categoryItems.Count} / {maxSlots} vật phẩm";

            // Automatically select first item
            SelectItem(categoryItems[0], firstCard);
        }
        else
        {
            if (lblItemCount != null)
                lblItemCount.text = $"0 / {maxSlots} vật phẩm";

            ShowEmptyDetails();
        }
    }

    private VisualElement CreateItemElement(InventoryItem item, out VisualElement card)
    {
        // Shadow wrapper
        var shadow = new VisualElement();
        shadow.AddToClassList("inventory-item-shadow");

        // Item card
        card = new VisualElement();
        card.AddToClassList("inventory-item");

        // Icon container
        var iconContainer = new VisualElement();
        iconContainer.AddToClassList("inventory-item-icon");

        var iconLabel = new Label(item.icon);
        iconLabel.AddToClassList("inventory-item-icon-text");
        iconContainer.Add(iconLabel);

        // Name
        var nameLabel = new Label(item.name);
        nameLabel.AddToClassList("inventory-item-name");

        // Quantity
        var qtyLabel = new Label($"x{item.quantity}");
        qtyLabel.AddToClassList("inventory-item-qty");

        // Assemble
        card.Add(iconContainer);
        card.Add(nameLabel);
        card.Add(qtyLabel);
        shadow.Add(card);

        // Click handler
        var clickedCard = card;
        card.RegisterCallback<ClickEvent>(evt =>
        {
            SelectItem(item, clickedCard);
        });

        return shadow;
    }

    private void SelectItem(InventoryItem item, VisualElement cardElement)
    {
        // Unselect previous card
        if (selectedItemCard != null)
        {
            selectedItemCard.RemoveFromClassList("inventory-item--selected");
        }

        selectedItem = item;
        selectedItemCard = cardElement;

        // Select new card
        if (selectedItemCard != null)
        {
            selectedItemCard.AddToClassList("inventory-item--selected");
        }

        ShowItemDetails(item);
    }

    private void ShowItemDetails(InventoryItem item)
    {
        if (detailEmptyState != null) detailEmptyState.style.display = DisplayStyle.None;
        if (detailContent != null) detailContent.style.display = DisplayStyle.Flex;

        if (lblDetailIcon != null) lblDetailIcon.text = item.icon;
        if (lblDetailName != null) lblDetailName.text = item.name;
        if (lblDetailQty != null) lblDetailQty.text = $"x{item.quantity}";
        if (lblDetailDesc != null)
        {
            // Nếu là CON VẬT -> chèn thêm thông tin nuôi (giá / ô đất / thức ăn).
            var animalDef = YWonderLand.Managers.AnimalManager.LookupDefinition(item.id);
            lblDetailDesc.text = animalDef != null
                ? item.description + AnimalInfoText(animalDef)
                : item.description;
        }

        if (btnDetailAction != null)
        {
            btnDetailAction.text = !string.IsNullOrEmpty(item.actionText) ? item.actionText : "Sử dụng";
        }
    }

    // Thông tin nuôi cơ bản của con vật (chèn vào mô tả): giá / số ô / thức ăn chính-phụ.
    private static string AnimalInfoText(YWonderLand.Data.AnimalDefinition d)
    {
        string Food(string name, int amount) =>
            string.IsNullOrEmpty(name) ? "—" : (amount > 0 ? $"{amount}x {name}" : name);

        return $"\n\nThông tin nuôi:"
             + $"\nGiá mua: {d.buyPrice} POS   |   Cần: {d.penSlots} ô đất"
             + $"\nThức ăn chính: {Food(d.foodMainName, d.foodMainAmount)}"
             + $"\nThức ăn phụ: {Food(d.foodAltName, d.foodAltAmount)}";
    }

    private void ShowEmptyDetails()
    {
        selectedItem = null;
        if (selectedItemCard != null)
        {
            selectedItemCard.RemoveFromClassList("inventory-item--selected");
            selectedItemCard = null;
        }

        if (detailEmptyState != null) detailEmptyState.style.display = DisplayStyle.Flex;
        if (detailContent != null) detailContent.style.display = DisplayStyle.None;
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
            UIPopupTracker.SetOpen(this, true);
            RefreshGrid();
            Debug.Log("[Inventory] Popup opened");
        }
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            UIPopupTracker.SetOpen(this, false);
            Debug.Log("[Inventory] Popup closed");
        }
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    /// <summary>
    /// Open inventory directly at a specific tab. Used by tutorial system.
    /// Valid tab names: "tools", "materials", "seeds", "food", "outfit", "special"
    /// </summary>
    public void ShowAtTab(string tabName)
    {
        Show();

        // Map tab name to button + display name
        switch (tabName)
        {
            case "seeds":
                SetActiveTab(tabSeeds, "seeds", "H\u1ea1t gi\u1ed1ng");
                break;
            case "tools":
                SetActiveTab(tabTools, "tools", "D\u1ee5ng c\u1ee5");
                break;
            case "materials":
                SetActiveTab(tabMaterials, "materials", "Nguy\u00ean li\u1ec7u");
                break;
            case "food":
                SetActiveTab(tabFood, "food", "Th\u1ef1c ph\u1ea9m");
                break;
            case "outfit":
                SetActiveTab(tabOutfit, "outfit", "Trang ph\u1ee5c");
                break;
            case "animals":
                SetActiveTab(tabAnimals, "animals", "Th\u00fa nu\u00f4i");
                break;
            case "special":
                SetActiveTab(tabSpecial, "special", "\u0110\u1eb7c bi\u1ec7t");
                break;
            default:
                Debug.LogWarning($"[Inventory] Unknown tab: {tabName}");
                break;
        }

        Debug.Log($"[Inventory] Opened at tab: {tabName}");
    }

}
