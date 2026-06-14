using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Event / Rewards Hub Popup.
/// Tab 0: Exchange — trade event items for rare rewards.
/// Tab 1: Bundle — limited-time UPOS deals.
/// Tab 2: Attendance — daily login rewards.
/// </summary>
public class EventPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument eventDocument;

    // Elements
    private VisualElement overlay;
    private Button btnClose;
    private Label lblTimer;

    // Tabs
    private Button tabExchange;
    private Button tabBundle;
    private Button tabAttendance;
    
    private VisualElement panelExchange;
    private VisualElement panelBundle;
    private VisualElement panelAttendance;

    // Grids
    private VisualElement exchangeGrid;
    private VisualElement bundleGrid;

    // Material counts
    private Label lblMatFish;
    private Label lblMatOre;
    private Label lblMatTicket;

    // ── Mock Data (Exchange) ──
    private int matFish = 5;
    private int matOre = 3;
    private int matTicket = 1;

    private struct ExchangeItem
    {
        public string icon;
        public string name;
        public string costText;
        public string costType; // "fish", "ore", "ticket"
        public int costAmount;
    }

    private List<ExchangeItem> exchangeItems = new List<ExchangeItem>
    {
        new ExchangeItem { icon = "🌟", name = "Hộp Sa Chi V2", costText = "🐟 x3 Cá event", costType = "fish", costAmount = 3 },
        new ExchangeItem { icon = "🍈", name = "Hộp Sầu Riêng V2", costText = "🐟 x5 Cá event", costType = "fish", costAmount = 5 },
        new ExchangeItem { icon = "🌿", name = "Búp Măng Tây V2", costText = "💎 x2 Quặng hiếm", costType = "ore", costAmount = 2 },
        new ExchangeItem { icon = "🧧", name = "Hộp Hồng Sâm V2", costText = "💎 x4 Quặng hiếm", costType = "ore", costAmount = 4 },
        new ExchangeItem { icon = "🐾", name = "Pet Mèo Vàng", costText = "🎫 x1 Vé sự kiện", costType = "ticket", costAmount = 1 },
        new ExchangeItem { icon = "👒", name = "Nón Mùa Hè", costText = "🐟 x2 + 💎 x1", costType = "fish", costAmount = 2 },
    };

    // ── Mock Data (Bundle) ──
    private struct BundleItem
    {
        public string icon;
        public string name;
        public string desc;
        public int oldPrice;
        public int newPrice;
        public string tag; // "HOT", "-50%", etc.
        public bool soldOut;
    }

    private List<BundleItem> bundleItems = new List<BundleItem>
    {
        new BundleItem { icon = "💎", name = "Gói Khởi Đầu", desc = "500 UPOS + 3 Vé sự kiện", oldPrice = 100000, newPrice = 49000, tag = "-50%", soldOut = false },
        new BundleItem { icon = "🎁", name = "Gói Mùa Hè", desc = "1000 UPOS + 1 Pet + 5 Hạt giống V2", oldPrice = 200000, newPrice = 99000, tag = "HOT", soldOut = false },
        new BundleItem { icon = "👑", name = "Gói VIP 30 ngày", desc = "VIP 30 ngày + 2000 UPOS", oldPrice = 500000, newPrice = 299000, tag = "-40%", soldOut = true },
    };

    // ── Mock Data (Attendance) ──
    private Button btnClaimAttendance;
    private List<VisualElement> daySlots = new List<VisualElement>();
    private int claimedDays = 2; // Days 1 & 2 claimed initially
    private bool hasClaimedToday = false; // Player can claim for Day 3 today

    private Dictionary<int, string> dayRewards = new Dictionary<int, string>
    {
        { 1, "🪙 500 Cá vàng" },
        { 2, "🥕 5 Hạt giống Cà rốt" },
        { 3, "🪙 1000 Cá vàng" },
        { 4, "🧪 2 Phân bón siêu tốc" },
        { 5, "🍎 5 Quả Táo" },
        { 6, "💎 2 Kim cương" },
        { 7, "🐢 1 Rùa con quý hiếm!" }
    };

    // ── Lifecycle ──

    private void Awake()
    {
        if (eventDocument == null)
            eventDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = eventDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("EventOverlay");
        btnClose = root.Q<Button>("BtnCloseEvent");
        lblTimer = root.Q<Label>("LblEventTimer");

        // Tabs
        tabExchange = root.Q<Button>("TabExchange");
        tabBundle = root.Q<Button>("TabBundle");
        tabAttendance = root.Q<Button>("TabAttendance");
        
        panelExchange = root.Q<VisualElement>("PanelExchange");
        panelBundle = root.Q<VisualElement>("PanelBundle");
        panelAttendance = root.Q<VisualElement>("PanelAttendance");

        // Exchange/Bundle Grids
        exchangeGrid = root.Q<VisualElement>("ExchangeGrid");
        bundleGrid = root.Q<VisualElement>("BundleGrid");

        lblMatFish = root.Q<Label>("LblMatFish");
        lblMatOre = root.Q<Label>("LblMatOre");
        lblMatTicket = root.Q<Label>("LblMatTicket");

        // Attendance
        btnClaimAttendance = root.Q<Button>("BtnClaimAttendance");
        
        daySlots.Clear();
        for (int i = 1; i <= 7; i++)
        {
            var slot = root.Q<VisualElement>($"DaySlot{i}");
            if (slot != null)
            {
                daySlots.Add(slot);
            }
        }
    }

    private void RegisterCallbacks()
    {
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay) Hide();
        });

        tabExchange?.RegisterCallback<ClickEvent>(evt => SwitchTab(0));
        tabBundle?.RegisterCallback<ClickEvent>(evt => SwitchTab(1));
        tabAttendance?.RegisterCallback<ClickEvent>(evt => SwitchTab(2));

        btnClaimAttendance?.RegisterCallback<ClickEvent>(evt => ClaimDailyReward());
    }

    // ── Public API ──

    public void Show()
    {
        ShowTab(0);
    }

    public void ShowTab(int tabIndex)
    {
        if (overlay == null) return;

        SwitchTab(tabIndex);
        UpdateMaterials();
        PopulateExchangeGrid();
        PopulateBundleGrid();
        UpdateAttendanceGridUI();

        overlay.style.display = DisplayStyle.Flex;
        Debug.Log("[Event] Opened Event Hub popup on tab " + tabIndex);
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[Event] Closed Event Hub popup");
        }
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Tabs ──

    private void SwitchTab(int tabIndex)
    {
        tabExchange?.RemoveFromClassList("event-tab--active");
        tabBundle?.RemoveFromClassList("event-tab--active");
        tabAttendance?.RemoveFromClassList("event-tab--active");

        if (tabIndex == 0) tabExchange?.AddToClassList("event-tab--active");
        else if (tabIndex == 1) tabBundle?.AddToClassList("event-tab--active");
        else if (tabIndex == 2) tabAttendance?.AddToClassList("event-tab--active");

        if (panelExchange != null)
            panelExchange.style.display = tabIndex == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (panelBundle != null)
            panelBundle.style.display = tabIndex == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        if (panelAttendance != null)
            panelAttendance.style.display = tabIndex == 2 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── Exchange Grid ──

    private void PopulateExchangeGrid()
    {
        if (exchangeGrid == null) return;
        exchangeGrid.Clear();

        foreach (var item in exchangeItems)
        {
            var row = new VisualElement();
            row.AddToClassList("event-exchange-row");

            var icon = new Label(item.icon);
            icon.AddToClassList("event-exchange-icon");

            var info = new VisualElement();
            info.AddToClassList("event-exchange-info");

            var name = new Label(item.name);
            name.AddToClassList("event-exchange-name");

            var cost = new Label(item.costText);
            cost.AddToClassList("event-exchange-cost");

            info.Add(name);
            info.Add(cost);

            bool canAfford = CanAfford(item);
            var btn = new Button { text = canAfford ? "ĐỔI" : "THIẾU" };
            btn.AddToClassList("event-exchange-btn");
            if (!canAfford) btn.SetEnabled(false);

            // Capture for closure
            var capturedItem = item;
            btn.RegisterCallback<ClickEvent>(evt =>
            {
                OnExchange(capturedItem);
            });

            row.Add(icon);
            row.Add(info);
            row.Add(btn);

            exchangeGrid.Add(row);
        }
    }

    private bool CanAfford(ExchangeItem item)
    {
        switch (item.costType)
        {
            case "fish": return matFish >= item.costAmount;
            case "ore": return matOre >= item.costAmount;
            case "ticket": return matTicket >= item.costAmount;
            default: return false;
        }
    }

    private void OnExchange(ExchangeItem item)
    {
        switch (item.costType)
        {
            case "fish": matFish -= item.costAmount; break;
            case "ore": matOre -= item.costAmount; break;
            case "ticket": matTicket -= item.costAmount; break;
        }

        UpdateMaterials();
        PopulateExchangeGrid();

        Debug.Log($"[Event] Đổi: {item.name} (trả {item.costText})");
    }

    // ── Bundle Grid ──

    private void PopulateBundleGrid()
    {
        if (bundleGrid == null) return;
        bundleGrid.Clear();

        foreach (var bundle in bundleItems)
        {
            var card = new VisualElement();
            card.AddToClassList("event-bundle-card");

            var icon = new Label(bundle.icon);
            icon.AddToClassList("event-bundle-icon");

            var name = new Label(bundle.name);
            name.AddToClassList("event-bundle-name");

            var desc = new Label(bundle.desc);
            desc.AddToClassList("event-bundle-desc");

            // Discount tag
            if (!string.IsNullOrEmpty(bundle.tag))
            {
                var tagWrap = new VisualElement();
                tagWrap.AddToClassList("event-bundle-tag");
                var tagText = new Label(bundle.tag);
                tagText.AddToClassList("event-bundle-tag-text");
                tagWrap.Add(tagText);
                card.Add(tagWrap);
            }

            card.Add(icon);
            card.Add(name);
            card.Add(desc);

            // Spacer to push price + button to bottom
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            card.Add(spacer);

            // Price
            var priceWrap = new VisualElement();
            priceWrap.AddToClassList("event-bundle-price-wrap");

            var oldPrice = new Label($"{bundle.oldPrice:N0}đ");
            oldPrice.AddToClassList("event-bundle-old-price");

            var newPrice = new Label($"{bundle.newPrice:N0}đ");
            newPrice.AddToClassList("event-bundle-price");

            priceWrap.Add(oldPrice);
            priceWrap.Add(newPrice);
            card.Add(priceWrap);

            if (bundle.soldOut)
            {
                var soldOut = new Label("ĐÃ HẾT");
                soldOut.AddToClassList("event-bundle-soldout");
                card.Add(soldOut);
            }
            else
            {
                var buyBtn = new Button { text = "MUA NGAY" };
                buyBtn.AddToClassList("event-bundle-buy-btn");
                buyBtn.RegisterCallback<ClickEvent>(evt =>
                {
                    Debug.Log($"[Event] Mua bundle: {bundle.name} ({bundle.newPrice:N0}đ)");
                });
                card.Add(buyBtn);
            }

            bundleGrid.Add(card);
        }
    }

    // ── Attendance Grid ──

    private void UpdateAttendanceGridUI()
    {
        for (int i = 0; i < daySlots.Count; i++)
        {
            var slot = daySlots[i];
            int dayNumber = i + 1;

            slot.RemoveFromClassList("claimed");
            slot.RemoveFromClassList("current");

            if (dayNumber <= claimedDays)
            {
                slot.AddToClassList("claimed");
            }
            else if (dayNumber == claimedDays + 1 && !hasClaimedToday)
            {
                slot.AddToClassList("current");
            }
        }

        if (btnClaimAttendance != null)
        {
            if (hasClaimedToday)
            {
                btnClaimAttendance.text = "Đã điểm danh";
                btnClaimAttendance.SetEnabled(false);
            }
            else
            {
                int nextDay = claimedDays + 1;
                btnClaimAttendance.text = nextDay <= 7 ? $"Điểm danh (Ngày {nextDay})" : "Đã hoàn thành!";
                btnClaimAttendance.SetEnabled(nextDay <= 7);
            }
        }
    }

    private void ClaimDailyReward()
    {
        if (hasClaimedToday || claimedDays >= 7) return;

        claimedDays++;
        hasClaimedToday = true;

        string reward = dayRewards.ContainsKey(claimedDays) ? dayRewards[claimedDays] : "Quà ngẫu nhiên";
        Debug.Log($"[Event] Điểm danh Ngày {claimedDays} thành công! Phần thưởng: {reward}");

        UpdateAttendanceGridUI();
    }

    // ── Helpers ──

    private void UpdateMaterials()
    {
        if (lblMatFish != null) lblMatFish.text = $"x{matFish}";
        if (lblMatOre != null) lblMatOre.text = $"x{matOre}";
        if (lblMatTicket != null) lblMatTicket.text = $"x{matTicket}";
    }
}
