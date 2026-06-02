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

    private UIDocument uiDocument;

    // Player Info
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
    private Button btnFriends;
    private Button btnMail;
    private Button btnCharacter;

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
        btnFriends = root.Q<Button>("BtnFriends");
        btnMail = root.Q<Button>("BtnMail");
        btnCharacter = root.Q<Button>("BtnCharacter");

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
        // Sidebar buttons
        btnLeaderboard?.RegisterCallback<ClickEvent>(evt =>
        {
            if (leaderboardPopup != null)
                leaderboardPopup.Show();
            else
                Debug.Log("[GameHUD] Leaderboard clicked (no popup assigned)");
        });

        btnFriends?.RegisterCallback<ClickEvent>(evt =>
        {
            if (friendsPopup != null)
                friendsPopup.Show();
            else
                Debug.Log("[GameHUD] Friends clicked (no popup assigned)");
        });

        btnMail?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[GameHUD] Mail clicked");
        });

        btnCharacter?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("[GameHUD] Character clicked");
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
