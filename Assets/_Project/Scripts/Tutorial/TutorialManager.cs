using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // FLOW MỚI (Giai đoạn 1 — đảo nông trại):
    // Lên đảo (chào) -> tới Cây -> chặt cây -> tới Mỏ -> đào khoáng -> tới Bãi ruộng
    // -> xây ruộng -> cuốc -> trồng -> tưới -> thu hoạch -> tới Bãi chuồng
    // -> xây chuồng -> thả thú -> cho ăn -> hoàn thành.
    // (Câu cá + sang đảo thành phố = Giai đoạn 2, làm sau.)
    public enum TutorialStep
    {
        WaitForStart,       // 0: chờ cutscene
        FollowToTree,       // 1: theo NPC tới cây
        ChopTree,           // 2: chặt cây (lấy gỗ)
        FollowToRock,       // 3: theo NPC tới mỏ
        MineRock,           // 4: đào khoáng (lấy đá)
        FollowToFarmPlot,   // 5: theo NPC tới bãi ruộng
        BuildFarmPlot,      // 6: xây ô ruộng (phím B)
        PlowTile,           // 7: cuốc đất
        PlantSeed,          // 8: trồng hạt
        WaterTile,          // 9: tưới nước
        WaitHarvest,        // 10: chờ chín + thu hoạch
        FollowToPenArea,    // 11: theo NPC tới bãi chuồng
        BuildPen,           // 12: xây chuồng
        PlaceAnimal,        // 13: thả thú vào chuồng
        FeedAnimal,         // 14: cho thú ăn
        Complete            // 15: hoàn thành (Giai đoạn 2: câu cá)
    }

    [Header("Current Progress")]
    public TutorialStep currentStep = TutorialStep.WaitForStart;

    [Header("Testing")]
    [Tooltip("BẬT khi đang phát triển: LUÔN chạy lại tutorial dù hồ sơ đã hoàn thành. NHỚ TẮT khi release.")]
    public bool forceRunTutorialForTesting = false;

    [Header("References")]
    public GuideNPC guideNPC;
    [Tooltip("Vị trí sinh ra NPC Tân Thủ. Để trống = sinh gần Player.")]
    public Transform guideNpcSpawnPoint;

    [Header("Điểm mốc NPC dẫn tới (kéo Empty vào)")]
    [Tooltip("Điểm gần CÂY để chặt")]
    public Transform targetTreeArea;
    [Tooltip("Điểm gần MỎ/ĐÁ để đào")]
    public Transform targetRockArea;
    [Tooltip("Điểm BÃI ĐẤT để xây ruộng")]
    public Transform targetFarmPlotArea;
    [Tooltip("Điểm BÃI để xây chuồng + nuôi thú")]
    public Transform targetPenArea;
    [Tooltip("Hiệu ứng phát sáng (tùy chọn)")]
    public Transform highlightEffect;

    // Ô đất người chơi vừa XÂY (gán động ở bước BuildFarmPlot).
    private FarmTile targetFarmTile;

    private UIDocument hudDocument;
    private Label questLabel;

    // Subtitle UI elements created dynamically via code
    private VisualElement subtitleContainer;
    private Label subtitleLabel;
    private Label subtitleSpeaker;

    // Instruction banner (big overlay)
    private VisualElement instructionBanner;
    private Label instructionText;
    private Label instructionHint;

    // Countdown timer (center overlay during growth)
    private VisualElement countdownContainer;
    private Label countdownNumber;
    private Label countdownLabel;

    // NPC exclamation mark
    private GameObject exclamationMark;

    private float harvestCountdown = 5f;
    private Coroutine countdownCoroutine;

    // Timeout hint system
    private float stepStartTime;
    private bool hasShownHint;
    private const float HINT_TIMEOUT = 120f;

    // Chống kẹt: tự nhảy bước ĐI THEO NPC nếu quá lâu không tới (NPC kẹt ngoài NavMesh / người chơi lạc).
    private bool hasAutoAdvanced;
    private const float FOLLOW_AUTOSKIP = 90f;

    // Ô đất đã biết TRƯỚC khi xây (để phát hiện ô ruộng mới sinh ra)
    private HashSet<FarmTile> knownTilesBeforeBuild = new HashSet<FarmTile>();

    private InventoryPopupController inventoryPopup;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Auto-discover guideNPC if null
        if (guideNPC == null) guideNPC = FindFirstObjectByType<GuideNPC>();

        // Spawn a dynamic GuideNPC if still null
        if (guideNPC == null)
        {
            Vector3 spawnPos = new Vector3(3f, 0.5f, 3f);
            if (guideNpcSpawnPoint != null) spawnPos = guideNpcSpawnPoint.position;
            else
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                    spawnPos = player.transform.position + player.transform.forward * 2f + player.transform.right * 1.5f;
            }

            GameObject npcGo = new GameObject("GuideNPC_DynamicFallback");
            npcGo.transform.position = spawnPos;
            UnityEngine.AI.NavMeshAgent agent = npcGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 2.5f;
            agent.stoppingDistance = 0.5f;
            guideNPC = npcGo.AddComponent<GuideNPC>();
            Debug.Log("[TutorialManager] Created dynamic GuideNPC fallback at: " + spawnPos);
        }
    }

    void Start()
    {
        if (guideNPC != null)
        {
            guideNPC.OnDialogueTriggered += ShowSubtitle;

            var nodes = new List<YWonderLand.Tutorial.TutorialNode>();

            // Node 0: CÂY (chặt cây)
            nodes.Add(BuildNode("TutorialNode_Tree", targetTreeArea, new Vector3(12f, 0.5f, 8f),
                walk: new[] { "Đi theo tôi, đừng có lề mề!", "Tôi đi trước, cậu bám theo sau." },
                wait: new[] { "Nhanh cái chân lên cậu ơi, tôi đợi mốc cả người rồi!", "Cậu vừa đi vừa ngắm cảnh à? Lẹ lên!" },
                action: new[] { "Thấy cái cây kia chứ? Cầm rìu bổ cho tôi vài nhát. Đừng bảo là chưa cầm rìu bao giờ đấy!" },
                idle: new[] { "Cậu đứng đực ra đó làm gì? Tay chân để làm cảnh à?", "Cây nó không tự đổ đâu, vung rìu lên!" },
                OnTreeArrived));

            // Node 1: MỎ (đào khoáng)
            nodes.Add(BuildNode("TutorialNode_Rock", targetRockArea, new Vector3(16f, 0.5f, 12f),
                walk: new[] { "Có gỗ rồi. Giờ theo tôi đi kiếm đá.", "Đi nào, mỏ đá ngay gần đây thôi." },
                wait: new[] { "Lề mề vừa thôi cậu ơi!", "Tôi già rồi mà còn nhanh hơn cậu đấy." },
                action: new[] { "Cầm cúp đập mấy tảng đá kia ra cho tôi. Cần đá để xây dựng đấy." },
                idle: new[] { "Đá nó cứng chứ có cắn cậu đâu mà sợ? Đập đi!", "Còn chờ gì nữa, đập đá lên!" },
                OnRockArrived));

            // Node 2: BÃI RUỘNG (xây ruộng + canh tác)
            nodes.Add(BuildNode("TutorialNode_FarmPlot", targetFarmPlotArea, new Vector3(8f, 0.5f, 8f),
                walk: new[] { "Đủ nguyên liệu rồi. Theo tôi ra bãi đất trống.", "Đi nào, tới lúc làm nông thật sự." },
                wait: new[] { "Nhanh lên, đất đang chờ cậu kìa!", "Lại đây, tôi chỉ cho cách trồng trọt." },
                action: new[] { "Giờ mở Xây Dựng (phím B), chọn Ruộng và đặt một ô đất xuống đây cho tôi." },
                idle: new[] { "Mở phím B lên đi cậu, đứng đó hoài!", "Ruộng không tự mọc ra đâu, xây đi!" },
                OnFarmPlotArrived));

            // Node 3: BÃI CHUỒNG (xây chuồng + nuôi thú)
            nodes.Add(BuildNode("TutorialNode_Pen", targetPenArea, new Vector3(5f, 0.5f, 14f),
                walk: new[] { "Trồng trọt xong rồi, giờ tới chăn nuôi. Theo tôi!", "Đi nào, qua khu chuồng trại." },
                wait: new[] { "Lẹ chân lên, lũ thú đói rồi kìa!", "Cậu lại la cà nữa à?" },
                action: new[] { "Mở Xây Dựng, chọn một cái Chuồng và đặt xuống đây nhé." },
                idle: new[] { "Chuồng đâu? Xây đi cậu!", "Đứng nhìn tôi làm gì, mở phím B xây chuồng đi!" },
                OnPenArrived));

            guideNPC.tutorialNodes = nodes.ToArray();
        }

        StartCoroutine(SetupHUDReferences());
    }

    // Helper tạo 1 TutorialNode động tại điểm mốc (hoặc vị trí mặc định nếu chưa đặt).
    private YWonderLand.Tutorial.TutorialNode BuildNode(string name, Transform anchor, Vector3 fallbackPos,
        string[] walk, string[] wait, string[] action, string[] idle, UnityEngine.Events.UnityAction onArrived)
    {
        GameObject go = new GameObject(name);
        go.transform.position = anchor != null ? anchor.position : fallbackPos;
        var node = go.AddComponent<YWonderLand.Tutorial.TutorialNode>();
        node.walkDialogues = walk;
        node.waitPlayerDialogues = wait;
        node.actionDialogues = action;
        node.idleWarningDialogues = idle;
        node.OnPlayerArrivedAtNode = new UnityEngine.Events.UnityEvent();
        node.OnPlayerArrivedAtNode.AddListener(onArrived);
        return node;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleInteractionRaycast();

        CheckTimeoutHint();
        CheckFollowAutoAdvance();
    }

    // Chống kẹt tutorial: nếu bước ĐI THEO NPC quá lâu không tới nơi (NPC kẹt ngoài NavMesh, người
    // chơi lạc đường), TỰ ĐỘNG tiến bước thay vì khoá tutorial vĩnh viễn. Chỉ áp cho bước đi-theo
    // (bước hành động như chặt/cuốc/trồng vẫn bắt người chơi tự làm).
    private void CheckFollowAutoAdvance()
    {
        if (hasAutoAdvanced) return;
        if (Time.time - stepStartTime < FOLLOW_AUTOSKIP) return;

        switch (currentStep)
        {
            case TutorialStep.FollowToTree:
                hasAutoAdvanced = true;
                Debug.Log("[Tutorial] Auto-nhảy: FollowToTree quá lâu -> tự tới Cây.");
                OnTreeArrived();
                break;
            case TutorialStep.FollowToRock:
                hasAutoAdvanced = true;
                Debug.Log("[Tutorial] Auto-nhảy: FollowToRock quá lâu -> tự tới Mỏ.");
                OnRockArrived();
                break;
            case TutorialStep.FollowToFarmPlot:
                hasAutoAdvanced = true;
                Debug.Log("[Tutorial] Auto-nhảy: FollowToFarmPlot quá lâu -> tự tới bãi ruộng.");
                OnFarmPlotArrived();
                break;
            case TutorialStep.FollowToPenArea:
                hasAutoAdvanced = true;
                Debug.Log("[Tutorial] Auto-nhảy: FollowToPenArea quá lâu -> tự tới bãi chuồng.");
                OnPenArrived();
                break;
        }
    }

    private void CheckTimeoutHint()
    {
        if (currentStep == TutorialStep.WaitForStart ||
            currentStep == TutorialStep.WaitHarvest ||
            currentStep == TutorialStep.Complete) return;

        if (hasShownHint) return;
        if (Time.time - stepStartTime < HINT_TIMEOUT) return;

        hasShownHint = true;
        string hintTitle = "";
        string hintDesc = "";

        switch (currentStep)
        {
            case TutorialStep.FollowToTree:
            case TutorialStep.FollowToRock:
            case TutorialStep.FollowToFarmPlot:
            case TutorialStep.FollowToPenArea:
                hintTitle = "Đi theo NPC!";
                hintDesc = "Dùng W A S D / joystick đi theo ông lão tới nơi.";
                break;
            case TutorialStep.ChopTree:
                hintTitle = "Chặt cây!";
                hintDesc = "Nhấn giữ chuột vào cây để đốn lấy gỗ.";
                break;
            case TutorialStep.MineRock:
                hintTitle = "Đào khoáng!";
                hintDesc = "Nhấn giữ chuột vào tảng đá để đập lấy đá.";
                break;
            case TutorialStep.BuildFarmPlot:
                hintTitle = "Xây ruộng!";
                hintDesc = "Mở Xây Dựng (B), chọn Ruộng và đặt xuống.";
                break;
            case TutorialStep.PlowTile:
                hintTitle = "Cuốc đất!";
                hintDesc = "Nhấp chuột vào ô đất vừa xây để cuốc.";
                break;
            case TutorialStep.PlantSeed:
                hintTitle = "Gieo hạt!";
                hintDesc = "Mở túi chọn hạt rồi nhấp vào ô đất để gieo.";
                break;
            case TutorialStep.WaterTile:
                hintTitle = "Tưới nước!";
                hintDesc = "Nhấp chuột vào ô đất để tưới cây.";
                break;
            case TutorialStep.BuildPen:
                hintTitle = "Xây chuồng!";
                hintDesc = "Mở Xây Dựng (B), chọn Chuồng và đặt xuống.";
                break;
            case TutorialStep.PlaceAnimal:
                hintTitle = "Thả thú!";
                hintDesc = "Bấm vào chuồng, chọn con vật trong túi để thả.";
                break;
            case TutorialStep.FeedAnimal:
                hintTitle = "Cho ăn!";
                hintDesc = "Bấm vào con thú đói, chọn thức ăn để cho ăn.";
                break;
        }

        if (!string.IsNullOrEmpty(hintTitle))
        {
            ShowInstructionBanner(hintTitle, hintDesc);
            ShowSubtitle(hintDesc);
        }
    }

    private void SetStep(TutorialStep newStep)
    {
        currentStep = newStep;
        stepStartTime = Time.time;
        hasShownHint = false;
        hasAutoAdvanced = false;
    }

    private IEnumerator SetupHUDReferences()
    {
        yield return new WaitForSeconds(0.5f);

        GameObject hudGo = GameObject.Find("GameHUD") ?? GameObject.Find("HUD");
        if (hudGo != null)
        {
            hudDocument = hudGo.GetComponent<UIDocument>();
            if (hudDocument != null && hudDocument.rootVisualElement != null)
            {
                var root = hudDocument.rootVisualElement;
                questLabel = root.Q<Label>("QuestText");
                CreateDynamicSubtitleUI(root);
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Gameplay)
            StartTutorial();

        inventoryPopup = FindFirstObjectByType<InventoryPopupController>();
    }

    public bool IsActive()
    {
        return currentStep != TutorialStep.WaitForStart && currentStep != TutorialStep.Complete;
    }

    public void StartTutorial()
    {
        var prof = YWonderLand.Backend.PlayerProfileService.Instance;
        if (!forceRunTutorialForTesting && prof != null && prof.Profile != null && prof.Profile.tutorialCompleted)
        {
            currentStep = TutorialStep.Complete;
            Debug.Log("[TutorialManager] Bỏ qua tutorial — hồ sơ đã hoàn thành trước đó. (Bật 'Force Run Tutorial For Testing' để chạy lại)");
            return;
        }

        SetStep(TutorialStep.FollowToTree);
        UpdateQuestHUD("[1/13] Đi theo NPC Tân Thủ tới chỗ cái cây");
        Debug.Log("[TutorialManager] Onboarding Tutorial (flow mới) bắt đầu.");

        // Lời chào của ông lão khi người chơi mới lên đảo, rồi dẫn tới node 0 (Cây).
        if (guideNPC != null) guideNPC.StartGreetingSequence(0);

        ShowSubtitle("Hừ! Lại một cậu trẻ thành phố lên đảo. Tôi trông coi nông trại này. Đi theo tôi, đừng có lề mề!", 9f);
        ShowInstructionBanner("Chào mừng tới đảo!", "Đi theo ông lão (NPC) tới chỗ cái cây.");

        CreateNPCExclamationMark();
    }

    // ═══════════════ HANDLERS THEO FLOW MỚI ═══════════════

    // --- Trạm 1: CÂY ---
    private void OnTreeArrived()
    {
        if (currentStep != TutorialStep.FollowToTree) return;
        SetStep(TutorialStep.ChopTree);
        UpdateQuestHUD("[2/13] Chặt cây để lấy gỗ");
        ShowInstructionBanner("Chặt cây!", "Nhấn giữ chuột vào cây để đốn gỗ.");

        // KHÔNG auto-nhảy bước dù túi đã có gỗ sẵn (vd loadout test) — bắt người chơi THỰC SỰ chặt 1 nhát.
        YWonderLand.Environment.HarvestableResource.OnResourceHarvested -= OnResourceHarvested;
        YWonderLand.Environment.HarvestableResource.OnResourceHarvested += OnResourceHarvested;
    }

    // --- Trạm 2: MỎ ---
    private void OnRockArrived()
    {
        if (currentStep != TutorialStep.FollowToRock) return;
        SetStep(TutorialStep.MineRock);
        UpdateQuestHUD("[4/14] Đào khoáng để lấy đá");
        ShowInstructionBanner("Đào khoáng!", "Nhấn giữ chuột vào tảng đá để đập lấy đá.");

        // KHÔNG auto-nhảy bước dù túi đã có đá sẵn (vd loadout test) — bắt người chơi THỰC SỰ đập 1 nhát.
        YWonderLand.Environment.HarvestableResource.OnResourceHarvested -= OnResourceHarvested;
        YWonderLand.Environment.HarvestableResource.OnResourceHarvested += OnResourceHarvested;
    }

    // Dùng chung cho chặt cây (ChopTree) và đào đá (MineRock).
    // Chấp nhận BẤT KỲ tài nguyên ở đúng bước (tránh kẹt do yieldItemId không khớp "wood"/"stone").
    private void OnResourceHarvested(string yieldId, int qty)
    {
        Debug.Log($"[Tutorial] OnResourceHarvested: yield='{yieldId}' | step={currentStep}");

        if (currentStep == TutorialStep.ChopTree)
        {
            YWonderLand.Environment.HarvestableResource.OnResourceHarvested -= OnResourceHarvested;
            CompleteNode(0);
            // BỎ bước đào đá (đào đá chỉ ở Thành phố) → sang thẳng bãi ruộng (node FarmPlot = node 2).
            SetStep(TutorialStep.FollowToFarmPlot);
            UpdateQuestHUD("[3/13] Đi theo NPC tới bãi đất trống");
            ShowSubtitleDelayed("Được đấy, có gỗ rồi! Giờ theo tôi ra bãi đất, tôi dạy cậu trồng trọt.");
            StartNode(2);
        }
        else if (currentStep == TutorialStep.MineRock)
        {
            YWonderLand.Environment.HarvestableResource.OnResourceHarvested -= OnResourceHarvested;
            CompleteNode(1);
            SetStep(TutorialStep.FollowToFarmPlot);
            UpdateQuestHUD("[5/14] Đi theo NPC tới bãi đất trống");
            ShowSubtitleDelayed("Tốt! Đủ gỗ đá rồi. Theo tôi ra bãi đất, tôi dạy cậu trồng trọt.");
            StartNode(2);
        }
    }

    // --- Trạm 3: BÃI RUỘNG ---
    private void OnFarmPlotArrived()
    {
        if (currentStep != TutorialStep.FollowToFarmPlot) return;
        SetStep(TutorialStep.BuildFarmPlot);
        UpdateQuestHUD("[4/13] Mở Xây Dựng (B), chọn Ruộng và đặt xuống");
        ShowInstructionBanner("Xây ruộng!", "Mở phím B, chọn Ruộng, đặt 1 ô đất xuống.");

        // Ghi nhớ các ô đất hiện có để phát hiện ô MỚI người chơi vừa xây.
        knownTilesBeforeBuild = new HashSet<FarmTile>(FindObjectsByType<FarmTile>(FindObjectsSortMode.None));

        GhostPlacementController.OnBuildingPlaced -= OnBuildingPlaced;
        GhostPlacementController.OnBuildingPlaced += OnBuildingPlaced;
    }

    // Dùng chung cho xây ruộng (BuildFarmPlot) và xây chuồng (BuildPen).
    private void OnBuildingPlaced(string itemName, int price)
    {
        string lower = string.IsNullOrEmpty(itemName) ? "" : itemName.ToLower();
        Debug.Log($"[Tutorial] OnBuildingPlaced: item='{itemName}' | step={currentStep}");

        if (currentStep == TutorialStep.BuildFarmPlot && (lower.Contains("ruộng") || lower.Contains("farm")))
        {
            GhostPlacementController.OnBuildingPlaced -= OnBuildingPlaced;

            // Tìm ô đất MỚI vừa sinh ra để theo dõi cho các bước cuốc/trồng/tưới/thu hoạch.
            targetFarmTile = FindNewlyBuiltTile();
            if (targetFarmTile != null)
            {
                targetFarmTile.OnTilePlowed += OnTilePlowed;
                targetFarmTile.OnTilePlanted += OnTilePlanted;
                targetFarmTile.OnTileWatered += OnTileWatered;
                targetFarmTile.OnTileHarvested += OnTileHarvested;
                targetFarmTile.tutorialGrowthTime = 5f;
            }

            SetStep(TutorialStep.PlowTile);
            UpdateQuestHUD("[5/13] Cầm cuốc, nhấp vào ô đất vừa xây để cuốc");
            ShowSubtitleDelayed("Ngon! Giờ cầm cuốc nhấp vào ô đất đó để xới lên nào.");
            ShowInstructionBanner("Cuốc đất!", "Nhấp chuột vào ô đất vừa xây.");
            SetNodeDialogues(2, "Cầm cuốc xới ô đất đó lên cho tôi!", "Cuốc đất đi cậu, đứng nhìn hoài vậy!");
        }
        else if (currentStep == TutorialStep.BuildPen && lower.Contains("chuồng"))
        {
            GhostPlacementController.OnBuildingPlaced -= OnBuildingPlaced;
            SetStep(TutorialStep.PlaceAnimal);
            UpdateQuestHUD("[12/13] Bấm vào chuồng, chọn con vật trong túi để thả");
            ShowSubtitleDelayed("Chuồng xong rồi! Giờ bấm vào chuồng, chọn con vật trong túi mà thả vào.");
            ShowInstructionBanner("Thả thú!", "Bấm vào chuồng → chọn con vật phù hợp.");
            SetNodeDialogues(3, "Bấm vào chuồng, chọn con vật mà thả vào!", "Thả con vật vào chuồng đi cậu, đứng đó hoài!");

            // Flow MỚI: nghe sự kiện thả thú từ FarmAnimal (hệ chuồng BuildSurfaceCell), KHÔNG dùng AnimalPenSpawner cũ.
            YWonderLand.Environment.FarmAnimal.OnAnimalSpawned -= OnAnimalSpawned;
            YWonderLand.Environment.FarmAnimal.OnAnimalSpawned += OnAnimalSpawned;
        }
    }

    private FarmTile FindNewlyBuiltTile()
    {
        var all = FindObjectsByType<FarmTile>(FindObjectsSortMode.None);
        foreach (var t in all)
            if (t != null && !knownTilesBeforeBuild.Contains(t)) return t;
        // Fallback: nếu không thấy ô mới, lấy ô bất kỳ để không kẹt.
        return all.Length > 0 ? all[0] : null;
    }

    private void OnTilePlowed(FarmTile tile)
    {
        if (currentStep != TutorialStep.PlowTile) return;
        SetStep(TutorialStep.PlantSeed);
        UpdateQuestHUD("[6/13] Mở túi, chọn hạt rồi nhấp vào ô đất để gieo");
        ShowSubtitleDelayed("Khá đấy! Giờ mở túi chọn hạt giống, rồi gieo xuống ô đất.");
        ShowInstructionBanner("Gieo hạt!", "Mở túi chọn hạt → nhấp ô đất để gieo.");
        SetNodeDialogues(2, "Mở túi chọn hạt rồi gieo xuống ô đó!", "Gieo hạt đi cậu, đất xới sẵn rồi đấy!");
    }

    private void OnTilePlanted(FarmTile tile)
    {
        if (currentStep != TutorialStep.PlantSeed && currentStep != TutorialStep.BuildFarmPlot) return;
        SetStep(TutorialStep.WaterTile);
        UpdateQuestHUD("[7/13] Nhấp vào ô đất để tưới nước");
        ShowSubtitleDelayed("Gieo xong rồi. Cây không có nước thì sao lớn? Tưới đi cậu!");
        ShowInstructionBanner("Tưới nước!", "Nhấp chuột vào ô đất để tưới.");
        SetNodeDialogues(2, "Tưới nước cho cây mau lớn đi!", "Cây khát khô rồi, tưới nước đi cậu!");
    }

    private void OnTileWatered(FarmTile tile)
    {
        if (currentStep != TutorialStep.WaterTile) return;
        SetStep(TutorialStep.WaitHarvest);

        harvestCountdown = 5f;
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(HarvestCountdownSequence());

        UpdateQuestHUD("[8/13] Chờ cây lớn...");
        ShowSubtitleDelayed("Tưới rồi đấy. Chờ xíu cho cây lớn, nôn nóng cũng chẳng nhanh hơn đâu.");
        ShowInstructionBanner("Đợi cây lớn!", "Cây đang phát triển, chờ vài giây để thu hoạch.");
        SetNodeDialogues(2, "Chờ cây lớn xíu, đừng nôn.", "Kiên nhẫn nào cậu, cây sắp lớn rồi.");
        ShowCountdownTimer();
    }

    private IEnumerator HarvestCountdownSequence()
    {
        while (harvestCountdown > 0f)
        {
            UpdateQuestHUD($"[8/13] Chờ cây chín (còn {Mathf.CeilToInt(harvestCountdown)}s)");
            if (countdownNumber != null)
            {
                countdownNumber.text = Mathf.CeilToInt(harvestCountdown).ToString();
                if (harvestCountdown > 3f) countdownNumber.style.color = new Color(0.4f, 0.9f, 0.3f, 1f);
                else if (harvestCountdown > 1.5f) countdownNumber.style.color = new Color(1f, 0.85f, 0.2f, 1f);
                else countdownNumber.style.color = new Color(1f, 0.3f, 0.2f, 1f);
            }
            yield return new WaitForSeconds(1f);
            harvestCountdown -= 1f;
        }

        HideCountdownTimer();
        UpdateQuestHUD("[9/13] Nhấp vào ô đất để thu hoạch!");
        ShowSubtitle("Chín rồi kìa! Mau nhấp vào thu hoạch đi cậu.");
        ShowInstructionBanner("Đã chín!", "Nhấp chuột vào ô đất để thu hoạch.");
        SetNodeDialogues(2, "Chín rồi, nhấp vào nhổ lên đi cậu!", "Cây chín rục rồi kìa, còn chờ gì nữa!");
    }

    private void OnTileHarvested(FarmTile tile)
    {
        if (currentStep != TutorialStep.WaitHarvest && currentStep != TutorialStep.PlantSeed) return;

        CompleteNode(2);
        SetStep(TutorialStep.FollowToPenArea);
        UpdateQuestHUD("[10/13] Đi theo NPC tới bãi chuồng trại");
        ShowSubtitleDelayed("Xuất sắc! Thu hoạch xong rồi. Giờ qua chuyện chăn nuôi — theo tôi!");
        ShowInstructionBanner("Đã thu hoạch!", "Đi theo NPC tới bãi xây chuồng.");
        StartNode(3);
    }

    // --- Trạm 4: BÃI CHUỒNG ---
    private void OnPenArrived()
    {
        if (currentStep != TutorialStep.FollowToPenArea) return;
        SetStep(TutorialStep.BuildPen);
        UpdateQuestHUD("[11/13] Mở Xây Dựng (B), chọn Chuồng và đặt xuống");
        ShowInstructionBanner("Xây chuồng!", "Mở phím B, chọn Chuồng, đặt xuống.");

        GhostPlacementController.OnBuildingPlaced -= OnBuildingPlaced;
        GhostPlacementController.OnBuildingPlaced += OnBuildingPlaced;
    }

    private void OnAnimalSpawned(YWonderLand.Environment.FarmAnimal animal)
    {
        if (currentStep != TutorialStep.PlaceAnimal) return;
        YWonderLand.Environment.FarmAnimal.OnAnimalSpawned -= OnAnimalSpawned;
        SetStep(TutorialStep.FeedAnimal);
        UpdateQuestHUD("[13/13] Bấm vào con thú đói, chọn thức ăn cho ăn");
        ShowSubtitleDelayed("Có thú rồi! Mà nó đang đói meo kìa — bấm vào nó, chọn ngô trong túi cho ăn ngay đi!");
        ShowInstructionBanner("Cho ăn!", "Bấm vào con thú → chọn thức ăn trong túi.");
        SetNodeDialogues(3, "Bấm vào con thú, chọn thức ăn cho nó ăn!", "Con vật đói kìa, cho nó ăn đi cậu!");

        YWonderLand.Environment.FarmAnimal.OnAnimalFed -= OnAnimalFed;
        YWonderLand.Environment.FarmAnimal.OnAnimalFed += OnAnimalFed;
    }

    private void OnAnimalFed(YWonderLand.Environment.FarmAnimal animal)
    {
        if (currentStep != TutorialStep.FeedAnimal) return;
        YWonderLand.Environment.FarmAnimal.OnAnimalFed -= OnAnimalFed;
        CompleteNode(3);
        CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        currentStep = TutorialStep.Complete;
        UpdateQuestHUD("Hoàn thành Hướng Dẫn Tân Thủ!");
        ShowSubtitleDelayed("Hừ, cũng không tệ lắm cho một cậu trẻ thành phố. Tự lo liệu được rồi đấy. Đi mà khám phá đi!", 2.5f, 7f);
        ShowInstructionBanner("Hoàn Thành!", "Cậu đã học xong các kỹ năng cơ bản!");

        GiveTutorialRewards();

        if (YWonderLand.Backend.PlayerProfileService.Instance != null)
            YWonderLand.Backend.PlayerProfileService.Instance.SetTutorialCompleted(true);

        CancelInvoke(nameof(HideSubtitle));
        Invoke(nameof(HideSubtitle), 5f);
    }

    // ── Tiện ích điều phối GuideNPC ──
    private void CompleteNode(int index)
    {
        if (guideNPC != null && guideNPC.tutorialNodes != null && index >= 0 && index < guideNPC.tutorialNodes.Length)
            guideNPC.tutorialNodes[index].CompleteNodeTask();
    }

    private void StartNode(int index)
    {
        if (guideNPC != null && guideNPC.tutorialNodes != null && index >= 0 && index < guideNPC.tutorialNodes.Length)
            guideNPC.StartNode(index);
    }

    // Cập nhật câu giục của NPC tại 1 trạm theo BƯỚC hiện tại (tránh NPC lặp lại câu cũ).
    private void SetNodeDialogues(int index, string action, string idle)
    {
        if (guideNPC == null || guideNPC.tutorialNodes == null || index < 0 || index >= guideNPC.tutorialNodes.Length) return;
        var n = guideNPC.tutorialNodes[index];
        n.actionDialogues = new[] { action };
        n.idleWarningDialogues = new[] { idle };
    }

    // ═══════════════ UI ĐỘNG (giữ nguyên từ bản cũ) ═══════════════

    private void CreateDynamicSubtitleUI(VisualElement root)
    {
        subtitleContainer = new VisualElement();
        subtitleContainer.style.position = Position.Absolute;
        subtitleContainer.style.bottom = 120;
        subtitleContainer.style.alignSelf = Align.Center;
        subtitleContainer.style.width = 500;
        subtitleContainer.style.backgroundColor = new Color(0.25f, 0.29f, 0.38f, 0.95f);
        subtitleContainer.style.borderTopWidth = 2f;
        subtitleContainer.style.borderBottomWidth = 2f;
        subtitleContainer.style.borderLeftWidth = 2f;
        subtitleContainer.style.borderRightWidth = 2f;
        Color subtitleBorderColor = new Color(0.4f, 0.45f, 0.55f, 1f);
        subtitleContainer.style.borderTopColor = subtitleBorderColor;
        subtitleContainer.style.borderBottomColor = subtitleBorderColor;
        subtitleContainer.style.borderLeftColor = subtitleBorderColor;
        subtitleContainer.style.borderRightColor = subtitleBorderColor;
        subtitleContainer.style.borderTopLeftRadius = 20f;
        subtitleContainer.style.borderTopRightRadius = 20f;
        subtitleContainer.style.borderBottomLeftRadius = 20f;
        subtitleContainer.style.borderBottomRightRadius = 20f;
        subtitleContainer.style.paddingLeft = 24;
        subtitleContainer.style.paddingRight = 24;
        subtitleContainer.style.paddingTop = 16;
        subtitleContainer.style.paddingBottom = 16;
        subtitleContainer.style.display = DisplayStyle.None;

        subtitleSpeaker = new Label("NPC Tân Thủ");
        subtitleSpeaker.style.fontSize = 13;
        subtitleSpeaker.style.unityFontStyleAndWeight = FontStyle.Bold;
        subtitleSpeaker.style.color = new Color(1f, 0.85f, 0.4f, 1f);
        subtitleSpeaker.style.marginBottom = 6;
        subtitleSpeaker.style.unityTextAlign = TextAnchor.MiddleCenter;

        subtitleLabel = new Label("Lời thoại của NPC...");
        subtitleLabel.style.fontSize = 15;
        subtitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        subtitleLabel.style.color = Color.white;
        subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
        subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        subtitleContainer.Add(subtitleSpeaker);
        subtitleContainer.Add(subtitleLabel);
        root.Add(subtitleContainer);

        instructionBanner = new VisualElement();
        instructionBanner.style.position = Position.Absolute;
        instructionBanner.style.top = 80;
        instructionBanner.style.alignSelf = Align.Center;
        instructionBanner.style.width = 500;
        instructionBanner.style.backgroundColor = new Color(0.18f, 0.48f, 1f, 0.95f);
        instructionBanner.style.borderTopLeftRadius = 16f;
        instructionBanner.style.borderTopRightRadius = 16f;
        instructionBanner.style.borderBottomLeftRadius = 16f;
        instructionBanner.style.borderBottomRightRadius = 16f;
        instructionBanner.style.borderTopWidth = 3f;
        instructionBanner.style.borderBottomWidth = 3f;
        instructionBanner.style.borderLeftWidth = 3f;
        instructionBanner.style.borderRightWidth = 3f;
        Color bannerBorder = new Color(0.24f, 0.21f, 0.21f, 1f);
        instructionBanner.style.borderTopColor = bannerBorder;
        instructionBanner.style.borderBottomColor = bannerBorder;
        instructionBanner.style.borderLeftColor = bannerBorder;
        instructionBanner.style.borderRightColor = bannerBorder;
        instructionBanner.style.paddingLeft = 24;
        instructionBanner.style.paddingRight = 24;
        instructionBanner.style.paddingTop = 16;
        instructionBanner.style.paddingBottom = 16;
        instructionBanner.style.display = DisplayStyle.None;

        instructionText = new Label();
        instructionText.style.fontSize = 20;
        instructionText.style.unityFontStyleAndWeight = FontStyle.Bold;
        instructionText.style.color = Color.white;
        instructionText.style.unityTextAlign = TextAnchor.MiddleCenter;
        instructionText.style.marginBottom = 8;

        instructionHint = new Label();
        instructionHint.style.fontSize = 13;
        instructionHint.style.color = new Color(0.85f, 0.9f, 1f, 1f);
        instructionHint.style.unityTextAlign = TextAnchor.MiddleCenter;
        instructionHint.style.whiteSpace = WhiteSpace.Normal;

        instructionBanner.Add(instructionText);
        instructionBanner.Add(instructionHint);
        root.Add(instructionBanner);

        countdownContainer = new VisualElement();
        countdownContainer.style.position = Position.Absolute;
        countdownContainer.style.top = Length.Percent(35);
        countdownContainer.style.alignSelf = Align.Center;
        countdownContainer.style.width = 200;
        countdownContainer.style.alignItems = Align.Center;
        countdownContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        countdownContainer.style.borderTopLeftRadius = 20f;
        countdownContainer.style.borderTopRightRadius = 20f;
        countdownContainer.style.borderBottomLeftRadius = 20f;
        countdownContainer.style.borderBottomRightRadius = 20f;
        countdownContainer.style.borderTopWidth = 3f;
        countdownContainer.style.borderBottomWidth = 3f;
        countdownContainer.style.borderLeftWidth = 3f;
        countdownContainer.style.borderRightWidth = 3f;
        Color timerBorder = new Color(0.4f, 0.8f, 0.3f, 1f);
        countdownContainer.style.borderTopColor = timerBorder;
        countdownContainer.style.borderBottomColor = timerBorder;
        countdownContainer.style.borderLeftColor = timerBorder;
        countdownContainer.style.borderRightColor = timerBorder;
        countdownContainer.style.paddingTop = 20;
        countdownContainer.style.paddingBottom = 20;
        countdownContainer.style.display = DisplayStyle.None;

        countdownNumber = new Label("5");
        countdownNumber.style.fontSize = 48;
        countdownNumber.style.unityFontStyleAndWeight = FontStyle.Bold;
        countdownNumber.style.color = new Color(0.4f, 0.9f, 0.3f, 1f);
        countdownNumber.style.unityTextAlign = TextAnchor.MiddleCenter;

        countdownLabel = new Label();
        countdownLabel.text = "Cây đang lớn...";
        countdownLabel.style.fontSize = 12;
        countdownLabel.style.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        countdownLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        countdownLabel.style.marginTop = 4;

        countdownContainer.Add(countdownNumber);
        countdownContainer.Add(countdownLabel);
        root.Add(countdownContainer);
    }

    public void ShowSubtitle(string text) => ShowSubtitle(text, 4.5f);

    public void ShowSubtitle(string text, float duration)
    {
        if (subtitleContainer == null || subtitleLabel == null) return;
        subtitleLabel.text = text;
        subtitleContainer.style.display = DisplayStyle.Flex;
        CancelInvoke(nameof(HideSubtitle));
        Invoke(nameof(HideSubtitle), duration);
    }

    // Hiện thoại SAU một khoảng trễ (đợi người chơi làm xong hoạt ảnh, đỡ dồn dập).
    private void ShowSubtitleDelayed(string text, float delay = 2.5f, float duration = 4.5f)
    {
        StartCoroutine(ShowSubtitleDelayedCo(text, delay, duration));
    }

    private IEnumerator ShowSubtitleDelayedCo(string text, float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        ShowSubtitle(text, duration);
    }

    private void HideSubtitle()
    {
        if (subtitleContainer != null) subtitleContainer.style.display = DisplayStyle.None;
    }

    private void UpdateQuestHUD(string questText)
    {
        if (questLabel != null) questLabel.text = questText;
        Debug.Log($"[Quest HUD Update] {questText}");
    }

    private void ShowInstructionBanner(string title, string hint)
    {
        if (instructionBanner == null) return;
        if (instructionText != null) instructionText.text = title;
        if (instructionHint != null) instructionHint.text = hint;
        instructionBanner.style.display = DisplayStyle.Flex;
        CancelInvoke(nameof(HideInstructionBanner));
        Invoke(nameof(HideInstructionBanner), 6f);
    }

    private void HideInstructionBanner()
    {
        if (instructionBanner != null) instructionBanner.style.display = DisplayStyle.None;
    }

    private void ShowCountdownTimer()
    {
        if (countdownContainer != null) countdownContainer.style.display = DisplayStyle.Flex;
    }

    private void HideCountdownTimer()
    {
        if (countdownContainer != null) countdownContainer.style.display = DisplayStyle.None;
    }

    private void GiveTutorialRewards()
    {
        Debug.Log("[TutorialManager] Giving Rewards: +50 POS, +20 EXP + starter items.");

        if (YWonderLand.Managers.EconomyManager.Instance != null)
            YWonderLand.Managers.EconomyManager.Instance.AddPOS(50);

        GameHUDController hudController = FindFirstObjectByType<GameHUDController>();
        if (hudController != null) hudController.SetPlayerEXP(20f);

        if (YWonderLand.Managers.InventoryManager.Instance != null)
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            inv.AddItem("carrot_seed_01", 5);
            inv.AddItem("cabbage_seed_01", 5);
            inv.AddItem("corn_seed_01", 5);
            inv.AddItem("wood_01", 10);
            inv.AddItem("stone_01", 5);
        }
    }

    // ── NPC Exclamation Mark ──
    private void CreateNPCExclamationMark()
    {
        if (guideNPC == null) return;

        exclamationMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exclamationMark.name = "NPC_ExclamationMark";
        exclamationMark.transform.SetParent(guideNPC.transform, false);
        exclamationMark.transform.localPosition = new Vector3(0, 3.2f, 0);
        exclamationMark.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
        Destroy(exclamationMark.GetComponent<Collider>());

        Renderer r = exclamationMark.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = new Color(1f, 0.85f, 0f, 1f);
            r.material.SetColor("_EmissionColor", new Color(1f, 0.85f, 0f, 0.5f));
            r.material.EnableKeyword("_EMISSION");
        }

        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "ExclamationDot";
        dot.transform.SetParent(exclamationMark.transform, false);
        dot.transform.localPosition = new Vector3(0, -1.4f, 0);
        dot.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        Destroy(dot.GetComponent<Collider>());

        Renderer dr = dot.GetComponent<Renderer>();
        if (dr != null)
        {
            dr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            dr.material.color = new Color(1f, 0.85f, 0f, 1f);
            dr.material.SetColor("_EmissionColor", new Color(1f, 0.85f, 0f, 0.5f));
            dr.material.EnableKeyword("_EMISSION");
        }

        StartCoroutine(BobExclamationMark());
    }

    private IEnumerator BobExclamationMark()
    {
        while (exclamationMark != null)
        {
            float y = 3.2f + Mathf.Sin(Time.time * 3f) * 0.2f;
            exclamationMark.transform.localPosition = new Vector3(0, y, 0);
            exclamationMark.transform.Rotate(0, 120f * Time.deltaTime, 0);
            yield return null;
        }
    }

    // ── Interaction Raycasting (hỗ trợ nhấp ô đất tutorial) ──
    private void HandleInteractionRaycast()
    {
        if (Camera.main == null || Mouse.current == null) return;
        if (targetFarmTile == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (float.IsNaN(mousePos.x) || float.IsNaN(mousePos.y)) return;

        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            FarmTile tile = hit.collider.GetComponentInParent<FarmTile>() ?? hit.collider.GetComponent<FarmTile>();
            if (tile != null && tile == targetFarmTile)
                ProcessTileInteraction(tile);
        }
    }

    private void ProcessTileInteraction(FarmTile tile)
    {
        switch (currentStep)
        {
            case TutorialStep.PlowTile:
                tile.InteractPlow();
                break;
            case TutorialStep.WaterTile:
                tile.InteractWater();
                break;
            case TutorialStep.WaitHarvest:
                if (tile.currentState == FarmTile.TileState.Ripe)
                    tile.InteractHarvest(out _, out _);
                break;
        }
    }
}
