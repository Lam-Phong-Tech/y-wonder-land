using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

/// <summary>
/// Controller overlay Câu Cá — bản GỌN (khách 21/06).
/// Chỉ còn 1 popup "căn thời gian" ở góc phải: hiện số lượt câu trong ngày + nút X.
/// Bỏ: chọn mồi, test cheat, chỉ số mồi, panel kết quả (báo bằng ScreenToast).
/// Luồng: bấm F (hoặc nút Quăng cần) -> cửa sổ căn cá ~8.7s -> giật đúng vùng xanh
///        -> +1 cá vào túi + toast. Trượt/hết giờ -> toast "sẩy cá".
/// </summary>
public class FishingOverlayController : MonoBehaviour
{
    public static FishingOverlayController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIDocument fishingDocument;
    private YWonderLand.Data.ItemDatabase itemDatabase;

    [Header("Luật câu cá (chỉnh được)")]
    [Tooltip("Tổng thời gian (giây) căn cá tính từ lúc bắt đầu 1 lần câu — khách: ~8.7s.")]
    [SerializeField] private float castDuration = 8.7f;
    [Tooltip("Số lượt câu MIỄN PHÍ mỗi ngày.")]
    [SerializeField] private int dailyTurns = 10;
    [Tooltip("Bề rộng vùng xanh 'ăn cá' (% thanh). To hơn = dễ hơn.")]
    [SerializeField] private float safeZoneWidthPercent = 28f;
    [Tooltip("Tốc độ kim chạy (%/giây).")]
    [SerializeField] private float pointerSpeed = 110f;
    [Tooltip("Ti le ca theo nhom Point. De trong thi dung bang khach chot ngay 29/06.")]
    [SerializeField] private List<FishRewardTier> fishRewardTiers = new List<FishRewardTier>();

    // UI elements
    private VisualElement root;
    private Button btnExit;
    private Button btnAction;
    private Label lblFreeTurns;
    private Label lblHint;
    private VisualElement qteSafeZone;
    private VisualElement qtePointer;
    private VisualElement qteTimerBar;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private bool hasSavedCursorState;
    private int autoFishingStartFrame = -1;

    private enum FishingState { Idle, Timing, AutoFishing }
    private FishingState state = FishingState.Idle;
    public bool IsAutoFishing => state == FishingState.AutoFishing;

    private int freeTurns = 10;

    // Căn cá
    private float timerElapsed = 0f;
    private float safeZoneLeftPercent = 36f;
    private float pointerPosPercent = 0f;
    private bool pointerMovingRight = true;

    // ── Bảng cá ──
    private struct FishItem
    {
        public string itemId;
        public string name;
        public string rarity;

        public FishItem(string itemId, string name, string rarity)
        {
            this.itemId = itemId;
            this.name = name;
            this.rarity = rarity;
        }
    }

    [System.Serializable]
    private class FishRewardTier
    {
        public int pointValue;
        [Range(0f, 100f)] public float chancePercent;
        public string[] itemIds;

        public FishRewardTier()
        {
        }

        public FishRewardTier(int pointValue, float chancePercent, params string[] itemIds)
        {
            this.pointValue = pointValue;
            this.chancePercent = chancePercent;
            this.itemIds = itemIds;
        }

        public bool HasValidItems()
        {
            if (itemIds == null || itemIds.Length == 0) return false;

            for (int i = 0; i < itemIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(itemIds[i])) return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (fishingDocument == null) fishingDocument = GetComponent<UIDocument>();
        itemDatabase = Resources.Load<YWonderLand.Data.ItemDatabase>("ItemDatabase");
        EnsureFishRewardTiers();

        // Reset lượt câu miễn phí theo NGÀY thật.
        string lastDate = PlayerPrefs.GetString("FishingLastDate", "");
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (lastDate != today)
        {
            freeTurns = dailyTurns;
            PlayerPrefs.SetString("FishingLastDate", today);
            PlayerPrefs.SetInt("FishingFreeTurns", freeTurns);
            PlayerPrefs.Save();
        }
        else
        {
            freeTurns = PlayerPrefs.GetInt("FishingFreeTurns", dailyTurns);
        }
    }

    private void OnEnable()
    {
        if (fishingDocument == null) return;
        root = fishingDocument.rootVisualElement;

        btnExit = root.Q<Button>("BtnExitFishing");
        btnAction = root.Q<Button>("BtnAction");
        lblFreeTurns = root.Q<Label>("LblFreeTurns");
        lblHint = root.Q<Label>("LblHint");
        qteSafeZone = root.Q<VisualElement>("QTESafeZone");
        qtePointer = root.Q<VisualElement>("QTEPointer");
        qteTimerBar = root.Q<VisualElement>("QTETimerBar");

        btnExit?.RegisterCallback<ClickEvent>(OnExitClicked);
        btnAction?.RegisterCallback<ClickEvent>(OnActionClicked);

        UpdateUI();
        Hide();
    }

    private void OnDisable()
    {
        btnExit?.UnregisterCallback<ClickEvent>(OnExitClicked);
        btnAction?.UnregisterCallback<ClickEvent>(OnActionClicked);

        // An toàn: gỡ khỏi tracker để chuột không kẹt nếu overlay tắt giữa chừng (đổi đảo...).
        UIPopupTracker.SetOpen(this, false);
        EndFishingCursorMode();
        GameHUDController.Instance?.HideFishingCancelProgress();
    }

    private void OnExitClicked(ClickEvent evt) => Hide();

    private void OnActionClicked(ClickEvent evt)
    {
        if (state == FishingState.Idle) StartCast(true);
        else AttemptPull();
    }

    private void Update()
    {
        // BẢN TẠM: câu tự động — đợi hết thời gian rồi trao cá (không cần thao tác).
        if (state == FishingState.AutoFishing)
        {
            var autoKeyboard = Keyboard.current;
            if (autoKeyboard != null && Time.frameCount > autoFishingStartFrame && autoKeyboard.fKey.wasPressedThisFrame)
            {
                CancelFishingFromHUD();
                return;
            }

            timerElapsed += Time.deltaTime;
            GameHUDController.Instance?.SetFishingCancelProgress(Mathf.Clamp01(timerElapsed / Mathf.Max(0.1f, castDuration)));
            if (timerElapsed >= castDuration)
                HandleCatch();
            return;
        }

        if (state != FishingState.Timing) return;

        // Phím tắt giật cần: Space hoặc F.
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame))
        {
            AttemptPull();
            return;
        }

        // Kim chạy qua lại.
        float delta = pointerSpeed * Time.deltaTime;
        if (pointerMovingRight)
        {
            pointerPosPercent += delta;
            if (pointerPosPercent >= 100f) { pointerPosPercent = 100f; pointerMovingRight = false; }
        }
        else
        {
            pointerPosPercent -= delta;
            if (pointerPosPercent <= 0f) { pointerPosPercent = 0f; pointerMovingRight = true; }
        }
        if (qtePointer != null) qtePointer.style.left = Length.Percent(pointerPosPercent);

        // Thời gian căn cá còn lại (8.7s).
        timerElapsed += Time.deltaTime;
        float ratio = Mathf.Clamp01(1f - (timerElapsed / castDuration));
        if (qteTimerBar != null) qteTimerBar.style.width = Length.Percent(ratio * 100f);

        if (timerElapsed >= castDuration)
        {
            HandleMiss("Hết giờ, cá bơi mất rồi!");
        }
    }

    /// <summary>
    /// BẢN TẠM (khách 21/06): ẩn popup câu cá. Bấm F -> đợi hết thời gian (animation 8.7s đang chạy)
    /// -> tự cộng 1 con cá + toast. Minigame căn-giờ (Show cũ) giữ lại để SỬA SAU.
    /// </summary>
    public void Show()
    {
        BeginAutoFishing(castDuration);
    }

    public bool CanStartFishing(bool showToast = true)
    {
        if (state != FishingState.Idle) return false;

        if (freeTurns > 0) return true;

        if (showToast)
            YWonderLand.Environment.ScreenToast.Show("Hết lượt câu hôm nay rồi! Mai quay lại nhé.");
        return false;
    }

    public bool BeginAutoFishing(float durationSec)
    {
        if (!CanStartFishing()) return false;

        castDuration = Mathf.Max(0.1f, durationSec);
        freeTurns--;
        PlayerPrefs.SetInt("FishingFreeTurns", freeTurns);
        PlayerPrefs.Save();

        if (fishingDocument != null && fishingDocument.rootVisualElement != null)
            fishingDocument.rootVisualElement.style.display = DisplayStyle.None;

        state = FishingState.AutoFishing;
        timerElapsed = 0f;
        autoFishingStartFrame = Time.frameCount;
        BeginFishingCursorMode();
        GameHUDController.Instance?.HideInteractionPrompt();
        GameHUDController.Instance?.ShowFishingCancelProgress(0f);
        YWonderLand.Environment.ScreenToast.ShowInfo("Đang câu cá... chờ chút nhé!");
        Debug.Log($"[Fishing] Auto-fishing started for {castDuration:F2}s.");
        return true;
    }

    public void CancelFishingFromHUD()
    {
        if (state != FishingState.AutoFishing) return;

        state = FishingState.Idle;
        timerElapsed = 0f;
        GameHUDController.Instance?.HideFishingCancelProgress();
        EndFishingCursorMode();

        if (YWonderLand.Environment.FishingLineController.Instance != null)
            YWonderLand.Environment.FishingLineController.Instance.Reel();

        if (PlayerController.Instance != null)
            PlayerController.Instance.CancelAction();

        YWonderLand.Environment.ScreenToast.Show("Đã hủy câu cá.");
        Debug.Log("[Fishing] Auto-fishing cancelled.");
    }

    private void BeginFishingCursorMode()
    {
        if (!hasSavedCursorState)
        {
            previousCursorLockState = UnityEngine.Cursor.lockState;
            previousCursorVisible = UnityEngine.Cursor.visible;
            hasSavedCursorState = true;
        }

        UIPopupTracker.SetOpen(this, true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void EndFishingCursorMode()
    {
        autoFishingStartFrame = -1;
        UIPopupTracker.SetOpen(this, false);

        if (!hasSavedCursorState) return;

        if (!UIPopupTracker.AnyOpen)
        {
            UnityEngine.Cursor.lockState = previousCursorLockState;
            UnityEngine.Cursor.visible = previousCursorVisible;
        }

        hasSavedCursorState = false;
    }

    /// <summary>Đóng overlay câu cá.</summary>
    public void Hide()
    {
        if (fishingDocument == null || fishingDocument.rootVisualElement == null) return;

        state = FishingState.Idle;
        fishingDocument.rootVisualElement.style.display = DisplayStyle.None;
        GameHUDController.Instance?.HideFishingCancelProgress();
        EndFishingCursorMode();

        // Thu dây câu + phao về.
        if (YWonderLand.Environment.FishingLineController.Instance != null)
            YWonderLand.Environment.FishingLineController.Instance.Reel();

        UIPopupTracker.SetOpen(this, false);

        var hud = FindFirstObjectByType<GameHUDController>();
        if (hud != null) hud.SetHUDVisible(true);

        Debug.Log("[Fishing] Exiting Fishing Mode");
    }

    /// <summary>Bắt đầu 1 lần căn cá (trừ 1 lượt). playAnim=true thì cho nhân vật vung cần lại.</summary>
    private void StartCast(bool playAnim)
    {
        if (state != FishingState.Idle) return;

        if (freeTurns <= 0)
        {
            YWonderLand.Environment.ScreenToast.Show("Hết lượt câu hôm nay rồi! Mai quay lại nhé.");
            UpdateUI();
            return;
        }

        freeTurns--;
        PlayerPrefs.SetInt("FishingFreeTurns", freeTurns);
        PlayerPrefs.Save();

        if (playAnim && PlayerController.Instance != null)
            PlayerController.Instance.PlayActionAnimation("Fishing", castDuration, YWonderLand.Player.ToolType.FishingRod);

        // Đặt vùng xanh ngẫu nhiên trong thanh.
        safeZoneLeftPercent = Random.Range(12f, 88f - safeZoneWidthPercent);
        if (qteSafeZone != null)
        {
            qteSafeZone.style.width = Length.Percent(safeZoneWidthPercent);
            qteSafeZone.style.left = Length.Percent(safeZoneLeftPercent);
        }

        state = FishingState.Timing;
        timerElapsed = 0f;
        pointerPosPercent = 0f;
        pointerMovingRight = true;

        UpdateUI();
        Debug.Log("[Fishing] Cast started. Timing window begins.");
    }

    /// <summary>Giật cần — trúng vùng xanh thì ăn cá.</summary>
    private void AttemptPull()
    {
        if (state != FishingState.Timing) return;

        float safeMin = safeZoneLeftPercent;
        float safeMax = safeZoneLeftPercent + safeZoneWidthPercent;
        bool success = pointerPosPercent >= safeMin && pointerPosPercent <= safeMax;

        if (success) HandleCatch();
        else HandleMiss("Trượt rồi! Giật khi phao nằm trong vùng xanh nhé.");
    }

    private void EnsureFishRewardTiers()
    {
        if (fishRewardTiers == null)
        {
            fishRewardTiers = new List<FishRewardTier>();
        }

        if (fishRewardTiers.Count > 0) return;

        fishRewardTiers.Add(new FishRewardTier(25, 2f, "fish_ca_rong_do_01"));
        fishRewardTiers.Add(new FishRewardTier(15, 4f, "fish_ca_hoang_de_01", "fish_ca_ngu_hoang_kim_01"));
        fishRewardTiers.Add(new FishRewardTier(10, 7f, "fish_ca_mat_quy_01", "fish_ca_heo_bien_01"));
        fishRewardTiers.Add(new FishRewardTier(6, 17f, "fish_ca_soc_dua_01", "fish_ca_khe_01", "fish_ca_mu_01"));
        fishRewardTiers.Add(new FishRewardTier(4, 25f, "fish_ca_su_tu_01", "fish_ca_naso_01", "fish_ca_nhong_01"));
        fishRewardTiers.Add(new FishRewardTier(2, 45f, "fish_ca_com_01", "fish_ca_nuc_01", "fish_ca_hong_01"));
    }

    private FishItem PickCaughtFish()
    {
        EnsureFishRewardTiers();

        FishRewardTier tier = PickFishTier();
        string itemId = PickFishItemId(tier);
        var itemDef = itemDatabase != null ? itemDatabase.GetItem(itemId) : null;
        string displayName = itemDef != null && !string.IsNullOrEmpty(itemDef.itemName)
            ? itemDef.itemName
            : itemId;

        return new FishItem(itemId, displayName, $"{tier.pointValue} Point");
    }

    private FishRewardTier PickFishTier()
    {
        float totalChance = 0f;
        FishRewardTier lastValidTier = null;

        foreach (var tier in fishRewardTiers)
        {
            if (tier == null || tier.chancePercent <= 0f || !tier.HasValidItems()) continue;

            totalChance += tier.chancePercent;
            lastValidTier = tier;
        }

        if (totalChance <= 0f)
        {
            return new FishRewardTier(2, 100f, "fish_ca_com_01", "fish_ca_nuc_01", "fish_ca_hong_01");
        }

        float roll = Random.Range(0f, totalChance);
        foreach (var tier in fishRewardTiers)
        {
            if (tier == null || tier.chancePercent <= 0f || !tier.HasValidItems()) continue;

            if (roll < tier.chancePercent) return tier;
            roll -= tier.chancePercent;
        }

        return lastValidTier ?? new FishRewardTier(2, 100f, "fish_ca_com_01", "fish_ca_nuc_01", "fish_ca_hong_01");
    }

    private static string PickFishItemId(FishRewardTier tier)
    {
        if (tier == null || !tier.HasValidItems()) return "fish_ca_com_01";

        for (int guard = 0; guard < 8; guard++)
        {
            string itemId = tier.itemIds[Random.Range(0, tier.itemIds.Length)];
            if (!string.IsNullOrWhiteSpace(itemId)) return itemId;
        }

        for (int i = 0; i < tier.itemIds.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tier.itemIds[i])) return tier.itemIds[i];
        }

        return "fish_ca_com_01";
    }

    private void HandleCatch()
    {
        state = FishingState.Idle;

        // Customer table 29/06: pick tier first, then a random fish inside that tier.
        FishItem caught = PickCaughtFish();

        var inv = YWonderLand.Managers.InventoryManager.Instance;
        if (inv != null && !string.IsNullOrEmpty(caught.itemId))
            inv.AddItem(caught.itemId, 1);

        var itemDef = itemDatabase != null ? itemDatabase.GetItem(caught.itemId) : null;
        var fallbackDef = itemDatabase != null ? itemDatabase.GetItem("fish_01") : null;
        Texture2D toastIconTexture = itemDef != null && itemDef.iconTexture != null
            ? itemDef.iconTexture
            : (fallbackDef != null ? fallbackDef.iconTexture : null);
        Sprite toastIconSprite = itemDef != null && itemDef.iconSprite != null
            ? itemDef.iconSprite
            : (fallbackDef != null ? fallbackDef.iconSprite : null);
        YWonderLand.Environment.ScreenToast.ShowInfoWithIcon(
            $"Câu được: +1 {caught.name} ({caught.rarity})",
            toastIconTexture,
            toastIconSprite,
            "Fish");
        Debug.Log($"[Fishing] Caught {caught.name}.");

        // Thu dây câu + phao về (vì StartFishing đã quăng dây ra).
        if (YWonderLand.Environment.FishingLineController.Instance != null)
            YWonderLand.Environment.FishingLineController.Instance.Reel();

        GameHUDController.Instance?.HideFishingCancelProgress();
        EndFishingCursorMode();
        UpdateUI();
    }

    private void HandleMiss(string reason)
    {
        state = FishingState.Idle;
        YWonderLand.Environment.ScreenToast.Show(reason);
        Debug.Log($"[Fishing] Miss: {reason}");
        GameHUDController.Instance?.HideFishingCancelProgress();
        EndFishingCursorMode();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (lblFreeTurns != null) lblFreeTurns.text = $"Lượt câu hôm nay: {freeTurns}/{dailyTurns}";

        bool timing = state == FishingState.Timing;

        if (btnAction != null)
        {
            btnAction.text = timing ? "GIẬT CẦN!" : "QUĂNG CẦN";
            btnAction.EnableInClassList("fish-action-btn--pull", timing);
            btnAction.SetEnabled(timing || freeTurns > 0); // hết lượt + đang Idle -> mờ nút
        }

        if (lblHint != null)
        {
            if (timing)
                lblHint.text = "Canh phao vào VÙNG XANH rồi Giật (F / Space)!";
            else if (freeTurns > 0)
                lblHint.text = "Bấm Quăng cần (F) để bắt đầu";
            else
                lblHint.text = "Hết lượt câu hôm nay — mai quay lại nhé!";
        }

        // Cập nhật vị trí kim; thanh giờ về đầy khi không trong lúc căn cá.
        if (qtePointer != null) qtePointer.style.left = Length.Percent(pointerPosPercent);
        if (!timing && qteTimerBar != null) qteTimerBar.style.width = Length.Percent(100f);
    }
}
