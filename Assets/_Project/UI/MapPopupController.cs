using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Visual World Map Popup.
/// Displays 5 islands on an ocean map; click an island to see info card, then confirm travel.
/// Includes cheat buttons for testing level/VIP states.
/// </summary>
public class MapPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument mapDocument;

    // Top-level
    private VisualElement overlay;
    private Button btnClose;
    private Label lblMapLevel;

    // Islands
    private VisualElement islandFarm;
    private VisualElement islandCity;
    private VisualElement islandMine;
    private VisualElement islandHaiphu;
    private VisualElement islandMocnhi;
    private VisualElement lockHaiphu;
    private VisualElement lockMocnhi;

    // Info card
    private VisualElement infoCard;
    private Label lblInfoIcon;
    private Label lblInfoName;
    private VisualElement infoStatusBadge;
    private Label lblInfoStatus;
    private Label lblInfoDesc;
    private VisualElement infoRequirements;
    private Label lblInfoReqLevel;
    private Label lblInfoReqExtra;
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

    private string selectedIslandId = null;
    private VisualElement selectedIslandElement = null;

    // Cheat level cycle
    private int[] cheatLevels = { 1, 5, 15, 45, 65 };
    private int cheatLevelIndex = 0;

    // ── Location Data ──

    private class MapLocation
    {
        public string id;
        public string icon;
        public string name;
        public string description;
        public int requiredLevel;
        public bool requiresVipOrTicket;
        public string ticketName;
        public string sceneName;
    }

    private Dictionary<string, MapLocation> locationData;

    private void Awake()
    {
        if (mapDocument == null)
            mapDocument = GetComponent<UIDocument>();

        InitLocationData();
    }

    private void OnEnable()
    {
        var root = mapDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
    }

    private void InitLocationData()
    {
        locationData = new Dictionary<string, MapLocation>
        {
            ["farm"] = new MapLocation
            {
                id = "farm", icon = "🏡", name = "Nông trại",
                description = "Khu vực chính! Cuốc đất, trồng hạt giống, tưới nước, chăn nuôi, chặt cây, xây dựng chuồng và đường đi.",
                requiredLevel = 0, requiresVipOrTicket = false, ticketName = "", sceneName = "FarmScene"
            },
            ["city"] = new MapLocation
            {
                id = "city", icon = "🏙️", name = "Thành phố",
                description = "Phố thương mại sầm uất với 13 cửa hàng: câu cá, workshop, store, mini garden, heo đất và nhiều hơn nữa!",
                requiredLevel = 0, requiresVipOrTicket = false, ticketName = "", sceneName = "CityScene"
            },
            ["mine"] = new MapLocation
            {
                id = "mine", icon = "⛏️", name = "Khai thác mỏ",
                description = "Hang động huyền bí với quặng quý. Mỗi ngày được đào miễn phí 10 lần. Quặng dùng để chế tác và xây đèn.",
                requiredLevel = 10, requiresVipOrTicket = false, ticketName = "", sceneName = "MineScene"
            },
            ["haiphu"] = new MapLocation
            {
                id = "haiphu", icon = "🏝️", name = "Đảo Hải Phú",
                description = "Đảo nhiệt đới biển xanh với bãi câu cá đặc biệt, hạt giống và vật nuôi exclusive theo mùa sự kiện.",
                requiredLevel = 40, requiresVipOrTicket = true, ticketName = "Vé Hải Phú", sceneName = "HaiphuIsland"
            },
            ["mocnhi"] = new MapLocation
            {
                id = "mocnhi", icon = "🌲", name = "Đảo Mộc Nhi",
                description = "Rừng huyền bí bao phủ sương mù. Vật phẩm event siêu hiếm, quest đặc biệt, drop tỉ lệ thấp nhưng giá trị cao.",
                requiredLevel = 60, requiresVipOrTicket = true, ticketName = "Vé Mộc Nhi", sceneName = "MocnhiIsland"
            },
        };
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("MapOverlay");
        btnClose = root.Q<Button>("BtnCloseMap");
        lblMapLevel = root.Q<Label>("LblMapLevel");

        // Islands
        islandFarm = root.Q<VisualElement>("IslandFarm");
        islandCity = root.Q<VisualElement>("IslandCity");
        islandMine = root.Q<VisualElement>("IslandMine");
        islandHaiphu = root.Q<VisualElement>("IslandHaiphu");
        islandMocnhi = root.Q<VisualElement>("IslandMocnhi");
        lockHaiphu = root.Q<VisualElement>("LockHaiphu");
        lockMocnhi = root.Q<VisualElement>("LockMocnhi");

        // Info card
        infoCard = root.Q<VisualElement>("MapInfoCard");
        lblInfoIcon = root.Q<Label>("LblInfoIcon");
        lblInfoName = root.Q<Label>("LblInfoName");
        infoStatusBadge = root.Q<VisualElement>("InfoStatusBadge");
        lblInfoStatus = root.Q<Label>("LblInfoStatus");
        lblInfoDesc = root.Q<Label>("LblInfoDesc");
        infoRequirements = root.Q<VisualElement>("InfoRequirements");
        lblInfoReqLevel = root.Q<Label>("LblInfoReqLevel");
        lblInfoReqExtra = root.Q<Label>("LblInfoReqExtra");
        btnTravel = root.Q<Button>("BtnTravel");

        // Cheat
        btnCheatLevel = root.Q<Button>("BtnCheatLevel");
        btnCheatVip = root.Q<Button>("BtnCheatVip");
    }

    private void RegisterCallbacks()
    {
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());

        // Click ocean to deselect
        var mapWorld = overlay?.Q<VisualElement>("MapWorld");
        mapWorld?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == mapWorld) DeselectIsland();
        });

        // Island clicks
        RegisterIslandClick(islandFarm, "farm");
        RegisterIslandClick(islandCity, "city");
        RegisterIslandClick(islandMine, "mine");
        RegisterIslandClick(islandHaiphu, "haiphu");
        RegisterIslandClick(islandMocnhi, "mocnhi");

        // Travel
        btnTravel?.RegisterCallback<ClickEvent>(evt => OnTravelClicked());

        // Cheat
        btnCheatLevel?.RegisterCallback<ClickEvent>(evt => OnCheatLevel());
        btnCheatVip?.RegisterCallback<ClickEvent>(evt => OnCheatVip());
    }

    private void RegisterIslandClick(VisualElement island, string id)
    {
        if (island == null) return;
        island.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            SelectIsland(id, island);
        });
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay == null) return;

        selectedIslandId = null;
        selectedIslandElement = null;
        cheatLevelIndex = 0;
        playerLevel = 1;
        isVip = false;
        hasHaiphuTicket = false;
        hasMocnhiTicket = false;

        UpdateLevelDisplay();
        RefreshIslandStates();
        HideInfoCard();

        overlay.style.display = DisplayStyle.Flex;
        Debug.Log("[Map] Opened Visual World Map");
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

    // ── Island State Management ──

    private void RefreshIslandStates()
    {
        // Update lock overlays
        UpdateLockOverlay(lockHaiphu, "haiphu");
        UpdateLockOverlay(lockMocnhi, "mocnhi");

        // Re-select current island if any (refresh info card)
        if (selectedIslandId != null && selectedIslandElement != null)
        {
            ShowInfoCard(selectedIslandId);
        }
    }

    private void UpdateLockOverlay(VisualElement lockElement, string id)
    {
        if (lockElement == null || !locationData.ContainsKey(id)) return;
        bool unlocked = IsUnlocked(locationData[id]);
        lockElement.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
    }

    // ── Selection ──

    private void SelectIsland(string id, VisualElement islandElement)
    {
        // Deselect previous
        selectedIslandElement?.RemoveFromClassList("map-island--selected");

        selectedIslandId = id;
        selectedIslandElement = islandElement;
        selectedIslandElement?.AddToClassList("map-island--selected");

        ShowInfoCard(id);
    }

    private void DeselectIsland()
    {
        selectedIslandElement?.RemoveFromClassList("map-island--selected");
        selectedIslandId = null;
        selectedIslandElement = null;
        HideInfoCard();
    }

    private void ShowInfoCard(string id)
    {
        if (!locationData.ContainsKey(id) || infoCard == null) return;

        var loc = locationData[id];
        bool unlocked = IsUnlocked(loc);

        if (lblInfoIcon != null) lblInfoIcon.text = loc.icon;
        if (lblInfoName != null) lblInfoName.text = loc.name;
        if (lblInfoDesc != null) lblInfoDesc.text = loc.description;

        // Status badge
        if (infoStatusBadge != null)
        {
            infoStatusBadge.RemoveFromClassList("map-info-status--unlocked");
            infoStatusBadge.RemoveFromClassList("map-info-status--locked");

            if (unlocked)
            {
                infoStatusBadge.AddToClassList("map-info-status--unlocked");
                if (lblInfoStatus != null) lblInfoStatus.text = "ĐÃ MỞ KHÓA";
            }
            else
            {
                infoStatusBadge.AddToClassList("map-info-status--locked");
                if (lblInfoStatus != null) lblInfoStatus.text = "ĐANG KHÓA";
            }
        }

        // Requirements
        if (infoRequirements != null)
        {
            if (!unlocked && loc.requiredLevel > 0)
            {
                infoRequirements.style.display = DisplayStyle.Flex;

                if (lblInfoReqLevel != null)
                {
                    string check = playerLevel >= loc.requiredLevel ? "✅" : "❌";
                    lblInfoReqLevel.text = $"{check} Cần cấp {loc.requiredLevel} (hiện: Lv.{playerLevel})";
                }
                if (lblInfoReqExtra != null)
                {
                    if (loc.requiresVipOrTicket)
                    {
                        bool hasAccess = isVip ||
                            (loc.id == "haiphu" && hasHaiphuTicket) ||
                            (loc.id == "mocnhi" && hasMocnhiTicket);
                        string check = hasAccess ? "✅" : "❌";
                        lblInfoReqExtra.text = $"{check} VIP hoặc 🎫 {loc.ticketName}";
                        lblInfoReqExtra.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        lblInfoReqExtra.style.display = DisplayStyle.None;
                    }
                }
            }
            else
            {
                infoRequirements.style.display = DisplayStyle.None;
            }
        }

        // Travel button
        if (btnTravel != null)
        {
            btnTravel.SetEnabled(unlocked);
            btnTravel.text = unlocked ? "🚀 DI CHUYỂN" : "🔒 CHƯA MỞ KHÓA";
        }

        infoCard.style.display = DisplayStyle.Flex;
    }

    private void HideInfoCard()
    {
        if (infoCard != null)
            infoCard.style.display = DisplayStyle.None;
    }

    // ── Unlock Logic ──

    private bool IsUnlocked(MapLocation loc)
    {
        if (loc.id == "farm") return true;
        if (loc.id == "city") return isTutorialCompleted;
        if (playerLevel < loc.requiredLevel) return false;

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
        if (selectedIslandId == null || !locationData.ContainsKey(selectedIslandId)) return;

        var loc = locationData[selectedIslandId];
        if (!IsUnlocked(loc))
        {
            Debug.Log($"[Map] Chưa mở khóa: {loc.name}");
            return;
        }

        Debug.Log($"[Map] ✈ Di chuyển đến: {loc.name} (Scene: {loc.sceneName})");
        // TODO: SceneManager.LoadScene(loc.sceneName);
        Hide();
    }

    // ── Cheat ──

    private void OnCheatLevel()
    {
        cheatLevelIndex = (cheatLevelIndex + 1) % cheatLevels.Length;
        playerLevel = cheatLevels[cheatLevelIndex];
        UpdateLevelDisplay();
        RefreshIslandStates();
        Debug.Log($"[Map Cheat] Level → {playerLevel}");
    }

    private void OnCheatVip()
    {
        if (!isVip && !hasHaiphuTicket && !hasMocnhiTicket)
        {
            isVip = true;
            Debug.Log("[Map Cheat] VIP ON");
        }
        else if (isVip)
        {
            isVip = false;
            hasHaiphuTicket = true;
            Debug.Log("[Map Cheat] VIP off → Vé Hải Phú ON");
        }
        else if (hasHaiphuTicket && !hasMocnhiTicket)
        {
            hasMocnhiTicket = true;
            Debug.Log("[Map Cheat] Vé Hải Phú + Mộc Nhi ON");
        }
        else
        {
            hasHaiphuTicket = false;
            hasMocnhiTicket = false;
            Debug.Log("[Map Cheat] All VIP/tickets OFF");
        }

        RefreshIslandStates();
    }

    private void UpdateLevelDisplay()
    {
        if (lblMapLevel != null)
            lblMapLevel.text = $"Lv. {playerLevel}";
    }
}
