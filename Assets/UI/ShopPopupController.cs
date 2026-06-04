using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Shop Popup.
/// Reusable template for all 12 shops — just pass different ShopData.
/// Supports Buy mode + Sell mode with category filters.
/// Usage: Call Show(shopData) to open with specific shop configuration.
/// </summary>
public class ShopPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument shopDocument;

    // Elements
    private VisualElement overlay;
    private Label shopTitle;
    private Button btnClose;
    private VisualElement gridContainer;
    private Label lblBalance;
    private Label lblMode;

    // Mode tabs
    private Button tabBuy;
    private Button tabSell;

    // Filters
    private Button filterAll;
    private Button filterSeeds;
    private Button filterAnimals;
    private Button filterTools;
    private Button filterItems;
    private Button activeFilter;

    // Detail panel
    private VisualElement detailEmpty;
    private VisualElement detailContent;
    private Label lblShopIcon;
    private Label lblShopName;
    private Label lblShopPrice;
    private Label lblShopDesc;
    private Label lblQty;
    private Label lblTotal;
    private Button btnQtyMinus;
    private Button btnQtyPlus;
    private Button btnAction;

    // State
    private bool isSellMode = false;
    private string activeCategory = "all";
    private int selectedQty = 1;
    private VisualElement selectedItemCard;
    private ShopItem? selectedItem;
    private int playerBalance = 5000; // Mock balance

    // Current shop data
    private ShopData currentShop;

    // ── Data Structures ──

    [System.Serializable]
    public struct ShopItem
    {
        public string icon;
        public string name;
        public int price;
        public string description;
        public string category; // "seeds", "animals", "tools", "items"
        public bool canSell;
        public int sellPrice;
    }

    [System.Serializable]
    public class ShopData
    {
        public string shopName;
        public List<ShopItem> buyItems;
        public List<ShopItem> sellItems;
        public bool hasSellTab;
    }

    private void Awake()
    {
        if (shopDocument == null)
            shopDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = shopDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("ShopOverlay");
        shopTitle = root.Q<Label>("ShopTitle");
        btnClose = root.Q<Button>("BtnCloseShop");
        gridContainer = root.Q<VisualElement>("ShopGrid");
        lblBalance = root.Q<Label>("LblShopBalance");
        lblMode = root.Q<Label>("LblShopMode");

        // Mode tabs
        tabBuy = root.Q<Button>("TabBuy");
        tabSell = root.Q<Button>("TabSell");

        // Filters
        filterAll = root.Q<Button>("FilterAll");
        filterSeeds = root.Q<Button>("FilterSeeds");
        filterAnimals = root.Q<Button>("FilterAnimals");
        filterTools = root.Q<Button>("FilterTools");
        filterItems = root.Q<Button>("FilterItems");

        // Detail
        detailEmpty = root.Q<Label>("ShopDetailEmpty");
        detailContent = root.Q<VisualElement>("ShopDetailContent");
        lblShopIcon = root.Q<Label>("LblShopIcon");
        lblShopName = root.Q<Label>("LblShopName");
        lblShopPrice = root.Q<Label>("LblShopPrice");
        lblShopDesc = root.Q<Label>("LblShopDesc");
        lblQty = root.Q<Label>("LblQty");
        lblTotal = root.Q<Label>("LblTotal");
        btnQtyMinus = root.Q<Button>("BtnQtyMinus");
        btnQtyPlus = root.Q<Button>("BtnQtyPlus");
        btnAction = root.Q<Button>("BtnShopAction");
    }

    private void RegisterCallbacks()
    {
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay) Hide();
        });

        // Mode tabs
        tabBuy?.RegisterCallback<ClickEvent>(evt => SetMode(false));
        tabSell?.RegisterCallback<ClickEvent>(evt => SetMode(true));

        // Filters
        filterAll?.RegisterCallback<ClickEvent>(evt => SetFilter(filterAll, "all"));
        filterSeeds?.RegisterCallback<ClickEvent>(evt => SetFilter(filterSeeds, "seeds"));
        filterAnimals?.RegisterCallback<ClickEvent>(evt => SetFilter(filterAnimals, "animals"));
        filterTools?.RegisterCallback<ClickEvent>(evt => SetFilter(filterTools, "tools"));
        filterItems?.RegisterCallback<ClickEvent>(evt => SetFilter(filterItems, "items"));

        // Quantity
        btnQtyMinus?.RegisterCallback<ClickEvent>(evt => ChangeQty(-1));
        btnQtyPlus?.RegisterCallback<ClickEvent>(evt => ChangeQty(1));

        // Action (Buy / Sell)
        btnAction?.RegisterCallback<ClickEvent>(evt => OnActionClicked());
    }

    // ── Public API ──

    /// <summary>
    /// Open the shop with specific data. Each NPC shop passes its own ShopData.
    /// </summary>
    public void Show(ShopData data)
    {
        if (overlay == null || data == null) return;

        currentShop = data;
        isSellMode = false;
        selectedQty = 1;
        selectedItem = null;
        selectedItemCard = null;

        // Set title
        if (shopTitle != null) shopTitle.text = data.shopName;

        // Show/hide sell tab
        if (tabSell != null)
        {
            tabSell.style.display = data.hasSellTab ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Reset to Buy mode
        SetMode(false);
        SetFilter(filterAll, "all");
        UpdateBalance();

        overlay.style.display = DisplayStyle.Flex;
        Debug.Log($"[Shop] Opened: {data.shopName}");
    }

    /// <summary>
    /// Open the shop with default mock data (for testing).
    /// </summary>
    public void Show()
    {
        Show(CreateMockShopData());
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[Shop] Closed");
        }
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Mode & Filter ──

    private void SetMode(bool sellMode)
    {
        isSellMode = sellMode;

        // Update tab styles
        tabBuy?.RemoveFromClassList("shop-mode-tab--active");
        tabSell?.RemoveFromClassList("shop-mode-tab--active");

        if (isSellMode)
        {
            tabSell?.AddToClassList("shop-mode-tab--active");
            if (lblMode != null) lblMode.text = "Chế độ: Bán";
            btnAction?.AddToClassList("shop-btn-action--sell");
        }
        else
        {
            tabBuy?.AddToClassList("shop-mode-tab--active");
            if (lblMode != null) lblMode.text = "Chế độ: Mua";
            btnAction?.RemoveFromClassList("shop-btn-action--sell");
        }

        RefreshGrid();
        ShowEmptyDetails();
    }

    private void SetFilter(Button filterBtn, string category)
    {
        // Update filter styles
        activeFilter?.RemoveFromClassList("shop-filter--active");
        activeFilter = filterBtn;
        activeFilter?.AddToClassList("shop-filter--active");
        activeCategory = category;

        RefreshGrid();
        ShowEmptyDetails();
    }

    // ── Grid ──

    private void RefreshGrid()
    {
        if (gridContainer == null || currentShop == null) return;
        gridContainer.Clear();

        var items = isSellMode ? currentShop.sellItems : currentShop.buyItems;
        if (items == null) return;

        // Filter by category
        var filtered = new List<ShopItem>();
        foreach (var item in items)
        {
            if (activeCategory == "all" || item.category == activeCategory)
            {
                filtered.Add(item);
            }
        }

        if (filtered.Count == 0)
        {
            ShowEmptyDetails();
            return;
        }

        VisualElement firstCard = null;
        for (int i = 0; i < filtered.Count; i++)
        {
            var item = filtered[i];
            var element = CreateItemCard(item, out var card);
            gridContainer.Add(element);
            if (i == 0) firstCard = card;
        }

        // Auto-select first
        SelectItem(filtered[0], firstCard);
    }

    private VisualElement CreateItemCard(ShopItem item, out VisualElement card)
    {
        var shadow = new VisualElement();
        shadow.AddToClassList("shop-item-shadow");

        card = new VisualElement();
        card.AddToClassList("shop-item");

        // Icon
        var iconWrap = new VisualElement();
        iconWrap.AddToClassList("shop-item-icon");
        var iconLabel = new Label(item.icon);
        iconLabel.AddToClassList("shop-item-icon-text");
        iconWrap.Add(iconLabel);

        // Name
        var nameLabel = new Label(item.name);
        nameLabel.AddToClassList("shop-item-name");

        // Price
        int displayPrice = isSellMode ? item.sellPrice : item.price;
        var priceLabel = new Label($"{displayPrice} POS");
        priceLabel.AddToClassList("shop-item-price");

        card.Add(iconWrap);
        card.Add(nameLabel);
        card.Add(priceLabel);
        shadow.Add(card);

        // Click
        var clickCard = card;
        card.RegisterCallback<ClickEvent>(evt => SelectItem(item, clickCard));

        return shadow;
    }

    // ── Selection & Detail ──

    private void SelectItem(ShopItem item, VisualElement cardElement)
    {
        // Unselect previous
        selectedItemCard?.RemoveFromClassList("shop-item--selected");
        selectedItem = item;
        selectedItemCard = cardElement;
        selectedItemCard?.AddToClassList("shop-item--selected");

        selectedQty = 1;
        ShowItemDetails(item);
    }

    private void ShowItemDetails(ShopItem item)
    {
        if (detailEmpty != null) detailEmpty.style.display = DisplayStyle.None;
        if (detailContent != null) detailContent.style.display = DisplayStyle.Flex;

        if (lblShopIcon != null) lblShopIcon.text = item.icon;
        if (lblShopName != null) lblShopName.text = item.name;
        if (lblShopDesc != null) lblShopDesc.text = item.description;

        int unitPrice = isSellMode ? item.sellPrice : item.price;
        if (lblShopPrice != null) lblShopPrice.text = $"{unitPrice} POS";

        UpdateQtyDisplay(unitPrice);

        if (btnAction != null)
        {
            btnAction.text = isSellMode ? "BÁN" : "MUA";
        }
    }

    private void ShowEmptyDetails()
    {
        selectedItem = null;
        selectedItemCard?.RemoveFromClassList("shop-item--selected");
        selectedItemCard = null;

        if (detailEmpty != null) detailEmpty.style.display = DisplayStyle.Flex;
        if (detailContent != null) detailContent.style.display = DisplayStyle.None;
    }

    // ── Quantity ──

    private void ChangeQty(int delta)
    {
        selectedQty = Mathf.Max(1, selectedQty + delta);

        if (selectedItem.HasValue)
        {
            int unitPrice = isSellMode ? selectedItem.Value.sellPrice : selectedItem.Value.price;
            UpdateQtyDisplay(unitPrice);
        }
    }

    private void UpdateQtyDisplay(int unitPrice)
    {
        if (lblQty != null) lblQty.text = selectedQty.ToString();
        int total = unitPrice * selectedQty;
        if (lblTotal != null) lblTotal.text = $"{total} POS";
    }

    // ── Action (Buy / Sell) ──

    private void OnActionClicked()
    {
        if (!selectedItem.HasValue) return;

        var item = selectedItem.Value;
        int unitPrice = isSellMode ? item.sellPrice : item.price;
        int totalCost = unitPrice * selectedQty;

        if (!isSellMode)
        {
            // Buy
            if (totalCost > playerBalance)
            {
                Debug.Log($"[Shop] Không đủ POS! Cần {totalCost}, có {playerBalance}");
                return;
            }

            playerBalance -= totalCost;
            Debug.Log($"[Shop] Mua {selectedQty}x {item.name} — Trừ {totalCost} POS. Còn {playerBalance} POS");
        }
        else
        {
            // Sell
            playerBalance += totalCost;
            Debug.Log($"[Shop] Bán {selectedQty}x {item.name} — Nhận {totalCost} POS. Còn {playerBalance} POS");
        }

        UpdateBalance();
    }

    private void UpdateBalance()
    {
        if (lblBalance != null)
        {
            lblBalance.text = $"💰 Số dư: {playerBalance:N0} POS";
        }
    }

    // ── Mock Data ──

    private ShopData CreateMockShopData()
    {
        return new ShopData
        {
            shopName = "HAI LÚA — VẬT TƯ NÔNG TRẠI",
            hasSellTab = false,
            buyItems = new List<ShopItem>
            {
                new ShopItem { icon = "🥕", name = "Hạt cà rốt", price = 10, description = "Hạt giống cà rốt. Thu hoạch sau 24h. Tưới mỗi 10h.", category = "seeds", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "🥬", name = "Hạt cải", price = 15, description = "Hạt giống rau cải xanh tươi. Sản lượng cao.", category = "seeds", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "🍉", name = "Hạt dưa hấu", price = 30, description = "Hạt giống dưa hấu ngọt. Chu kỳ dài hơn nhưng lời nhiều.", category = "seeds", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "🌽", name = "Hạt bắp", price = 20, description = "Hạt giống bắp ngô vàng. Trồng dễ, năng suất ổn.", category = "seeds", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "🎃", name = "Hạt bí ngô", price = 25, description = "Hạt giống bí ngô mùa event. Giá trị thu hoạch cao.", category = "seeds", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "🌿", name = "Cỏ voi", price = 5, description = "Cỏ voi làm thức ăn cho vật nuôi. Gieo nhanh, thu hoạch lẹ.", category = "seeds", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "🧪", name = "Phân bón", price = 50, description = "Phân bón tăng tốc sinh trưởng cây trồng. Giảm 50% thời gian.", category = "items", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "💉", name = "Vắc-xin", price = 80, description = "Vắc-xin kháng bệnh cho vật nuôi. Phòng bệnh 7 ngày.", category = "items", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "💊", name = "Thuốc trị", price = 100, description = "Thuốc điều trị cho vật nuôi đã bị bệnh.", category = "items", canSell = false, sellPrice = 0 },
                new ShopItem { icon = "🪱", name = "Mồi câu", price = 20, description = "Mồi câu cá. Tăng 20% tỉ lệ câu được cá hiếm.", category = "items", canSell = false, sellPrice = 0 },
            },
            sellItems = null,
        };
    }
}
