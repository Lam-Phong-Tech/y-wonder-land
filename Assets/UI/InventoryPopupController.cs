using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Inventory Popup.
/// Manages category tabs and item grid display.
/// </summary>
public class InventoryPopupController : MonoBehaviour
{
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
    private Button tabSpecial;

    private Button activeTab;
    private string activeCategory = "tools";

    // Max inventory slots
    private const int MAX_SLOTS = 50;

    // Mock inventory data
    private Dictionary<string, List<InventoryItem>> mockData;

    private struct InventoryItem
    {
        public string icon;
        public string name;
        public int quantity;
    }

    private void Awake()
    {
        if (inventoryDocument == null)
            inventoryDocument = GetComponent<UIDocument>();

        InitMockData();
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
        tabSpecial = root.Q<Button>("TabSpecial");

        // Register callbacks
        RegisterCallbacks();

        // Set default tab
        activeTab = tabTools;
        SetActiveTab(tabTools, "tools", "Dụng cụ");

        // Start hidden
        Hide();
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
        tabSpecial?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabSpecial, "special", "Đặc biệt"));
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

        // Get items for active category
        if (mockData.TryGetValue(activeCategory, out var items))
        {
            foreach (var item in items)
            {
                var itemElement = CreateItemElement(item);
                gridContainer.Add(itemElement);
            }

            // Update count
            if (lblItemCount != null)
                lblItemCount.text = $"{items.Count} / {MAX_SLOTS} vật phẩm";
        }
        else
        {
            if (lblItemCount != null)
                lblItemCount.text = $"0 / {MAX_SLOTS} vật phẩm";
        }
    }

    private VisualElement CreateItemElement(InventoryItem item)
    {
        // Shadow wrapper
        var shadow = new VisualElement();
        shadow.AddToClassList("inventory-item-shadow");

        // Item card
        var card = new VisualElement();
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
        card.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log($"[Inventory] Item clicked: {item.name}");
            // TODO: Show item detail popup
        });

        return shadow;
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
            RefreshGrid();
            Debug.Log("[Inventory] Popup opened");
        }
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[Inventory] Popup closed");
        }
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Mock Data ──

    private void InitMockData()
    {
        mockData = new Dictionary<string, List<InventoryItem>>
        {
            ["tools"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "⛏", name = "Cuốc Lv1", quantity = 1 },
                new InventoryItem { icon = "🎣", name = "Cần câu", quantity = 1 },
                new InventoryItem { icon = "🪓", name = "Rìu gỗ", quantity = 1 },
                new InventoryItem { icon = "🪣", name = "Xô tưới", quantity = 2 },
            },
            ["materials"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🪵", name = "Gỗ", quantity = 15 },
                new InventoryItem { icon = "🪨", name = "Đá", quantity = 8 },
                new InventoryItem { icon = "⛓", name = "Sắt", quantity = 3 },
                new InventoryItem { icon = "🧱", name = "Gạch", quantity = 10 },
                new InventoryItem { icon = "🪢", name = "Dây thừng", quantity = 5 },
            },
            ["seeds"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🌾", name = "Hạt lúa", quantity = 20 },
                new InventoryItem { icon = "🌻", name = "Hạt hoa", quantity = 10 },
                new InventoryItem { icon = "🥕", name = "Hạt cà rốt", quantity = 8 },
                new InventoryItem { icon = "🍅", name = "Hạt cà chua", quantity = 5 },
            },
            ["food"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🍞", name = "Bánh mì", quantity = 3 },
                new InventoryItem { icon = "🥛", name = "Sữa tươi", quantity = 2 },
                new InventoryItem { icon = "🍎", name = "Táo đỏ", quantity = 6 },
            },
            ["outfit"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "👒", name = "Nón rơm", quantity = 1 },
                new InventoryItem { icon = "👕", name = "Áo nông dân", quantity = 1 },
            },
            ["special"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🎫", name = "Vé sự kiện", quantity = 1 },
                new InventoryItem { icon = "🎁", name = "Hộp quà", quantity = 2 },
                new InventoryItem { icon = "💎", name = "Kim cương", quantity = 1 },
            },
        };
    }
}
