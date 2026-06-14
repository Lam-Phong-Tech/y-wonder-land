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
    
    // Database
    private YWonderLand.Data.ItemDatabase itemDatabase;

    // ── Data Structures ──

    [System.Serializable]
    public struct ShopItem
    {
        public string id;
        public string icon;
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
                        itemsToDisplay.Add(new ShopItem
                        {
                            id = def.id,
                            icon = !string.IsNullOrEmpty(def.iconEmoji) ? def.iconEmoji : "📦",
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
        if (lblTotal != null) lblTotal.text = $"{total:N0} POS";
    }

    private void UpdateQtyDisplay(int unitPrice)
    {
        if (txtQty != null) txtQty.SetValueWithoutNotify(selectedQty.ToString());
        int total = unitPrice * selectedQty;
        if (lblTotal != null) lblTotal.text = $"{total:N0} POS";
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
            // Special handling for Animals (spawn them directly into pens)
            if (item.category == "animals")
            {
                // Pre-check POS for the FULL selected quantity. If they want 3, they must afford 3.
                if (YWonderLand.Managers.EconomyManager.Instance.GetPOS() < totalCost)
                {
                    Debug.Log($"[Shop] Không đủ POS để mua thú!");
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

                if (spawnedCount == 0) return; // Completely failed to spawn any

                int actualCost = unitPrice * spawnedCount;
                YWonderLand.Managers.EconomyManager.Instance.SpendPOS(actualCost);
                Debug.Log($"[Shop] Mua {spawnedCount}x {item.name} — Trừ {actualCost} POS.");
            }
            else
            {
                // Standard items go to inventory
                if (!YWonderLand.Managers.EconomyManager.Instance.SpendPOS(totalCost))
                {
                    Debug.Log($"[Shop] Không đủ POS!");
                    return;
                }
                YWonderLand.Managers.InventoryManager.Instance.AddItem(item.id, selectedQty);
                Debug.Log($"[Shop] Mua {selectedQty}x {item.name} — Trừ {totalCost} POS.");
            }
        }
        else
        {
            // Sell
            if (!YWonderLand.Managers.InventoryManager.Instance.RemoveItem(item.id, selectedQty))
            {
                Debug.Log($"[Shop] Không đủ item để bán!");
                return;
            }

            YWonderLand.Managers.EconomyManager.Instance.AddPOS(totalCost);
            Debug.Log($"[Shop] Bán {selectedQty}x {item.name} — Nhận {totalCost} POS.");
            
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
            lblBalance.text = $"{balance:N0} POS";
        }
    }

    // ── Mock Data ──

    private ShopData CreateMockShopData()
    {
        return new ShopData
        {
            shopName = "HAI LÚA — VẬT TƯ NÔNG TRẠI",
            hasSellTab = true,
            buyItems = new List<ShopItem>
            {
                new ShopItem { id = "carrot_seed_01", icon = "🥕", name = "Hạt cà rốt", price = 10, description = "Hạt giống cà rốt. Thu hoạch sau 24h. Tưới mỗi 10h.", category = "seeds", canSell = true, sellPrice = 5 },
                new ShopItem { id = "cabbage_seed_01", icon = "🥬", name = "Hạt cải", price = 15, description = "Hạt giống rau cải xanh tươi. Sản lượng cao.", category = "seeds", canSell = true, sellPrice = 7 },
                new ShopItem { id = "watermelon_seed_01", icon = "🍉", name = "Hạt dưa hấu", price = 30, description = "Hạt giống dưa hấu ngọt. Chu kỳ dài hơn nhưng lời nhiều.", category = "seeds", canSell = true, sellPrice = 15 },
                new ShopItem { id = "corn_seed_01", icon = "🌽", name = "Hạt bắp", price = 20, description = "Hạt giống bắp ngô vàng. Trồng dễ, năng suất ổn.", category = "seeds", canSell = true, sellPrice = 10 },
                new ShopItem { id = "pumpkin_seed_01", icon = "🎃", name = "Hạt bí ngô", price = 25, description = "Hạt giống bí ngô mùa event. Giá trị thu hoạch cao.", category = "seeds", canSell = true, sellPrice = 12 },
                new ShopItem { id = "grass_seed_01", icon = "🌿", name = "Cỏ voi", price = 5, description = "Cỏ voi làm thức ăn cho vật nuôi. Gieo nhanh, thu hoạch lẹ.", category = "seeds", canSell = true, sellPrice = 2 },
                new ShopItem { id = "fertilizer_01", icon = "🧪", name = "Phân bón", price = 50, description = "Phân bón tăng tốc sinh trưởng cây trồng. Giảm 50% thời gian.", category = "items", canSell = true, sellPrice = 25 },
                new ShopItem { id = "vaccine_01", icon = "💉", name = "Vắc-xin", price = 80, description = "Vắc-xin kháng bệnh cho vật nuôi. Phòng bệnh 7 ngày.", category = "items", canSell = true, sellPrice = 40 },
                new ShopItem { id = "medicine_01", icon = "💊", name = "Thuốc trị", price = 100, description = "Thuốc điều trị cho vật nuôi đã bị bệnh.", category = "items", canSell = true, sellPrice = 50 },
                new ShopItem { id = "bait_01", icon = "🪱", name = "Mồi câu", price = 20, description = "Mồi câu cá. Tăng 20% tỉ lệ câu được cá hiếm.", category = "items", canSell = true, sellPrice = 10 },
                // Thú nuôi
                new ShopItem { id = "chicken_01", icon = "🐔", name = "Gà", price = 500, description = "Gà đẻ trứng mỗi 30s. Thức ăn: 1 Thức ăn/lần.", category = "animals", canSell = false, sellPrice = 0 },
                new ShopItem { id = "cow_01", icon = "🐄", name = "Bò sữa", price = 1500, description = "Bò vắt sữa mỗi 45s. Giá trị dinh dưỡng cao.", category = "animals", canSell = false, sellPrice = 0 },
                new ShopItem { id = "pig_01", icon = "🐷", name = "Heo", price = 1000, description = "Heo cho thịt. Béo mầm mạp mạp.", category = "animals", canSell = false, sellPrice = 0 },
            },
            sellItems = new List<ShopItem>() // Sell items are loaded dynamically now
        };
    }
}
