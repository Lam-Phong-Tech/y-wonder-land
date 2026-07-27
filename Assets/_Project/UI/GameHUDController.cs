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

    // Tên file icon trong Resources/UI/InteractionIcons (không có đuôi .png). Để trống
    // thì HUD tự tra theo actionName — xem InteractionIconByAction trong GameHUDController.
    public string iconName;
}

/// <summary>
/// Controller for the In-Game HUD.
/// Số hiển thị là THẬT: Point từ EconomyManager, cấp/EXP từ ExperienceManager.
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
    private VisualElement questRedDot;
    private VisualElement calendarRedDot;
    private VisualElement buildRedDot;
    private VisualElement bagRedDot;
    private float nextGuidanceDotRefreshTime;
    private const float GuidanceDotRefreshInterval = 0.25f;

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
    [SerializeField, Range(0f, 0.4f)] private float joystickAutoRunCancelThreshold = 0.08f;
    private bool enableJoystickAutoSprint = false;
    [SerializeField, Range(0.1f, 1f)] private float joystickSprintHoldSeconds = 0.35f;
    [SerializeField, Range(0f, 1f)] private float joystickSprintForwardMin = 0.55f;
    [SerializeField] private bool previewMobileHudScaleInEditor = false;
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
    private VisualElement cancelButtonFrame;
    private bool fishingCancelMode;
    private float fishingCancelProgress01;
    private float nextPlayerInfoRefreshTime;
    private const float PlayerInfoRefreshInterval = 0.25f;



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

        // FIX MOBILE (APK kh\u00f4ng b\u1ea5m \u0111\u01b0\u1ee3c n\u00fat/joystick): tr\u00ean GameObject "GameHUD", th\u1ee9 t\u1ef1
        // component l\u00e0 GameHUDController \u0110\u1ee8NG TR\u01af\u1edaC UIDocument, n\u00ean OnEnable c\u1ee7a script n\u00e0y
        // ch\u1ea1y TR\u01af\u1edaC UIDocument.OnEnable. Trong b\u1ea3n build (Android), UIDocument ch\u01b0a k\u1ecbp d\u1ef1ng
        // c\u00e2y UI -> rootVisualElement = null -> n\u1ebfu n\u1ed1i n\u00fat l\u00fac n\u00e0y th\u00ec QueryElements/
        // RegisterCallbacks ch\u1ea1y tr\u00ean c\u00e2y r\u1ed7ng, KH\u00d4NG n\u00fat/joystick n\u00e0o \u0111\u01b0\u1ee3c n\u1ed1i (Editor kh\u00f4ng
        // d\u00ednh v\u00ec panel \u0111\u00e3 d\u1ef1ng s\u1eb5n \u1edf edit-mode). => \u0110\u1ee3i root s\u1eb5n s\u00e0ng r\u1ed3i m\u1edbi n\u1ed1i.
        if (uiDocument.rootVisualElement == null)
        {
            StartCoroutine(WireHudWhenRootReady());
            return;
        }

        WireHud();
    }

    // \u0110\u1ee3i UIDocument d\u1ef1ng xong c\u00e2y UI r\u1ed3i m\u1edbi n\u1ed1i HUD (fix n\u00fat/joystick kh\u00f4ng b\u1ea5m \u0111\u01b0\u1ee3c tr\u00ean APK).
    private System.Collections.IEnumerator WireHudWhenRootReady()
    {
        float timeout = 5f;
        while (uiDocument != null && uiDocument.rootVisualElement == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!isActiveAndEnabled || uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogError("[GameHUD] rootVisualElement v\u1eabn null sau khi ch\u1edd \u2014 HUD kh\u00f4ng n\u1ed1i \u0111\u01b0\u1ee3c n\u00fat/joystick.");
            yield break;
        }

        WireHud();
    }

    // N\u1ed1i to\u00e0n b\u1ed9 HUD: query element + \u0111\u0103ng k\u00fd callback n\u00fat/joystick + set gi\u00e1 tr\u1ecb ban \u0111\u1ea7u.
    private void WireHud()
    {
        var root = uiDocument.rootVisualElement;
        ApplyPlatformLayoutClass(root);
        QueryElements(root);
        SetupGuidanceDots();
        RegisterCallbacks();
        EnsureMultiTouchUI();



        // Set initial values
        SetPlayerInfo("YWonderPlayer", 1);
        UpdateExpLabel();
        SetQuest("Kh\u00e1m ph\u00e1 \u0111\u1ea3o hoang v\u00e0 t\u00ecm ng\u00f4i nh\u00e0 \u0111\u1ea7u ti\u00ean!");
        UpdateAvatar();

        // Sync player name from GameManager (retry until available)
        StartCoroutine(SyncPlayerName());

        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            var economy = YWonderLand.Managers.EconomyManager.Instance;
            SetCurrency(economy.GetPOS());
            economy.OnPOSChanged += SetCurrency;
        }

        // EXP/Level (tối giản) — hiện cấp + % EXP thật, cập nhật khi cộng EXP.
        _expMgr = YWonderLand.Managers.ExperienceManager.Instance;
        if (_expMgr != null)
        {
            if (playerLevel != null) playerLevel.text = $"Level: {_expMgr.Level}";
            UpdateExpLabel();
            _expMgr.OnEXPChanged += OnExpChanged;
        }
        RefreshPlayerInfoFromSession(true);

        // Nhạc nền (tự tạo AudioManager; thiếu file Resources/Audio/bgm thì im, không lỗi).
        YWonderLand.Managers.AudioManager.Instance?.PlayMusic("bgm");
    }

    // Mobile đa chạm: mặc định InputSystemUIInputModule là SingleUnifiedPointer -> gộp mọi
    // ngón thành 1 pointer, nên đang GIỮ joystick mà bấm nút thì nút không ăn. Đặt AllPointersAsIs
    // để mỗi ngón là 1 pointer riêng (di chuyển + bấm nút cùng lúc). Chuột trên Editor vẫn chạy bình thường.
    private void EnsureMultiTouchUI()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        var module = es != null ? es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() : null;
        if (module != null)
            module.pointerBehavior = UnityEngine.InputSystem.UI.UIPointerBehavior.AllPointersAsIs;
    }

    void OnDisable()
    {
        if (cancelButtonFrame != null)
            cancelButtonFrame.generateVisualContent -= DrawFishingCancelProgress;

        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            var economy = YWonderLand.Managers.EconomyManager.Instance;
            economy.OnPOSChanged -= SetCurrency;
        }

        if (_expMgr != null) _expMgr.OnEXPChanged -= OnExpChanged;
        if (PlayerController.Instance != null) PlayerController.Instance.SetStickAutoSprint(false);
    }

    // Cập nhật cấp + EXP lên HUD khi ExperienceManager báo đổi.
    private void OnExpChanged(int level, float percent)
    {
        if (playerLevel != null) playerLevel.text = $"Level: {level}";
        UpdateExpLabel();
    }

    // Nhãn EXP hiện dạng SỐ "hiện tại / cần" (tester dễ test), thay vì phần trăm.
    private void UpdateExpLabel()
    {
        if (playerCurrencySmall == null) return;
        if (_expMgr == null) { playerCurrencySmall.text = "0 / 250"; return; }
        playerCurrencySmall.text = _expMgr.IsMaxLevel
            ? "MAX"
            : $"{_expMgr.ExpInLevel} / {_expMgr.ExpForNextLevel}";
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
        cancelButtonFrame = root.Q<VisualElement>("CancelActionFrame");
        if (cancelButtonFrame != null)
        {
            cancelButtonFrame.pickingMode = PickingMode.Position;
            cancelButtonFrame.generateVisualContent -= DrawFishingCancelProgress;
            cancelButtonFrame.generateVisualContent += DrawFishingCancelProgress;
            cancelButtonFrame.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                TryCancelCurrentActionFromHUD();
                evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
        }
        if (rightActionsContainer != null)
            rightActionsContainer.pickingMode = PickingMode.Position;
        if (btnCancel != null)
            btnCancel.pickingMode = PickingMode.Position;


    }

    private void SetupGuidanceDots()
    {
        questRedDot = AttachGuidanceDot(questBubble, "hud-red-dot--quest");
        calendarRedDot = AttachGuidanceDot(btnCalendar, "hud-red-dot--button");
        buildRedDot = AttachGuidanceDot(btnBuild, "hud-red-dot--button");
        bagRedDot = AttachGuidanceDot(btnBag, "hud-red-dot--button");
        UpdateGuidanceDots(true);
    }

    private VisualElement AttachGuidanceDot(VisualElement target, string extraClass)
    {
        if (target == null) return null;

        var dot = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };
        dot.AddToClassList("hud-red-dot");
        if (!string.IsNullOrEmpty(extraClass))
            dot.AddToClassList(extraClass);
        dot.style.display = DisplayStyle.None;
        target.Add(dot);
        return dot;
    }

    private void SetGuidanceDot(VisualElement dot, bool visible)
    {
        if (dot == null) return;
        dot.EnableInClassList("hud-red-dot--visible", visible);
        dot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateGuidanceDots(bool force = false)
    {
        if (!force && Time.unscaledTime < nextGuidanceDotRefreshTime) return;
        nextGuidanceDotRefreshTime = Time.unscaledTime + GuidanceDotRefreshInterval;

        bool tutorialComplete = IsTutorialCompleteForGuidance();
        var tutorial = TutorialManager.Instance;
        bool tutorialActive = tutorial != null && tutorial.IsActive();
        var step = tutorial != null ? tutorial.currentStep : TutorialManager.TutorialStep.WaitForStart;

        bool needsBuild = tutorialActive &&
                          (step == TutorialManager.TutorialStep.BuildFarmPlot ||
                           step == TutorialManager.TutorialStep.BuildPen);
        bool needsBag = tutorialActive &&
                        (step == TutorialManager.TutorialStep.PlantSeed ||
                         step == TutorialManager.TutorialStep.PlaceAnimal ||
                         step == TutorialManager.TutorialStep.FeedAnimal);

        SetGuidanceDot(questRedDot, !tutorialComplete);
        SetGuidanceDot(calendarRedDot, EventPopupController.IsAttendanceReadyToClaim());
        SetGuidanceDot(buildRedDot, needsBuild);
        SetGuidanceDot(bagRedDot, needsBag);
    }

    private static bool IsTutorialCompleteForGuidance()
    {
        var prof = YWonderLand.Backend.PlayerProfileService.Instance;
        if (prof != null && prof.Profile != null && prof.Profile.tutorialCompleted)
            return true;

        var tutorial = TutorialManager.Instance;
        return tutorial != null && tutorial.currentStep == TutorialManager.TutorialStep.Complete;
    }

    private void ApplyPlatformLayoutClass(VisualElement root)
    {
        VisualElement hudRoot = root.Q<VisualElement>(className: "hud-root");
        if (hudRoot == null) return;

#if UNITY_EDITOR
        bool useMobileHud = previewMobileHudScaleInEditor || Application.isMobilePlatform;
#else
        bool useMobileHud = Application.isMobilePlatform;
#endif
        hudRoot.EnableInClassList("hud-mobile", useMobileHud);
    }

    private void RegisterCallbacks()
    {
        // Clickable HUD Elements (Player Info & Quest)
        playerInfo?.RegisterCallback<ClickEvent>(evt =>
        {
            if (profilePopup != null)
            {
                // Chỉ truyền TÊN. Cấp/EXP popup tự đọc từ ExperienceManager — bóc ngược chuỗi
                // trên label ("120 / 250") parse không ra số nên thanh EXP luôn đứng 0%.
                string name = playerName != null ? playerName.text : "Player";
                profilePopup.Show(name);
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
            TryCancelCurrentActionFromHUD();
            evt.StopPropagation();
        });

        btnJump?.RegisterCallback<ClickEvent>(evt =>
        {
            if (PlayerController.Instance != null) PlayerController.Instance.TriggerJump();
        });

        if (btnSprint != null)
        {
            // Tap/click = bật/tắt auto-run; giữ nút không còn sprint thủ công.
            btnSprint.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (PlayerController.Instance == null) return;
                sprintPressStartTime = Time.unscaledTime;
                suppressNextSprintClick = false;
                btnSprint.CapturePointer(evt.pointerId);
                PlayerController.Instance.SetSprintUI(false);
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
    /// Update currency display (Point).
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
        for (int i = 0; i < 20; i++)
        {
            RefreshPlayerInfoFromSession(true);
            if (!string.IsNullOrEmpty(ResolveCurrentPlayerName()))
                yield break;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void RefreshPlayerInfoFromSession(bool force = false)
    {
        if (!force && Time.unscaledTime < nextPlayerInfoRefreshTime) return;
        nextPlayerInfoRefreshTime = Time.unscaledTime + PlayerInfoRefreshInterval;

        string resolvedName = ResolveCurrentPlayerName();
        if (string.IsNullOrEmpty(resolvedName)) return;

        int level = _expMgr != null ? _expMgr.Level : 1;
        string expectedLevel = $"Level: {level}";
        bool changed = force
                       || (playerName != null && playerName.text != resolvedName)
                       || (playerLevel != null && playerLevel.text != expectedLevel);
        if (!changed) return;

        SetPlayerInfo(resolvedName, level);
        Debug.Log($"[GameHUD] Synced player info: {resolvedName}");
    }

    private static string ResolveCurrentPlayerName()
    {
        if (GameManager.Instance != null && !string.IsNullOrWhiteSpace(GameManager.Instance.playerName))
            return GameManager.Instance.playerName.Trim();

        var auth = YWonderLand.Backend.AuthService.Instance;
        if (auth != null && !string.IsNullOrWhiteSpace(auth.Username))
            return auth.Username.Trim();

        var profile = YWonderLand.Backend.PlayerProfileService.Instance?.Profile;
        if (profile != null && !string.IsNullOrWhiteSpace(profile.name))
            return profile.name.Trim();

        return "";
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
        RefreshPlayerInfoFromSession();
        UpdateGuidanceDots();

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
        joystickOuter.pickingMode = PickingMode.Position;
        joystickOuter.RegisterCallback<PointerDownEvent>(OnJoystickDown);
        joystickOuter.RegisterCallback<PointerMoveEvent>(OnJoystickMove);
        joystickOuter.RegisterCallback<PointerUpEvent>(OnJoystickUp);
        // Mất quyền bắt con trỏ (kéo ra ngoài / nhả ngoài vùng) -> reset cho an toàn
        joystickOuter.RegisterCallback<PointerCaptureOutEvent>(OnJoystickCaptureOut);
    }

    private void OnJoystickDown(PointerDownEvent evt)
    {
        if (joystickPointerId != -1 && joystickPointerId != evt.pointerId)
        {
            evt.StopPropagation();
            return;
        }

        if (lookPointerId == evt.pointerId)
            ReleaseLookPointer(evt.pointerId);

        joystickPointerId = evt.pointerId;
        joystickOuter.CapturePointer(evt.pointerId);
        UpdateJoystick(joystickOuter.WorldToLocal((Vector2)evt.position));
        evt.StopPropagation();
    }

    private void OnJoystickMove(PointerMoveEvent evt)
    {
        if (joystickPointerId != evt.pointerId || !joystickOuter.HasPointerCapture(evt.pointerId)) return;
        UpdateJoystick(joystickOuter.WorldToLocal((Vector2)evt.position));
        evt.StopPropagation();
    }

    private void OnJoystickUp(PointerUpEvent evt)
    {
        if (joystickPointerId != evt.pointerId) return;
        if (joystickOuter.HasPointerCapture(evt.pointerId)) joystickOuter.ReleasePointer(evt.pointerId);
        ResetJoystick();
        evt.StopPropagation();
    }

    private void OnJoystickCaptureOut(PointerCaptureOutEvent evt)
    {
        if (joystickPointerId != evt.pointerId) return;
        ResetJoystick();
        evt.StopPropagation();
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
        if (PlayerController.Instance != null)
        {
            if (rawMagnitude > joystickAutoRunCancelThreshold && PlayerController.Instance.IsAutoRunOn)
            {
                PlayerController.Instance.ToggleAutoRun();
                RefreshSprintVisual();
            }

            PlayerController.Instance.SetMoveInput(move, rawMagnitude);
        }
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

        if (enableJoystickAutoSprint && !alreadyLatched && eligible && joystickSprintHoldTimer >= joystickSprintHoldSeconds)
        {
            alreadyLatched = false;
        }

        if (sprintHint != null)
        {
            bool showHint = false;
            sprintHint.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;
            sprintHint.EnableInClassList("sprint-hint-active", false);
        }

        RefreshSprintVisual();
    }

    // ───────────────────────────────────────────────
    //  VÙNG NHÌN (MOBILE) — kéo 1 ngón nửa phải để xoay camera (giống joystick nửa trái)
    // ───────────────────────────────────────────────
    private void SetupLookZone()
    {
        if (lookZone == null) return;
        lookZone.pickingMode = PickingMode.Position;
        lookZone.RegisterCallback<PointerDownEvent>(OnLookDown);
        lookZone.RegisterCallback<PointerMoveEvent>(OnLookMove);
        lookZone.RegisterCallback<PointerUpEvent>(OnLookUp);
        lookZone.RegisterCallback<PointerCaptureOutEvent>(OnLookCaptureOut);
    }

    private void OnLookDown(PointerDownEvent evt)
    {
        if (UIPopupTracker.AnyOpen || IsPointerReservedForJoystick(evt.pointerId, (Vector2)evt.position))
        {
            evt.StopPropagation();
            return;
        }

        lookPointerId = evt.pointerId;
        lookZone.CapturePointer(evt.pointerId);
        lookLastPos = (Vector2)evt.position;
        evt.StopPropagation();
    }

    private void OnLookMove(PointerMoveEvent evt)
    {
        if (lookPointerId != evt.pointerId || !lookZone.HasPointerCapture(evt.pointerId)) return;
        if (IsPointerReservedForJoystick(evt.pointerId, (Vector2)evt.position))
        {
            ReleaseLookPointer(evt.pointerId);
            evt.StopPropagation();
            return;
        }

        Vector2 cur = (Vector2)evt.position;
        Vector2 delta = cur - lookLastPos;
        lookLastPos = cur;
        if (!UIPopupTracker.AnyOpen && ThirdPersonCamera.Instance != null)
            ThirdPersonCamera.Instance.AddTouchLook(delta);
        evt.StopPropagation();
    }

    private void OnLookUp(PointerUpEvent evt)
    {
        if (lookPointerId != evt.pointerId) return;
        ReleaseLookPointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnLookCaptureOut(PointerCaptureOutEvent evt)
    {
        if (lookPointerId != evt.pointerId) return;
        lookPointerId = -1;
        evt.StopPropagation();
    }

    private bool IsPointerReservedForJoystick(int pointerId, Vector2 panelPosition)
    {
        if (joystickPointerId == pointerId) return true;
        return joystickOuter != null && joystickOuter.worldBound.Contains(panelPosition);
    }

    private void ReleaseLookPointer(int pointerId)
    {
        if (lookZone != null && lookZone.HasPointerCapture(pointerId))
            lookZone.ReleasePointer(pointerId);
        if (lookPointerId == pointerId)
            lookPointerId = -1;
    }

    // ── Cụm nút tương tác hình VÒNG CUNG quanh nút Nhảy (kiểu MOBA, khách chốt 23/07) ──
    // Tâm cung = tâm nút Nhảy. Mấy số này suy ra từ GameHUD.uss:
    //   .hud-bottom-right { bottom: 90px; right: 24px; }  .jump-btn { 96x96 }
    // Sửa USS thì phải sửa luôn ở đây, không thì nút lệch khỏi ngón cái.
    private const float ArcAnchorRight = 24f + 48f;
    private const float ArcAnchorBottom = 90f + 48f;

    private const float ArcStartDeg = 115f;   // đầu cung, phía TRÊN nút Nhảy
    private const float ArcEndDeg = 190f;     // cuối cung, phía TRÁI nút Nhảy (gần ngón cái nhất)
    private const float ArcSingleDeg = 168f;  // chỉ có 1 hành động thì đặt ngay tầm với
    // 175 chứ không phải 150: bán kính ngắn hơn thì nút trên cùng của cung đè lên nút Búa
    // (tâm cách nhau chỉ ~70px trong khi hai nút cộng lại đã 72px). Cuối cung cũng phải
    // dừng ở 190° để nhãn chữ của nút thấp nhất không bị mép dưới màn hình cắt mất.
    private const float ArcMinRadius = 175f;
    private const float ArcLabelHeight = 18f; // chỗ chừa cho dòng chữ dưới nút
    private const float ArcLabelGap = 8f;     // khớp margin-top của .interaction-arc-label

    /// <summary>
    /// Nút to hay nhỏ tuỳ số hành động. Giữ nguyên 72px cho mọi trường hợp thì cụm 5 nút
    /// (vật nuôi: cho ăn/thu hoạch/chữa bệnh/tiêm/thông tin) buộc bán kính phải nở tới
    /// ~360px — vượt tầm với của ngón cái. Thu nhỏ một chút vẫn thừa to để chạm.
    /// </summary>
    private static float ArcButtonSizeFor(int count)
    {
        if (count >= 5) return 56f;
        if (count == 4) return 64f;
        return 72f;
    }

    /// <summary>
    /// Bảng tra icon theo tên hành động. Cố ý tra bằng TÊN thay vì bắt mọi nơi gọi phải
    /// truyền icon: nhãn động (chặt cây/đào khoáng, nhãn NPC) tự khớp mà không phải sửa
    /// FarmInteractionController. Muốn ép icon riêng thì set InteractionAction.iconName.
    /// </summary>
    private static readonly Dictionary<string, string> InteractionIconByAction =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Cuốc đất", "Icon_Hoe" },
        { "Gieo hạt", "Icon_Seed" },
        { "Tưới nước", "Icon_WateringCan" },
        { "Thu hoạch", "Icon_Basket" },
        { "Hủy ô trồng", "Icon_Cancel" },
        { "Cho ăn", "Icon_FeedBowl" },
        { "Chữa bệnh", "Icon_Cross" },
        { "Tiêm vắc-xin", "Icon_Syringe" },
        { "Thông tin", "Icon_Info" },
        { "Xem chuồng", "Icon_Eye" },
        { "Thả thú", "Icon_HandRelease" },
        { "Hủy chuồng", "Icon_Cancel" },
        { "Múc nước", "Icon_WaterBucket" },
        { "Chặt cây", "Icon_Axe" },
        { "Đào khoáng", "Icon_Pickaxe" },
        { "Câu cá", "Icon_FishingRod" },
        { "Mua hàng", "Icon_Shop" },
        { "Bán đồ", "Icon_Shop" },
        { "Nâng cấp", "Icon_Anvil" },
        { "Gửi tiết kiệm", "Icon_PiggyBank" },
        { "Giao thương", "Icon_Shop" },
    };

    private const string InteractionIconFolder = "UI/InteractionIcons/";
    private const string InteractionIconFallback = "Icon_Hand";
    private static readonly Dictionary<string, Texture2D> interactionIconCache =
        new Dictionary<string, Texture2D>();

    private static Texture2D LoadInteractionIcon(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (interactionIconCache.TryGetValue(key, out Texture2D cached)) return cached;
        Texture2D tex = Resources.Load<Texture2D>(InteractionIconFolder + key);
        interactionIconCache[key] = tex; // cache cả khi null -> khỏi Resources.Load lại mỗi lần rê chuột
        return tex;
    }

    private static Texture2D ResolveInteractionIcon(InteractionAction action)
    {
        Texture2D tex = LoadInteractionIcon(action.iconName);
        if (tex == null && !string.IsNullOrEmpty(action.actionName) &&
            InteractionIconByAction.TryGetValue(action.actionName.Trim(), out string mapped))
        {
            tex = LoadInteractionIcon(mapped);
        }
        return tex != null ? tex : LoadInteractionIcon(InteractionIconFallback);
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

        // Khung phủ full màn nhưng KHÔNG bắt chuột — chỉ mấy nút tròn con mới bắt.
        interactionContainer.pickingMode = PickingMode.Ignore;
        interactionContainer.style.display = DisplayStyle.Flex;

        int count = actions.Count;
        float btnSize = ArcButtonSizeFor(count);
        float itemWidth = btnSize + 24f;
        // Khoảng cách tối thiểu giữa 2 tâm nút. Dòng chữ nằm LỌT GIỮA nút của nó và nút
        // kế trên, nên phải chừa khe ở CẢ HAI đầu: nửa nút + khe + chữ + khe + nửa nút.
        // Chỉ tính một khe là chữ chạm vành nút trên (đã thấy khi dựng thử 5 hành động).
        float pitch = btnSize + ArcLabelHeight + 2f * ArcLabelGap;

        float radius = ArcMinRadius;
        float stepDeg = 0f;
        if (count > 1)
        {
            stepDeg = (ArcEndDeg - ArcStartDeg) / (count - 1);
            radius = Mathf.Max(ArcMinRadius, pitch / (stepDeg * Mathf.Deg2Rad));
        }

        for (int i = 0; i < count; i++)
        {
            var action = actions[i];

            // Hành động đầu tiên (quan trọng nhất) nằm ở cuối cung = sát ngón cái nhất.
            float angleDeg = count == 1 ? ArcSingleDeg : ArcEndDeg - i * stepDeg;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            VisualElement item = new VisualElement();
            item.pickingMode = PickingMode.Ignore;
            item.AddToClassList("interaction-arc-item");
            item.style.width = itemWidth;
            item.style.height = btnSize;
            // Cos âm -> lệch sang trái, tức là "right" tăng lên. Sin dương -> lên cao.
            item.style.right = ArcAnchorRight - radius * Mathf.Cos(angleRad) - itemWidth * 0.5f;
            item.style.bottom = ArcAnchorBottom + radius * Mathf.Sin(angleRad) - btnSize * 0.5f;

            VisualElement btn = new VisualElement();
            btn.pickingMode = PickingMode.Position;
            btn.AddToClassList("interaction-arc-btn");
            // Kích thước đặt từ C# (không để USS quyết) vì công thức toạ độ ở trên phụ
            // thuộc thẳng vào nó — tách ra hai nơi là sớm muộn cũng lệch.
            btn.style.width = btnSize;
            btn.style.height = btnSize;

            VisualElement icon = new VisualElement();
            icon.pickingMode = PickingMode.Ignore;
            icon.AddToClassList("interaction-arc-icon");
            icon.style.width = Mathf.Round(btnSize * 0.56f);
            icon.style.height = Mathf.Round(btnSize * 0.56f);
            Texture2D iconTex = ResolveInteractionIcon(action);
            if (iconTex != null) icon.style.backgroundImage = new StyleBackground(iconTex);
            btn.Add(icon);

            // Phím tắt cho bản PC: gắn thành huy hiệu nhỏ ở góc nút, không chiếm chỗ.
            if (!string.IsNullOrEmpty(action.keyName) &&
                !string.Equals(action.keyName, "Click", StringComparison.OrdinalIgnoreCase))
            {
                Label keyLabel = new Label(action.keyName);
                keyLabel.pickingMode = PickingMode.Ignore;
                keyLabel.AddToClassList("interaction-arc-key");
                btn.Add(keyLabel);
            }

            item.Add(btn);

            Label actionLabel = new Label(action.actionName);
            actionLabel.pickingMode = PickingMode.Ignore;
            actionLabel.AddToClassList("interaction-arc-label");
            if (count >= 4) actionLabel.style.fontSize = 12f; // nút nhỏ lại thì chữ cũng phải nhỏ theo
            item.Add(actionLabel);

            // Gán sự kiện: GIỮ-ĐỂ-LẶP (vd chặt cây) hoặc click thường.
            if (action.onHoldStart != null || action.onHoldEnd != null)
            {
                var holdStart = action.onHoldStart;
                var holdEnd = action.onHoldEnd;
                VisualElement heldBtn = btn;
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
                var click = action.onClick;
                string clickKeyName = action.keyName;
                string clickActionName = action.actionName;
                VisualElement clickBtn = btn;
                bool pressedOnButton = false;
                int pressedPointerId = -1;

                btn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    pressedOnButton = true;
                    pressedPointerId = evt.pointerId;
                    clickBtn.CapturePointer(evt.pointerId);
                    evt.StopImmediatePropagation();
                }, TrickleDown.TrickleDown);

                btn.RegisterCallback<PointerUpEvent>(evt =>
                {
                    bool shouldClick = pressedOnButton && pressedPointerId == evt.pointerId;
                    pressedOnButton = false;
                    pressedPointerId = -1;
                    if (clickBtn.HasPointerCapture(evt.pointerId))
                        clickBtn.ReleasePointer(evt.pointerId);
                    evt.StopImmediatePropagation();
                    if (shouldClick)
                    {
                        Debug.Log($"[InteractionPrompt] UI click: {clickKeyName} {clickActionName}");
                        click?.Invoke();
                    }
                }, TrickleDown.TrickleDown);

                btn.RegisterCallback<PointerCaptureOutEvent>(evt =>
                {
                    pressedOnButton = false;
                    pressedPointerId = -1;
                });

                btn.RegisterCallback<ClickEvent>(evt => evt.StopImmediatePropagation(), TrickleDown.TrickleDown);
            }

            interactionContainer.Add(item);
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
    private bool TryCancelCurrentActionFromHUD()
    {
        if (FishingOverlayController.Instance != null && FishingOverlayController.Instance.IsAutoFishing)
        {
            FishingOverlayController.Instance.CancelFishingFromHUD();
            return true;
        }

        if (YWonderLand.Environment.FarmInteractionController.Instance != null &&
            YWonderLand.Environment.FarmInteractionController.Instance.CancelTimedActionFromHUD())
        {
            return true;
        }

        if (PlayerController.Instance != null &&
            PlayerController.Instance.IsBusy &&
            !PlayerController.Instance.IsJoystickCancelableEmote)
        {
            PlayerController.Instance.CancelAction();
            return true;
        }

        return false;
    }

    private void UpdateCancelButton()
    {
        if (rightActionsContainer == null) return;
        bool busy = PlayerController.Instance != null &&
            PlayerController.Instance.IsBusy &&
            !PlayerController.Instance.IsJoystickCancelableEmote;
        bool suppressWhileBuilding = buildModeOverlay != null && buildModeOverlay.IsVisible();
        bool show = !suppressWhileBuilding && (fishingCancelMode || busy);
        rightActionsContainer.EnableInClassList("hud-right-actions--fishing", fishingCancelMode);
        rightActionsContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void ShowFishingCancelProgress(float progress01)
    {
        fishingCancelMode = true;
        SetFishingCancelProgress(progress01);
        UpdateCancelButton();
    }

    public void SetFishingCancelProgress(float progress01)
    {
        fishingCancelProgress01 = Mathf.Clamp01(progress01);
        cancelButtonFrame?.MarkDirtyRepaint();
    }

    public void HideFishingCancelProgress()
    {
        fishingCancelMode = false;
        fishingCancelProgress01 = 0f;
        cancelButtonFrame?.MarkDirtyRepaint();
        UpdateCancelButton();
    }

    public void ShowActionCancelProgress(float progress01) => ShowFishingCancelProgress(progress01);

    public void SetActionCancelProgress(float progress01) => SetFishingCancelProgress(progress01);

    public void HideActionCancelProgress() => HideFishingCancelProgress();

    private void DrawFishingCancelProgress(MeshGenerationContext context)
    {
        if (!fishingCancelMode || cancelButtonFrame == null) return;

        Rect rect = cancelButtonFrame.contentRect;
        float radius = Mathf.Max(8f, Mathf.Min(rect.width, rect.height) * 0.5f - 6f);
        Vector2 center = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
        var painter = context.painter2D;

        painter.lineWidth = 6f;
        painter.strokeColor = new Color(1f, 1f, 1f, 0.24f);
        painter.BeginPath();
        painter.Arc(center, radius, 0f, 360f);
        painter.Stroke();

        float endAngle = -90f + 360f * fishingCancelProgress01;
        painter.strokeColor = new Color(0.992f, 0.937f, 0.439f, 1f);
        painter.BeginPath();
        painter.Arc(center, radius, -90f, endAngle);
        painter.Stroke();
    }

    private void RefreshSprintVisual()
    {
        if (btnSprint == null || PlayerController.Instance == null) return;
        bool sprintLit = PlayerController.Instance.IsSprintActive();
        btnSprint.EnableInClassList("sprint-btn-active", sprintLit);
    }
}
