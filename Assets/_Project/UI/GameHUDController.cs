using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using System.Collections.Generic;

public struct InteractionAction
{
    public string keyName;
    public string actionName;
    public Action onClick;

    // GIỮ-ĐỂ-LẶP (vd chặt cây): set 2 cái này thay cho onClick. Giữ nút -> onHoldStart, thả -> onHoldEnd.
    // Việc lặp hành động mỗi frame do logic bên ngoài lo (đặt cờ lúc Start, xử lý trong Update).
    public Action onHoldStart;
    public Action onHoldEnd;
}

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
    private VisualElement playerAvatar;
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
    private Button btnCancel;
    private Button btnJump;
    private Button btnBag;
    private Button btnSettings;
    private Button btnSprint;
    private VisualElement interactionContainer;

    // Joystick ảo (mobile)
    private VisualElement joystickOuter;
    private VisualElement joystickKnob;
    private VisualElement sprintHint;
    private int joystickPointerId = -1;
    // Tầm kéo núm + chuẩn hoá input. Đi theo size .joystick-outer trong GameHUD.uss
    // (outer 200 → bán kính ~70). Đổi size joystick thì chỉnh số này theo tỉ lệ.
    private const float JoystickRadius = 70f;
    [Header("Mobile Feel")]
    [SerializeField, Range(0f, 0.4f)] private float joystickDeadZone = 0.18f;
    [SerializeField, Range(1f, 3f)] private float joystickResponseExponent = 1.6f;
    [SerializeField, Range(0.1f, 1f)] private float joystickSprintHoldSeconds = 0.35f;
    [SerializeField, Range(0f, 1f)] private float joystickSprintForwardMin = 0.55f;
    private float joystickRawMagnitude = 0f;
    private float joystickRawForward = 0f;
    private float joystickSprintHoldTimer = 0f;
    private float sprintPressStartTime = -1f;
    private bool suppressNextSprintClick = false;
    private const float SprintTapThresholdSeconds = 0.18f;

    // Vùng nhìn (mobile) — kéo 1 ngón nửa phải để xoay camera
    private VisualElement lookZone;
    private int lookPointerId = -1;
    private Vector2 lookLastPos;

    private YWonderLand.Managers.ExperienceManager _expMgr;

    // Cụm nút phải: chỉ còn nút X (hủy hoạt ảnh), hiện khi nhân vật đang bận.
    private VisualElement rightActionsContainer;



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
        UpdateAvatar();

        // Sync player name from GameManager (retry until available)
        StartCoroutine(SyncPlayerName());

        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            SetCurrency(YWonderLand.Managers.EconomyManager.Instance.GetPOS());
            YWonderLand.Managers.EconomyManager.Instance.OnPOSChanged += SetCurrency;
        }

        // EXP/Level (tối giản) — hiện cấp + % EXP thật, cập nhật khi cộng EXP.
        _expMgr = YWonderLand.Managers.ExperienceManager.Instance;
        if (_expMgr != null)
        {
            if (playerLevel != null) playerLevel.text = $"Level: {_expMgr.Level}";
            SetPlayerEXP(_expMgr.ExpPercent);
            _expMgr.OnEXPChanged += OnExpChanged;
        }

        // Nhạc nền (tự tạo AudioManager; thiếu file Resources/Audio/bgm thì im, không lỗi).
        YWonderLand.Managers.AudioManager.Instance?.PlayMusic("bgm");
    }

    void OnDisable()
    {
        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            YWonderLand.Managers.EconomyManager.Instance.OnPOSChanged -= SetCurrency;
        }

        if (_expMgr != null) _expMgr.OnEXPChanged -= OnExpChanged;
        if (PlayerController.Instance != null) PlayerController.Instance.SetStickAutoSprint(false);
    }

    // Cập nhật cấp + % EXP lên HUD khi ExperienceManager báo đổi.
    private void OnExpChanged(int level, float percent)
    {
        if (playerLevel != null) playerLevel.text = $"Level: {level}";
        SetPlayerEXP(percent);
    }

    private void QueryElements(VisualElement root)
    {
        // Player Info
        playerInfo = root.Q<VisualElement>("PlayerInfo");
        playerAvatar = root.Q<VisualElement>("PlayerAvatar");
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
        btnCancel = root.Q<Button>("BtnCancel");
        btnJump = root.Q<Button>("BtnJump");
        btnBag = root.Q<Button>("BtnBag");
        btnSettings = root.Q<Button>("BtnSettings");
        btnSprint = root.Q<Button>("BtnSprint");
        interactionContainer = root.Q<VisualElement>("InteractionContainer");
        joystickOuter = root.Q<VisualElement>("Joystick");
        joystickKnob = joystickOuter?.Q<VisualElement>(className: "joystick-inner");
        sprintHint = root.Q<VisualElement>("SprintHint");
        lookZone = root.Q<VisualElement>("LookZone");
        rightActionsContainer = root.Q<VisualElement>(className: "hud-right-actions");


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

        // Nút X (Cancel) = HỦY HOẠT ẢNH đang chạy (chặt/đào/cuốc/tưới/câu...).
        btnCancel?.RegisterCallback<ClickEvent>(evt =>
        {
            if (PlayerController.Instance != null) PlayerController.Instance.CancelAction();
        });

        btnJump?.RegisterCallback<ClickEvent>(evt =>
        {
            if (PlayerController.Instance != null) PlayerController.Instance.TriggerJump();
        });

        if (btnSprint != null)
        {
            // Mặc định mobile TPS: GIỮ nút để sprint. Auto-run giữ riêng, không là cơ chế chính.
            btnSprint.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (PlayerController.Instance == null) return;
                sprintPressStartTime = Time.unscaledTime;
                suppressNextSprintClick = false;
                btnSprint.CapturePointer(evt.pointerId);
                PlayerController.Instance.SetSprintUI(true);
                RefreshSprintVisual();
            }, TrickleDown.TrickleDown);

            btnSprint.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (PlayerController.Instance == null) return;
                float heldTime = sprintPressStartTime >= 0f ? (Time.unscaledTime - sprintPressStartTime) : 0f;
                suppressNextSprintClick = heldTime > SprintTapThresholdSeconds;
                if (btnSprint.HasPointerCapture(evt.pointerId)) btnSprint.ReleasePointer(evt.pointerId);
                PlayerController.Instance.SetSprintUI(false);
                RefreshSprintVisual();
            }, TrickleDown.TrickleDown);

            btnSprint.RegisterCallback<PointerCaptureOutEvent>(evt =>
            {
                if (PlayerController.Instance == null) return;
                sprintPressStartTime = -1f;
                PlayerController.Instance.SetSprintUI(false);
                RefreshSprintVisual();
            });

            // BẤM NHẢ (click/tap) = bật/tắt auto-run.
            // Cho phép người chơi đứng yên rồi bấm sprint để nhân vật tự chạy tới.
            btnSprint.RegisterCallback<ClickEvent>(evt =>
            {
                if (PlayerController.Instance == null) return;
                if (suppressNextSprintClick)
                {
                    suppressNextSprintClick = false;
                    return;
                }
                PlayerController.Instance.ToggleAutoRun();
                RefreshSprintVisual();
            });
        }

        SetupJoystick();
        SetupLookZone();

        // Bag
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
        UpdateAvatar();
    }

    public void UpdateAvatar()
    {
        if (playerAvatar != null)
        {
            playerAvatar.RemoveFromClassList("avatar-male");
            playerAvatar.RemoveFromClassList("avatar-female");
            
            int gender = GameManager.Instance != null ? GameManager.Instance.selectedCharacterIndex : 0;
            if (gender == 0)
                playerAvatar.AddToClassList("avatar-male");
            else
                playerAvatar.AddToClassList("avatar-female");
        }
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
                int lvl = _expMgr != null ? _expMgr.Level : 1;
                SetPlayerInfo(GameManager.Instance.playerName, lvl);
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

        // Hiện/ẩn nút X (hủy hoạt ảnh) theo trạng thái bận — chạy cả khi không có bàn phím (mobile).
        UpdateCancelButton();
        UpdateJoystickSprintState();

        // Chặn các phím tắt nếu người chơi đang gõ phím trong khung chat
        if (ChatPanelController.Instance != null && ChatPanelController.Instance.IsTyping())
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

        // Đã xóa phím tắt E gọi Event Popup để nhường cho phím tắt Tương tác (Vuốt ve)

        // [Đã gỡ] Phím F toàn cục mở câu cá. Câu cá giờ theo TÂM NGẮM: chỉ câu được khi
        // chĩa tâm vào vùng nước (FishingSpot) — xử lý trong FarmInteractionController.

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

        // [Đã gỡ] Phím R toàn cục mở Workshop — nhường R cho phím tắt Thu hoạch động vật.
        // Workshop sẽ được mở qua click vào NPC tương ứng (giống Shop).

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

    // ───────────────────────────────────────────────
    //  JOYSTICK ẢO (MOBILE) — kéo núm để di chuyển nhân vật
    // ───────────────────────────────────────────────
    private void SetupJoystick()
    {
        if (joystickOuter == null) return;
        joystickOuter.RegisterCallback<PointerDownEvent>(OnJoystickDown);
        joystickOuter.RegisterCallback<PointerMoveEvent>(OnJoystickMove);
        joystickOuter.RegisterCallback<PointerUpEvent>(OnJoystickUp);
        // Mất quyền bắt con trỏ (kéo ra ngoài / nhả ngoài vùng) -> reset cho an toàn
        joystickOuter.RegisterCallback<PointerCaptureOutEvent>(evt => ResetJoystick());
    }

    private void OnJoystickDown(PointerDownEvent evt)
    {
        joystickPointerId = evt.pointerId;
        joystickOuter.CapturePointer(evt.pointerId);
        UpdateJoystick(joystickOuter.WorldToLocal((Vector2)evt.position));
        evt.StopPropagation();
    }

    private void OnJoystickMove(PointerMoveEvent evt)
    {
        if (joystickPointerId != evt.pointerId || !joystickOuter.HasPointerCapture(evt.pointerId)) return;
        UpdateJoystick(joystickOuter.WorldToLocal((Vector2)evt.position));
    }

    private void OnJoystickUp(PointerUpEvent evt)
    {
        if (joystickPointerId != evt.pointerId) return;
        if (joystickOuter.HasPointerCapture(evt.pointerId)) joystickOuter.ReleasePointer(evt.pointerId);
        ResetJoystick();
    }

    // Tính vector di chuyển từ vị trí chạm (local trong joystick-outer) rồi đẩy vào PlayerController.
    private void UpdateJoystick(Vector2 localPos)
    {
        Vector2 center = new Vector2(joystickOuter.resolvedStyle.width * 0.5f, joystickOuter.resolvedStyle.height * 0.5f);
        Vector2 offset = Vector2.ClampMagnitude(localPos - center, JoystickRadius);

        if (joystickKnob != null) joystickKnob.style.translate = new Translate(offset.x, offset.y, 0);

        float rawMagnitude = Mathf.Clamp01(offset.magnitude / JoystickRadius);
        float curvedMagnitude = 0f;
        if (rawMagnitude > joystickDeadZone)
        {
            float normalizedMagnitude = Mathf.InverseLerp(joystickDeadZone, 1f, rawMagnitude);
            curvedMagnitude = Mathf.Pow(normalizedMagnitude, joystickResponseExponent);
        }

        Vector2 direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.zero;
        // Trục Y của UI hướng XUỐNG -> đảo dấu để kéo lên = đi tới (forward).
        Vector2 move = new Vector2(direction.x * curvedMagnitude, -direction.y * curvedMagnitude);
        joystickRawMagnitude = rawMagnitude;
        joystickRawForward = Mathf.Max(0f, -direction.y) * rawMagnitude;
        if (PlayerController.Instance != null) PlayerController.Instance.SetMoveInput(move, rawMagnitude);
    }

    private void ResetJoystick()
    {
        joystickPointerId = -1;
        if (joystickKnob != null) joystickKnob.style.translate = new Translate(0, 0, 0);
        joystickRawMagnitude = 0f;
        joystickRawForward = 0f;
        joystickSprintHoldTimer = 0f;
        if (sprintHint != null)
        {
            sprintHint.style.display = DisplayStyle.None;
            sprintHint.EnableInClassList("sprint-hint-active", false);
        }
        if (PlayerController.Instance != null) PlayerController.Instance.SetMoveInput(Vector2.zero, 0f);
        RefreshSprintVisual();
    }

    private void UpdateJoystickSprintState()
    {
        if (PlayerController.Instance == null) return;

        float sprintThreshold = PlayerController.Instance.StickAutoSprintThreshold;
        bool alreadyLatched = PlayerController.Instance.IsStickAutoSprintOn;
        if (alreadyLatched) joystickSprintHoldTimer = 0f;
        bool eligible = joystickRawMagnitude >= sprintThreshold && joystickRawForward >= joystickSprintForwardMin;
        if (!alreadyLatched && eligible)
        {
            joystickSprintHoldTimer += Time.deltaTime;
        }
        else if (!alreadyLatched)
        {
            joystickSprintHoldTimer = 0f;
        }

        if (!alreadyLatched && eligible && joystickSprintHoldTimer >= joystickSprintHoldSeconds)
        {
            PlayerController.Instance.SetStickAutoSprint(true);
            alreadyLatched = true;
        }

        if (sprintHint != null)
        {
            bool showHint = eligible;
            sprintHint.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;
            sprintHint.EnableInClassList("sprint-hint-active", alreadyLatched || (eligible && joystickSprintHoldTimer >= joystickSprintHoldSeconds));
        }

        RefreshSprintVisual();
    }

    // ───────────────────────────────────────────────
    //  VÙNG NHÌN (MOBILE) — kéo 1 ngón nửa phải để xoay camera (giống joystick nửa trái)
    // ───────────────────────────────────────────────
    private void SetupLookZone()
    {
        if (lookZone == null) return;
        lookZone.RegisterCallback<PointerDownEvent>(OnLookDown);
        lookZone.RegisterCallback<PointerMoveEvent>(OnLookMove);
        lookZone.RegisterCallback<PointerUpEvent>(OnLookUp);
        lookZone.RegisterCallback<PointerCaptureOutEvent>(evt => lookPointerId = -1);
    }

    private void OnLookDown(PointerDownEvent evt)
    {
        if (UIPopupTracker.AnyOpen) return;
        lookPointerId = evt.pointerId;
        lookZone.CapturePointer(evt.pointerId);
        lookLastPos = (Vector2)evt.position;
        evt.StopPropagation();
    }

    private void OnLookMove(PointerMoveEvent evt)
    {
        if (lookPointerId != evt.pointerId || !lookZone.HasPointerCapture(evt.pointerId)) return;
        Vector2 cur = (Vector2)evt.position;
        Vector2 delta = cur - lookLastPos;
        lookLastPos = cur;
        if (!UIPopupTracker.AnyOpen && ThirdPersonCamera.Instance != null)
            ThirdPersonCamera.Instance.AddTouchLook(delta);
    }

    private void OnLookUp(PointerUpEvent evt)
    {
        if (lookPointerId != evt.pointerId) return;
        if (lookZone.HasPointerCapture(evt.pointerId)) lookZone.ReleasePointer(evt.pointerId);
        lookPointerId = -1;
    }

    public void ShowInteractionPrompts(List<InteractionAction> actions)
    {
        if (interactionContainer == null) return;
        
        interactionContainer.Clear();
        
        if (actions == null || actions.Count == 0)
        {
            interactionContainer.style.display = DisplayStyle.None;
            return;
        }

        interactionContainer.style.display = DisplayStyle.Flex;

        foreach (var action in actions)
        {
            Button btn = new Button();
            btn.AddToClassList("interaction-action-btn");
            
            // Add key hint if PC
            if (!string.IsNullOrEmpty(action.keyName))
            {
                Label keyLabel = new Label(action.keyName);
                keyLabel.AddToClassList("interaction-key-label");
                btn.Add(keyLabel);
            }
            
            Label actionLabel = new Label(action.actionName);
            actionLabel.AddToClassList("interaction-action-label");
            btn.Add(actionLabel);

            // Gán sự kiện: GIỮ-ĐỂ-LẶP (vd chặt cây) hoặc click thường.
            if (action.onHoldStart != null || action.onHoldEnd != null)
            {
                var holdStart = action.onHoldStart;
                var holdEnd = action.onHoldEnd;
                Button heldBtn = btn;
                btn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    holdStart?.Invoke();
                    heldBtn.CapturePointer(evt.pointerId); // bắt con trỏ -> nhả ngoài nút vẫn nhận PointerUp
                }, TrickleDown.TrickleDown);
                btn.RegisterCallback<PointerUpEvent>(evt => holdEnd?.Invoke(), TrickleDown.TrickleDown);
                btn.RegisterCallback<PointerCaptureOutEvent>(evt => holdEnd?.Invoke()); // mất bắt -> dừng an toàn
            }
            else if (action.onClick != null)
            {
                btn.clicked += action.onClick;
            }

            interactionContainer.Add(btn);
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionContainer == null) return;
        interactionContainer.style.display = DisplayStyle.None;
        interactionContainer.Clear();
    }

    // Nút X (hủy hoạt ảnh) chỉ hiện khi nhân vật đang khóa trong một hành động.
    // Đặt TRƯỚC mọi early-return phụ thuộc bàn phím trong Update() để chạy cả trên mobile.
    private void UpdateCancelButton()
    {
        if (rightActionsContainer == null) return;
        bool busy = PlayerController.Instance != null && PlayerController.Instance.IsBusy;
        rightActionsContainer.style.display = busy ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RefreshSprintVisual()
    {
        if (btnSprint == null || PlayerController.Instance == null) return;
        bool sprintLit = PlayerController.Instance.IsSprintActive();
        btnSprint.EnableInClassList("sprint-btn-active", sprintLit);
    }
}
