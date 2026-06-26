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

    [Header("Luật câu cá (chỉnh được)")]
    [Tooltip("Tổng thời gian (giây) căn cá tính từ lúc bắt đầu 1 lần câu — khách: ~8.7s.")]
    [SerializeField] private float castDuration = 8.7f;
    [Tooltip("Số lượt câu MIỄN PHÍ mỗi ngày.")]
    [SerializeField] private int dailyTurns = 10;
    [Tooltip("Bề rộng vùng xanh 'ăn cá' (% thanh). To hơn = dễ hơn.")]
    [SerializeField] private float safeZoneWidthPercent = 28f;
    [Tooltip("Tốc độ kim chạy (%/giây).")]
    [SerializeField] private float pointerSpeed = 110f;

    // UI elements
    private VisualElement root;
    private Button btnExit;
    private Button btnAction;
    private Label lblFreeTurns;
    private Label lblHint;
    private VisualElement qteSafeZone;
    private VisualElement qtePointer;
    private VisualElement qteTimerBar;

    private enum FishingState { Idle, Timing, AutoFishing }
    private FishingState state = FishingState.Idle;

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

    private readonly List<FishItem> commonFish = new List<FishItem>
    {
        new FishItem("fish_01", "Cá Rô Phi", "Thường"),
        new FishItem("fish_01", "Cá Chép", "Thường")
    };

    private readonly List<FishItem> rareFish = new List<FishItem>
    {
        new FishItem("fish_02", "Cá Trắm Đen", "Hiếm"),
        new FishItem("fish_02", "Cá Hồi Vây Đỏ", "Hiếm")
    };

    private readonly List<FishItem> epicFish = new List<FishItem>
    {
        new FishItem("fish_02", "Cá Kiếm Vàng", "Sử Thi")
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (fishingDocument == null) fishingDocument = GetComponent<UIDocument>();

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
            timerElapsed += Time.deltaTime;
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
        if (state != FishingState.Idle) return; // đang câu rồi thì bỏ qua

        if (freeTurns <= 0)
        {
            YWonderLand.Environment.ScreenToast.Show("Hết lượt câu hôm nay rồi! Mai quay lại nhé.");
            return;
        }

        freeTurns--;
        PlayerPrefs.SetInt("FishingFreeTurns", freeTurns);
        PlayerPrefs.Save();

        // KHÔNG hiện popup; chỉ chờ animation + thời gian rồi trao cá.
        state = FishingState.AutoFishing;
        timerElapsed = 0f;
        YWonderLand.Environment.ScreenToast.ShowInfo("Đang câu cá... chờ chút nhé!");
        Debug.Log("[Fishing] Auto-fishing started (popup hidden).");
    }

    /// <summary>Đóng overlay câu cá.</summary>
    public void Hide()
    {
        if (fishingDocument == null || fishingDocument.rootVisualElement == null) return;

        state = FishingState.Idle;
        fishingDocument.rootVisualElement.style.display = DisplayStyle.None;

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

    private void HandleCatch()
    {
        state = FishingState.Idle;

        // Bốc loại cá theo tỉ lệ (không còn mồi -> tỉ lệ cơ bản).
        FishItem caught;
        float roll = Random.Range(0f, 1f);
        if (roll < 0.08f) caught = epicFish[Random.Range(0, epicFish.Count)];
        else if (roll < 0.33f) caught = rareFish[Random.Range(0, rareFish.Count)];
        else caught = commonFish[Random.Range(0, commonFish.Count)];

        var inv = YWonderLand.Managers.InventoryManager.Instance;
        if (inv != null && !string.IsNullOrEmpty(caught.itemId))
            inv.AddItem(caught.itemId, 1);

        YWonderLand.Environment.ScreenToast.ShowInfo($"Câu được: +1 {caught.name} ({caught.rarity})");
        Debug.Log($"[Fishing] Caught {caught.name}.");

        // Thu dây câu + phao về (vì StartFishing đã quăng dây ra).
        if (YWonderLand.Environment.FishingLineController.Instance != null)
            YWonderLand.Environment.FishingLineController.Instance.Reel();

        UpdateUI();
    }

    private void HandleMiss(string reason)
    {
        state = FishingState.Idle;
        YWonderLand.Environment.ScreenToast.Show(reason);
        Debug.Log($"[Fishing] Miss: {reason}");
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
