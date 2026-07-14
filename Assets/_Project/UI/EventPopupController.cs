using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using YWonderLand.Backend;

/// <summary>
/// Controller for the Event / Rewards Hub Popup.
/// Tab 0: Exchange — trade event items for rare rewards.
/// Tab 1: Bundle — limited-time UPoint deals.
/// Tab 2: Attendance — daily login rewards.
/// </summary>
public class EventPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument eventDocument;
    private YWonderLand.Data.ItemDatabase itemDatabase;

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

    // Vòng quay may mắn - popup riêng cho NPC Game Boy
    private VisualElement wheelOverlay;
    private Button btnCloseWheel;
    private VisualElement wheelCircle;
    private VisualElement wheelHub;
    private Button btnSpin;
    private Label lblSpinsLeft;
    private Label lblWheelResult;
    private float wheelAngle = 0f;
    private bool isSpinning = false;
    private Texture2D wheelSliceTexture;

    private static readonly Color32[] WheelSegmentColors =
    {
        new Color32(239, 68, 68, 255),
        new Color32(245, 158, 11, 255),
        new Color32(250, 204, 21, 255),
        new Color32(132, 204, 22, 255),
        new Color32(34, 197, 94, 255),
        new Color32(20, 184, 166, 255),
        new Color32(14, 165, 233, 255),
        new Color32(59, 130, 246, 255),
        new Color32(124, 58, 237, 255),
        new Color32(168, 85, 247, 255),
        new Color32(236, 72, 153, 255),
        new Color32(249, 115, 22, 255),
    };

    private const int WheelTextureSize = 384;
    private const float WheelDiameter = 268f;
    private const float WheelBorderWidth = 7f;
    private const float WheelPrizeSlotSize = 64f;
    private const float WheelPrizeRadius = 88f;
    private const float WheelContentCenter = (WheelDiameter - WheelBorderWidth * 2f) * 0.5f;

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
        new BundleItem { icon = "💎", name = "Gói Khởi Đầu", desc = "500 UPoint + 3 Vé sự kiện", oldPrice = 100000, newPrice = 49000, tag = "-50%", soldOut = false },
        new BundleItem { icon = "🎁", name = "Gói Mùa Hè", desc = "1000 UPoint + 1 Pet + 5 Hạt giống V2", oldPrice = 200000, newPrice = 99000, tag = "HOT", soldOut = false },
        new BundleItem { icon = "👑", name = "Gói VIP 30 ngày", desc = "VIP 30 ngày + 2000 UPoint", oldPrice = 500000, newPrice = 299000, tag = "-40%", soldOut = true },
    };

    // ── Điểm danh TÂN THỦ 15 ngày (khách chốt 22/06: trao thưởng THẬT, chỉ 1 lần/ngày thật) ──
    private Button btnClaimAttendance;
    private VisualElement attendanceGrid;
    private int claimedDays = 0;
    private bool hasClaimedToday = false;

    private const int AttendanceTotalDays = 15;
    private const string AttDaysKey = "YW_AttendanceClaimedDays";
    private const string AttDateKey = "YW_AttendanceLastDate";

    private struct DayReward
    {
        public string itemId; public string name; public string emoji;
        public string iconClass;
        public int qty; public bool isPoint; public bool isNothing;
    }

    // Lịch thưởng: chỉ các ngày mốc có quà; ngày khác = vẫn điểm danh, không quà.
    private DayReward GetDayReward(int day)
    {
        switch (day)
        {
            case 1:  return new DayReward { isPoint = true,        name = "Point",   emoji = "🪙", iconClass = "attendance-icon-point", qty = 26 };
            case 3:  return new DayReward { itemId = "wood_01",    name = "Gỗ",      emoji = "🪵", iconClass = "attendance-icon-wood", qty = 4 };
            case 5:  return new DayReward { isPoint = true,        name = "Point",   emoji = "🪙", iconClass = "attendance-icon-point", qty = 26 };
            case 7:  return new DayReward { itemId = "corn_01",    name = "Bắp ngô", emoji = "🌽", qty = 10 };
            case 10: return new DayReward { itemId = "pumpkin_01", name = "Bí ngô",  emoji = "🎃", qty = 10 };
            case 11: return new DayReward { itemId = "wood_01",    name = "Gỗ",      emoji = "🪵", iconClass = "attendance-icon-wood", qty = 8 };
            case 15: return new DayReward { itemId = "rabbit_01",  name = "Thỏ",     emoji = "🐰", iconClass = "attendance-icon-rabbit", qty = 1 };
            default: return new DayReward { isNothing = true, name = "—", emoji = "📅", iconClass = "attendance-icon-calendar", qty = 0 };
        }
    }

    public static bool IsAttendanceReadyToClaim()
    {
        int days = PlayerScopedPrefs.GetInt(AttDaysKey, 0);
        if (days >= AttendanceTotalDays) return false;
        if (IsAttendanceClaimedToday()) return false;
        return !IsAttendanceTutorialLocked(days);
    }

    public static bool IsAttendanceLockedByTutorial()
    {
        int days = PlayerScopedPrefs.GetInt(AttDaysKey, 0);
        return IsAttendanceTutorialLocked(days);
    }

    private static bool IsAttendanceClaimedToday()
    {
        return PlayerScopedPrefs.GetString(AttDateKey, "") == System.DateTime.Now.ToString("yyyyMMdd");
    }

    private static bool IsAttendanceTutorialLocked(int currentClaimedDays)
    {
        return currentClaimedDays <= 0 && !IsTutorialCompleteForAttendance();
    }

    private static bool IsTutorialCompleteForAttendance()
    {
        var prof = YWonderLand.Backend.PlayerProfileService.Instance;
        if (prof != null && prof.Profile != null && prof.Profile.tutorialCompleted)
            return true;

        var tutorial = TutorialManager.Instance;
        return tutorial != null && tutorial.currentStep == TutorialManager.TutorialStep.Complete;
    }

    // ── Vòng quay may mắn (khách chốt 22/06: 3 lượt/ngày, trao thưởng thật) ──
    private struct WheelPrize
    {
        public string itemId; public string name; public string emoji;
        public string iconClass;
        public int qty; public int weight; public bool isNothing;
    }

    private readonly List<WheelPrize> wheelPrizes = new List<WheelPrize>
    {
        new WheelPrize { itemId = "goat_01",            name = "Dê",           emoji = "🐐", iconClass = "wheel-icon-goat", qty = 1,  weight = 1 },
        new WheelPrize { itemId = "duck_01",            name = "Vịt",          emoji = "🦆", iconClass = "wheel-icon-duck", qty = 1,  weight = 3 },
        new WheelPrize { itemId = "rabbit_01",          name = "Thỏ",          emoji = "🐰", iconClass = "wheel-icon-rabbit", qty = 1,  weight = 5 },
        new WheelPrize { itemId = "wood_01",            name = "Gỗ",           emoji = "🪵", iconClass = "wheel-icon-wood", qty = 4,  weight = 10 },
        new WheelPrize { itemId = "stone_01",           name = "Đá lát đường", emoji = "🪨", iconClass = "wheel-icon-stone", qty = 4,  weight = 5 },
        new WheelPrize { itemId = "corn_01",            name = "Bắp ngô",      emoji = "🌽", qty = 5,  weight = 1 },
        new WheelPrize { itemId = "cabbage_01",         name = "Bắp cải",      emoji = "🥬", qty = 5,  weight = 1 },
        new WheelPrize { itemId = "grass_01",           name = "Cỏ voi",       emoji = "🌿", qty = 5,  weight = 1 },
        new WheelPrize { itemId = "chicken_01",         name = "Gà",           emoji = "🐔", iconClass = "wheel-icon-chicken", qty = 1,  weight = 3 },
        new WheelPrize { itemId = "carrot_seed_01",     name = "Hạt cà rốt",   emoji = "🥕", qty = 10, weight = 5 },
        new WheelPrize { itemId = "watermelon_seed_01", name = "Hạt dưa hấu",  emoji = "🍉", qty = 5,  weight = 5 },
        new WheelPrize { name = "Chúc may mắn lần sau", emoji = "", iconClass = "", qty = 0,  weight = 60, isNothing = true },
    };

    private const int MaxSpinsPerDay = 3;
    private const string SpinDateKey = "YW_WheelSpinDate";
    private const string SpinCountKey = "YW_WheelSpinCount";
    private int spinsUsedToday = 0;

    // ── Lifecycle ──

    private void Awake()
    {
        if (eventDocument == null)
            eventDocument = GetComponent<UIDocument>();
        itemDatabase = Resources.Load<YWonderLand.Data.ItemDatabase>("ItemDatabase");
    }

    private void OnEnable()
    {
        var root = eventDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
        HideLuckyWheel();
    }

    private void OnDestroy()
    {
        if (wheelSliceTexture != null)
            Object.Destroy(wheelSliceTexture);
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

        // Vòng quay may mắn riêng cho NPC Game Boy
        wheelOverlay = root.Q<VisualElement>("LuckyWheelOverlay");
        btnCloseWheel = root.Q<Button>("BtnCloseLuckyWheel");
        wheelCircle = root.Q<VisualElement>("WheelCircle");
        wheelHub = root.Q<VisualElement>("WheelHub");
        btnSpin = root.Q<Button>("BtnSpin");
        lblSpinsLeft = root.Q<Label>("LblSpinsLeft");
        lblWheelResult = root.Q<Label>("LblWheelResult");

        // Exchange/Bundle Grids
        exchangeGrid = root.Q<VisualElement>("ExchangeGrid");
        bundleGrid = root.Q<VisualElement>("BundleGrid");

        lblMatFish = root.Q<Label>("LblMatFish");
        lblMatOre = root.Q<Label>("LblMatOre");
        lblMatTicket = root.Q<Label>("LblMatTicket");

        // Attendance
        btnClaimAttendance = root.Q<Button>("BtnClaimAttendance");
        attendanceGrid = root.Q<VisualElement>("AttendanceGrid");
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
        btnCloseWheel?.RegisterCallback<ClickEvent>(evt => HideLuckyWheel());
        wheelOverlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == wheelOverlay) HideLuckyWheel();
        });
        btnSpin?.RegisterCallback<ClickEvent>(evt => OnSpin());
    }

    // ── Public API ──

    public void Show()
    {
        ShowTab(0);
    }

    public void ShowTab(int tabIndex)
    {
        if (overlay == null) return;

        HideLuckyWheel();
        SwitchTab(tabIndex);
        UpdateMaterials();
        PopulateExchangeGrid();
        PopulateBundleGrid();
        UpdateAttendanceGridUI();
        overlay.style.display = DisplayStyle.Flex;
        Debug.Log("[Event] Opened Event Hub popup on tab " + tabIndex);
    }

    public void ShowLuckyWheel()
    {
        if (wheelOverlay == null) return;

        Hide();
        BuildWheelSegmented();
        RefreshWheel();

        wheelOverlay.style.display = DisplayStyle.Flex;
        Debug.Log("[Event] Opened NPC Lucky Wheel popup");
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

    public void HideLuckyWheel()
    {
        if (wheelOverlay != null)
        {
            wheelOverlay.style.display = DisplayStyle.None;
            Debug.Log("[Event] Closed NPC Lucky Wheel popup");
        }
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

    // Đọc tiến độ điểm danh + xem hôm nay đã điểm danh chưa (theo NGÀY THẬT).
    private void LoadAttendance()
    {
        claimedDays = PlayerScopedPrefs.GetInt(AttDaysKey, 0);
        hasClaimedToday = IsAttendanceClaimedToday();
    }

    private void UpdateAttendanceGridUI()
    {
        LoadAttendance();
        bool lockedByTutorial = IsAttendanceTutorialLocked(claimedDays);

        if (attendanceGrid != null)
        {
            attendanceGrid.Clear();
            for (int day = 1; day <= AttendanceTotalDays; day++)
            {
                var r = GetDayReward(day);
                bool special = (day == AttendanceTotalDays);

                var slot = new VisualElement();
                slot.AddToClassList("day-slot");
                if (special) slot.AddToClassList("day-slot-special");
                if (day <= claimedDays) slot.AddToClassList("claimed");
                else if (day == claimedDays + 1 && !hasClaimedToday && !lockedByTutorial) slot.AddToClassList("current");
                if (lockedByTutorial && day == 1) slot.AddToClassList("locked");

                var title = new Label($"Ngày {day}");
                title.AddToClassList("day-slot-title");
                if (special) title.AddToClassList("title-special");
                slot.Add(title);

                slot.Add(CreateAttendanceRewardIcon(r, special));

                var amount = new Label(r.isNothing ? "—" : (r.isPoint ? $"+{r.qty}" : $"x{r.qty}"));
                amount.AddToClassList("day-slot-amount");
                if (special) amount.AddToClassList("amount-special");
                slot.Add(amount);

                var overlay = new VisualElement();
                overlay.AddToClassList("claimed-overlay");
                var check = new Label("✔️");
                check.AddToClassList("claimed-check");
                overlay.Add(check);
                slot.Add(overlay);

                attendanceGrid.Add(slot);
            }
        }

        if (btnClaimAttendance != null)
        {
            if (claimedDays >= AttendanceTotalDays)
            {
                btnClaimAttendance.text = "Đã hoàn thành!";
                btnClaimAttendance.SetEnabled(false);
            }
            else if (hasClaimedToday)
            {
                btnClaimAttendance.text = "Đã điểm danh hôm nay";
                btnClaimAttendance.SetEnabled(false);
            }
            else if (lockedByTutorial)
            {
                btnClaimAttendance.text = "Làm xong hướng dẫn NPC để nhận";
                btnClaimAttendance.SetEnabled(false);
            }
            else
            {
                btnClaimAttendance.text = $"Điểm danh (Ngày {claimedDays + 1})";
                btnClaimAttendance.SetEnabled(true);
            }
        }
    }

    private VisualElement CreateAttendanceRewardIcon(DayReward reward, bool special)
    {
        if (!string.IsNullOrEmpty(reward.iconClass))
        {
            var icon = new VisualElement();
            icon.AddToClassList("day-slot-icon");
            icon.AddToClassList(reward.iconClass);
            if (special) icon.AddToClassList("day-slot-icon-special");
            return icon;
        }

        var itemDef = !string.IsNullOrEmpty(reward.itemId) && itemDatabase != null
            ? itemDatabase.GetItem(reward.itemId)
            : null;

        if (itemDef != null && (itemDef.iconTexture != null || itemDef.iconSprite != null))
        {
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("day-slot-icon");
            if (special) icon.AddToClassList("day-slot-icon-special");

            if (itemDef.iconTexture != null)
                icon.image = itemDef.iconTexture;
            else
                icon.sprite = itemDef.iconSprite;

            return icon;
        }

        var fallback = new Label(reward.emoji);
        fallback.AddToClassList("day-slot-emoji");
        if (special) fallback.AddToClassList("emoji-special");
        return fallback;
    }

    private void ClaimDailyReward()
    {
        LoadAttendance();
        if (hasClaimedToday || claimedDays >= AttendanceTotalDays) return;
        if (IsAttendanceTutorialLocked(claimedDays))
        {
            YWonderLand.Environment.ScreenToast.ShowInfo("Làm xong hướng dẫn NPC tân thủ trước khi nhận điểm danh ngày đầu.");
            UpdateAttendanceGridUI();
            return;
        }

        claimedDays++;
        var r = GetDayReward(claimedDays);

        // Trao thưởng THẬT.
        if (!r.isNothing && r.qty > 0)
        {
            if (r.isPoint)
                YWonderLand.Managers.EconomyManager.Instance?.AddPOS(r.qty);
            else if (!string.IsNullOrEmpty(r.itemId))
                YWonderLand.Managers.InventoryManager.Instance?.AddItem(r.itemId, r.qty);
        }

        PlayerScopedPrefs.SetInt(AttDaysKey, claimedDays);
        PlayerScopedPrefs.SetString(AttDateKey, System.DateTime.Now.ToString("yyyyMMdd"));
        PlayerScopedPrefs.Save();
        hasClaimedToday = true;

        string msg = r.isNothing
            ? $"📅 Đã điểm danh Ngày {claimedDays}!"
            : $"📅 Ngày {claimedDays}: nhận {r.name} {(r.isPoint ? "+" : "x")}{r.qty}!";
        if (!r.isNothing && !r.isPoint && !string.IsNullOrEmpty(r.itemId) && r.qty > 0)
            YWonderLand.Environment.ScreenToast.ShowItemReward(r.itemId, r.qty, $"Ngày {claimedDays}");
        else
            YWonderLand.Environment.ScreenToast.ShowInfo(msg);

        UpdateAttendanceGridUI();
    }

    // ── Vòng quay may mắn ──

    // Dựng nền 12 múi màu và icon phần thưởng trong từng múi.
    private void BuildWheelSegmented()
    {
        if (wheelCircle == null) return;
        wheelCircle.Clear();
        LockWheelCircleLayout();

        int count = wheelPrizes.Count;
        if (count <= 0) return;

        ApplyWheelSliceBackground(count);

        float segmentDegrees = 360f / count;
        for (int i = 0; i < count; i++)
        {
            wheelCircle.Add(CreateWheelPrizeSlot(wheelPrizes[i], i, segmentDegrees));
        }
    }

    private void LockWheelCircleLayout()
    {
        wheelCircle.style.width = WheelDiameter;
        wheelCircle.style.height = WheelDiameter;
        wheelCircle.style.minWidth = WheelDiameter;
        wheelCircle.style.maxWidth = WheelDiameter;
        wheelCircle.style.minHeight = WheelDiameter;
        wheelCircle.style.maxHeight = WheelDiameter;
        wheelCircle.style.flexGrow = 0f;
        wheelCircle.style.flexShrink = 0f;
    }

    private void ApplyWheelSliceBackground(int segmentCount)
    {
        if (wheelSliceTexture != null)
            Object.Destroy(wheelSliceTexture);

        wheelSliceTexture = CreateWheelSliceTexture(segmentCount);
        wheelCircle.style.backgroundImage = new StyleBackground(wheelSliceTexture);
    }

    private Texture2D CreateWheelSliceTexture(int segmentCount)
    {
        var texture = new Texture2D(WheelTextureSize, WheelTextureSize, TextureFormat.RGBA32, false)
        {
            name = "RuntimeLuckyWheelSlices",
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (WheelTextureSize - 1) * 0.5f;
        float radius = center - 2f;
        float radiusSqr = radius * radius;
        float rimRadius = radius * 0.94f;
        float rimRadiusSqr = rimRadius * rimRadius;
        float segmentDegrees = 360f / segmentCount;

        for (int y = 0; y < WheelTextureSize; y++)
        {
            for (int x = 0; x < WheelTextureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distSqr = dx * dx + dy * dy;
                if (distSqr > radiusSqr)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                int segmentIndex = Mathf.FloorToInt((angle + segmentDegrees * 0.5f) / segmentDegrees) % segmentCount;
                Color color = WheelSegmentColors[segmentIndex % WheelSegmentColors.Length];
                if (distSqr > rimRadiusSqr)
                    color = Color.Lerp(color, Color.white, 0.18f);

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    private VisualElement CreateWheelPrizeSlot(WheelPrize prize, int index, float segmentDegrees)
    {
        float angleDeg = index * segmentDegrees;
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float x = WheelContentCenter + WheelPrizeRadius * Mathf.Sin(angleRad);
        float y = WheelContentCenter - WheelPrizeRadius * Mathf.Cos(angleRad);

        var slot = new VisualElement();
        slot.AddToClassList("wheel-prize-slot");
        slot.style.position = Position.Absolute;
        slot.style.left = x - WheelPrizeSlotSize * 0.5f;
        slot.style.top = y - WheelPrizeSlotSize * 0.5f;
        slot.style.rotate = new Rotate(new Angle(angleDeg, AngleUnit.Degree));

        if (prize.isNothing)
            return slot;

        var icon = CreateWheelPrizeWheelIcon(prize);
        icon.AddToClassList("wheel-prize-slot-icon");
        slot.Add(icon);

        return slot;
    }

    private VisualElement CreateWheelPrizeWheelIcon(WheelPrize prize)
    {
        var itemDef = !string.IsNullOrEmpty(prize.itemId) && itemDatabase != null
            ? itemDatabase.GetItem(prize.itemId)
            : null;

        if (itemDef != null && (itemDef.iconTexture != null || itemDef.iconSprite != null))
        {
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("wheel-icon");
            if (itemDef.iconTexture != null)
                icon.image = itemDef.iconTexture;
            else
                icon.sprite = itemDef.iconSprite;
            return icon;
        }

        if (!string.IsNullOrEmpty(prize.iconClass))
        {
            var icon = new VisualElement();
            icon.AddToClassList("wheel-icon");
            icon.AddToClassList(prize.iconClass);
            return icon;
        }

        var fallback = new Label(prize.emoji);
        fallback.AddToClassList("wheel-emoji");
        return fallback;
    }

    private void BuildWheel()
    {
        if (wheelCircle == null) return;
        wheelCircle.Clear();

        int n = wheelPrizes.Count;
        float seg = 360f / n;
        const float radius = 92f;   // bán kính đặt icon
        const float center = 112f;  // tâm content-box vòng (236 - 2*6 viền = 224 → /2)
        for (int i = 0; i < n; i++)
        {
            float ang = i * seg * Mathf.Deg2Rad;
            float x = center + radius * Mathf.Sin(ang);
            float y = center - radius * Mathf.Cos(ang);

            var icon = CreateWheelPrizeIcon(wheelPrizes[i]);
            icon.style.position = Position.Absolute;
            icon.style.left = x - 13;
            icon.style.top = y - 13;
            wheelCircle.Add(icon);
        }
    }

    private VisualElement CreateWheelPrizeIcon(WheelPrize prize)
    {
        if (!string.IsNullOrEmpty(prize.iconClass))
        {
            var icon = new VisualElement();
            icon.AddToClassList("wheel-icon");
            icon.AddToClassList(prize.iconClass);
            return icon;
        }

        var itemDef = !string.IsNullOrEmpty(prize.itemId) && itemDatabase != null
            ? itemDatabase.GetItem(prize.itemId)
            : null;

        if (itemDef != null && (itemDef.iconTexture != null || itemDef.iconSprite != null))
        {
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("wheel-icon");
            if (itemDef.iconTexture != null)
                icon.image = itemDef.iconTexture;
            else
                icon.sprite = itemDef.iconSprite;
            return icon;
        }

        var fallback = new Label(prize.emoji);
        fallback.AddToClassList("wheel-emoji");
        return fallback;
    }

    // Đếm lượt quay theo NGÀY THẬT (reset khi sang ngày mới).
    private void LoadSpins()
    {
        string today = System.DateTime.Now.ToString("yyyyMMdd");
        if (PlayerScopedPrefs.GetString(SpinDateKey, "") != today)
        {
            PlayerScopedPrefs.SetString(SpinDateKey, today);
            PlayerScopedPrefs.SetInt(SpinCountKey, 0);
            spinsUsedToday = 0;
        }
        else
        {
            spinsUsedToday = PlayerScopedPrefs.GetInt(SpinCountKey, 0);
        }
    }

    private void RefreshWheel()
    {
        LoadSpins();
        int left = Mathf.Max(0, MaxSpinsPerDay - spinsUsedToday);
        if (lblSpinsLeft != null) lblSpinsLeft.text = $"Lượt còn: {left}/{MaxSpinsPerDay}";
        if (btnSpin != null)
        {
            btnSpin.SetEnabled(left > 0);
            btnSpin.text = string.Empty;
        }
    }

    private void OnSpin()
    {
        if (isSpinning) return;
        LoadSpins();
        if (spinsUsedToday >= MaxSpinsPerDay) return;

        // Chọn quà theo TRỌNG SỐ.
        int total = 0;
        foreach (var p in wheelPrizes) total += p.weight;
        if (total <= 0) return;
        int roll = UnityEngine.Random.Range(0, total);
        int acc = 0, idx = wheelPrizes.Count - 1;
        for (int i = 0; i < wheelPrizes.Count; i++)
        {
            acc += wheelPrizes[i].weight;
            if (roll < acc) { idx = i; break; }
        }
        WheelPrize won = wheelPrizes[idx];

        // Trừ lượt (lưu ngay).
        spinsUsedToday++;
        PlayerScopedPrefs.SetInt(SpinCountKey, spinsUsedToday);
        PlayerScopedPrefs.Save();

        isSpinning = true;
        if (btnSpin != null) { btnSpin.SetEnabled(false); btnSpin.text = string.Empty; }
        if (lblWheelResult != null) lblWheelResult.text = "";
        SetWheelHubIcon(null);

        // Quay 5 vòng + dừng sao cho icon trúng nằm trên đỉnh (dưới kim).
        float seg = 360f / wheelPrizes.Count;
        float targetMod = (360f - (idx * seg) % 360f) % 360f;
        float curMod = ((wheelAngle % 360f) + 360f) % 360f;
        float delta = ((targetMod - curMod) % 360f + 360f) % 360f;
        wheelAngle += 360f * 5 + delta;
        if (wheelCircle != null)
            wheelCircle.style.rotate = new Rotate(new Angle(wheelAngle, AngleUnit.Degree));

        // Quay xong (~3s khớp transition-duration) → hiện quà + trao thưởng.
        wheelCircle?.schedule.Execute(() => RevealWheelResult(won)).StartingIn(3150);
    }

    private void RevealWheelResult(WheelPrize won)
    {
        isSpinning = false;

        // Trao thưởng THẬT vào túi đồ.
        if (!won.isNothing && !string.IsNullOrEmpty(won.itemId) && won.qty > 0)
            YWonderLand.Managers.InventoryManager.Instance?.AddItem(won.itemId, won.qty);

        SetWheelHubIcon(won);
        string msg = won.isNothing ? "Chúc may mắn lần sau!" : $"Trúng: {won.name} x{won.qty}!";
        if (lblWheelResult != null) lblWheelResult.text = msg;
        if (!won.isNothing && !string.IsNullOrEmpty(won.itemId) && won.qty > 0)
            YWonderLand.Environment.ScreenToast.ShowItemReward(won.itemId, won.qty, "Trúng");
        else
            YWonderLand.Environment.ScreenToast.ShowInfo(msg);

        RefreshWheel();
    }

    private void SetWheelHubIcon(WheelPrize? prize)
    {
        if (wheelHub == null) return;

        wheelHub.Clear();
        wheelHub.RemoveFromClassList("wheel-icon-goat");
        wheelHub.RemoveFromClassList("wheel-icon-duck");
        wheelHub.RemoveFromClassList("wheel-icon-rabbit");
        wheelHub.RemoveFromClassList("wheel-icon-wood");
        wheelHub.RemoveFromClassList("wheel-icon-stone");
        wheelHub.RemoveFromClassList("wheel-icon-chicken");
        wheelHub.RemoveFromClassList("wheel-icon-luck");
    }

    // ── Helpers ──

    private void UpdateMaterials()
    {
        if (lblMatFish != null) lblMatFish.text = $"x{matFish}";
        if (lblMatOre != null) lblMatOre.text = $"x{matOre}";
        if (lblMatTicket != null) lblMatTicket.text = $"x{matTicket}";
    }
}
