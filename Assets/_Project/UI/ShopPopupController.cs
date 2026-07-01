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
    public static event System.Action<string, int> OnItemSold;

    [Header("References")]
    [SerializeField] private UIDocument shopDocument;

    // Elements
    private VisualElement overlay;
    private Label shopTitle;
    private Button btnClose;
    private VisualElement gridContainer;
    private Label lblBalance;

    // Mode tabs
    private Button tabBuy;
    private Button tabSell;
    private VisualElement modeSection; // khối "CHẾ ĐỘ" (label + 2 tab) — ẩn khi mở single-mode

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
    private VisualElement detailIconWrap;
    private Image detailIconImage;
    private Label lblShopIcon;
    private Label lblShopName;
    private Label lblShopPrice;
    private Label lblShopDesc;
    private Label lblOwned;
    private TextField txtQty;
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

    // Current shop data
    private ShopData currentShop;

    // Whitelist ID hàng shop chấp nhận THU MUA (null/rỗng = thu mua mọi thứ bán được trong túi).
    private List<string> sellFilterIds;

    // Database
    private YWonderLand.Data.ItemDatabase itemDatabase;

    // ── Data Structures ──

    [System.Serializable]
    public struct ShopItem
    {
        public string id;
        public string icon;
        public Sprite iconSprite;
        public Texture2D iconTexture;
        public string name;
        public int price;
        public string description;
        public string category; // "seeds", "animals", "tools", "items"
        public bool canSell;
        public int sellPrice;
        public int maxAvailable;
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

        itemDatabase = Resources.Load<YWonderLand.Data.ItemDatabase>("ItemDatabase");
        if (itemDatabase == null) Debug.LogError("[Shop] ItemDatabase not found!");
    }

    private void OnEnable()
    {
        var root = shopDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();

        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            YWonderLand.Managers.EconomyManager.Instance.OnPOSChanged += OnBalanceChanged;
        }
    }

    private void OnDisable()
    {
        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            YWonderLand.Managers.EconomyManager.Instance.OnPOSChanged -= OnBalanceChanged;
        }
        // Gỡ khỏi UIPopupTracker phòng khi bị tắt/destroy lúc đang mở (vd đổi đảo)
        // mà chưa kịp Hide() -> tránh chuột kẹt + tương tác thế giới chết.
        UIPopupTracker.SetOpen(this, false);
    }

    private void OnBalanceChanged(long newBalance)
    {
        UpdateBalance();
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("ShopOverlay");
        if (overlay == null) Debug.LogError("[ShopPopup] LỖI: Không tìm thấy 'ShopOverlay' trong UXML!");

        shopTitle = root.Q<Label>("ShopTitle");
        btnClose = root.Q<Button>("BtnCloseShop");
        gridContainer = root.Q<VisualElement>("ShopGrid");
        lblBalance = root.Q<Label>("LblShopBalance");

        // Mode tabs
        tabBuy = root.Q<Button>("TabBuy");
        tabSell = root.Q<Button>("TabSell");
        modeSection = root.Q<VisualElement>("ModeSection");

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
        detailIconWrap = lblShopIcon?.parent;
        detailIconImage = root.Q<Image>("ShopDetailIconImage");
        if (detailIconWrap != null && detailIconImage == null)
        {
            detailIconImage = new Image
            {
                name = "ShopDetailIconImage",
                scaleMode = ScaleMode.ScaleToFit
            };
            detailIconImage.AddToClassList("shop-detail-icon-image");
            detailIconWrap.Add(detailIconImage);
        }
        lblShopName = root.Q<Label>("LblShopName");
        lblShopPrice = root.Q<Label>("LblShopPrice");
        lblShopDesc = root.Q<Label>("LblShopDesc");
        lblOwned = root.Q<Label>("LblOwned");
        txtQty = root.Q<TextField>("TxtQty");
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
        txtQty?.RegisterValueChangedCallback(evt => OnQtyTextChanged(evt.newValue));

        // Action (Buy / Sell)
        btnAction?.RegisterCallback<ClickEvent>(evt => OnActionClicked());
    }

    // ── Public API ──

    /// <summary>
    /// Open the shop with specific data. Each NPC shop passes its own ShopData.
    /// </summary>
    public void Show(ShopData data)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log("[ShopPopup] GameObject đang tắt, tiến hành bật lại!");
            gameObject.SetActive(true);
        }

        if (overlay == null && shopDocument != null && shopDocument.rootVisualElement != null)
        {
            Debug.Log("[ShopPopup] Tự động load lại UI Elements do bị null...");
            QueryElements(shopDocument.rootVisualElement);
            RegisterCallbacks();
        }

        Debug.Log($"[ShopPopup] Đang gọi Show(ShopData). overlay null? {overlay == null}. data null? {data == null}");
        if (overlay == null || data == null) return;

        currentShop = data;
        isSellMode = false;
        selectedQty = 1;
        selectedItem = null;
        selectedItemCard = null;

        // Set title
        if (shopTitle != null) shopTitle.text = data.shopName;

        // Reset hiển thị 2 tab về mặc định (phòng lần trước bị NPC Mua/Bán ẩn bớt 1 tab)
        if (tabBuy != null) tabBuy.style.display = DisplayStyle.Flex;
        if (tabSell != null)
        {
            tabSell.style.display = data.hasSellTab ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Reset to Buy mode
        SetMode(false);
        UpdateFilterVisibility(); // chỉ hiện tab filter có hàng trong shop này (#1)
        SetFilter(filterAll, "all");
        UpdateBalance();

        overlay.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        Debug.Log($"[Shop] Opened: {data.shopName}");
    }

    /// <summary>
    /// Open the shop with default mock data (for testing).
    /// </summary>
    public void Show()
    {
        sellFilterIds = null; // luồng mock/legacy: thu mua mọi thứ bán được
        Show(CreateMockShopData());
    }

    /// <summary>Chế độ truy cập quầy: cả hai / chỉ Mua / chỉ Bán.</summary>
    public enum ShopAccessMode { Both, BuyOnly, SellOnly }

    /// <summary>
    /// Mở shop theo chế độ truy cập — TÁCH RIÊNG quầy Mua và quầy Bán.
    /// NPC Mua dùng BuyOnly (ẩn tab Bán), NPC Bán dùng SellOnly (ẩn tab Mua).
    /// </summary>
    public void Show(ShopAccessMode mode)
    {
        Show(); // dựng mock data + reset về Mua
        ApplyAccessMode(mode);
    }

    private void ApplyAccessMode(ShopAccessMode mode)
    {
        bool canSell = currentShop != null && currentShop.hasSellTab;

        // Mở bằng NPC Mua/Bán riêng -> ẨN HẲN khối "CHẾ ĐỘ" (label + 2 tab) cho gọn mắt.
        // Chỉ giữ toggle khi mở chế độ Both (vd phím tắt cũ / NPC gộp).
        if (modeSection != null)
            modeSection.style.display = mode == ShopAccessMode.Both ? DisplayStyle.Flex : DisplayStyle.None;

        if (mode == ShopAccessMode.SellOnly && canSell)
        {
            SetMode(true); // hiển thị danh sách Bán
            if (shopTitle != null) shopTitle.text = "BÁN ĐỒ";
        }
        else
        {
            SetMode(false); // danh sách Mua
            if (shopTitle != null && mode == ShopAccessMode.BuyOnly) shopTitle.text = "CỬA HÀNG";
        }
    }

    /// <summary>
    /// Mở shop theo 1 ShopDefinition (asset) — catalog riêng của từng NPC.
    /// Giá/tên/icon tra từ ItemDatabase (nguồn duy nhất). Đây là cổng chính cho hệ NPC shop data-driven.
    /// </summary>
    public void Show(YWonderLand.Data.ShopDefinition def)
    {
        if (def == null) { Show(); return; }

        // Whitelist thu mua: rỗng = thu mua mọi thứ bán được.
        sellFilterIds = (def.sellItemIds != null && def.sellItemIds.Count > 0) ? def.sellItemIds : null;

        Show(BuildShopDataFrom(def));      // set currentShop + title + tab + về chế độ Mua
        ApplyAccessMode(MapAccessMode(def.accessMode));

        // Giữ TÊN RIÊNG của shop trên tiêu đề (ApplyAccessMode vừa ghi đè thành "CỬA HÀNG"/"BÁN ĐỒ"
        // cho luồng NPC Mua/Bán cũ — ở đây có tên riêng nên đặt lại để phân biệt từng cửa hàng).
        if (shopTitle != null && !string.IsNullOrEmpty(def.shopName))
            shopTitle.text = def.shopName;
    }

    private ShopAccessMode MapAccessMode(YWonderLand.Data.ShopDefinition.AccessMode m)
    {
        switch (m)
        {
            case YWonderLand.Data.ShopDefinition.AccessMode.BuyOnly:  return ShopAccessMode.BuyOnly;
            case YWonderLand.Data.ShopDefinition.AccessMode.SellOnly: return ShopAccessMode.SellOnly;
            default:                                                  return ShopAccessMode.Both;
        }
    }

    // Dựng ShopData cho popup từ ShopDefinition: chỉ tra ID -> lấy giá/tên/icon trong ItemDatabase.
    private ShopData BuildShopDataFrom(YWonderLand.Data.ShopDefinition def)
    {
        var data = new ShopData
        {
            shopName = def.shopName,
            hasSellTab = def.HasSellTab,
            buyItems = new List<ShopItem>(),
            sellItems = new List<ShopItem>() // tab Bán nạp động từ túi đồ (lọc theo sellFilterIds)
        };

        if (itemDatabase != null && def.buyItemIds != null)
        {
            foreach (var id in def.buyItemIds)
            {
                var idef = itemDatabase.GetItem(id);
                if (idef == null)
                {
                    Debug.LogWarning($"[Shop] '{def.shopName}': bỏ qua ID không có trong ItemDatabase: '{id}'");
                    continue;
                }
                data.buyItems.Add(new ShopItem
                {
                    id = idef.id,
                    icon = !string.IsNullOrEmpty(idef.iconEmoji) ? idef.iconEmoji : "📦",
                    iconSprite = idef.iconSprite,
                    iconTexture = idef.iconTexture,
                    name = idef.itemName,
                    price = idef.buyPrice,
                    description = idef.description,
                    category = idef.category,
                    canSell = idef.canSell,
                    sellPrice = idef.sellPrice
                });
            }
        }
        return data;
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            UIPopupTracker.SetOpen(this, false);
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
            btnAction?.AddToClassList("shop-btn-action--sell");
        }
        else
        {
            tabBuy?.AddToClassList("shop-mode-tab--active");
            btnAction?.RemoveFromClassList("shop-btn-action--sell");
        }

        RefreshGrid();
        ShowEmptyDetails();
    }

    // #1: Chỉ hiện các tab filter có hàng trong shop hiện tại (gom category từ buy items + whitelist bán).
    private void UpdateFilterVisibility()
    {
        var present = new HashSet<string>();
        if (currentShop != null && currentShop.buyItems != null)
            foreach (var it in currentShop.buyItems)
                if (!string.IsNullOrEmpty(it.category)) present.Add(it.category); // ShopItem là struct → không so null
        if (sellFilterIds != null && itemDatabase != null)
            foreach (var id in sellFilterIds)
            {
                var d = itemDatabase.GetItem(id);
                if (d != null && !string.IsNullOrEmpty(d.category)) present.Add(d.category);
            }

        SetFilterBtnVisible(filterSeeds, present.Contains("seeds"));
        SetFilterBtnVisible(filterAnimals, present.Contains("animals"));
        SetFilterBtnVisible(filterTools, present.Contains("tools"));
        SetFilterBtnVisible(filterItems, present.Contains("items"));
        // filterAll luôn hiện (xem tất cả).
    }

    private void SetFilterBtnVisible(Button b, bool show)
    {
        if (b != null) b.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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

        List<ShopItem> itemsToDisplay = new List<ShopItem>();

        if (isSellMode)
        {
            // Load dynamically from InventoryManager
            if (YWonderLand.Managers.InventoryManager.Instance != null && itemDatabase != null)
            {
                var slots = YWonderLand.Managers.InventoryManager.Instance.GetAllSlots();
                foreach (var slot in slots)
                {
                    var def = itemDatabase.GetItem(slot.itemId);
                    if (def != null && def.canSell)
                    {
                        // Shop có whitelist -> chỉ thu mua hàng trong danh sách (vd Mini Garden chỉ thu nông sản).
                        if (sellFilterIds != null && !sellFilterIds.Contains(def.id)) continue;
                        itemsToDisplay.Add(new ShopItem
                        {
                            id = def.id,
                            icon = !string.IsNullOrEmpty(def.iconEmoji) ? def.iconEmoji : "📦",
                            iconSprite = def.iconSprite,
                            iconTexture = def.iconTexture,
                            name = def.itemName,
                            price = def.buyPrice,
                            description = def.description,
                            category = def.category,
                            canSell = true,
                            sellPrice = def.sellPrice,
                            maxAvailable = slot.quantity
                        });
                    }
                }
            }
        }
        else
        {
            itemsToDisplay = currentShop.buyItems;
        }

        if (itemsToDisplay == null) return;

        // Filter by category
        var filtered = new List<ShopItem>();
        foreach (var item in itemsToDisplay)
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
        var iconImage = new Image { scaleMode = ScaleMode.ScaleToFit };
        iconImage.AddToClassList("shop-item-icon-image");
        if (ApplyGraphicIcon(iconImage, item))
        {
            iconWrap.Add(iconImage);
        }
        else
        {
            var iconLabel = new Label(item.icon);
            iconLabel.AddToClassList("shop-item-icon-text");
            iconWrap.Add(iconLabel);
        }

        // Name
        var nameLabel = new Label(item.name);
        nameLabel.AddToClassList("shop-item-name");

        // Price
        int displayPrice = isSellMode ? item.sellPrice : item.price;
        var priceLabel = new Label($"{displayPrice} Point");
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

        bool hasGraphicIcon = ApplyGraphicIcon(detailIconImage, item);
        if (detailIconImage != null) detailIconImage.style.display = hasGraphicIcon ? DisplayStyle.Flex : DisplayStyle.None;
        if (lblShopIcon != null)
        {
            lblShopIcon.style.display = hasGraphicIcon ? DisplayStyle.None : DisplayStyle.Flex;
            lblShopIcon.text = item.icon;
        }
        if (lblShopName != null) lblShopName.text = item.name;
        if (lblShopDesc != null)
        {
            // Nếu là CON VẬT -> chèn thêm thông tin nuôi (giá / ô đất / thức ăn).
            var animalDef = YWonderLand.Managers.AnimalManager.LookupDefinition(item.id);
            lblShopDesc.text = animalDef != null
                ? item.description + AnimalInfoText(animalDef)
                : item.description;
        }

        int unitPrice = isSellMode ? item.sellPrice : item.price;
        if (lblShopPrice != null) lblShopPrice.text = $"{unitPrice} Point";

        if (lblOwned != null)
        {
            int owned = isSellMode ? item.maxAvailable : 0;
            if (!isSellMode && YWonderLand.Managers.InventoryManager.Instance != null)
            {
                owned = YWonderLand.Managers.InventoryManager.Instance.GetItemQuantity(item.id);
            }
            lblOwned.text = $"Đang có: {owned}";
        }

        UpdateQtyDisplay(unitPrice);

        if (btnAction != null)
        {
            btnAction.text = isSellMode ? "BÁN" : "MUA";
        }
    }

    // Thông tin nuôi cơ bản của con vật (chèn vào mô tả): giá / số ô / thức ăn chính-phụ.
    private static string AnimalInfoText(YWonderLand.Data.AnimalDefinition d)
    {
        string Food(string name, int amount) =>
            string.IsNullOrEmpty(name) ? "—" : (amount > 0 ? $"{amount}x {name}" : name);

        return $"\n\nThông tin nuôi:"
             + $"\nGiá mua: {d.buyPrice} Point   |   Cần: {d.penSlots} ô đất"
             + $"\nThức ăn chính: {Food(d.foodMainName, d.foodMainAmount)}"
             + $"\nThức ăn phụ: {Food(d.foodAltName, d.foodAltAmount)}";
    }

    private void ShowEmptyDetails()
    {
        selectedItem = null;
        selectedItemCard?.RemoveFromClassList("shop-item--selected");
        selectedItemCard = null;

        if (detailEmpty != null) detailEmpty.style.display = DisplayStyle.Flex;
        if (detailContent != null) detailContent.style.display = DisplayStyle.None;
    }

    private bool ApplyGraphicIcon(Image iconImage, ShopItem item)
    {
        if (iconImage == null) return false;

        iconImage.image = null;
        iconImage.sprite = null;

        Texture2D texture = ResolveIconTexture(item);
        if (texture != null)
        {
            iconImage.image = texture;
            return true;
        }

        Sprite sprite = ResolveIconSprite(item);
        if (sprite != null)
        {
            iconImage.sprite = sprite;
            return true;
        }

        return false;
    }

    private Texture2D ResolveIconTexture(ShopItem item)
    {
        if (item.iconTexture != null) return item.iconTexture;
        var def = itemDatabase != null ? itemDatabase.GetItem(item.id) : null;
        return def != null ? def.iconTexture : null;
    }

    private Sprite ResolveIconSprite(ShopItem item)
    {
        if (item.iconSprite != null) return item.iconSprite;
        var def = itemDatabase != null ? itemDatabase.GetItem(item.id) : null;
        return def != null ? def.iconSprite : null;
    }

    // ── Quantity ──

    private void ChangeQty(int delta)
    {
        if (!selectedItem.HasValue) return;
        
        int maxQty = isSellMode ? selectedItem.Value.maxAvailable : 999;
        selectedQty = Mathf.Clamp(selectedQty + delta, 1, maxQty);

        int unitPrice = isSellMode ? selectedItem.Value.sellPrice : selectedItem.Value.price;
        UpdateQtyDisplay(unitPrice);
    }

    private void OnQtyTextChanged(string newValue)
    {
        if (!selectedItem.HasValue) return;

        if (int.TryParse(newValue, out int parsed))
        {
            int maxQty = isSellMode ? selectedItem.Value.maxAvailable : 999;
            selectedQty = Mathf.Clamp(parsed, 1, maxQty);
        }
        else
        {
            selectedQty = 1;
        }

        int unitPrice = isSellMode ? selectedItem.Value.sellPrice : selectedItem.Value.price;
        int total = unitPrice * selectedQty;
        if (lblTotal != null) lblTotal.text = $"{total:N0} Point";
    }

    private void UpdateQtyDisplay(int unitPrice)
    {
        if (txtQty != null) txtQty.SetValueWithoutNotify(selectedQty.ToString());
        int total = unitPrice * selectedQty;
        if (lblTotal != null) lblTotal.text = $"{total:N0} Point";
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
            // Special handling for Animals
            if (item.category == "animals")
            {
                // Current demo flow: buy animal tokens into inventory, then place them into
                // build-mode enclosures. Pen capacity is validated when placing, not buying.
                if (!YWonderLand.Managers.EconomyManager.Instance.SpendPOS(totalCost))
                {
                    Debug.Log($"[Shop] Không đủ Point để mua thú!");
                    YWonderLand.Environment.ScreenToast.Show("Không đủ Point để mua thú!");
                    return;
                }

                YWonderLand.Managers.InventoryManager.Instance.AddItem(item.id, selectedQty);
                Debug.Log($"[Shop] Mua {selectedQty}x {item.name} vào túi đồ. Trừ {totalCost} Point.");
                YWonderLand.Environment.ScreenToast.ShowInfo($"Đã mua: {selectedQty}x {item.name}  (-{totalCost} Point)");
                YWonderLand.Managers.AudioManager.Instance?.PlaySFX("coin");
                UpdateBalance();
                ShowEmptyDetails();
                return;

                /*
                // Pre-check Point balance for the FULL selected quantity. If they want 3, they must afford 3.
                if (!YWonderLand.Managers.EconomyManager.Instance.SpendPOS(totalCost))
                {
                    Debug.Log($"[Shop] Không đủ Point để mua thú!");
                    YWonderLand.Environment.ScreenToast.Show("Không đủ Point để mua thú!");
                    return;
                }

                int spawnedCount = 0;
                for (int i = 0; i < selectedQty; i++)
                {
                    if (YWonderLand.Managers.AnimalManager.Instance != null && 
                        YWonderLand.Managers.AnimalManager.Instance.BuyAndSpawnAnimal(item.id))
                    {
                        spawnedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[Shop] Không đủ chỗ trong chuồng để chứa con thứ {i+1}!");
                        break;
                    }
                }

                if (spawnedCount == 0)
                {
                    // Mua thú nhưng chuồng đã đầy/chưa có chuồng → báo người chơi.
                    YWonderLand.Environment.ScreenToast.Show("Chuồng đã đầy! Xây thêm chuồng trước khi mua thú.");
                    return;
                }

                int actualCost = unitPrice * spawnedCount;
                YWonderLand.Managers.EconomyManager.Instance.SpendPOS(actualCost);
                Debug.Log($"[Shop] Mua {spawnedCount}x {item.name} — Trừ {actualCost} Point.");
                YWonderLand.Environment.ScreenToast.ShowInfo($"Đã mua: {spawnedCount}x {item.name}  (-{actualCost} Point)");
                YWonderLand.Managers.AudioManager.Instance?.PlaySFX("coin");
                */
            }
            else
            {
                // Standard items go to inventory
                if (!YWonderLand.Managers.EconomyManager.Instance.SpendPOS(totalCost))
                {
                    Debug.Log($"[Shop] Không đủ Point!");
                    YWonderLand.Environment.ScreenToast.Show("Không đủ Point để mua!");
                    return;
                }
                YWonderLand.Managers.InventoryManager.Instance.AddItem(item.id, selectedQty);
                Debug.Log($"[Shop] Mua {selectedQty}x {item.name} — Trừ {totalCost} Point.");
                YWonderLand.Environment.ScreenToast.ShowInfo($"Đã mua: {selectedQty}x {item.name}  (-{totalCost} Point)");
                YWonderLand.Managers.AudioManager.Instance?.PlaySFX("coin");
            }
        }
        else
        {
            // Sell
            if (!YWonderLand.Managers.InventoryManager.Instance.RemoveItem(item.id, selectedQty))
            {
                Debug.Log($"[Shop] Không đủ item để bán!");
                YWonderLand.Environment.ScreenToast.Show("Không đủ vật phẩm để bán!");
                return;
            }

            YWonderLand.Managers.EconomyManager.Instance.AddPOS(totalCost);
            Debug.Log($"[Shop] Bán {selectedQty}x {item.name} — Nhận {totalCost} Point.");
            YWonderLand.Environment.ScreenToast.ShowInfo($"Đã bán: {selectedQty}x {item.name}  (+{totalCost} Point)");
            YWonderLand.Managers.AudioManager.Instance?.PlaySFX("coin");
            
            OnItemSold?.Invoke(item.id, selectedQty);
            
            RefreshGrid();
        }

        ShowEmptyDetails();
    }

    private void UpdateBalance()
    {
        if (lblBalance != null && YWonderLand.Managers.EconomyManager.Instance != null)
        {
            long balance = YWonderLand.Managers.EconomyManager.Instance.GetPOS();
            lblBalance.text = $"{balance:N0} Point";
        }
    }

    // ── Mock Data ──

    private ShopData CreateMockShopData()
    {
        string[] defaultBuyIds =
        {
            "carrot_seed_01",
            "cabbage_seed_01",
            "watermelon_seed_01",
            "corn_seed_01",
            "pumpkin_seed_01",
            "grass_seed_01",
            "fertilizer_01",
            "vaccine_01",
            "medicine_01",
            "bait_01",
            "chicken_01",
            "cow_01",
            "pig_01"
        };

        var dbItems = new List<ShopItem>();
        foreach (string id in defaultBuyIds)
        {
            dbItems.Add(BuildShopItemFromDatabase(id));
        }

        return new ShopData
        {
            shopName = "HAI LÚA — VẬT TƯ NÔNG TRẠI",
            hasSellTab = true,
            buyItems = dbItems,
            sellItems = new List<ShopItem>()
        };
    }

    private ShopItem BuildShopItemFromDatabase(string id)
    {
        var def = itemDatabase != null ? itemDatabase.GetItem(id) : null;
        if (def == null)
        {
            Debug.LogWarning($"[Shop] Mock catalog ID not found in ItemDatabase: '{id}'");
            return new ShopItem
            {
                id = id,
                icon = "?",
                name = id,
                price = 0,
                description = "",
                category = "items",
                canSell = false,
                sellPrice = 0
            };
        }

        return new ShopItem
        {
            id = def.id,
            icon = !string.IsNullOrEmpty(def.iconEmoji) ? def.iconEmoji : "?",
            iconSprite = def.iconSprite,
            iconTexture = def.iconTexture,
            name = def.itemName,
            price = def.buyPrice,
            description = def.description,
            category = def.category,
            canSell = def.canSell,
            sellPrice = def.sellPrice
        };
    }
}
