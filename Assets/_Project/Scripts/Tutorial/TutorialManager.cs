using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public enum TutorialStep
    {
        WaitForStart,   // 0: Waiting for cutscene to arrive
        FollowNPC,      // 1: Follow Guide NPC to farm tile
        PlowTile,       // 2: Plow the farm tile
        OpenInventory,  // 3: Open inventory and select carrot seed
        PlantSeed,      // 4: Plant carrot seed on tile
        WaterTile,      // 5: Water the planted seed
        WaitHarvest,    // 6: Wait for carrot to ripen (60s countdown)
        FollowNPCToAnimalPen, // 7: Follow Guide NPC to Animal Pen
        InteractAnimalPen,    // 8: Hear about Animal Pen
        FollowNPCToMarket,    // 9: Follow Guide NPC to Market
        InteractMarket,       // 10: Open shop and sell carrot
        FollowNPCToResource,  // 11: Follow NPC to forest
        InteractResource,     // 12: Chop tree & mine rock
        FollowNPCToBuild,     // 13: Follow NPC to empty land
        InteractBuild,        // 14: Place a building
        Complete              // 15: Tutorial complete, give rewards
    }

    [Header("Current Progress")]
    public TutorialStep currentStep = TutorialStep.WaitForStart;

    [Header("References")]
    public GuideNPC guideNPC;
    [Tooltip("Vị trí sinh ra NPC Tân Thủ (kéo thả Transform vào đây). Nếu để trống, NPC sẽ sinh ra gần Player.")]
    public Transform guideNpcSpawnPoint;
    public FarmTile targetFarmTile;
    public Transform targetAnimalPen; // Chuồng thú
    public Transform targetMarket; // Quầy giao thương (Chợ)
    public Transform targetResourceArea; // Khu rừng / mỏ đá
    public Transform targetBuildArea; // Bãi đất trống để xây dựng
    public Transform highlightEffect; // Glowing effect on the farm tile

    private UIDocument hudDocument;
    private Label questLabel;           // Reference to HUD's quest text

    // Subtitle UI elements created dynamically via code
    private VisualElement subtitleContainer;
    private Label subtitleLabel;
    private Label subtitleSpeaker;

    // Instruction banner (big overlay when tutorial starts)
    private VisualElement instructionBanner;
    private Label instructionText;
    private Label instructionHint;

    // Countdown timer (big center overlay during growth)
    private VisualElement countdownContainer;
    private Label countdownNumber;
    private Label countdownLabel;

    // NPC exclamation mark
    private GameObject exclamationMark;

    private float harvestCountdown = 60f;
    private Coroutine countdownCoroutine;

    // Timeout hint system (120s)
    private float stepStartTime;
    private bool hasShownHint;
    private const float HINT_TIMEOUT = 120f;
    
    // Resource tracking
    private bool hasHarvestedWood = false;
    private bool hasHarvestedStone = false;

    // Inventory integration
    private InventoryPopupController inventoryPopup;

    void Awake()
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

        // Auto-discover targetFarmTile if null
        if (targetFarmTile == null)
        {
            targetFarmTile = FindFirstObjectByType<FarmTile>();
        }
        
        // Spawn a dynamic FarmTile if still null
        if (targetFarmTile == null)
        {
            GameObject tileGo = new GameObject("FarmTile_DynamicFallback");
            tileGo.transform.position = new Vector3(8f, 0.05f, 8f); // Default coordinate on land
            
            // Add BoxCollider for physics/raycasting
            BoxCollider col = tileGo.AddComponent<BoxCollider>();
            col.size = new Vector3(2f, 0.1f, 2f);
            
            targetFarmTile = tileGo.AddComponent<FarmTile>();
            Debug.Log("[TutorialManager] Created dynamic FarmTile fallback at: " + tileGo.transform.position);
        }

        // Auto-discover guideNPC if null
        if (guideNPC == null)
        {
            guideNPC = FindFirstObjectByType<GuideNPC>();
        }

        // Spawn a dynamic GuideNPC if still null
        if (guideNPC == null)
        {
            Vector3 spawnPos = new Vector3(3f, 0.5f, 3f);
            if (guideNpcSpawnPoint != null)
            {
                spawnPos = guideNpcSpawnPoint.position;
            }
            else
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    spawnPos = player.transform.position + player.transform.forward * 2f + player.transform.right * 1.5f;
                }
            }

            GameObject npcGo = new GameObject("GuideNPC_DynamicFallback");
            npcGo.transform.position = spawnPos;

            // Configure agent
            UnityEngine.AI.NavMeshAgent agent = npcGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 2.5f;
            agent.stoppingDistance = 0.5f;

            guideNPC = npcGo.AddComponent<GuideNPC>();
            Debug.Log("[TutorialManager] Created dynamic GuideNPC fallback at: " + spawnPos);
        }
    }

    void Start()
    {
            // 1. Listen to NPC events
        if (guideNPC != null)
        {
            guideNPC.OnDialogueTriggered += ShowSubtitle;
            
            var dynamicNodesList = new System.Collections.Generic.List<YWonderLand.Tutorial.TutorialNode>();

            // Node 0: Farm Tile
            if (targetFarmTile != null)
            {
                GameObject nodeGo = new GameObject("TutorialNode_FarmTile");
                nodeGo.transform.position = targetFarmTile.transform.position - new Vector3(1.5f, 0, 1.5f);
                YWonderLand.Tutorial.TutorialNode node = nodeGo.AddComponent<YWonderLand.Tutorial.TutorialNode>();
                
                node.walkDialogues = new string[] { "Theo tôi đi nào cậu ơi!", "Tôi đi trước, cậu bước theo sau nhé!" };
                
                node.waitPlayerDialogues = new string[] { 
                    "Nhanh cái chân lên nào cậu ơi, tôi đợi ở đây nè!", 
                    "Alo alo, Trái đất gọi phi hành gia, mạng lag hả cậu? 🐢",
                    "Cậu vừa đi vừa ngắm cảnh à? Nhanh lên tôi chờ tới rễ mọc trên đầu rồi đây này!"
                };
                
                node.actionDialogues = new string[] { "Chúng ta đã đến nơi! Cậu hãy dùng Cuốc nhấp vào mảnh đất vàng để xới tơi đất lên nhé." };
                
                node.idleWarningDialogues = new string[] {
                    "Cậu không biết làm hả? Nhìn lên màn hình có hướng dẫn chi tiết đó!",
                    "Cậu cuốc đất rề rà quá vậy? Cần tôi xắn tay áo vào cuốc dùm luôn không? 😂",
                    "Đứng nhìn ô đất thì nó không tự nảy mầm đâu, bắt tay vào việc đi sếp!"
                };
                
                // When player reaches the node, start the plowing step
                node.OnPlayerArrivedAtNode = new UnityEngine.Events.UnityEvent();
                node.OnPlayerArrivedAtNode.AddListener(OnNPCArrivedAtFarm);
                
                dynamicNodesList.Add(node);
            }

            // Node 1: Animal Pen
            if (targetAnimalPen != null)
            {
                GameObject nodeGo = new GameObject("TutorialNode_AnimalPen");
                nodeGo.transform.position = targetAnimalPen.position;
                YWonderLand.Tutorial.TutorialNode node = nodeGo.AddComponent<YWonderLand.Tutorial.TutorialNode>();
                
                node.walkDialogues = new string[] { "Theo tôi qua xem khu vực chuồng chăn nuôi nào!", "Chuẩn bị làm nông dân chăn lợn chưa cậu?" };
                node.waitPlayerDialogues = new string[] { "Nhanh lên cậu ơi, lợn nó đói rống lên rồi kìa!", "Lại đây nhanh lên, đi dạo hoài vậy!" };
                node.actionDialogues = new string[] { "Đây là khu vực chuồng trại! Tương lai cậu có thể mua động vật để nuôi ở đây." };
                node.idleWarningDialogues = new string[] { "Ngắm nghía chuồng xong chưa cậu?" };

                node.OnPlayerArrivedAtNode = new UnityEngine.Events.UnityEvent();
                node.OnPlayerArrivedAtNode.AddListener(OnNPCArrivedAtAnimalPen);
                
                dynamicNodesList.Add(node);
            }

            // Node 2: Market
            if (targetMarket == null)
            {
                GameObject marketGo = new GameObject("TutorialNode_MarketFallback");
                marketGo.transform.position = new Vector3(10f, 0.5f, 10f); // Default point for market
                targetMarket = marketGo.transform;
            }

            if (targetMarket != null)
            {
                GameObject nodeGo = new GameObject("TutorialNode_Market");
                nodeGo.transform.position = targetMarket.position;
                YWonderLand.Tutorial.TutorialNode node = nodeGo.AddComponent<YWonderLand.Tutorial.TutorialNode>();
                
                node.walkDialogues = new string[] { "Đi theo tôi ra khu vực Chợ Giao Thương nhé!", "Thu hoạch xong rồi thì mang đi bán kiếm lời thôi!" };
                node.waitPlayerDialogues = new string[] { "Lẹ lên cậu ơi, khách hàng đang đợi mua cà rốt kìa!", "Lại đây nhanh lên, thời gian là vàng bạc!" };
                node.actionDialogues = new string[] { "Đây là khu vực Chợ! Cậu hãy mở Giỏ Hàng (Shop), chuyển sang tab 'BÁN' và bán củ Cà rốt nhé." };
                node.idleWarningDialogues = new string[] { "Cậu không muốn kiếm tiền à? Mở Shop lên bán cà rốt đi!" };

                node.OnPlayerArrivedAtNode = new UnityEngine.Events.UnityEvent();
                node.OnPlayerArrivedAtNode.AddListener(OnNPCArrivedAtMarket);
                
                dynamicNodesList.Add(node);
            }

            // Node 3: Resource
            if (targetResourceArea == null)
            {
                GameObject resGo = new GameObject("TutorialNode_ResourceFallback");
                resGo.transform.position = new Vector3(15f, 0.5f, 15f);
                targetResourceArea = resGo.transform;
            }
            if (targetResourceArea != null)
            {
                GameObject nodeGo = new GameObject("TutorialNode_Resource");
                nodeGo.transform.position = targetResourceArea.position;
                YWonderLand.Tutorial.TutorialNode node = nodeGo.AddComponent<YWonderLand.Tutorial.TutorialNode>();
                node.walkDialogues = new string[] { "Tiếp theo, ta đi kiếm chút nguyên liệu nhé!", "Muốn xây nhà thì phải có gỗ và đá!" };
                node.waitPlayerDialogues = new string[] { "Nhanh chân lên nào, rừng thẳm đang vẫy gọi!" };
                node.actionDialogues = new string[] { "Tới nơi rồi! Cậu hãy nhấn giữ chuột vào cây xanh để đốn củi, và đá xám để đập đá nhé. Cần ít nhất 1 Gỗ và 1 Đá." };
                node.idleWarningDialogues = new string[] { "Chặt cây đập đá đi cậu, nhìn tôi làm gì?" };
                node.OnPlayerArrivedAtNode = new UnityEngine.Events.UnityEvent();
                node.OnPlayerArrivedAtNode.AddListener(OnNPCArrivedAtResource);
                dynamicNodesList.Add(node);
            }

            // Node 4: Build
            if (targetBuildArea == null)
            {
                GameObject bldGo = new GameObject("TutorialNode_BuildFallback");
                bldGo.transform.position = new Vector3(5f, 0.5f, 15f);
                targetBuildArea = bldGo.transform;
            }
            if (targetBuildArea != null)
            {
                GameObject nodeGo = new GameObject("TutorialNode_Build");
                nodeGo.transform.position = targetBuildArea.position;
                YWonderLand.Tutorial.TutorialNode node = nodeGo.AddComponent<YWonderLand.Tutorial.TutorialNode>();
                node.walkDialogues = new string[] { "Có nguyên liệu rồi, đi xây công trình đầu tiên thôi!", "Về lại nông trại nào!" };
                node.waitPlayerDialogues = new string[] { "Về đây nhanh cậu ơi!" };
                node.actionDialogues = new string[] { "Bây giờ, hãy mở Chế độ Xây Dựng (phím B), chọn một Hàng rào hoặc Đường đất và đặt xuống nhé!" };
                node.idleWarningDialogues = new string[] { "Mở Xây Dựng lên đi cậu!" };
                node.OnPlayerArrivedAtNode = new UnityEngine.Events.UnityEvent();
                node.OnPlayerArrivedAtNode.AddListener(OnNPCArrivedAtBuild);
                dynamicNodesList.Add(node);
            }

            guideNPC.tutorialNodes = dynamicNodesList.ToArray();
        }

        // 2. Listen to FarmTile events
        if (targetFarmTile != null)
        {
            targetFarmTile.OnTilePlowed += OnTilePlowed;
            targetFarmTile.OnTilePlanted += OnTilePlanted;
            targetFarmTile.OnTileWatered += OnTileWatered;
            targetFarmTile.OnTileHarvested += OnTileHarvested;
            
            // Set tutorial speed
            targetFarmTile.tutorialGrowthTime = 60f;
        }

        // 3. Highlight target tile
        if (highlightEffect != null)
        {
            highlightEffect.position = targetFarmTile.transform.position + Vector3.up * 0.1f;
            highlightEffect.gameObject.SetActive(false); // Enable only when plowing starts
        }
        else if (targetFarmTile != null)
        {
            // Dynamically create a simple visual ring/highlight if none exists
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TutorialHighlightRing";
            ring.transform.position = targetFarmTile.transform.position + Vector3.up * 0.02f;
            ring.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);
            
            // Set glowing yellow material
            Renderer r = ring.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = new Color(1f, 0.92f, 0.016f, 0.5f);
                // Simple transparency setup
                r.material.SetFloat("_Mode", 3); // Transparent
                r.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                r.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                r.material.SetInt("_ZWrite", 0);
                r.material.DisableKeyword("_ALPHATEST_ON");
                r.material.EnableKeyword("_ALPHABLEND_ON");
                r.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                r.material.renderQueue = 3000;
            }
            
            // Remove collider so it doesn't block raycasts
            Destroy(ring.GetComponent<Collider>());
            
            highlightEffect = ring.transform;
            highlightEffect.gameObject.SetActive(false);
        }

        // Auto-discover HUD
        StartCoroutine(SetupHUDReferences());
    }

    void Update()
    {
        // Handle debug raycasting for PC/Mobile interaction in Tutorial
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleInteractionRaycast();
        }

        // Timeout hint system: if player is stuck >120s, show help
        CheckTimeoutHint();
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
            case TutorialStep.FollowNPC:
                hintTitle = "B\u1ea1n c\u1ea7n tr\u1ee3 gi\u00fap!";
                hintDesc = "\u0110i theo NPC t\u00edm! D\u00f9ng ph\u00edm W A S D \u0111\u1ec3 di chuy\u1ec3n";
                break;
            case TutorialStep.PlowTile:
                hintTitle = "Cu\u1ed1c \u0111\u1ea5t!";
                hintDesc = "Nh\u1ea5p chu\u1ed9t v\u00e0o \u00f4 \u0111\u1ea5t \u0111ang ph\u00e1t s\u00e1ng m\u00e0u v\u00e0ng!";
                break;
            case TutorialStep.OpenInventory:
                hintTitle = "M\u1edf t\u00fai \u0111\u1ed3!";
                hintDesc = "T\u00fai \u0111\u1ed3 \u0111ang m\u1edf, h\u00e3y nh\u1ea5p v\u00e0o \u2018H\u1ea1t c\u00e0 r\u1ed1t\u2019 r\u1ed3i b\u1ea5m \u2018Gieo h\u1ea1t\u2019!";
                break;
            case TutorialStep.PlantSeed:
                hintTitle = "Gieo h\u1ea1t!";
                hintDesc = "Nh\u1ea5p chu\u1ed9t v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 gieo h\u1ea1t c\u00e0 r\u1ed1t!";
                break;
            case TutorialStep.WaterTile:
                hintTitle = "T\u01b0\u1edbi n\u01b0\u1edbc!";
                hintDesc = "Nh\u1ea5p chu\u1ed9t v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 t\u01b0\u1edbi n\u01b0\u1edbc cho c\u00e2y!";
                break;
            case TutorialStep.InteractAnimalPen:
                hintTitle = "Mua V\u1eadt Nu\u00f4i!";
                hintDesc = "M\u1edf C\u1eeda H\u00e0ng v\u00e0 mua m\u1ed9t con g\u00e0 ho\u1eb7c heo!";
                break;
            case TutorialStep.FollowNPCToMarket:
                hintTitle = "B\u1ea1n C\u1ea7n Tr\u1ee3 Gi\u00fap!";
                hintDesc = "\u0110i theo NPC T\u00e2n Th\u1ee7 \u0111\u1ebfn Ch\u1ee3!";
                break;
            case TutorialStep.InteractMarket:
                hintTitle = "B\u00e1n C\u00e0 R\u1ed1t!";
                hintDesc = "M\u1edf C\u1eeda H\u00e0ng, ch\u1ecdn B\u00c1N v\u00e0 b\u00e1n C\u00e0 R\u1ed1t!";
                break;
            case TutorialStep.FollowNPCToResource:
                hintTitle = "Theo NPC T\u00e2n Th\u1ee7!";
                hintDesc = "\u0110i theo v\u00e0o khu v\u1ef1c r\u1eebng / m\u1ecf.";
                break;
            case TutorialStep.InteractResource:
                hintTitle = "Khai th\u00e1c!";
                hintDesc = "Nh\u1ea5n gi\u1eef chu\u1ed9t l\u00ean c\u00e2y ho\u1eb7c \u0111\u00e1 \u0111\u1ec3 khai th\u00e1c.";
                break;
            case TutorialStep.FollowNPCToBuild:
                hintTitle = "Theo NPC T\u00e2n Th\u1ee7!";
                hintDesc = "\u0110i v\u1ec1 b\u00e3i \u0111\u1ea5t tr\u1ed1ng \u0111\u1ec3 x\u00e2y d\u1ef1ng.";
                break;
            case TutorialStep.InteractBuild:
                hintTitle = "X\u00e2y d\u1ef1ng!";
                hintDesc = "M\u1edf ch\u1ebf \u0111\u1ed9 x\u00e2y (B), ch\u1ecdn 1 \u0111\u1ed3 v\u00e0 \u0111\u1eb7t xu\u1ed1ng.";
                break;
        }

        if (!string.IsNullOrEmpty(hintTitle))
        {
            ShowInstructionBanner(hintTitle, hintDesc);
            ShowSubtitle(hintDesc);
            Debug.Log($"[TutorialManager] Timeout hint shown for step: {currentStep}");
        }
    }

    private void SetStep(TutorialStep newStep)
    {
        currentStep = newStep;
        stepStartTime = Time.time;
        hasShownHint = false;
    }

    private IEnumerator SetupHUDReferences()
    {
        // Wait until GameHUD is loaded and registered in Scene
        yield return new WaitForSeconds(0.5f);

        GameObject hudGo = GameObject.Find("GameHUD") ?? GameObject.Find("HUD");
        if (hudGo != null)
        {
            hudDocument = hudGo.GetComponent<UIDocument>();
            if (hudDocument != null && hudDocument.rootVisualElement != null)
            {
                var root = hudDocument.rootVisualElement;
                questLabel = root.Q<Label>("QuestText");
                
                // Construct and inject the Dynamic Subtitle Panel in HUD root VisualElement
                CreateDynamicSubtitleUI(root);
            }
        }

        // Check current game state to start tutorial
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Gameplay)
        {
            StartTutorial();
        }

        // Find and connect to Inventory popup
        inventoryPopup = FindFirstObjectByType<InventoryPopupController>();
        if (inventoryPopup != null)
        {
            inventoryPopup.OnItemUsed += OnInventoryItemUsed;
            Debug.Log("[TutorialManager] Connected to InventoryPopupController.");
        }
    }

    /// <summary>
    /// Returns true if tutorial is still in progress (not complete).
    /// Used by FarmInteractionController to avoid conflict.
    /// </summary>
    public bool IsActive()
    {
        return currentStep != TutorialStep.WaitForStart && currentStep != TutorialStep.Complete;
    }

    public void StartTutorial()
    {
        SetStep(TutorialStep.FollowNPC);
        UpdateQuestHUD("[1/6] Đi theo NPC Tân Thủ tới mảnh đất hoang");
        Debug.Log("[TutorialManager] Onboarding Tutorial Started.");

        // Khởi động Trạm Hướng Dẫn số 1
        if (guideNPC != null)
        {
            guideNPC.StartNode(0);
        }

        // Force reset the tutorial tile so it doesn't get stuck if FarmManager loaded a saved state
        if (targetFarmTile != null)
        {
            targetFarmTile.currentState = FarmTile.TileState.Soil;
            targetFarmTile.plantedSeedId = "";
        }

        // Show big instruction banner for young players
        ShowInstructionBanner(
            "Đi theo NPC Hướng Dẫn!",
            "Dùng phím W A S D hoặc Joystick để di chuyển đến NPC màu tím"
        );

        // Create exclamation mark above NPC
        CreateNPCExclamationMark();
    }

    // ── Subtitle UI Dynamic Injection (Tangible Playground Standard) ──

    private void CreateDynamicSubtitleUI(VisualElement root)
    {
        // Container
        subtitleContainer = new VisualElement();
        subtitleContainer.style.position = Position.Absolute;
        subtitleContainer.style.bottom = 120;
        subtitleContainer.style.alignSelf = Align.Center;
        subtitleContainer.style.width = 500;
        subtitleContainer.style.backgroundColor = new Color(0.25f, 0.29f, 0.38f, 0.95f); // Dark blue-grey background
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
        
        // Hide by default
        subtitleContainer.style.display = DisplayStyle.None;

        // Speaker Name
        subtitleSpeaker = new Label("NPC Tân Thủ");
        subtitleSpeaker.style.fontSize = 13;
        subtitleSpeaker.style.unityFontStyleAndWeight = FontStyle.Bold;
        subtitleSpeaker.style.color = new Color(1f, 0.85f, 0.4f, 1f); // Accent Yellow
        subtitleSpeaker.style.marginBottom = 6;
        subtitleSpeaker.style.unityTextAlign = TextAnchor.MiddleCenter;

        // Dialogue Content
        subtitleLabel = new Label("Lời thoại của NPC...");
        subtitleLabel.style.fontSize = 15;
        subtitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        subtitleLabel.style.color = Color.white; 
        subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
        subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        // Assemble
        subtitleContainer.Add(subtitleSpeaker);
        subtitleContainer.Add(subtitleLabel);
        root.Add(subtitleContainer);

        // ── Instruction Banner (big top overlay) ──
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

        // ── Countdown Timer (big center overlay) ──
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

        countdownNumber = new Label("60");
        countdownNumber.style.fontSize = 48;
        countdownNumber.style.unityFontStyleAndWeight = FontStyle.Bold;
        countdownNumber.style.color = new Color(0.4f, 0.9f, 0.3f, 1f);
        countdownNumber.style.unityTextAlign = TextAnchor.MiddleCenter;

        countdownLabel = new Label();
        countdownLabel.text = "C\u00e2y \u0111ang l\u1edbn...";
        countdownLabel.style.fontSize = 12;
        countdownLabel.style.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        countdownLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        countdownLabel.style.marginTop = 4;

        countdownContainer.Add(countdownNumber);
        countdownContainer.Add(countdownLabel);
        root.Add(countdownContainer);
    }

    public void ShowSubtitle(string text)
    {
        if (subtitleContainer == null || subtitleLabel == null) return;

        subtitleLabel.text = text;
        subtitleContainer.style.display = DisplayStyle.Flex;

        // Auto hide subtitle after 4 seconds
        CancelInvoke(nameof(HideSubtitle));
        Invoke(nameof(HideSubtitle), 4.5f);
    }

    private void HideSubtitle()
    {
        if (subtitleContainer != null)
        {
            subtitleContainer.style.display = DisplayStyle.None;
        }
    }

    private void OnNPCArrivedAtAnimalPen()
    {
        if (currentStep == TutorialStep.FollowNPCToAnimalPen)
        {
            SetStep(TutorialStep.InteractAnimalPen);

            // Kiểm tra: Nếu người chơi đã nhanh tay mua thú trước khi NPC tới, qua màn luôn!
            var existingAnimals = FindObjectsByType<YWonderLand.Environment.FarmAnimal>(FindObjectsSortMode.None);
            if (existingAnimals != null && existingAnimals.Length > 0)
            {
                OnTutorialAnimalBought(existingAnimals[0].data.animalId);
                return;
            }

            UpdateQuestHUD("[7/x] Mở Shop và mua 1 con vật nuôi");
            
            ShowSubtitle("Cậu xem đây là khu vực chăn nuôi. Hiện tại đang trống, cậu hãy mở Shop lên và mua thử một chú lợn hoặc gà nhé!");
            ShowInstructionBanner(
                "Mua Vật Nuôi!",
                "Mở cửa hàng (Nút Giỏ Hàng) và mua một con vật bất kỳ"
            );

            // Subscribe event here to ensure AnimalManager is already instantiated
            if (YWonderLand.Managers.AnimalManager.Instance != null)
            {
                YWonderLand.Managers.AnimalManager.Instance.OnAnimalBought -= OnTutorialAnimalBought;
                YWonderLand.Managers.AnimalManager.Instance.OnAnimalBought += OnTutorialAnimalBought;
            }
        }
    }

    private void OnTutorialAnimalBought(string animalId)
    {
        if (currentStep == TutorialStep.InteractAnimalPen)
        {
            if (YWonderLand.Managers.AnimalManager.Instance != null)
            {
                YWonderLand.Managers.AnimalManager.Instance.OnAnimalBought -= OnTutorialAnimalBought;
            }

            if (guideNPC != null && guideNPC.tutorialNodes != null && guideNPC.tutorialNodes.Length > 1)
            {
                guideNPC.tutorialNodes[1].CompleteNodeTask();
                
                SetStep(TutorialStep.FollowNPCToMarket);
                UpdateQuestHUD("[8/9] Đi theo NPC Tân Thủ tới khu vực Chợ");
                ShowSubtitle("Tuyệt vời! Chú thú cưng mới của cậu trông đáng yêu quá. Bây giờ hãy theo tôi ra Chợ để bán Cà rốt lấy tiền nhé!");
                
                ShowInstructionBanner(
                    "Theo NPC Tân Thủ!",
                    "Hãy đi theo NPC tới khu vực Chợ"
                );

                if (guideNPC.tutorialNodes.Length > 2)
                {
                    guideNPC.StartNode(2);
                }
            }
        }
    }

    private void OnNPCArrivedAtMarket()
    {
        if (currentStep == TutorialStep.FollowNPCToMarket)
        {
            SetStep(TutorialStep.InteractMarket);

            // Kiểm tra: Nếu người chơi đã lỡ bán mất củ cà rốt trên đường đi
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null && inv.GetItemQuantity("carrot_01") <= 0)
            {
                OnTutorialItemSold("carrot_01", 1);
                return;
            }

            UpdateQuestHUD("[9/9] Mở Shop, chọn tab BÁN và bán Cà rốt");
            
            ShowSubtitle("Cậu hãy nhấn vào Nút Giỏ Hàng, chuyển sang mục BÁN, và bán củ Cà rốt để nhận POS nhé!");
            ShowInstructionBanner(
                "Bán Nông Sản!",
                "Mở Shop, chuyển sang tab BÁN và bán Cà rốt"
            );

            // Subscribe to Shop event
            ShopPopupController.OnItemSold -= OnTutorialItemSold;
            ShopPopupController.OnItemSold += OnTutorialItemSold;
        }
    }

    private void OnTutorialItemSold(string itemId, int quantity)
    {
        if (currentStep == TutorialStep.InteractMarket)
        {
            // Accept any item sold during tutorial, or specifically carrot
            if (itemId.Contains("carrot") || itemId.Contains("c\u00e0 r\u1ed1t") || itemId.Contains("seed"))
            {
                if (guideNPC != null && guideNPC.tutorialNodes != null && guideNPC.tutorialNodes.Length > 2)
                {
                    guideNPC.tutorialNodes[2].CompleteNodeTask();
                }
                
                ShopPopupController.OnItemSold -= OnTutorialItemSold;
                
                SetStep(TutorialStep.FollowNPCToResource);
                UpdateQuestHUD("[10/12] Đi theo NPC Tân Thủ tới khu Khai thác");
                ShowSubtitle("Bán được tiền rồi! Giờ cậu đi theo tôi để học cách thu thập nguyên liệu nhé!");
                
                ShowInstructionBanner(
                    "Theo NPC Tân Thủ!",
                    "Di chuyển tới khu Khai thác"
                );

                if (guideNPC.tutorialNodes.Length > 3)
                {
                    guideNPC.StartNode(3);
                }
            }
        }
    }

    private void OnNPCArrivedAtResource()
    {
        if (currentStep == TutorialStep.FollowNPCToResource)
        {
            SetStep(TutorialStep.InteractResource);
            hasHarvestedWood = false;
            hasHarvestedStone = false;

            // Kiểm tra: Nếu người chơi đã có sẵn Gỗ và Đá trong túi
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            if (inv != null)
            {
                if (inv.GetItemQuantity("wood_01") > 0) hasHarvestedWood = true;
                if (inv.GetItemQuantity("stone_01") > 0 || inv.GetItemQuantity("ore_01") > 0) hasHarvestedStone = true;
                
                if (hasHarvestedWood && hasHarvestedStone)
                {
                    OnTutorialResourceHarvested("wood_01", 1);
                    return;
                }
            }

            UpdateQuestHUD("[11/12] Đập 1 tảng đá và chặt 1 cái cây");
            
            ShowSubtitle("Cậu hãy nhấn giữ chuột vào cây xanh và tảng đá để thu hoạch Gỗ và Đá nhé!");
            ShowInstructionBanner(
                "Khai Thác!",
                "Thu thập ít nhất 1 Gỗ và 1 Đá"
            );

            // Subscribe to harvest event
            YWonderLand.Environment.HarvestableResource.OnResourceHarvested -= OnTutorialResourceHarvested;
            YWonderLand.Environment.HarvestableResource.OnResourceHarvested += OnTutorialResourceHarvested;
        }
    }

    private void OnTutorialResourceHarvested(string yieldId, int qty)
    {
        if (currentStep == TutorialStep.InteractResource)
        {
            if (yieldId.Contains("wood") || yieldId.Contains("g\u1ed7")) hasHarvestedWood = true;
            if (yieldId.Contains("stone") || yieldId.Contains("ore") || yieldId.Contains("\u0111\u00e1")) hasHarvestedStone = true;

            if (hasHarvestedWood && hasHarvestedStone)
            {
                YWonderLand.Environment.HarvestableResource.OnResourceHarvested -= OnTutorialResourceHarvested;
                
                if (guideNPC != null && guideNPC.tutorialNodes.Length > 3)
                {
                    guideNPC.tutorialNodes[3].CompleteNodeTask();
                }
                
                SetStep(TutorialStep.FollowNPCToBuild);
                UpdateQuestHUD("[11.5/12] Theo NPC Tân Thủ tới bãi đất trống");
                ShowSubtitle("Đủ nguyên liệu rồi! Ta kiếm chỗ trống để xây thử gì đó nào!");
                
                if (guideNPC.tutorialNodes.Length > 4)
                {
                    guideNPC.StartNode(4);
                }
            }
        }
    }

    private void OnNPCArrivedAtBuild()
    {
        if (currentStep == TutorialStep.FollowNPCToBuild)
        {
            SetStep(TutorialStep.InteractBuild);
            UpdateQuestHUD("[12/12] Mở Xây dựng (B), chọn đường/hàng rào và xây");
            
            ShowSubtitle("Mở chế độ Xây dựng, chọn 1 công trình, xoay nếu thích, rồi nhấn Xác nhận để xây nhé!");
            ShowInstructionBanner(
                "Xây Dựng!",
                "Xây công trình đầu tiên của bạn"
            );

            // Subscribe to build event
            GhostPlacementController.OnBuildingPlaced -= OnTutorialBuildingPlaced;
            GhostPlacementController.OnBuildingPlaced += OnTutorialBuildingPlaced;
        }
    }

    private void OnTutorialBuildingPlaced(int price)
    {
        if (currentStep == TutorialStep.InteractBuild)
        {
            if (guideNPC != null && guideNPC.tutorialNodes.Length > 4)
            {
                guideNPC.tutorialNodes[4].CompleteNodeTask();
            }
            
            GhostPlacementController.OnBuildingPlaced -= OnTutorialBuildingPlaced;
            
            currentStep = TutorialStep.Complete;
            UpdateQuestHUD("Hoàn thành Hướng Dẫn Tân Thủ!");
            ShowSubtitle("Tuyệt vời! Cậu đã nắm vững cách Trồng trọt, Chăn nuôi, Kiếm tiền và Xây dựng. Chúc cậu chơi game vui vẻ nhé!");
            
            ShowInstructionBanner(
                "Hoàn Thành!",
                "Bạn đã tốt nghiệp khóa Hướng Dẫn Tân Thủ!"
            );

            CancelInvoke(nameof(HideSubtitle));
            Invoke(nameof(HideSubtitle), 5f);
        }
    }

    // ── State Handlers & Callbacks ──

    private void OnNPCArrivedAtFarm()
    {
        if (currentStep == TutorialStep.FollowNPC)
        {
            SetStep(TutorialStep.PlowTile);
            UpdateQuestHUD("[2/6] Sử dụng Cuốc nhấp vào ô đất phát sáng");
            
            // Highlight the farm tile
            if (highlightEffect != null) highlightEffect.gameObject.SetActive(true);
            
            ShowInstructionBanner(
                "Cuốc đất!",
                "Nhấp chuột vào ô đất màu vàng đang phát sáng"
            );
        }
    }

    private void OnTilePlowed(FarmTile tile)
    {
        if (currentStep == TutorialStep.PlowTile)
        {
            SetStep(TutorialStep.OpenInventory);
            UpdateQuestHUD("[3/6] Mở túi đồ -> chọn Hạt cà rốt -> bấm Gieo hạt");
            
            ShowSubtitle("Tuyệt vời! Bây giờ hãy mở túi đồ, chọn hạt cà rốt và bấm 'Gieo hạt' nhé!");
            ShowInstructionBanner(
                "Mở túi đồ!",
                "Chọn 'Hạt cà rốt' rồi bấm nút 'Gieo hạt'"
            );

            // Cập nhật lời thoại cho anh Lâm để không bị lải nhải câu cũ
            if (guideNPC != null && guideNPC.tutorialNodes != null && guideNPC.tutorialNodes.Length > 0)
            {
                var node = guideNPC.tutorialNodes[0];
                node.actionDialogues = new string[] { 
                    "Tuyệt vời! Mở túi đồ ra và chọn hạt Cà rốt để gieo nhé!",
                    "Cậu làm tốt lắm, giờ hãy gieo hạt xuống đất đi."
                };
                node.idleWarningDialogues = new string[] {
                    "Cậu không biết gieo hạt hả? Mở túi đồ (phím B) lên nhé!",
                    "Đứng nhìn thì hạt không tự nhảy xuống đất đâu, mở túi đồ ra nào!"
                };
            }

            // Auto-open inventory at Seeds tab after 2 seconds
            StartCoroutine(AutoOpenInventorySeeds());
        }
    }

    private IEnumerator AutoOpenInventorySeeds()
    {
        yield return new WaitForSeconds(2f);

        if (currentStep != TutorialStep.OpenInventory) yield break;

        if (inventoryPopup == null)
        {
            inventoryPopup = FindFirstObjectByType<InventoryPopupController>();
            if (inventoryPopup != null)
            {
                inventoryPopup.OnItemUsed += OnInventoryItemUsed;
            }
        }

        if (inventoryPopup != null)
        {
            inventoryPopup.ShowAtTab("seeds");
            Debug.Log("[TutorialManager] Auto-opened Inventory at Seeds tab.");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] InventoryPopupController not found! Skipping to PlantSeed.");
            SetStep(TutorialStep.PlantSeed);
            UpdateQuestHUD("[3/6] Nhấp vào ô đất để gieo hạt Cà Rốt");
            ShowSubtitle("Hãy nhấp vào ô đất để gieo hạt!");
        }
    }

    private void OnInventoryItemUsed(string itemIdOrName)
    {
        // Only react during OpenInventory step
        if (currentStep != TutorialStep.OpenInventory) return;

        // OnItemUsed sends item ID (e.g. "carrot_seed_01") or Vietnamese name
        // Accept any seed item during tutorial
        if (itemIdOrName.Contains("carrot") || itemIdOrName.Contains("seed") ||
            itemIdOrName.Contains("c\u00e0 r\u1ed1t") || itemIdOrName.Contains("C\u00e0 R\u1ed1t") ||
            itemIdOrName.Contains("h\u1ea1t"))
        {
            Debug.Log($"[TutorialManager] Seed selected: {itemIdOrName}");

            // Close inventory
            if (inventoryPopup != null)
            {
                inventoryPopup.Hide();
            }

            // Move to PlantSeed step (which instantly triggers OnTilePlanted if FarmInteractionController plants it, or waits for manual plant)
            SetStep(TutorialStep.PlantSeed);
            UpdateQuestHUD("[3.5/6] Nhấp vào ô đất để gieo hạt Cà Rốt");
            ShowSubtitle("Đã chọn hạt cà rốt! Giờ hãy nhấp vào ô đất để gieo hạt nhé.");
            ShowInstructionBanner(
                "Gieo hạt!",
                "Nhấp chuột vào ô đất để gieo hạt cà rốt"
            );
        }
    }

    private void OnTilePlanted(FarmTile tile)
    {
        if (currentStep == TutorialStep.PlantSeed || currentStep == TutorialStep.OpenInventory)
        {
            SetStep(TutorialStep.WaterTile);
            UpdateQuestHUD("[4/6] Nhấp vào ô đất để tưới nước");
            
            ShowSubtitle("Hạt giống đã được gieo! Hãy nhấp vào ô đất một lần nữa để tưới nước cho cây mau lớn.");
            ShowInstructionBanner(
                "Tưới nước!",
                "Nhấp chuột vào ô đất để tưới nước"
            );

            // Cập nhật lời thoại cho anh Lâm để không bị lải nhải câu cũ
            if (guideNPC != null && guideNPC.tutorialNodes != null && guideNPC.tutorialNodes.Length > 0)
            {
                var node = guideNPC.tutorialNodes[0];
                node.actionDialogues = new string[] { 
                    "Hạt đã nằm ngoan dưới đất rồi, giờ cậu tưới nước đi!",
                    "Cậu nhấp vào ô đất một lần nữa để tưới nước nhé."
                };
                node.idleWarningDialogues = new string[] {
                    "Cây không có nước thì sao lớn được? Tưới nước đi cậu!",
                    "Trời nắng chang chang mà không tưới nước cho cây à?"
                };
            }
        }
    }

    private void OnTileWatered(FarmTile tile)
    {
        if (currentStep == TutorialStep.WaterTile)
        {
            SetStep(TutorialStep.WaitHarvest);
            
            // Hide highlight effect
            if (highlightEffect != null) highlightEffect.gameObject.SetActive(false);
            
            // Start countdown (Reduced from 60 to 5 seconds for tutorial speed)
            harvestCountdown = 5f;
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(HarvestCountdownSequence());

            UpdateQuestHUD("[5/6] Chờ cây lớn...");
            ShowSubtitle("Nước đã được tưới! Cà rốt sẽ lớn rất nhanh. Hãy đợi một chút nhé!");

            // Show big instruction about waiting
            ShowInstructionBanner(
                "Đợi cây lớn!",
                "Cà rốt đang phát triển. Hãy đợi 5 giây để thu hoạch!"
            );

            // Cập nhật lời thoại cho anh Lâm
            if (guideNPC != null && guideNPC.tutorialNodes != null && guideNPC.tutorialNodes.Length > 0)
            {
                var node = guideNPC.tutorialNodes[0];
                node.actionDialogues = new string[] { 
                    "Cây đang lớn kìa, cậu chờ xíu để thu hoạch nhé!",
                    "Chờ 5 giây thôi là có cà rốt ăn rồi."
                };
                node.idleWarningDialogues = new string[] {
                    "Chờ xíu đi cậu, nôn nóng thì cây cũng không lớn nhanh hơn đâu!",
                    "Ngắm cảnh chút đi, cây sắp lớn rồi."
                };
            }

            // Show big countdown timer
            ShowCountdownTimer();
        }
    }

    private IEnumerator HarvestCountdownSequence()
    {
        while (harvestCountdown > 0f)
        {
            UpdateQuestHUD($"[5/6] Chờ Cà Rốt chín và thu hoạch (còn {Mathf.CeilToInt(harvestCountdown)}s)");

            // Update big countdown number
            if (countdownNumber != null)
            {
                countdownNumber.text = Mathf.CeilToInt(harvestCountdown).ToString();

                // Color changes: green > yellow > red
                if (harvestCountdown > 3f)
                    countdownNumber.style.color = new Color(0.4f, 0.9f, 0.3f, 1f);
                else if (harvestCountdown > 1.5f)
                    countdownNumber.style.color = new Color(1f, 0.85f, 0.2f, 1f);
                else
                    countdownNumber.style.color = new Color(1f, 0.3f, 0.2f, 1f);
            }

            yield return new WaitForSeconds(1f);
            harvestCountdown -= 1f;
        }

        // Time's up, make tile ripe
        HideCountdownTimer();
        UpdateQuestHUD("[5.5/6] Nhấp vào ô đất để thu hoạch Cà Rốt!");
        ShowSubtitle("Cà rốt đã chín vàng ruộm rồi! Hãy nhấp vào để thu hoạch nông sản đầu tay của bạn!");
        ShowInstructionBanner(
            "Cà rốt đã chín!",
            "Nhấp chuột vào ô đất để thu hoạch"
        );

        // Cập nhật lời thoại cho anh Lâm lần cuối
        if (guideNPC != null && guideNPC.tutorialNodes != null && guideNPC.tutorialNodes.Length > 0)
        {
            var node = guideNPC.tutorialNodes[0];
            node.actionDialogues = new string[] { 
                "Chín rồi kìa, mau bấm vào thu hoạch đi cậu!",
                "Tuyệt vời, nhấp vào củ cà rốt to bự kia để thu hoạch nào!"
            };
            node.idleWarningDialogues = new string[] {
                "Cây chín rục rồi kìa cậu không thu hoạch à?",
                "Còn chờ gì nữa, nhấp vào ô đất để nhổ cà rốt lên đi!"
            };
        }
    }

    private void OnTileHarvested(FarmTile tile)
    {
        if (currentStep == TutorialStep.WaitHarvest || currentStep == TutorialStep.PlantSeed) // Fallback support
        {
            currentStep = TutorialStep.FollowNPCToAnimalPen;
            UpdateQuestHUD("[6/7] Nhiệm vụ: Đi theo NPC Tân Thủ sang Chuồng Thú");
            
            // Tell the node that task is complete so GuideNPC can proceed
            if (guideNPC != null && guideNPC.tutorialNodes != null && guideNPC.tutorialNodes.Length > 0)
            {
                guideNPC.tutorialNodes[0].CompleteNodeTask();
            }

            ShowSubtitle("Thật xuất sắc! Cậu đã thu hoạch thành công củ cà rốt đầu tiên!");
            
            // Big banner for completing Farm part
            ShowInstructionBanner(
                "Đã Thu Hoạch!",
                "Bạn đã học được cách làm nông trại. Tiếp tục đi theo NPC nhé!"
            );

            // Hide after a few seconds
            CancelInvoke(nameof(HideSubtitle));
            Invoke(nameof(HideSubtitle), 4f);
        }
    }

    private void GiveTutorialRewards()
    {
        Debug.Log("[TutorialManager] Giving Rewards: +50 POS, +20 EXP + starter seeds.");
        
        // Add POS via EconomyManager (not SET on HUD)
        if (YWonderLand.Managers.EconomyManager.Instance != null)
        {
            YWonderLand.Managers.EconomyManager.Instance.AddPOS(50);
            Debug.Log("[TutorialManager] +50 POS added via EconomyManager.");
        }

        // Update EXP on HUD
        GameHUDController hudController = FindFirstObjectByType<GameHUDController>();
        if (hudController != null)
        {
            hudController.SetPlayerEXP(20f);
        }

        // Give free starter seeds (5 of each type)
        if (YWonderLand.Managers.InventoryManager.Instance != null)
        {
            var inv = YWonderLand.Managers.InventoryManager.Instance;
            inv.AddItem("carrot_seed_01", 5);
            inv.AddItem("cabbage_seed_01", 5);
            inv.AddItem("corn_seed_01", 5);
            inv.AddItem("watermelon_seed_01", 3);
            inv.AddItem("pumpkin_seed_01", 3);
            inv.AddItem("morning_glory_seed_01", 5);
            inv.AddItem("sweet_potato_seed_01", 3);
            inv.AddItem("grass_seed_01", 5);
            // Also give some basic resources to get started
            inv.AddItem("wood_01", 10);
            inv.AddItem("stone_01", 5);
            Debug.Log("[TutorialManager] Starter seeds and resources added to inventory.");
        }
    }

    private void UpdateQuestHUD(string questText)
    {
        if (questLabel != null)
        {
            questLabel.text = questText;
        }
        Debug.Log($"[Quest HUD Update] {questText}");
    }

    // ── Instruction Banner Helpers ──

    private void ShowInstructionBanner(string title, string hint)
    {
        if (instructionBanner == null) return;

        if (instructionText != null) instructionText.text = title;
        if (instructionHint != null) instructionHint.text = hint;
        instructionBanner.style.display = DisplayStyle.Flex;

        // Auto hide after 6 seconds
        CancelInvoke(nameof(HideInstructionBanner));
        Invoke(nameof(HideInstructionBanner), 6f);
    }

    private void HideInstructionBanner()
    {
        if (instructionBanner != null)
        {
            instructionBanner.style.display = DisplayStyle.None;
        }
    }

    // ── Countdown Timer Helpers ──

    private void ShowCountdownTimer()
    {
        if (countdownContainer != null)
        {
            countdownContainer.style.display = DisplayStyle.Flex;
        }
    }

    private void HideCountdownTimer()
    {
        if (countdownContainer != null)
        {
            countdownContainer.style.display = DisplayStyle.None;
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

        // Remove collider
        Destroy(exclamationMark.GetComponent<Collider>());

        // Bright yellow material
        Renderer r = exclamationMark.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = new Color(1f, 0.85f, 0f, 1f);
            r.material.SetColor("_EmissionColor", new Color(1f, 0.85f, 0f, 0.5f));
            r.material.EnableKeyword("_EMISSION");
        }

        // Add small dot below
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

        // Animate bobbing
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

    // ── Interaction Raycasting (Mobile/PC) ──

    private void HandleInteractionRaycast()
    {
        if (Camera.main == null || Mouse.current == null) return;

        // 1. Create ray from center of screen (or click point)
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 10f))
        {
            FarmTile tile = hit.collider.GetComponentInParent<FarmTile>() ?? hit.collider.GetComponent<FarmTile>();
            if (tile != null && tile == targetFarmTile)
            {
                ProcessTileInteraction(tile);
            }
        }
    }

    private void ProcessTileInteraction(FarmTile tile)
    {
        switch (currentStep)
        {
            case TutorialStep.PlowTile:
                if (tile.InteractPlow())
                {
                    Debug.Log("[Tutorial] Tile Plowed successfully.");
                }
                break;

            case TutorialStep.PlantSeed:
                if (tile.InteractPlant("carrot_seed_01"))
                {
                    Debug.Log("[Tutorial] Seed planted successfully.");
                }
                break;

            case TutorialStep.WaterTile:
                if (tile.InteractWater())
                {
                    Debug.Log("[Tutorial] Tile watered successfully. Growth started.");
                }
                break;

            case TutorialStep.WaitHarvest:
                if (tile.currentState == FarmTile.TileState.Ripe)
                {
                    string item;
                    int amount;
                    if (tile.InteractHarvest(out item, out amount))
                    {
                        Debug.Log($"[Tutorial] Harvested: {amount}x '{item}'!");
                    }
                }
                break;
        }
    }
}
