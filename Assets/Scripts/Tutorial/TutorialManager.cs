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
        Complete        // 7: Tutorial complete, give rewards
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

    public void StartTutorial()
    {
        SetStep(TutorialStep.FollowNPC);
        UpdateQuestHUD("\u0110i theo L\u00e2m H\u01b0\u1edbng D\u1eabn t\u1edbi m\u1ea3nh \u0111\u1ea5t hoang");
        Debug.Log("[TutorialManager] Onboarding Tutorial Started.");

        // Show big instruction banner for young players
        ShowInstructionBanner(
            "\u0110i theo NPC H\u01b0\u1edbng D\u1eabn!",
            "D\u00f9ng ph\u00edm W A S D ho\u1eb7c Joystick \u0111\u1ec3 di chuy\u1ec3n \u0111\u1ebfn NPC m\u00e0u t\u00edm"
        );

        // NPC greets immediately
        ShowSubtitle("Ch\u00e0o m\u1eebng b\u1ea1n \u0111\u1ebfn \u0111\u1ea3o hoang! H\u00e3y \u0111i theo t\u00f4i nh\u00e9!");

        // Create exclamation mark above NPC
        CreateNPCExclamationMark();
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

    // ── State Handlers & Callbacks ──

    private void OnNPCArrivedAtFarm()
    {
        if (currentStep == TutorialStep.FollowNPC)
        {
            SetStep(TutorialStep.PlowTile);
            UpdateQuestHUD("S\u1eed d\u1ee5ng Cu\u1ed1c nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t ph\u00e1t s\u00e1ng");
            
            // Highlight the farm tile
            if (highlightEffect != null) highlightEffect.gameObject.SetActive(true);
            
            ShowSubtitle("Ch\u00fang ta \u0111\u00e3 \u0111\u1ebfn n\u01a1i! H\u00e3y d\u00f9ng Cu\u1ed1c nh\u1ea5p v\u00e0o m\u1ea3nh \u0111\u1ea5t ph\u00e1t s\u00e1ng n\u00e0y \u0111\u1ec3 x\u1edbi t\u01a1i \u0111\u1ea5t l\u00ean nh\u00e9.");
            ShowInstructionBanner(
                "Cu\u1ed1c \u0111\u1ea5t!",
                "Nh\u1ea5p chu\u1ed9t v\u00e0o \u00f4 \u0111\u1ea5t m\u00e0u v\u00e0ng \u0111ang ph\u00e1t s\u00e1ng"
            );
        }
    }

    private void OnTilePlowed(FarmTile tile)
    {
        if (currentStep == TutorialStep.PlowTile)
        {
            SetStep(TutorialStep.OpenInventory);
            UpdateQuestHUD("M\u1edf t\u00fai \u0111\u1ed3 \u2192 ch\u1ecdn H\u1ea1t c\u00e0 r\u1ed1t \u2192 b\u1ea5m Gieo h\u1ea1t");
            
            ShowSubtitle("Tuy\u1ec7t v\u1eddi! B\u00e2y gi\u1edd h\u00e3y m\u1edf t\u00fai \u0111\u1ed3, ch\u1ecdn h\u1ea1t c\u00e0 r\u1ed1t v\u00e0 b\u1ea5m 'Gieo h\u1ea1t' nh\u00e9!");
            ShowInstructionBanner(
                "M\u1edf t\u00fai \u0111\u1ed3!",
                "Ch\u1ecdn 'H\u1ea1t c\u00e0 r\u1ed1t' r\u1ed3i b\u1ea5m n\u00fat 'Gieo h\u1ea1t'"
            );

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
            UpdateQuestHUD("Nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 gieo h\u1ea1t C\u00e0 R\u1ed1t");
            ShowSubtitle("H\u00e3y nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 gieo h\u1ea1t!");
        }
    }

    private void OnInventoryItemUsed(string itemName)
    {
        // Only react during OpenInventory step
        if (currentStep != TutorialStep.OpenInventory) return;

        if (itemName.Contains("c\u00e0 r\u1ed1t") || itemName.Contains("C\u00e0 R\u1ed1t") || itemName.Contains("c\u00e0 r\u1ed1t"))
        {
            Debug.Log($"[TutorialManager] Carrot seed selected: {itemName}");

            // Close inventory
            if (inventoryPopup != null)
            {
                inventoryPopup.Hide();
            }

            // Move to PlantSeed step
            SetStep(TutorialStep.PlantSeed);
            UpdateQuestHUD("Nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 gieo h\u1ea1t C\u00e0 R\u1ed1t");
            ShowSubtitle("\u0110\u00e3 ch\u1ecdn h\u1ea1t c\u00e0 r\u1ed1t! Gi\u1edd h\u00e3y nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 gieo h\u1ea1t nh\u00e9.");
            ShowInstructionBanner(
                "Gieo h\u1ea1t!",
                "Nh\u1ea5p chu\u1ed9t v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 gieo h\u1ea1t c\u00e0 r\u1ed1t"
            );
        }
    }

    private void OnTilePlanted(FarmTile tile)
    {
        if (currentStep == TutorialStep.PlantSeed)
        {
            SetStep(TutorialStep.WaterTile);
            UpdateQuestHUD("Nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 t\u01b0\u1edbi n\u01b0\u1edbc");
            
            ShowSubtitle("H\u1ea1t gi\u1ed1ng \u0111\u00e3 \u0111\u01b0\u1ee3c gieo! H\u00e3y nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t m\u1ed9t l\u1ea7n n\u1eefa \u0111\u1ec3 t\u01b0\u1edbi n\u01b0\u1edbc cho c\u00e2y mau l\u1edbn.");
            ShowInstructionBanner(
                "T\u01b0\u1edbi n\u01b0\u1edbc!",
                "Nh\u1ea5p chu\u1ed9t v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 t\u01b0\u1edbi n\u01b0\u1edbc"
            );
        }
    }

    private void OnTileWatered(FarmTile tile)
    {
        if (currentStep == TutorialStep.WaterTile)
        {
            SetStep(TutorialStep.WaitHarvest);
            
            // Hide highlight effect
            if (highlightEffect != null) highlightEffect.gameObject.SetActive(false);
            
            // Start countdown
            harvestCountdown = 60f;
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(HarvestCountdownSequence());

            ShowSubtitle("N\u01b0\u1edbc \u0111\u00e3 \u0111\u01b0\u1ee3c t\u01b0\u1edbi! C\u00e0 r\u1ed1t s\u1ebd ch\u00edn sau 60 gi\u00e2y. H\u00e3y \u0111\u1ee3i nh\u00e9!");

            // Show big instruction about waiting
            ShowInstructionBanner(
                "\u0110\u1ee3i c\u00e2y l\u1edbn!",
                "C\u00e0 r\u1ed1t \u0111ang ph\u00e1t tri\u1ec3n. H\u00e3y \u0111\u1ee3i 60 gi\u00e2y \u0111\u1ec3 thu ho\u1ea1ch!"
            );

            // Show big countdown timer
            ShowCountdownTimer();
        }
    }

    private IEnumerator HarvestCountdownSequence()
    {
        while (harvestCountdown > 0f)
        {
            UpdateQuestHUD($"Ch\u1edd C\u00e0 R\u1ed1t ch\u00edn v\u00e0 thu ho\u1ea1ch (c\u00f2n {Mathf.CeilToInt(harvestCountdown)}s)");

            // Update big countdown number
            if (countdownNumber != null)
            {
                countdownNumber.text = Mathf.CeilToInt(harvestCountdown).ToString();

                // Color changes: green > yellow > red
                if (harvestCountdown > 30f)
                    countdownNumber.style.color = new Color(0.4f, 0.9f, 0.3f, 1f);
                else if (harvestCountdown > 10f)
                    countdownNumber.style.color = new Color(1f, 0.85f, 0.2f, 1f);
                else
                    countdownNumber.style.color = new Color(1f, 0.3f, 0.2f, 1f);
            }

            yield return new WaitForSeconds(1f);
            harvestCountdown -= 1f;
        }

        // Time's up, make tile ripe
        HideCountdownTimer();
        UpdateQuestHUD("Nh\u1ea5p v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 thu ho\u1ea1ch C\u00e0 R\u1ed1t!");
        ShowSubtitle("C\u00e0 r\u1ed1t \u0111\u00e3 ch\u00edn v\u00e0ng ru\u1ed9m r\u1ed3i! H\u00e3y nh\u1ea5p v\u00e0o \u0111\u1ec3 thu ho\u1ea1ch n\u00f4ng s\u1ea3n \u0111\u1ea7u tay c\u1ee7a b\u1ea1n!");
        ShowInstructionBanner(
            "C\u00e0 r\u1ed1t \u0111\u00e3 ch\u00edn!",
            "Nh\u1ea5p chu\u1ed9t v\u00e0o \u00f4 \u0111\u1ea5t \u0111\u1ec3 thu ho\u1ea1ch"
        );
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
