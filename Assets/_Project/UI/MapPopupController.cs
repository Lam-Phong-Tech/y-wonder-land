using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Visual World Map Popup.
/// Displays 5 islands on an ocean map. Click an island to confirm travel via ConfirmDialog.
/// Includes cheat buttons for testing level/VIP states.
/// </summary>
public class MapPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument mapDocument;

    [Tooltip("BẬT để hiện nút cheat Level/VIP (CHỈ để dev test). Build cho người chơi PHẢI tắt.")]
    [SerializeField] private bool showCheatButtons = false;

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
    private VisualElement lockMine;
    private VisualElement lockHaiphu;
    private VisualElement lockMocnhi;

    // Cheat
    private Button btnCheatLevel;
    private Button btnCheatVip;

    // Player Indicator
    private Button btnCompass;
    private VisualElement playerIndicator;
    private string currentLocation = "farm"; // Default player location
    private IVisualElementScheduledItem blinkSchedule;

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
                requiredLevel = 0, requiresVipOrTicket = false, ticketName = "", sceneName = "MineScene"
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
        lockMine = root.Q<VisualElement>("LockMine");
        lockHaiphu = root.Q<VisualElement>("LockHaiphu");
        lockMocnhi = root.Q<VisualElement>("LockMocnhi");

        // Cheat
        btnCheatLevel = root.Q<Button>("BtnCheatLevel");
        btnCheatVip = root.Q<Button>("BtnCheatVip");

        // Ẩn cả thanh cheat khỏi người chơi (trừ khi dev bật cờ test).
        var cheatBar = root.Q<VisualElement>(className: "map-cheat-bar");
        if (cheatBar != null) cheatBar.style.display = showCheatButtons ? DisplayStyle.Flex : DisplayStyle.None;

        // Player Indicator
        btnCompass = root.Q<Button>("BtnCompass");
        playerIndicator = root.Q<VisualElement>("PlayerIndicator");
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

        // Cheat
        btnCheatLevel?.RegisterCallback<ClickEvent>(evt => OnCheatLevel());
        btnCheatVip?.RegisterCallback<ClickEvent>(evt => OnCheatVip());

        // Compass
        btnCompass?.RegisterCallback<ClickEvent>(evt => OnCompassClicked());
    }

    private void RegisterIslandClick(VisualElement island, string id)
    {
        if (island == null) return;
        island.RegisterCallback<ClickEvent>(evt =>
        {
            evt.StopPropagation();
            HandleIslandClick(id, island);
        });
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay == null) return;

        selectedIslandId = null;
        selectedIslandElement = null;
        cheatLevelIndex = 0;
        var expManager = YWonderLand.Managers.ExperienceManager.Instance;
        playerLevel = expManager != null ? expManager.Level : 1;
        isVip = false;
        hasHaiphuTicket = false;
        hasMocnhiTicket = false;

        // Đồng bộ vị trí hiện tại của người chơi từ IslandTravelManager (nếu có)
        if (IslandTravelManager.Instance != null)
            currentLocation = IslandTravelManager.Instance.CurrentIslandId;

        UpdateLevelDisplay();
        RefreshIslandStates();
        UpdatePlayerIndicator();

        // Start blinking schedule
        if (playerIndicator != null)
        {
            blinkSchedule?.Pause();
            blinkSchedule = playerIndicator.schedule.Execute(() => 
            {
                playerIndicator.ToggleInClassList("map-indicator-blink");
            }).Every(800);
        }

        overlay.style.display = DisplayStyle.Flex;
        UIPopupTracker.SetOpen(this, true);
        Debug.Log("[Map] Opened Visual World Map");
    }

    public void Hide()
    {
        if (overlay != null)
        {
            blinkSchedule?.Pause();
            overlay.style.display = DisplayStyle.None;
            UIPopupTracker.SetOpen(this, false);
            Debug.Log("[Map] Closed World Map");
        }
    }

    // An toàn: nếu popup bị tắt/destroy khi đang mở (vd đổi đảo) mà chưa kịp gọi Hide(),
    // vẫn gỡ khỏi UIPopupTracker để chuột không bị kẹt + tương tác thế giới không chết.
    private void OnDisable()
    {
        UIPopupTracker.SetOpen(this, false);
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Island State Management ──

    private void RefreshIslandStates()
    {
        // Update lock overlays
        UpdateLockOverlay(lockMine, "mine");
        UpdateLockOverlay(lockHaiphu, "haiphu");
        UpdateLockOverlay(lockMocnhi, "mocnhi");
    }

    private void UpdateLockOverlay(VisualElement lockElement, string id)
    {
        if (lockElement == null || !locationData.ContainsKey(id)) return;
        bool unlocked = IsUnlocked(locationData[id]);
        lockElement.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void UpdatePlayerIndicator()
    {
        if (playerIndicator == null) return;
        
        // Remove all old position classes
        playerIndicator.RemoveFromClassList("map-island--farm");
        playerIndicator.RemoveFromClassList("map-island--city");
        playerIndicator.RemoveFromClassList("map-island--mine");
        playerIndicator.RemoveFromClassList("map-island--haiphu");
        playerIndicator.RemoveFromClassList("map-island--mocnhi");

        // Add the current location's position class to perfectly align it
        playerIndicator.AddToClassList($"map-island--{currentLocation}");
    }

    private void OnCompassClicked()
    {
        Debug.Log($"[Map] Compass clicked. Current location: {currentLocation}");
        
        // Find the visual element for the current island
        VisualElement currentIslandElement = null;
        switch (currentLocation)
        {
            case "farm": currentIslandElement = islandFarm; break;
            case "city": currentIslandElement = islandCity; break;
            case "mine": currentIslandElement = islandMine; break;
            case "haiphu": currentIslandElement = islandHaiphu; break;
            case "mocnhi": currentIslandElement = islandMocnhi; break;
        }

        // Programmatically select it to visually show where they are without triggering travel dialog
        if (currentIslandElement != null)
        {
            // Deselect previous
            selectedIslandElement?.RemoveFromClassList("map-island--selected");
            
            // Select current
            selectedIslandId = currentLocation;
            selectedIslandElement = currentIslandElement;
            selectedIslandElement.AddToClassList("map-island--selected");
        }
    }

    // ── Selection & Interaction ──

    private void HandleIslandClick(string id, VisualElement islandElement)
    {
        // Deselect previous visually
        selectedIslandElement?.RemoveFromClassList("map-island--selected");

        selectedIslandId = id;
        selectedIslandElement = islandElement;
        selectedIslandElement?.AddToClassList("map-island--selected");

        if (!locationData.ContainsKey(id)) return;
        var loc = locationData[id];
        bool unlocked = IsUnlocked(loc);

        ConfirmDialogController dialog = FindFirstObjectByType<ConfirmDialogController>();

        if (unlocked)
        {
            if (dialog != null)
            {
                dialog.Show(
                    "XÁC NHẬN CHUYỂN ĐẢO",
                    $"Bạn có chắc chắn muốn di chuyển đến {loc.name} không?",
                    "Di chuyển",
                    "Hủy",
                    () =>
                    {
                        Hide();
                        Debug.Log($"[Map] ✈ Di chuyển đến: {loc.name} (id: {loc.id})");
                        if (IslandTravelManager.Instance != null)
                        {
                            IslandTravelManager.Instance.TravelToIsland(loc.id);
                        }
                        else
                        {
                            Debug.LogWarning($"[Map] Chưa có IslandTravelManager trong scene! Không thể tới {loc.name}. " +
                                             "Hãy thêm GameObject 'IslandTravelManager' vào Scene 0.");
                        }
                    },
                    ConfirmDialogType.Info
                );
            }
            else
            {
                Debug.Log($"[Map] ✈ Di chuyển đến: {loc.name} (Scene: {loc.sceneName})");
                Hide();
            }
        }
        else
        {
            string reqMsg = "Chưa đủ điều kiện để di chuyển.";

            if (dialog != null)
            {
                dialog.Show(
                    "CHƯA THỂ DI CHUYỂN",
                    reqMsg,
                    "Đóng",
                    "",
                    null,
                    ConfirmDialogType.Warning
                );
            }
            else
            {
                Debug.Log($"[Map] Chưa mở khóa: {loc.name}. {reqMsg}");
            }
        }
    }

    private void DeselectIsland()
    {
        selectedIslandElement?.RemoveFromClassList("map-island--selected");
        selectedIslandId = null;
        selectedIslandElement = null;
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
