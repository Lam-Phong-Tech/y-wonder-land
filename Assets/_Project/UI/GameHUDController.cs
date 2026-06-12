using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Controller for the In-Game HUD.
/// Mockup only — logs button clicks for testing.
/// Hook up with actual game systems later.
/// </summary>
public class GameHUDController : MonoBehaviour
{
    public static GameHUDController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SettingsPopupController settingsPopup;
    [SerializeField] private InventoryPopupController inventoryPopup;
    [SerializeField] private LeaderboardPopupController leaderboardPopup;
    [SerializeField] private FriendsPopupController friendsPopup;
    [SerializeField] private MailboxPopupController mailboxPopup;
    [SerializeField] private ProfilePopupController profilePopup;

    [SerializeField] private QuestPopupController questPopup;
    [SerializeField] private ShopPopupController shopPopup;
    [SerializeField] private MapPopupController mapPopup;
    [SerializeField] private PiggyBankPopupController piggyBankPopup;
    [SerializeField] private LevelUpOverlayController levelUpOverlay;
    [SerializeField] private EventPopupController eventPopup;
    [SerializeField] private FishingOverlayController fishingOverlay;
    [SerializeField] private BuildModeOverlayController buildModeOverlay;
    [SerializeField] private WorkshopPopupController workshopPopup;

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
    private Button btnEvent;
    private Button btnPiggy;
    private Button btnFishing;
    private Button btnWorkshop;
    private Button btnBuild;

    // Action buttons
    private Button btnInteract;
    private Button btnCancel;
    private Button btnJump;
    private Button btnBag;
    private Button btnSettings;
    private Button btnSprint;
    private Label interactionPrompt;



    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[GameHUD] UIDocument component not found!");
            return;
        }

        // Fallback auto-find if references are not assigned in Inspector
        if (shopPopup == null) shopPopup = FindFirstObjectByType<ShopPopupController>(FindObjectsInactive.Include);
        if (settingsPopup == null) settingsPopup = FindFirstObjectByType<SettingsPopupController>(FindObjectsInactive.Include);
        if (inventoryPopup == null) inventoryPopup = FindFirstObjectByType<InventoryPopupController>(FindObjectsInactive.Include);
        if (leaderboardPopup == null) leaderboardPopup = FindFirstObjectByType<LeaderboardPopupController>(FindObjectsInactive.Include);
        if (friendsPopup == null) friendsPopup = FindFirstObjectByType<FriendsPopupController>(FindObjectsInactive.Include);
        if (mailboxPopup == null) mailboxPopup = FindFirstObjectByType<MailboxPopupController>(FindObjectsInactive.Include);        if (profilePopup == null) profilePopup = FindFirstObjectByType<ProfilePopupController>(FindObjectsInactive.Include);
        if (questPopup == null) questPopup = FindFirstObjectByType<QuestPopupController>(FindObjectsInactive.Include);
        if (mapPopup == null) mapPopup = FindFirstObjectByType<MapPopupController>(FindObjectsInactive.Include);
        if (piggyBankPopup == null) piggyBankPopup = FindFirstObjectByType<PiggyBankPopupController>(FindObjectsInactive.Include);
        if (levelUpOverlay == null) levelUpOverlay = FindFirstObjectByType<LevelUpOverlayController>(FindObjectsInactive.Include);
        if (eventPopup == null) eventPopup = FindFirstObjectByType<EventPopupController>(FindObjectsInactive.Include);
        if (fishingOverlay == null) fishingOverlay = FindFirstObjectByType<FishingOverlayController>(FindObjectsInactive.Include);
        if (buildModeOverlay == null) buildModeOverlay = FindFirstObjectByType<BuildModeOverlayController>(FindObjectsInactive.Include);
        if (workshopPopup == null) workshopPopup = FindFirstObjectByType<WorkshopPopupController>(FindObjectsInactive.Include);

        // Đảm bảo GameHUD luôn nằm dưới các Popup để popup block được thao tác chuột
        uiDocument.sortingOrder = -10;

        var root = uiDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();



        // Set initial values
        SetPlayerInfo("YWonderPlayer", 1);
        SetPlayerEXP(0.00f);
        SetQuest("Kh\u00e1m ph\u00e1 \u0111\u1ea3o hoang v\u00e0 t\u00ecm ng\u00f4i nh\u00e0 \u0111\u1ea7u ti\u00ean!");

        // Sync player name from GameManager (retry until available)
        StartCoroutine(SyncPlayerName());

        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            SetCurrency(YWonderLand.Managers.EconomyManager.Instance.GetPOS());
            YWonderLand.Managers.EconomyManager.Instance.OnPOSChanged += SetCurrency;
        }
    }

    void OnDisable()
    {
        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            YWonderLand.Managers.EconomyManager.Instance.OnPOSChanged -= SetCurrency;
        }
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
        btnEvent = root.Q<Button>("BtnEvent");
        btnMail = root.Q<Button>("BtnMail");
        btnFriends = root.Q<Button>("BtnFriends");
        btnShop = root.Q<Button>("BtnShop");
        btnMap = root.Q<Button>("BtnMap");
        btnPiggy = root.Q<Button>("BtnPiggy");
        btnFishing = root.Q<Button>("BtnFishing");
        btnWorkshop = root.Q<Button>("BtnWorkshop");
        btnBuild = root.Q<Button>("BtnBuild");

        // Actions
        btnInteract = root.Q<Button>("BtnInteract");
        btnCancel = root.Q<Button>("BtnCancel");
        btnJump = root.Q<Button>("BtnJump");
        btnBag = root.Q<Button>("BtnBag");
        btnSettings = root.Q<Button>("BtnSettings");
        btnSprint = root.Q<Button>("BtnSprint");
        interactionPrompt = root.Q<Label>("InteractionPrompt");


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
            HideAllPopups();
            if (leaderboardPopup != null)
                leaderboardPopup.Show();
            else
                Debug.Log("[GameHUD] Leaderboard clicked (no popup assigned)");
        });

        btnCalendar?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (eventPopup != null)
                eventPopup.ShowTab(2);
            else
                Debug.Log("[GameHUD] Daily Attendance clicked (no event popup assigned)");
        });

        btnMail?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (mailboxPopup != null)
                mailboxPopup.Show();
            else
                Debug.Log("[GameHUD] Mail clicked (no popup assigned)");
        });

        btnFriends?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (friendsPopup != null)
                friendsPopup.Show();
            else
                Debug.Log("[GameHUD] Friends clicked (no popup assigned)");
        });

        btnShop?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (shopPopup != null)
                shopPopup.Show();
            else
                Debug.Log("[GameHUD] Shop clicked (no shop popup assigned)");
        });

        btnMap?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (mapPopup != null)
                mapPopup.Show();
            else
                Debug.Log("[GameHUD] Map clicked (no map popup assigned)");
        });

        btnPiggy?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (piggyBankPopup != null)
                piggyBankPopup.Show();
            else
                Debug.Log("[GameHUD] Piggy Bank clicked (no piggy popup assigned)");
        });

        btnFishing?.RegisterCallback<ClickEvent>(evt =>
        {
            // Fishing is an overlay, but let's hide other popups anyway
            HideAllPopups();
            if (fishingOverlay != null)
                fishingOverlay.Show();
            else
                Debug.Log("[GameHUD] Fishing clicked (no popup assigned)");
        });

        btnWorkshop?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (workshopPopup != null)
                workshopPopup.Show();
            else
                Debug.Log("[GameHUD] Workshop clicked (no popup assigned)");
        });

        btnBuild?.RegisterCallback<ClickEvent>(evt =>
        {
            if (buildModeOverlay != null)
            {
                if (buildModeOverlay.IsVisible())
                    buildModeOverlay.Hide();
                else
                    buildModeOverlay.Show();
            }
            else
                Debug.Log("[GameHUD] Build clicked (no overlay assigned)");
        });

        btnEvent?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (eventPopup != null)
                eventPopup.Show();
            else
                Debug.Log("[GameHUD] Event clicked (no event popup assigned)");
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

        if (btnSprint != null)
        {
            btnSprint.RegisterCallback<PointerDownEvent>(evt => 
            {
                if (PlayerController.Instance != null) PlayerController.Instance.SetSprintUI(true);
            });
            btnSprint.RegisterCallback<PointerUpEvent>(evt => 
            {
                if (PlayerController.Instance != null) PlayerController.Instance.SetSprintUI(false);
            });
            btnSprint.RegisterCallback<PointerOutEvent>(evt => 
            {
                if (PlayerController.Instance != null) PlayerController.Instance.SetSprintUI(false);
            });
        }        // Bag
        btnBag?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (inventoryPopup != null)
                inventoryPopup.Show();
            else
                Debug.Log("[GameHUD] Bag / Inventory clicked (no popup assigned)");
        });

        // Settings
        btnSettings?.RegisterCallback<ClickEvent>(evt =>
        {
            HideAllPopups();
            if (settingsPopup != null)
                settingsPopup.Show();
            else
                Debug.Log("[GameHUD] Settings clicked (no popup assigned)");
        });
    }

    /// <summary>
    /// Đảm bảo tính "kỷ luật", không cho phép mở chồng chéo nhiều popup.
    /// </summary>
    public void HideAllPopups()
    {
        if (shopPopup != null) shopPopup.Hide();
        if (settingsPopup != null) settingsPopup.Hide();
        if (inventoryPopup != null) inventoryPopup.Hide();
        if (leaderboardPopup != null) leaderboardPopup.Hide();
        if (friendsPopup != null) friendsPopup.Hide();
        if (mailboxPopup != null) mailboxPopup.Hide();
        if (profilePopup != null) profilePopup.Hide();
        if (questPopup != null) questPopup.Hide();
        if (mapPopup != null) mapPopup.Hide();
        if (piggyBankPopup != null) piggyBankPopup.Hide();
        if (eventPopup != null) eventPopup.Hide();
        if (workshopPopup != null) workshopPopup.Hide();
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
    /// Update currency display (POS).
    /// </summary>
    public void SetCurrency(long amount)
    {
        if (currencyValue != null) currencyValue.text = amount.ToString("N0");
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

    // ── Test: Press L = Level Up, E = Event ──
    private IEnumerator SyncPlayerName()
    {
        // Retry every 0.5s until GameManager has a valid playerName
        for (int i = 0; i < 20; i++) // Max 10 seconds
        {
            if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.playerName))
            {
                SetPlayerInfo(GameManager.Instance.playerName, 1);
                Debug.Log($"[GameHUD] Synced player name: {GameManager.Instance.playerName}");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // Không xử lý phím tắt nếu GameHUD đang bị ẩn (ví dụ: đang ở Login hoặc Cutscene)
        if (uiDocument == null || uiDocument.rootVisualElement == null || uiDocument.rootVisualElement.style.display == DisplayStyle.None)
            return;

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.iKey.wasPressedThisFrame && inventoryPopup != null)
        {
            if (inventoryPopup.IsVisible())
            {
                inventoryPopup.Hide();
            }
            else
            {
                HideAllPopups();
                inventoryPopup.Show();
            }
        }

        if (keyboard.lKey.wasPressedThisFrame && levelUpOverlay != null)
        {
            levelUpOverlay.TestLevelUp();
        }

        if (keyboard.eKey.wasPressedThisFrame && eventPopup != null)
        {
            if (eventPopup.IsVisible())
            {
                eventPopup.Hide();
            }
            else
            {
                HideAllPopups();
                eventPopup.Show();
            }
        }

        if (keyboard.fKey.wasPressedThisFrame && fishingOverlay != null)
        {
            HideAllPopups();
            fishingOverlay.Show();
        }

        if (keyboard.bKey.wasPressedThisFrame && buildModeOverlay != null)
        {
            if (buildModeOverlay.IsVisible())
            {
                buildModeOverlay.Hide();
            }
            else
            {
                HideAllPopups();
                buildModeOverlay.Show();
            }
        }

        if (keyboard.rKey.wasPressedThisFrame && workshopPopup != null)
        {
            HideAllPopups();
            workshopPopup.Show();
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            Debug.Log($"[GameHUD] Đã bấm phím 1. Biến shopPopup có null không: {shopPopup == null}");
            if (shopPopup != null)
            {
                if (shopPopup.IsVisible())
                {
                    Debug.Log("[GameHUD] shopPopup đang hiện -> Gọi Hide()");
                    shopPopup.Hide();
                }
                else
                {
                    Debug.Log("[GameHUD] shopPopup đang ẩn -> Gọi Show()");
                    HideAllPopups();
                    shopPopup.Show();
                }
            }
        }

        if (keyboard.mKey.wasPressedThisFrame && mapPopup != null)
        {
            if (mapPopup.IsVisible())
            {
                mapPopup.Hide();
            }
            else
            {
                HideAllPopups();
                mapPopup.Show();
            }
        }
    }

    public void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt == null)
        {
            Debug.LogError("[GameHUDController] KHÔNG TÌM THẤY interactionPrompt! Vui lòng kiểm tra lại GameHUD.uxml xem đã có Label tên là 'InteractionPrompt' chưa.");
            return;
        }
        interactionPrompt.text = text;
        interactionPrompt.style.opacity = 1f;
    }

    public void HideInteractionPrompt()
    {
        if (interactionPrompt == null) return;
        interactionPrompt.style.opacity = 0f;
    }
}
