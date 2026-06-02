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

    // Mock inventory data
    private Dictionary<string, List<InventoryItem>> mockData;

    private struct InventoryItem
    {
        public string icon;
        public string name;
        public int quantity;
        public string description;
        public string actionText;
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

        // Detail Action clicks
        btnDetailAction?.RegisterCallback<ClickEvent>(evt =>
        {
            if (selectedItem.HasValue)
            {
                Debug.Log($"[Inventory] Executed action '{selectedItem.Value.actionText}' on item: {selectedItem.Value.name}");
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

        // Get items for active category
        if (mockData.TryGetValue(activeCategory, out var items) && items.Count > 0)
        {
            VisualElement firstCard = null;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var itemElement = CreateItemElement(item, out var card);
                gridContainer.Add(itemElement);

                if (i == 0)
                {
                    firstCard = card;
                }
            }

            // Update count
            if (lblItemCount != null)
                lblItemCount.text = $"{items.Count} / {MAX_SLOTS} vật phẩm";

            // Automatically select first item
            SelectItem(items[0], firstCard);
        }
        else
        {
            if (lblItemCount != null)
                lblItemCount.text = $"0 / {MAX_SLOTS} vật phẩm";

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
        if (lblDetailDesc != null) lblDetailDesc.text = item.description;

        if (btnDetailAction != null)
        {
            btnDetailAction.text = !string.IsNullOrEmpty(item.actionText) ? item.actionText : "Sử dụng";
        }
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
                new InventoryItem { icon = "⛏", name = "Cuốc Lv1", quantity = 1, description = "Dụng cụ dùng để cuốc đất gieo hạt, khai hoang đất trồng trọt trù phú.", actionText = "Trang bị" },
                new InventoryItem { icon = "🎣", name = "Cần câu", quantity = 1, description = "Cần câu bằng tre dẻo dai. Dùng để câu các loài cá sông nước ngọt.", actionText = "Trang bị" },
                new InventoryItem { icon = "🪓", name = "Rìu gỗ", quantity = 1, description = "Rìu gỗ thô sơ nhưng chắc chắn. Thích hợp để đốn củi và chặt cây nhỏ.", actionText = "Trang bị" },
                new InventoryItem { icon = "🪣", name = "Xô tưới", quantity = 2, description = "Xô đựng nước làm vườn. Dùng để tưới nước giúp cây trồng mau lớn.", actionText = "Trang bị" },
            },
            ["materials"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🪵", name = "Gỗ", quantity = 15, description = "Thân cây gỗ chắc nịch. Nguyên liệu cơ bản dùng để chế tạo và xây dựng.", actionText = "Chế tạo" },
                new InventoryItem { icon = "🪨", name = "Đá", quantity = 8, description = "Đá cuội nhặt ven sông. Có ích trong việc nâng cấp nhà cửa và công cụ.", actionText = "Chế tạo" },
                new InventoryItem { icon = "⛓", name = "Sắt", quantity = 3, description = "Quặng sắt đã tinh chế. Cần thiết để rèn các công cụ cao cấp hơn.", actionText = "Chế tạo" },
                new InventoryItem { icon = "🧱", name = "Gạch", quantity = 10, description = "Gạch đất nung chắc chắn. Thích hợp để làm hàng rào hoặc lát nền.", actionText = "Chế tạo" },
                new InventoryItem { icon = "🪢", name = "Dây thừng", quantity = 5, description = "Dây thừng bện từ sợi đay. Rất dai và hữu dụng trong nhiều việc.", actionText = "Chế tạo" },
            },
            ["seeds"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🌾", name = "Hạt lúa", quantity = 20, description = "Hạt giống lúa nước. Gieo hạt trên đất trồng và nhớ tưới nước nhé.", actionText = "Gieo hạt" },
                new InventoryItem { icon = "🌻", name = "Hạt hoa", quantity = 10, description = "Hạt giống hoa hướng dương rực rỡ. Giúp trang trí nông trại thêm đẹp.", actionText = "Gieo hạt" },
                new InventoryItem { icon = "🥕", name = "Hạt cà rốt", quantity = 8, description = "Hạt giống cà rốt ngon ngọt. Thời gian sinh trưởng trung bình.", actionText = "Gieo hạt" },
                new InventoryItem { icon = "🍅", name = "Hạt cà chua", quantity = 5, description = "Hạt giống cà chua chín đỏ. Cho sản lượng thu hoạch cao.", actionText = "Gieo hạt" },
            },
            ["food"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🍞", name = "Bánh mì", quantity = 3, description = "Bánh mì thơm ngon vừa mới nướng. Hồi phục 20 thể lực khi ăn.", actionText = "Ăn" },
                new InventoryItem { icon = "🥛", name = "Sữa tươi", quantity = 2, description = "Sữa bò nguyên chất vừa vắt sáng nay. Cung cấp năng lượng dồi dào.", actionText = "Uống" },
                new InventoryItem { icon = "🍎", name = "Táo đỏ", quantity = 6, description = "Táo chín đỏ ngọt lịm từ vườn nhà. Giúp phục hồi sinh lực tức thì.", actionText = "Ăn" },
            },
            ["outfit"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "👒", name = "Nón rơm", quantity = 1, description = "Chiếc nón rơm rộng vành. Che nắng cực tốt khi đi làm vườn.", actionText = "Mặc" },
                new InventoryItem { icon = "👕", name = "Áo nông dân", quantity = 1, description = "Trang phục lao động bằng vải bố bền bỉ, thấm hút mồ hôi tốt.", actionText = "Mặc" },
            },
            ["special"] = new List<InventoryItem>
            {
                new InventoryItem { icon = "🎫", name = "Vé sự kiện", quantity = 1, description = "Tấm vé tham gia lễ hội nông trang đặc biệt. Đừng làm mất nó nhé!", actionText = "Sử dụng" },
                new InventoryItem { icon = "🎁", name = "Hộp quà", quantity = 2, description = "Hộp quà may mắn từ thị trưởng. Mở ra để nhận những vật phẩm ngẫu nhiên.", actionText = "Mở" },
                new InventoryItem { icon = "💎", name = "Kim cương", quantity = 1, description = "Viên kim cương lấp lánh cực kỳ quý hiếm. Có thể bán được rất nhiều xu.", actionText = "Bán" },
            },
        };
    }
}
