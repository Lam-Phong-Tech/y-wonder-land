using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the World Map Popup.
/// Shows 5 game locations with lock/unlock logic.
/// Includes cheat buttons for testing different level/VIP states.
/// </summary>
public class MapPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument mapDocument;

    // Elements
    private VisualElement overlay;
    private VisualElement mapList;
    private Button btnClose;
    private Label lblMapLevel;

    // Detail panel
    private VisualElement detailPanel;
    private Label detailEmpty;
    private VisualElement detailContent;
    private Label lblMapIcon;
    private Label lblMapName;
    private VisualElement statusBadge;
    private Label lblMapStatus;
    private Label lblMapDesc;
    private VisualElement mapRequirements;
    private Label lblMapReqLevel;
    private Label lblMapReqExtra;
    private VisualElement mapFeatures;
    private Label lblMapFeatures;
    private Button btnTravel;

    // Cheat
    private Button btnCheatLevel;
    private Button btnCheatVip;

    // State
    private int playerLevel = 1;
    private bool isVip = false;
    private bool hasHaiphuTicket = false;
    private bool hasMocnhiTicket = false;
    private bool isTutorialCompleted = true;
    private MapLocation? selectedLocation;
    private VisualElement selectedCard;

    // Cheat level cycle
    private int[] cheatLevels = { 1, 5, 15, 45, 65 };
    private int cheatLevelIndex = 0;

    // ── Data ──

    private struct MapLocation
    {
        public string id;
        public string icon;
        public string name;
        public string subtitle;
        public string description;
        public string features;
        public int requiredLevel;
        public bool requiresVipOrTicket;
        public string ticketName;
        public string sceneName;
    }

    private List<MapLocation> locations;

    private void Awake()
    {
        if (mapDocument == null)
            mapDocument = GetComponent<UIDocument>();

        InitLocations();
    }

    private void OnEnable()
    {
        var root = mapDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
    }

    private void InitLocations()
    {
        locations = new List<MapLocation>
        {
            new MapLocation
            {
                id = "farm", icon = "🏡", name = "Nông trại",
                subtitle = "Trồng trọt, chăn nuôi, chặt cây",
                description = "Khu vực chính của bạn! Cuốc đất, trồng hạt giống, tưới nước, chăn nuôi vật nuôi. Có thể xây chuồng, đường đi và đèn chiếu sáng.",
                features = "🌾 Trồng trọt (cà rốt, cải, dưa...)\n🐔 Chăn nuôi (gà, bò, heo...)\n🪵 Chặt cây lấy gỗ\n🪨 Đào đá\n🔨 Xây dựng chuồng / đường / đèn",
                requiredLevel = 0, requiresVipOrTicket = false, ticketName = "",
                sceneName = "FarmScene"
            },
            new MapLocation
            {
                id = "city", icon = "🏙️", name = "Thành phố",
                subtitle = "13 cửa hàng, câu cá, heo đất",
                description = "Phố thương mại sầm uất với 13 cửa hàng: Câu cá, Workshop, Store, Mini Garden, Hai Lúa, Pet Shop, Heo Đất và nhiều hơn nữa!",
                features = "🎣 Câu cá (10 lượt free/ngày)\n🛒 12 cửa hàng NPC\n🐷 Heo đất gửi lãi suất\n🔧 Workshop nâng cấp dụng cụ\n🎁 Gift Box tặng bạn bè",
                requiredLevel = 0, requiresVipOrTicket = false, ticketName = "",
                sceneName = "CityScene"
            },
            new MapLocation
            {
                id = "mine", icon = "⛏️", name = "Khai thác mỏ",
                subtitle = "Đào quặng, vật liệu quý",
                description = "Hang động huyền bí với các tảng đá chứa quặng. Mỗi ngày được đào miễn phí 10 lần. Quặng dùng để xây đèn và chế tác.",
                features = "⛏️ Đào quặng (10 lượt free/ngày)\n💎 Quặng hiếm (xác suất thấp)\n🎫 Mua thêm vé đào\n🪨 Nguyên liệu xây đèn (8 quặng/đèn)",
                requiredLevel = 10, requiresVipOrTicket = false, ticketName = "",
                sceneName = "MineScene"
            },
            new MapLocation
            {
                id = "haiphu", icon = "🏝️", name = "Đảo Hải Phú",
                subtitle = "Đảo nhiệt đới, cá hiếm, nông sản đặc biệt",
                description = "Đảo nhiệt đới biển xanh với bãi câu cá đặc biệt, hạt giống và vật nuôi exclusive. Nông sản theo mùa sự kiện.",
                features = "🐟 Câu cá hiếm (giá trị cao)\n🌴 Hạt giống đảo exclusive\n🐑 Vật nuôi đặc biệt\n🎪 Nông sản sự kiện theo mùa",
                requiredLevel = 40, requiresVipOrTicket = true, ticketName = "Vé Hải Phú",
                sceneName = "HaiphuIsland"
            },
            new MapLocation
            {
                id = "mocnhi", icon = "🌲", name = "Đảo Mộc Nhi",
                subtitle = "Rừng huyền bí, vật phẩm event hiếm",
                description = "Đảo rừng huyền bí bao phủ sương mù. Chứa vật phẩm sự kiện cực hiếm với tỉ lệ drop thấp. Vùng đất endgame.",
                features = "🌫️ Rừng sương mù bí ẩn\n🎁 Vật phẩm event siêu hiếm\n📜 Quest đảo đặc biệt\n💰 Drop tỉ lệ thấp, giá trị cao",
                requiredLevel = 60, requiresVipOrTicket = true, ticketName = "Vé Mộc Nhi",
                sceneName = "MocnhiIsland"
            },
        };
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("MapOverlay");
        mapList = root.Q<VisualElement>("MapList");
        btnClose = root.Q<Button>("BtnCloseMap");
        lblMapLevel = root.Q<Label>("LblMapLevel");

        // Detail
        detailPanel = root.Q<VisualElement>("MapDetailPanel");
        detailEmpty = root.Q<Label>("MapDetailEmpty");
        detailContent = root.Q<VisualElement>("MapDetailContent");
        lblMapIcon = root.Q<Label>("LblMapIcon");
        lblMapName = root.Q<Label>("LblMapName");
        statusBadge = root.Q<VisualElement>("MapStatusBadge");
        lblMapStatus = root.Q<Label>("LblMapStatus");
        lblMapDesc = root.Q<Label>("LblMapDesc");
        mapRequirements = root.Q<VisualElement>("MapRequirements");
        lblMapReqLevel = root.Q<Label>("LblMapReqLevel");
        lblMapReqExtra = root.Q<Label>("LblMapReqExtra");
        mapFeatures = root.Q<VisualElement>("MapFeatures");
        lblMapFeatures = root.Q<Label>("LblMapFeatures");
        btnTravel = root.Q<Button>("BtnTravel");

        // Cheat
        btnCheatLevel = root.Q<Button>("BtnCheatLevel");
        btnCheatVip = root.Q<Button>("BtnCheatVip");
    }

    private void RegisterCallbacks()
    {
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay) Hide();
        });

        btnTravel?.RegisterCallback<ClickEvent>(evt => OnTravelClicked());

        // Cheat buttons
        btnCheatLevel?.RegisterCallback<ClickEvent>(evt => OnCheatLevel());
        btnCheatVip?.RegisterCallback<ClickEvent>(evt => OnCheatVip());
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay == null) return;

        selectedLocation = null;
        selectedCard = null;
        cheatLevelIndex = 0;
        playerLevel = 1;
        isVip = false;
        hasHaiphuTicket = false;
        hasMocnhiTicket = false;

        UpdateLevelDisplay();
        RefreshLocationList();
        ShowEmptyDetails();

        overlay.style.display = DisplayStyle.Flex;
        Debug.Log("[Map] Opened World Map");
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[Map] Closed World Map");
        }
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Location List ──

    private void RefreshLocationList()
    {
        if (mapList == null) return;
        mapList.Clear();

        for (int i = 0; i < locations.Count; i++)
        {
            var loc = locations[i];
            bool unlocked = IsUnlocked(loc);
            var cardElement = CreateLocationCard(loc, unlocked, out var card);
            mapList.Add(cardElement);

            // Auto-select first
            if (i == 0)
            {
                SelectLocation(loc, card);
            }
        }
    }

    private VisualElement CreateLocationCard(MapLocation loc, bool unlocked, out VisualElement card)
    {
        var shadow = new VisualElement();
        shadow.AddToClassList("map-card-shadow");

        card = new VisualElement();
        card.AddToClassList("map-card");
        if (!unlocked) card.AddToClassList("map-card--locked");

        // Icon
        var iconWrap = new VisualElement();
        iconWrap.AddToClassList("map-card-icon-wrap");
        if (!unlocked) iconWrap.AddToClassList("map-card-icon-wrap--locked");
        var iconLabel = new Label(loc.icon);
        iconLabel.AddToClassList("map-card-icon");
        iconWrap.Add(iconLabel);

        // Info
        var info = new VisualElement();
        info.AddToClassList("map-card-info");

        var nameLabel = new Label(loc.name);
        nameLabel.AddToClassList("map-card-name");
        if (!unlocked) nameLabel.AddToClassList("map-card-name--locked");

        var subtitleLabel = new Label(loc.subtitle);
        subtitleLabel.AddToClassList("map-card-subtitle");

        info.Add(nameLabel);
        info.Add(subtitleLabel);

        card.Add(iconWrap);
        card.Add(info);

        // Lock badge
        if (!unlocked)
        {
            var lockBadge = new VisualElement();
            lockBadge.AddToClassList("map-card-lock");
            var lockIcon = new Label("🔒");
            lockIcon.AddToClassList("map-card-lock-icon");
            lockBadge.Add(lockIcon);
            card.Add(lockBadge);
        }

        shadow.Add(card);

        // Click
        var clickCard = card;
        var clickLoc = loc;
        card.RegisterCallback<ClickEvent>(evt => SelectLocation(clickLoc, clickCard));

        return shadow;
    }

    // ── Selection ──

    private void SelectLocation(MapLocation loc, VisualElement cardElement)
    {
        selectedCard?.RemoveFromClassList("map-card--selected");
        selectedLocation = loc;
        selectedCard = cardElement;
        selectedCard?.AddToClassList("map-card--selected");

        ShowLocationDetails(loc);
    }

    private void ShowLocationDetails(MapLocation loc)
    {
        if (detailEmpty != null) detailEmpty.style.display = DisplayStyle.None;
        if (detailContent != null) detailContent.style.display = DisplayStyle.Flex;

        bool unlocked = IsUnlocked(loc);

        if (lblMapIcon != null) lblMapIcon.text = loc.icon;
        if (lblMapName != null) lblMapName.text = loc.name;
        if (lblMapDesc != null) lblMapDesc.text = loc.description;

        // Status badge
        if (statusBadge != null)
        {
            statusBadge.RemoveFromClassList("map-status-badge--unlocked");
            statusBadge.RemoveFromClassList("map-status-badge--locked");

            if (unlocked)
            {
                statusBadge.AddToClassList("map-status-badge--unlocked");
                if (lblMapStatus != null) lblMapStatus.text = "ĐÃ MỞ KHÓA";
            }
            else
            {
                statusBadge.AddToClassList("map-status-badge--locked");
                if (lblMapStatus != null) lblMapStatus.text = "ĐANG KHÓA";
            }
        }

        // Requirements
        if (mapRequirements != null)
        {
            if (!unlocked && loc.requiredLevel > 0)
            {
                mapRequirements.style.display = DisplayStyle.Flex;
                if (lblMapReqLevel != null)
                {
                    string levelCheck = playerLevel >= loc.requiredLevel ? "✅" : "❌";
                    lblMapReqLevel.text = $"{levelCheck} Cấp {loc.requiredLevel} (hiện tại: Cấp {playerLevel})";
                }
                if (lblMapReqExtra != null)
                {
                    if (loc.requiresVipOrTicket)
                    {
                        bool hasAccess = isVip ||
                            (loc.id == "haiphu" && hasHaiphuTicket) ||
                            (loc.id == "mocnhi" && hasMocnhiTicket);
                        string extraCheck = hasAccess ? "✅" : "❌";
                        lblMapReqExtra.text = $"{extraCheck} VIP hoặc 🎫 {loc.ticketName}";
                        lblMapReqExtra.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        lblMapReqExtra.style.display = DisplayStyle.None;
                    }
                }
            }
            else
            {
                mapRequirements.style.display = DisplayStyle.None;
            }
        }

        // Features
        if (lblMapFeatures != null) lblMapFeatures.text = loc.features;

        // Travel button
        if (btnTravel != null)
        {
            btnTravel.SetEnabled(unlocked);
            btnTravel.text = unlocked ? "DI CHUYỂN" : "CHƯA MỞ KHÓA";
        }
    }

    private void ShowEmptyDetails()
    {
        selectedLocation = null;
        selectedCard?.RemoveFromClassList("map-card--selected");
        selectedCard = null;

        if (detailEmpty != null) detailEmpty.style.display = DisplayStyle.Flex;
        if (detailContent != null) detailContent.style.display = DisplayStyle.None;
    }

    // ── Unlock Logic ──

    private bool IsUnlocked(MapLocation loc)
    {
        // Farm is always unlocked
        if (loc.id == "farm") return true;

        // City requires tutorial completion
        if (loc.id == "city") return isTutorialCompleted;

        // Level check
        if (playerLevel < loc.requiredLevel) return false;

        // VIP or Ticket check
        if (loc.requiresVipOrTicket)
        {
            if (isVip) return true;
            if (loc.id == "haiphu" && hasHaiphuTicket) return true;
            if (loc.id == "mocnhi" && hasMocnhiTicket) return true;
            return false;
        }

        return true;
    }

    // ── Actions ──

    private void OnTravelClicked()
    {
        if (!selectedLocation.HasValue) return;

        var loc = selectedLocation.Value;
        if (!IsUnlocked(loc))
        {
            Debug.Log($"[Map] Không thể di chuyển đến {loc.name} — chưa mở khóa!");
            return;
        }

        Debug.Log($"[Map] Di chuyển đến: {loc.name} (Scene: {loc.sceneName})");
        // TODO: SceneManager.LoadScene(loc.sceneName);
        Hide();
    }

    // ── Cheat Buttons ──

    private void OnCheatLevel()
    {
        cheatLevelIndex = (cheatLevelIndex + 1) % cheatLevels.Length;
        playerLevel = cheatLevels[cheatLevelIndex];
        UpdateLevelDisplay();
        RefreshLocationList();
        Debug.Log($"[Map Cheat] Level set to: {playerLevel}");
    }

    private void OnCheatVip()
    {
        // Cycle: none → VIP → Hải Phú ticket → Mộc Nhi ticket → both tickets → none
        if (!isVip && !hasHaiphuTicket && !hasMocnhiTicket)
        {
            isVip = true;
            Debug.Log("[Map Cheat] VIP activated!");
        }
        else if (isVip)
        {
            isVip = false;
            hasHaiphuTicket = true;
            Debug.Log("[Map Cheat] VIP off, Vé Hải Phú on");
        }
        else if (hasHaiphuTicket && !hasMocnhiTicket)
        {
            hasMocnhiTicket = true;
            Debug.Log("[Map Cheat] Vé Hải Phú + Mộc Nhi on");
        }
        else
        {
            hasHaiphuTicket = false;
            hasMocnhiTicket = false;
            Debug.Log("[Map Cheat] All VIP/tickets off");
        }

        RefreshLocationList();
    }

    private void UpdateLevelDisplay()
    {
        if (lblMapLevel != null)
        {
            lblMapLevel.text = $"Lv. {playerLevel}";
        }
    }
}
