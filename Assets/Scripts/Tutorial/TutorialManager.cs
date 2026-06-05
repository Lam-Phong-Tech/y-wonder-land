using UnityEngine;
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
        PlantSeed,      // 3: Plant carrot seed
        WaterTile,      // 4: Water the planted seed
        WaitHarvest,    // 5: Wait for carrot to ripen (60s countdown)
        Complete        // 6: Tutorial complete, give rewards
    }

    [Header("Current Progress")]
    public TutorialStep currentStep = TutorialStep.WaitForStart;

    [Header("References")]
    public GuideNPC guideNPC;
    public FarmTile targetFarmTile;
    public Transform highlightEffect; // Glowing effect on the farm tile

    private UIDocument hudDocument;
    private Label questLabel;           // Reference to HUD's quest text

    // Subtitle UI elements created dynamically via code
    private VisualElement subtitleContainer;
    private Label subtitleLabel;
    private Label subtitleSpeaker;

    private float harvestCountdown = 60f;
    private Coroutine countdownCoroutine;

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
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                spawnPos = player.transform.position + player.transform.forward * 2f + player.transform.right * 1.5f;
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
            guideNPC.OnDestinationReached += OnNPCArrivedAtFarm;
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
                r.material = new Material(Shader.Find("Standard"));
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
        if (Input.GetMouseButtonDown(0))
        {
            HandleInteractionRaycast();
        }
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
    }

    public void StartTutorial()
    {
        currentStep = TutorialStep.FollowNPC;
        UpdateQuestHUD("Đi theo Lâm Hướng Dẫn tới mảnh đất hoang");
        Debug.Log("[TutorialManager] Onboarding Tutorial Started.");
    }

    // ── Subtitle UI Dynamic Injection (Tangible Playground Standard) ──

    private void CreateDynamicSubtitleUI(VisualElement root)
    {
        // Container
        subtitleContainer = new VisualElement();
        subtitleContainer.style.position = Position.Absolute;
        subtitleContainer.style.bottom = 40;
        subtitleContainer.style.alignSelf = Align.Center;
        subtitleContainer.style.width = 440;
        subtitleContainer.style.backgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f); // #EFEFEF
        subtitleContainer.style.borderTopWidth = 3f;
        subtitleContainer.style.borderBottomWidth = 3f;
        subtitleContainer.style.borderLeftWidth = 3f;
        subtitleContainer.style.borderRightWidth = 3f;

        Color subtitleBorderColor = new Color(0.24f, 0.21f, 0.21f, 1f);
        subtitleContainer.style.borderTopColor = subtitleBorderColor;
        subtitleContainer.style.borderBottomColor = subtitleBorderColor;
        subtitleContainer.style.borderLeftColor = subtitleBorderColor;
        subtitleContainer.style.borderRightColor = subtitleBorderColor;

        subtitleContainer.style.borderTopLeftRadius = 16f;
        subtitleContainer.style.borderTopRightRadius = 16f;
        subtitleContainer.style.borderBottomLeftRadius = 16f;
        subtitleContainer.style.borderBottomRightRadius = 16f;
        subtitleContainer.style.paddingLeft = 16;
        subtitleContainer.style.paddingRight = 16;
        subtitleContainer.style.paddingTop = 12;
        subtitleContainer.style.paddingBottom = 12;
        
        // Solid black retro shadow
        subtitleContainer.style.marginRight = 6;
        subtitleContainer.style.marginBottom = 6;
        
        // Hide by default
        subtitleContainer.style.display = DisplayStyle.None;

        // Speaker Name
        subtitleSpeaker = new Label("Lâm Hướng Dẫn");
        subtitleSpeaker.style.fontSize = 11;
        subtitleSpeaker.style.unityFontStyleAndWeight = FontStyle.Bold;
        subtitleSpeaker.style.color = new Color(0.36f, 0.26f, 0.95f, 1f); // #5B42F3 (Hero color)
        subtitleSpeaker.style.marginBottom = 4;
        subtitleSpeaker.style.paddingLeft = 0;
        subtitleSpeaker.style.paddingRight = 0;

        // Dialogue Content
        subtitleLabel = new Label("Lời thoại của NPC...");
        subtitleLabel.style.fontSize = 13;
        subtitleLabel.style.color = new Color(0.24f, 0.21f, 0.21f, 1f); // #3D3535
        subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
        subtitleLabel.style.paddingLeft = 0;
        subtitleLabel.style.paddingRight = 0;

        // Assemble
        subtitleContainer.Add(subtitleSpeaker);
        subtitleContainer.Add(subtitleLabel);
        root.Add(subtitleContainer);
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

    // ── State Handlers & Callbacks ──

    private void OnNPCArrivedAtFarm()
    {
        if (currentStep == TutorialStep.FollowNPC)
        {
            currentStep = TutorialStep.PlowTile;
            UpdateQuestHUD("Sử dụng Cuốc nhấp vào ô đất phát sáng");
            
            // Highlight the farm tile
            if (highlightEffect != null) highlightEffect.gameObject.SetActive(true);
            
            ShowSubtitle("Chúng ta đã đến nơi! Hãy dùng Cuốc nhấp vào mảnh đất phát sáng này để xới tơi đất lên nhé.");
        }
    }

    private void OnTilePlowed(FarmTile tile)
    {
        if (currentStep == TutorialStep.PlowTile)
        {
            currentStep = TutorialStep.PlantSeed;
            UpdateQuestHUD("Nhấp vào ô đất để gieo hạt Cà Rốt");
            
            ShowSubtitle("Tuyệt vời! Bây giờ hãy gieo hạt giống Cà Rốt lên ô đất vừa cuốc đi nào.");
        }
    }

    private void OnTilePlanted(FarmTile tile)
    {
        if (currentStep == TutorialStep.PlantSeed)
        {
            currentStep = TutorialStep.WaterTile;
            UpdateQuestHUD("Nhấp vào ô đất để tưới nước");
            
            ShowSubtitle("Hạt giống đã được gieo! Hãy nhấp vào ô đất một lần nữa để tưới nước cho cây mau lớn.");
        }
    }

    private void OnTileWatered(FarmTile tile)
    {
        if (currentStep == TutorialStep.WaterTile)
        {
            currentStep = TutorialStep.WaitHarvest;
            
            // Hide highlight effect
            if (highlightEffect != null) highlightEffect.gameObject.SetActive(false);
            
            // Start countdown
            harvestCountdown = 60f;
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(HarvestCountdownSequence());

            ShowSubtitle("Nước đã được tưới! Bình thường cà rốt cần 24 giờ để chín, nhưng hôm nay tôi sẽ dùng phân bón thần kỳ để nó chín sau 60 giây!");
        }
    }

    private IEnumerator HarvestCountdownSequence()
    {
        while (harvestCountdown > 0f)
        {
            UpdateQuestHUD($"Chờ Cà Rốt chín và thu hoạch (còn {Mathf.CeilToInt(harvestCountdown)}s)");
            yield return new WaitForSeconds(1f);
            harvestCountdown -= 1f;
        }

        // Time's up, make tile ripe
        UpdateQuestHUD("Nhấp vào ô đất để thu hoạch Cà Rốt!");
        ShowSubtitle("Cà rốt đã chín vàng ruộm rồi! Hãy nhấp vào để thu hoạch nông sản đầu tay của bạn!");
    }

    private void OnTileHarvested(FarmTile tile)
    {
        if (currentStep == TutorialStep.WaitHarvest || currentStep == TutorialStep.PlantSeed) // Fallback support
        {
            currentStep = TutorialStep.Complete;
            UpdateQuestHUD("Nhiệm vụ: Chúc mừng bạn đã hoàn thành hướng dẫn!");
            
            // Give Rewards! (+50 POS, +20 EXP mockup on HUD)
            GiveTutorialRewards();

            ShowSubtitle("Thật xuất sắc! Bạn đã biết cách làm nông trại rồi. Chúc bạn có những giây phút vui vẻ tại Y WONDER LAND!");
            
            // Auto hide HUD subtitle permanently after 6 seconds
            CancelInvoke(nameof(HideSubtitle));
            Invoke(nameof(HideSubtitle), 6f);
        }
    }

    private void GiveTutorialRewards()
    {
        Debug.Log("[TutorialManager] Giving Rewards: +50 POS, +20 EXP.");
        
        // Find GameHUDController to update Currency/EXP UI dynamically
        GameHUDController hudController = FindFirstObjectByType<GameHUDController>();
        if (hudController != null)
        {
            // Set mock values or call public update APIs
            hudController.SetCurrency(50); // Set POS Currency
            hudController.SetPlayerEXP(20f); // Set Player EXP progress
            Debug.Log("[TutorialManager] Successfully updated HUD currency and EXP fields.");
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

    // ── Interaction Raycasting (Mobile/PC) ──

    private void HandleInteractionRaycast()
    {
        // 1. Create ray from center of screen (or click point)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
                if (tile.InteractPlant("carrot"))
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
