using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

/// <summary>
/// Controller for the Fishing Overlay and mini-game.
/// Handles casting, waiting, QTE timing gauge, bait stocks, results, and cheats.
/// </summary>
public class FishingOverlayController : MonoBehaviour
{
    public static FishingOverlayController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIDocument fishingDocument;
    [SerializeField] private ConfirmDialogController confirmDialog;

    // UI Elements
    private VisualElement root;
    private Button btnExitFishing;
    private Label lblFreeTurns;
    private Label lblBaitNormal;
    private Label lblBaitPremium;

    // Ready Panel
    private VisualElement panelReady;
    private Button btnBaitNone;
    private Button btnBaitNormal;
    private Button btnBaitPremium;
    private Button btnCast;

    // Waiting Panel
    private VisualElement panelWaiting;
    private Label lblBobberEmoji;
    private Button btnCancelCast;

    // QTE Panel
    private VisualElement panelQte;
    private VisualElement qteSafeZone;
    private VisualElement qtePointer;
    private VisualElement qteTimerBar;
    private Button btnPull;

    // Result Panel
    private VisualElement panelResultShadow;
    private VisualElement panelResult;
    private VisualElement resultHeader;
    private Label lblResultTitle;
    private Label lblResultEmoji;
    private Label lblResultName;
    private Label lblResultRarity;
    private Label lblResultDesc;
    private Button btnResultClose;

    // Cheat Bar
    private Button btnCheatRefill;
    private Button btnCheatAddBait;
    private Toggle toggleCheatAutoWin;

    // Gameplay States
    private enum FishingState { Idle, Casting, Waiting, QTE, Result }
    private FishingState state = FishingState.Idle;

    private enum BaitType { None, Normal, Premium }
    private BaitType selectedBait = BaitType.None;

    // Game Variables
    private int freeTurns = 10;
    private int normalBait = 5;

    // QTE Variables
    private float qteTimerElapsed = 0f;
    private const float QTE_TIME_LIMIT = 1.5f;
    private float safeZoneWidthPercent = 20f;
    private float safeZoneLeftPercent = 40f;
    private float pointerPosPercent = 0f;
    private float pointerOscillationSpeed = 160f; // Bounces back and forth quickly
    private bool pointerMovingRight = true;

    // Coroutine tracking
    private Coroutine waitingCoroutine;

    // Fish Database Item
    private struct FishItem
    {
        public string itemId;
        public string name;
        public string emoji;
        public string rarity;
        public string desc;
        public float rewardCoins;
        public string rarityColorHex;

        public FishItem(string itemId, string name, string emoji, string rarity, string desc, float rewardCoins, string colorHex)
        {
            this.itemId = itemId;
            this.name = name;
            this.emoji = emoji;
            this.rarity = rarity;
            this.desc = desc;
            this.rewardCoins = rewardCoins;
            this.rarityColorHex = colorHex;
        }
    }

    private readonly List<FishItem> commonFish = new List<FishItem>
    {
        new FishItem("fish_01", "Cá Rô Phi", "🐟", "Thường", "Cá rô phi ngọt thịt, rất phổ biến ở ao thành phố. Nhận +20 POS!", 20f, "#9E9E9E"),
        new FishItem("fish_01", "Cá Chép", "🐟", "Thường", "Cá chép sông dày mình, nấu canh rất ngon. Nhận +30 POS!", 30f, "#9E9E9E")
    };

    private readonly List<FishItem> rareFish = new List<FishItem>
    {
        new FishItem("fish_02", "Cá Trắm Đen", "🐟", "Hiếm", "Cá trắm đen to lớn khỏe mạnh, cực kỳ hiếm gặp ở vùng nước ngọt. Nhận +60 POS!", 60f, "#5B42F3"),
        new FishItem("fish_02", "Cá Hồi Vây Đỏ", "🐟", "Hiếm", "Cá hồi di cư ngược dòng nước xiết, thịt béo bổ dưỡng. Nhận +80 POS!", 80f, "#5B42F3")
    };

    private readonly List<FishItem> epicFish = new List<FishItem>
    {
        new FishItem("fish_02", "Cá Kiếm Vàng", "🐠", "Sử Thi", "Chú cá cảnh lấp lánh mang sắc vàng hoàng gia kiêu hãnh. Nhận +150 POS!", 150f, "#FFC107"),
        new FishItem("gift_box_01", "Bao Lì Xì Event", "🎁", "Sự Kiện", "Bao lì xì rớt ra từ sự kiện sông nước Y WONDER LAND. Nhận +3 Vật phẩm sự kiện 🎫!", 0f, "#9C27B0")
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (fishingDocument == null)
            fishingDocument = GetComponent<UIDocument>();

        // Check date for resetting free turns
        string lastDate = PlayerPrefs.GetString("FishingLastDate", "");
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        
        if (lastDate != today)
        {
            freeTurns = 10;
            PlayerPrefs.SetString("FishingLastDate", today);
            PlayerPrefs.SetInt("FishingFreeTurns", freeTurns);
            PlayerPrefs.Save();
        }
        else
        {
            freeTurns = PlayerPrefs.GetInt("FishingFreeTurns", 10);
        }
    }

    private void OnEnable()
    {
        if (confirmDialog == null) confirmDialog = FindFirstObjectByType<ConfirmDialogController>();
        if (fishingDocument == null) return;

        root = fishingDocument.rootVisualElement;

        // Query Status Bar
        btnExitFishing = root.Q<Button>("BtnExitFishing");
        lblFreeTurns = root.Q<Label>("LblFreeTurns");
        lblBaitNormal = root.Q<Label>("LblBaitNormal");
        lblBaitPremium = root.Q<Label>("LblBaitPremium");

        // Query Ready Panel
        panelReady = root.Q<VisualElement>("PanelReady");
        btnBaitNone = root.Q<Button>("BtnBaitNone");
        btnBaitNormal = root.Q<Button>("BtnBaitNormal");
        btnBaitPremium = root.Q<Button>("BtnBaitPremium");
        btnCast = root.Q<Button>("BtnCast");

        // Query Waiting Panel
        panelWaiting = root.Q<VisualElement>("PanelWaiting");
        lblBobberEmoji = root.Q<Label>("LblBobberEmoji");
        btnCancelCast = root.Q<Button>("BtnCancelCast");

        // Query QTE Panel
        panelQte = root.Q<VisualElement>("PanelQTE");
        qteSafeZone = root.Q<VisualElement>("QTESafeZone");
        qtePointer = root.Q<VisualElement>("QTEPointer");
        qteTimerBar = root.Q<VisualElement>("QTETimerBar");
        btnPull = root.Q<Button>("BtnPull");

        // Query Result Panel
        panelResultShadow = root.Q<VisualElement>("PanelResultShadow");
        panelResult = root.Q<VisualElement>("PanelResult");
        resultHeader = root.Q<VisualElement>("ResultHeader");
        lblResultTitle = root.Q<Label>("LblResultTitle");
        lblResultEmoji = root.Q<Label>("LblResultEmoji");
        lblResultName = root.Q<Label>("LblResultName");
        lblResultRarity = root.Q<Label>("LblResultRarity");
        lblResultDesc = root.Q<Label>("LblResultDesc");
        btnResultClose = root.Q<Button>("BtnResultClose");

        // Query Cheat Bar
        btnCheatRefill = root.Q<Button>("BtnCheatRefill");
        btnCheatAddBait = root.Q<Button>("BtnCheatAddBait");
        toggleCheatAutoWin = root.Q<Toggle>("ToggleCheatAutoWin");

        RegisterCallbacks();
        UpdateUI();
        
        // Start Hidden
        Hide();
    }

    private void RegisterCallbacks()
    {
        // Exit
        btnExitFishing?.RegisterCallback<ClickEvent>(evt => Hide());

        // Cast & Cancel
        btnCast?.RegisterCallback<ClickEvent>(evt => TryCastRod());
        btnCancelCast?.RegisterCallback<ClickEvent>(evt => CancelFishing());

        // Bait choices
        btnBaitNone?.RegisterCallback<ClickEvent>(evt => SelectBait(BaitType.None));
        btnBaitNormal?.RegisterCallback<ClickEvent>(evt => SelectBait(BaitType.Normal));
        btnBaitPremium?.RegisterCallback<ClickEvent>(evt => SelectBait(BaitType.Premium));

        // Pull & Result Close
        btnPull?.RegisterCallback<ClickEvent>(evt => AttemptPull());
        btnResultClose?.RegisterCallback<ClickEvent>(evt => CloseResult());

        // Cheats
        btnCheatRefill?.RegisterCallback<ClickEvent>(evt =>
        {
            freeTurns = 10;
            PlayerPrefs.SetInt("FishingFreeTurns", freeTurns);
            UpdateUI();
            Debug.Log("[Fishing] Cheat: Refilled free turns to 10.");
        });

        btnCheatAddBait?.RegisterCallback<ClickEvent>(evt =>
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null) inv.AddItem("bait_01", 10);
            UpdateUI();
            Debug.Log("[Fishing] Cheat: Added 10 normal baits.");
        });
    }

    private void Update()
    {
        if (state == FishingState.Idle) return;

        // Global hotkey Space to Pull when in QTE state
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (state == FishingState.QTE && keyboard.spaceKey.wasPressedThisFrame)
            {
                AttemptPull();
            }
        }

        // State 2 Waiting: Bobber Animation
        if (state == FishingState.Waiting && lblBobberEmoji != null)
        {
            float bobAmount = Mathf.Sin(Time.time * 6f) * 12f;
            lblBobberEmoji.style.translate = new Translate(0f, bobAmount, 0f);
        }

        // State 3 QTE: Needle oscillation + Timer decay
        if (state == FishingState.QTE)
        {
            // Bouncing pointer calculation
            float delta = pointerOscillationSpeed * Time.deltaTime;
            if (pointerMovingRight)
            {
                pointerPosPercent += delta;
                if (pointerPosPercent >= 100f)
                {
                    pointerPosPercent = 100f;
                    pointerMovingRight = false;
                }
            }
            else
            {
                pointerPosPercent -= delta;
                if (pointerPosPercent <= 0f)
                {
                    pointerPosPercent = 0f;
                    pointerMovingRight = true;
                }
            }

            if (qtePointer != null)
            {
                qtePointer.style.left = Length.Percent(pointerPosPercent);
            }

            // Time decay
            qteTimerElapsed += Time.deltaTime;
            float timeRatio = Mathf.Clamp01(1f - (qteTimerElapsed / QTE_TIME_LIMIT));
            if (qteTimerBar != null)
            {
                qteTimerBar.style.width = Length.Percent(timeRatio * 100f);
            }

            if (qteTimerElapsed >= QTE_TIME_LIMIT)
            {
                HandleQTEFail("Cá đớp mồi chạy mất tiêu vì bạn giật quá chậm! 😢");
            }
        }
    }

    /// <summary>
    /// Open the Fishing interface screen.
    /// </summary>
    public void Show()
    {
        if (fishingDocument != null && fishingDocument.rootVisualElement != null)
        {
            fishingDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            state = FishingState.Idle;
            SelectBait(BaitType.None);
            UpdateUI();

            // Hide GameHUD to avoid overlap
            var hud = FindFirstObjectByType<GameHUDController>();
            if (hud != null) hud.SetHUDVisible(false);

            Debug.Log("[Fishing] Entering Fishing Mode");
        }
    }

    /// <summary>
    /// Close the Fishing interface screen.
    /// </summary>
    public void Hide()
    {
        if (fishingDocument != null && fishingDocument.rootVisualElement != null)
        {
            CancelFishing();
            fishingDocument.rootVisualElement.style.display = DisplayStyle.None;

            // Restore GameHUD
            var hud = FindFirstObjectByType<GameHUDController>();
            if (hud != null) hud.SetHUDVisible(true);

            Debug.Log("[Fishing] Exiting Fishing Mode");
        }
    }

    private void UpdateUI()
    {
        var inv = YWonderLand.Managers.InventoryManager.Instance;
        int currentNormalBait = inv != null ? inv.GetItemQuantity("bait_01") : 0;
        int currentPremiumBait = 0; // Not implemented yet
        
        if (lblFreeTurns != null) lblFreeTurns.text = $"Lượt câu: {freeTurns}/10";
        if (lblBaitNormal != null) lblBaitNormal.text = $"Mồi thường: {currentNormalBait}";
        if (lblBaitPremium != null) lblBaitPremium.text = $"Mồi xịn: {currentPremiumBait}";

        // Bait button active classes
        btnBaitNone?.EnableInClassList("active-bait", selectedBait == BaitType.None);
        btnBaitNormal?.EnableInClassList("active-bait", selectedBait == BaitType.Normal);
        btnBaitPremium?.EnableInClassList("active-bait", selectedBait == BaitType.Premium);

        // Display Panels based on current state
        panelReady?.EnableInClassList("hidden", state != FishingState.Idle);
        panelWaiting?.EnableInClassList("hidden", state != FishingState.Waiting);
        panelQte?.EnableInClassList("hidden", state != FishingState.QTE);
        panelResultShadow?.EnableInClassList("hidden", state != FishingState.Result);
    }

    private void SelectBait(BaitType baitType)
    {
        if (state != FishingState.Idle) return;

        var inv = YWonderLand.Managers.InventoryManager.Instance;

        // Validation: do we have enough?
        if (baitType == BaitType.Normal && (inv == null || inv.GetItemQuantity("bait_01") <= 0))
        {
            Debug.LogWarning("[Fishing] Hết mồi thường!");
            return;
        }
        if (baitType == BaitType.Premium)
        {
            Debug.LogWarning("[Fishing] Hết mồi xịn!");
            return;
        }

        selectedBait = baitType;
        UpdateUI();
    }

    private void TryCastRod()
    {
        if (state != FishingState.Idle) return;

        // Check turn count
        if (freeTurns <= 0 && selectedBait == BaitType.None)
        {
            // Require bait if no free turns
            if (confirmDialog != null)
            {
                confirmDialog.Show(
                    "HẾT LƯỢT MIỄN PHÍ",
                    "Bạn đã hết lượt câu miễn phí hôm nay. Hãy chọn Mồi câu để tiếp tục, hoặc mua thêm Mồi từ cửa hàng.",
                    "Đã hiểu",
                    "Thoát",
                    () => { }
                );
            }
            else
            {
                Debug.LogWarning("[Fishing] Hết lượt câu, yêu cầu mồi!");
            }
            return;
        }

        // Deduct turn and bait
        if (freeTurns > 0)
        {
            freeTurns--;
            PlayerPrefs.SetInt("FishingFreeTurns", freeTurns);
        }
        
        var inv = YWonderLand.Managers.InventoryManager.Instance;
        if (selectedBait == BaitType.Normal && inv != null)
        {
            inv.RemoveItem("bait_01", 1);
        }

        state = FishingState.Waiting;
        UpdateUI();
        Debug.Log($"[Fishing] CastRod successful. Spent 1 turn. Bait selected: {selectedBait}");

        // Start waiting coroutine (random wait time 3 to 6s)
        float waitTime = Random.Range(3f, 6f);
        waitingCoroutine = StartCoroutine(CastingWaitRoutine(waitTime));
    }

    private void OnConfirmBaitPurchase()
    {
        // Mock buying turns/bait
        freeTurns += 5;
        normalBait += 5;
        UpdateUI();
        Debug.Log("[Fishing] Mua 5 lượt câu thành công bằng POS!");
    }

    private IEnumerator CastingWaitRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        
        // Trigger QTE Phase
        state = FishingState.QTE;
        qteTimerElapsed = 0f;
        pointerPosPercent = 0f;
        pointerMovingRight = true;

        // Configure Safe Zone sizes and location dynamically
        // Bait gives benefits: None = 15% width, Normal = 26% width, Premium = 40% width
        switch (selectedBait)
        {
            case BaitType.None:
                safeZoneWidthPercent = 15f;
                pointerOscillationSpeed = 160f;
                break;
            case BaitType.Normal:
                safeZoneWidthPercent = 26f;
                pointerOscillationSpeed = 130f; // slightly slower pointer
                break;
            case BaitType.Premium:
                safeZoneWidthPercent = 40f;
                pointerOscillationSpeed = 100f; // much slower pointer, easier
                break;
        }

        // Random safe zone position (keep it inside the 0-100% track boundary)
        safeZoneLeftPercent = Random.Range(10f, 90f - safeZoneWidthPercent);

        if (qteSafeZone != null)
        {
            qteSafeZone.style.width = Length.Percent(safeZoneWidthPercent);
            qteSafeZone.style.left = Length.Percent(safeZoneLeftPercent);
        }

        UpdateUI();
        Debug.Log("[Fishing] Fish is biting! QTE phase started.");
    }

    private void CancelFishing()
    {
        if (waitingCoroutine != null)
        {
            StopCoroutine(waitingCoroutine);
            waitingCoroutine = null;
        }

        state = FishingState.Idle;
        UpdateUI();
        Debug.Log("[Fishing] Fishing cancelled.");
    }

    private void AttemptPull()
    {
        if (state != FishingState.QTE) return;

        bool isAutoWin = toggleCheatAutoWin != null && toggleCheatAutoWin.value;
        bool isSuccess = false;

        if (isAutoWin)
        {
            isSuccess = true;
            Debug.Log("[Fishing] Cheat: QTE auto-win activated.");
        }
        else
        {
            // Safe zone boundary check
            float safeMin = safeZoneLeftPercent;
            float safeMax = safeZoneLeftPercent + safeZoneWidthPercent;
            if (pointerPosPercent >= safeMin && pointerPosPercent <= safeMax)
            {
                isSuccess = true;
            }
        }

        if (isSuccess)
        {
            HandleQTESuccess();
        }
        else
        {
            HandleQTEFail("Hụt mất rồi! Bạn giật cần khi cá chưa đớp đúng tầm phao! 😢");
        }
    }

    private void HandleQTESuccess()
    {
        state = FishingState.Result;
        
        // Payout determination based on bait
        FishItem caught;
        float roll = Random.Range(0f, 1f);

        // Odds modify based on Premium vs Normal vs None bait
        float epicChance = 0.08f; // 8% base
        float rareChance = 0.25f; // 25% base

        if (selectedBait == BaitType.Normal)
        {
            epicChance = 0.15f;
            rareChance = 0.40f;
        }
        else if (selectedBait == BaitType.Premium)
        {
            epicChance = 0.35f;
            rareChance = 0.50f;
        }

        if (roll < epicChance)
        {
            caught = epicFish[Random.Range(0, epicFish.Count)];
        }
        else if (roll < epicChance + rareChance)
        {
            caught = rareFish[Random.Range(0, rareFish.Count)];
        }
        else
        {
            caught = commonFish[Random.Range(0, commonFish.Count)];
        }

        // Configure Result Screen
        resultHeader?.EnableInClassList("success-bg", true);
        resultHeader?.EnableInClassList("fail-bg", false);

        if (lblResultTitle != null) lblResultTitle.text = "CÂU THÀNH CÔNG!";
        if (lblResultEmoji != null) lblResultEmoji.text = caught.emoji;
        if (lblResultName != null) lblResultName.text = caught.name;
        if (lblResultRarity != null)
        {
            lblResultRarity.text = $"Độ hiếm: {caught.rarity}";
            lblResultRarity.style.color = ParseColor(caught.rarityColorHex);
            lblResultRarity.style.backgroundColor = ParseColor(caught.rarityColorHex + "1A"); // 10% opacity
        }
        if (lblResultDesc != null) lblResultDesc.text = caught.desc;

        UpdateUI();
        Debug.Log($"[Fishing] QTE success. Caught: {caught.name} ({caught.rarity})");

        // Reward Payout Log
        if (caught.rewardCoins > 0)
        {
            // Simulating coin reward addition
            Debug.Log($"[Fishing] Reward coin added to player: +{caught.rewardCoins} POS");
        }
        
        // Add to Inventory
        var inv = YWonderLand.Managers.InventoryManager.Instance;
        if (inv != null && !string.IsNullOrEmpty(caught.itemId))
        {
            inv.AddItem(caught.itemId, 1);
            Debug.Log($"[Fishing] Added 1x {caught.itemId} to inventory.");
        }
        
        // Connect to Event exchange if Gift box caught
        if (caught.rarity == "Sự Kiện")
        {
            Debug.Log("[Fishing] Event gift box added! Triggered +3 tickets.");
        }
    }

    private void HandleQTEFail(string failureMessage)
    {
        state = FishingState.Result;

        // Configure Result Screen for Failures
        resultHeader?.EnableInClassList("success-bg", false);
        resultHeader?.EnableInClassList("fail-bg", true);

        if (lblResultTitle != null) lblResultTitle.text = "HỤT MẤT RỒI!";
        if (lblResultEmoji != null) lblResultEmoji.text = "💨";
        if (lblResultName != null) lblResultName.text = "Cá Đã Thoát";
        if (lblResultRarity != null)
        {
            lblResultRarity.text = "Độ hiếm: Không";
            lblResultRarity.style.color = Color.gray;
            lblResultRarity.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
        }
        if (lblResultDesc != null) lblResultDesc.text = failureMessage;

        UpdateUI();
        Debug.Log("[Fishing] QTE failed. Fish escaped.");
    }

    private void CloseResult()
    {
        state = FishingState.Idle;
        SelectBait(BaitType.None); // default back to none
        UpdateUI();
    }

    /// <summary>
    /// Helper to parse color hex strings.
    /// </summary>
    private Color ParseColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        return Color.white;
    }
}
