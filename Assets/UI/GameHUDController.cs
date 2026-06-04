using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the In-Game HUD.
/// Mockup only — logs button clicks for testing.
/// Hook up with actual game systems later.
/// </summary>
public class GameHUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SettingsPopupController settingsPopup;
    [SerializeField] private InventoryPopupController inventoryPopup;
    [SerializeField] private LeaderboardPopupController leaderboardPopup;
    [SerializeField] private FriendsPopupController friendsPopup;
    [SerializeField] private MailboxPopupController mailboxPopup;
    [SerializeField] private ProfilePopupController profilePopup;
    [SerializeField] private AttendancePopupController attendancePopup;
    [SerializeField] private QuestPopupController questPopup;
    [SerializeField] private ShopPopupController shopPopup;
    [SerializeField] private MapPopupController mapPopup;

    private UIDocument uiDocument;

    // Player Info
    private VisualElement playerInfo;
    private Label playerName;
    private Label playerLevel;
    private Label playerCurrencySmall;

    // Currency
    private Label currencyValue;

    // Quest
    private VisualElement questBubble;
    private Label questText;

    // Sidebar buttons
    private Button btnLeaderboard;
    private Button btnCalendar;
    private Button btnMail;
    private Button btnFriends;
    private Button btnShop;
    private Button btnMap;

    // Action buttons
    private Button btnInteract;
    private Button btnCancel;
    private Button btnJump;
    private Button btnBag;
    private Button btnSettings;

    // Messages
    private Button btnEmoji;
    private Button btnExpand;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[GameHUD] UIDocument component not found!");
            return;
        }

        // Fallback auto-find if references are not assigned in Inspector
        if (shopPopup == null) shopPopup = FindFirstObjectByType<ShopPopupController>();
        if (settingsPopup == null) settingsPopup = FindFirstObjectByType<SettingsPopupController>();
        if (inventoryPopup == null) inventoryPopup = FindFirstObjectByType<InventoryPopupController>();
        if (leaderboardPopup == null) leaderboardPopup = FindFirstObjectByType<LeaderboardPopupController>();
        if (friendsPopup == null) friendsPopup = FindFirstObjectByType<FriendsPopupController>();
        if (mailboxPopup == null) mailboxPopup = FindFirstObjectByType<MailboxPopupController>();
        if (profilePopup == null) profilePopup = FindFirstObjectByType<ProfilePopupController>();
        if (attendancePopup == null) attendancePopup = FindFirstObjectByType<AttendancePopupController>();
        if (questPopup == null) questPopup = FindFirstObjectByType<QuestPopupController>();
        if (mapPopup == null) mapPopup = FindFirstObjectByType<MapPopupController>();

        var root = uiDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();

        // Set initial values (mockup data)
        SetPlayerInfo("YWonderPlayer", 1);
        SetPlayerEXP(0.00f);
        SetCurrency(84.00f);
        SetQuest("Khám phá đảo hoang và tìm ngôi nhà đầu tiên!");
    }

    private void QueryElements(VisualElement root)
    {
        // Player Info
        playerInfo = root.Q<VisualElement>("PlayerInfo");
        playerName = root.Q<Label>("PlayerName");
        playerLevel = root.Q<Label>("PlayerLevel");
        playerCurrencySmall = root.Q<Label>("PlayerEXP");

        // Currency
        currencyValue = root.Q<Label>("CurrencyValue");

        // Quest
        questBubble = root.Q<VisualElement>("QuestBubble");
        questText = root.Q<Label>("QuestText");

        // Sidebar
        btnLeaderboard = root.Q<Button>("BtnLeaderboard");
        btnCalendar = root.Q<Button>("BtnCalendar");
        btnMail = root.Q<Button>("BtnMail");
        btnFriends = root.Q<Button>("BtnFriends");
        btnShop = root.Q<Button>("BtnShop");
        btnMap = root.Q<Button>("BtnMap");

        // Actions
        btnInteract = root.Q<Button>("BtnInteract");
        btnCancel = root.Q<Button>("BtnCancel");
        btnJump = root.Q<Button>("BtnJump");
        btnBag = root.Q<Button>("BtnBag");
        btnSettings = root.Q<Button>("BtnSettings");

        // Messages
        btnEmoji = root.Q<Button>("BtnEmoji");
        btnExpand = root.Q<Button>("BtnExpand");
    }

    private void RegisterCallbacks()
    {
        // Clickable HUD Elements (Player Info & Quest)
        playerInfo?.RegisterCallback<ClickEvent>(evt =>
        {
            if (profilePopup != null)
            {
                string name = playerName != null ? playerName.text : "Player";
                string levelStr = playerLevel != null ? playerLevel.text : "Level: 1";
                string expStr = playerCurrencySmall != null ? playerCurrencySmall.text : "0.00";
                profilePopup.Show(name, levelStr, expStr);
            }
            else
                Debug.Log("[GameHUD] Player Info / Avatar clicked (no profile popup assigned)");
        });

        questBubble?.RegisterCallback<ClickEvent>(evt =>
        {
            if (questPopup != null)
                questPopup.Show();
            else
                Debug.Log("[GameHUD] Quest Bubble clicked (no quest popup assigned)");
        });

        // Sidebar buttons
        btnLeaderboard?.RegisterCallback<ClickEvent>(evt =>
        {
            if (leaderboardPopup != null)
                leaderboardPopup.Show();
            else
                Debug.Log("[GameHUD] Leaderboard clicked (no popup assigned)");
        });

        btnCalendar?.RegisterCallback<ClickEvent>(evt =>
        {
            if (attendancePopup != null)
                attendancePopup.Show();
            else
                Debug.Log("[GameHUD] Daily Attendance clicked (no popup assigned)");
        });

        btnMail?.RegisterCallback<ClickEvent>(evt =>
        {
            if (mailboxPopup != null)
                mailboxPopup.Show();
            else
                Debug.Log("[GameHUD] Mail clicked (no popup assigned)");
        });

        btnFriends?.RegisterCallback<ClickEvent>(evt =>
        {
            if (friendsPopup != null)
                friendsPopup.Show();
            else
                Debug.Log("[GameHUD] Friends clicked (no popup assigned)");
        });

        btnShop?.RegisterCallback<ClickEvent>(evt =>
        {
            if (shopPopup != null)
                shopPopup.Show();
            else
                Debug.Log("[GameHUD] Shop clicked (no shop popup assigned)");
        });

        btnMap?.RegisterCallback<ClickEvent>(evt =>
        {
            if (mapPopup != null)
                mapPopup.Show();
            else
                Debug.Log("[GameHUD] Map clicked (no map popup assigned)");
        });

        // Action buttons
        btnInteract?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[GameHUD] Interact clicked");
        });

        btnCancel?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[GameHUD] Cancel clicked");
        });

        btnJump?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[GameHUD] Jump clicked");
        });

        // Messages
        btnEmoji?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[GameHUD] Emoji clicked");
        });

        btnExpand?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[GameHUD] Expand messages clicked");
        });

        // Bag
        btnBag?.RegisterCallback<ClickEvent>(evt =>
        {
            if (inventoryPopup != null)
                inventoryPopup.Show();
            else
                Debug.Log("[GameHUD] Bag / Inventory clicked (no popup assigned)");
        });

        // Settings
        btnSettings?.RegisterCallback<ClickEvent>(evt =>
        {
            if (settingsPopup != null)
                settingsPopup.Show();
            else
                Debug.Log("[GameHUD] Settings clicked (no popup assigned)");
        });
    }

    // ── Public API for Game Systems ──

    /// <summary>
    /// Update player name and level display.
    /// </summary>
    public void SetPlayerInfo(string name, int level)
    {
        if (playerName != null) playerName.text = name;
        if (playerLevel != null) playerLevel.text = $"Level: {level}";
    }

    /// <summary>
    /// Update EXP display (0.00 to 100.00 = level up).
    /// </summary>
    public void SetPlayerEXP(float exp)
    {
        if (playerCurrencySmall != null) playerCurrencySmall.text = exp.ToString("F2");
    }

    /// <summary>
    /// Update currency display (single currency).
    /// </summary>
    public void SetCurrency(float amount)
    {
        if (currencyValue != null) currencyValue.text = amount.ToString("F2");
    }

    /// <summary>
    /// Update or hide the quest notification.
    /// </summary>
    public void SetQuest(string text)
    {
        if (questBubble == null) return;

        if (string.IsNullOrEmpty(text))
        {
            questBubble.style.display = DisplayStyle.None;
        }
        else
        {
            questBubble.style.display = DisplayStyle.Flex;
            if (questText != null) questText.text = text;
        }
    }

    /// <summary>
    /// Show or hide the entire HUD.
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
