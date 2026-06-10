using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class BoatCutscene : MonoBehaviour
{
    [Header("Movement Path")]
    public List<Transform> waypoints = new List<Transform>();
    public float movementSpeed = 3.0f;
    public float arrivalDistance = 0.5f;

    [Header("Cinematic Camera Angles")]
    public Transform cameraPosition1; // Wide Angle (e.g., High in the sky looking at boat)
    public Transform cameraPosition2; // Character Close-up (usually parented to the boat!)
    public Transform cameraPosition3; // Shore View (stationed on the island shore looking out)

    [Header("Main Camera Reference")]
    public Transform mainCameraTransform;

    [Header("Camera Transition Speeds")]
    public float camLerpSpeed = 2.0f;

    [Header("Failsafe Settings")]
    public float cutsceneTimeout = 35.0f; // Force end cutscene after 35 seconds as a failsafe
    private float cutsceneTimer = 0f;

    private Vector3 startPosition;
    private float totalJourneyDistance;
    private Vector3 finalWaypointPosition;
    private int currentWaypointIndex = 0;
    private bool isCutscenePlaying = false;
    private GameObject spawnedPlayer;

    // Cutscene UI elements
    private UIDocument cutsceneUIDocument;
    private VisualElement cutsceneRoot;
    private Button skipButton;
    private Label subtitleLabel;

    void Start()
    {
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    public void StartCutscene(GameObject player)
    {
        spawnedPlayer = player;
        currentWaypointIndex = 0;
        isCutscenePlaying = true;
        startPosition = transform.position;
        cutsceneTimer = 0f;

        if (waypoints.Count > 0)
        {
            Transform lastWaypoint = waypoints[waypoints.Count - 1];
            if (lastWaypoint != null)
            {
                finalWaypointPosition = lastWaypoint.position;
                totalJourneyDistance = Vector3.Distance(startPosition, finalWaypointPosition);
            }
            Debug.Log($"[BoatCutscene] Cutscene started with {waypoints.Count} waypoints. Failsafe timeout set to {cutsceneTimeout}s.");
        }
        else
        {
            Debug.LogWarning("Please configure Waypoints for the Boat Cutscene in Inspector!");
        }

        // Setup the dynamic Cutscene UI
        SetupCutsceneUI();
    }

    void Update()
    {
        if (!isCutscenePlaying) return;

        // Failsafe timer
        cutsceneTimer += Time.deltaTime;
        if (cutsceneTimer >= cutsceneTimeout)
        {
            Debug.LogWarning($"[BoatCutscene] Cutscene exceeded timeout failsafe of {cutsceneTimeout} seconds! Forcing completion to ensure gameplay starts.");
            EndCutscene();
            return;
        }

        // Enable Skip button after 3 seconds
        if (cutsceneTimer >= 3.0f && skipButton != null && skipButton.style.display == DisplayStyle.None)
        {
            skipButton.style.display = DisplayStyle.Flex;
        }

        // Update subtitle text based on timer
        UpdateSubtitleText();

        // 1. Boat Movement
        MoveBoat();

        // 2. Camera Cutscene Angles
        UpdateCinematicCamera();
    }

    private void MoveBoat()
    {
        if (waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
        {
            EndCutscene();
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            currentWaypointIndex++;
            return;
        }

        // Move towards target waypoint
        Vector3 targetPos = targetWaypoint.position;
        // Keep boat on water level
        targetPos.y = transform.position.y; 

        transform.position = Vector3.MoveTowards(transform.position, targetPos, movementSpeed * Time.deltaTime);

        // Rotate boat to face target waypoint smoothly
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Check if arrived at waypoint
        if (Vector3.Distance(transform.position, targetPos) < arrivalDistance)
        {
            Debug.Log($"[BoatCutscene] Arrived at waypoint {currentWaypointIndex + 1}/{waypoints.Count}: {targetWaypoint.name}");
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                EndCutscene();
            }
        }
    }

    private int currentStage = -1;

    private void UpdateCinematicCamera()
    {
        if (mainCameraTransform == null || waypoints.Count == 0) return;

        // Calculate continuous progress based on actual distance to final destination
        float progress = 0f;
        if (totalJourneyDistance > 0.1f)
        {
            float currentDist = Vector3.Distance(transform.position, finalWaypointPosition);
            progress = 1f - (currentDist / totalJourneyDistance);
            progress = Mathf.Clamp01(progress);
        }

        int newStage = 1;
        if (progress < 0.35f) newStage = 1;
        else if (progress < 0.70f) newStage = 2;
        else newStage = 3;

        if (newStage != currentStage)
        {
            currentStage = newStage;
            Debug.Log($"[BoatCutscene] Hard Cut to Camera Stage {currentStage}");
        }

        if (currentStage == 1)
        {
            if (cameraPosition1 != null)
            {
                mainCameraTransform.position = cameraPosition1.position;
                mainCameraTransform.rotation = cameraPosition1.rotation;
            }
        }
        else if (currentStage == 2)
        {
            if (cameraPosition2 != null)
            {
                mainCameraTransform.position = cameraPosition2.position;
                mainCameraTransform.rotation = cameraPosition2.rotation;
            }
        }
        else if (currentStage == 3)
        {
            if (cameraPosition3 != null)
            {
                mainCameraTransform.position = cameraPosition3.position;
                mainCameraTransform.LookAt(transform.position + Vector3.up * 1.5f);
            }
        }
    }

    private void SetupCutsceneUI()
    {
        // 1. Try to find PanelSettings from existing UIDocuments in the scene
        PanelSettings settings = null;
        UIDocument[] allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var doc in allDocs)
        {
            if (doc != null && doc.panelSettings != null)
            {
                settings = doc.panelSettings;
                break;
            }
        }

        // 2. Create GameObject for Cutscene UI
        GameObject uiGo = new GameObject("CutsceneUI");
        uiGo.transform.SetParent(this.transform);
        
        cutsceneUIDocument = uiGo.AddComponent<UIDocument>();
        if (settings != null)
        {
            cutsceneUIDocument.panelSettings = settings;
        }

        // 3. Construct Visual Elements
        cutsceneRoot = new VisualElement();
        cutsceneRoot.style.position = Position.Absolute;
        cutsceneRoot.style.width = Length.Percent(100);
        cutsceneRoot.style.height = Length.Percent(100);
        
        // Add Skip Button (Top-Right)
        skipButton = new Button();
        skipButton.text = "Bỏ qua >>";
        skipButton.style.position = Position.Absolute;
        skipButton.style.top = 24;
        skipButton.style.right = 24;
        skipButton.style.paddingLeft = 24;
        skipButton.style.paddingRight = 24;
        skipButton.style.paddingTop = 10;
        skipButton.style.paddingBottom = 10;
        skipButton.style.fontSize = 16;
        skipButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        // Palia Style Skip Button (Accent Yellow Pill)
        Color accentYellow = new Color(0.992f, 0.937f, 0.439f, 1f);
        Color yellowLip = new Color(0.815f, 0.764f, 0.313f, 1f);
        Color mysticBlack = new Color(0.082f, 0.101f, 0.152f, 1f);
        
        skipButton.style.backgroundColor = accentYellow;
        skipButton.style.borderTopWidth = 0f;
        skipButton.style.borderLeftWidth = 0f;
        skipButton.style.borderRightWidth = 0f;
        skipButton.style.borderBottomWidth = 0f; // Bỏ hoàn toàn đổ bóng/viền lún
        
        // Pill shape
        skipButton.style.borderTopLeftRadius = 24f;
        skipButton.style.borderTopRightRadius = 24f;
        skipButton.style.borderBottomLeftRadius = 24f;
        skipButton.style.borderBottomRightRadius = 24f;
        skipButton.style.color = mysticBlack;
        
        // Skip button start hidden (first 3s)
        skipButton.style.display = DisplayStyle.None;
        
        // Register skip click callback
        skipButton.clicked += SkipCutscene;
        
        // Hover/Active states (Chỉ đổi độ sáng nền, không lún)
        skipButton.RegisterCallback<PointerOverEvent>(evt => {
            skipButton.style.backgroundColor = new Color(1f, 0.97f, 0.56f, 1f); // Lighter yellow
        });
        skipButton.RegisterCallback<PointerOutEvent>(evt => {
            skipButton.style.backgroundColor = accentYellow;
        });
        skipButton.RegisterCallback<PointerDownEvent>(evt => {
            skipButton.style.backgroundColor = yellowLip; // Màu nhấn
            skipButton.style.marginTop = 0f;
        });
        skipButton.RegisterCallback<PointerUpEvent>(evt => {
            skipButton.style.backgroundColor = accentYellow;
            skipButton.style.marginTop = 0f;
        });
        
        // Add Subtitle Panel (Bottom Center)
        VisualElement subtitleContainer = new VisualElement();
        subtitleContainer.style.position = Position.Absolute;
        subtitleContainer.style.bottom = 80; // Raised higher to give space for chat panel
        subtitleContainer.style.alignSelf = Align.Center;
        subtitleContainer.style.width = 600; // Wider
        
        // Primary Navy background
        subtitleContainer.style.backgroundColor = new Color(0.227f, 0.278f, 0.400f, 0.95f);
        subtitleContainer.style.borderTopWidth = 2f;
        subtitleContainer.style.borderBottomWidth = 2f;
        subtitleContainer.style.borderLeftWidth = 2f;
        subtitleContainer.style.borderRightWidth = 2f;

        Color borderLight = new Color(1f, 1f, 1f, 0.1f);
        subtitleContainer.style.borderTopColor = borderLight;
        subtitleContainer.style.borderBottomColor = borderLight;
        subtitleContainer.style.borderLeftColor = borderLight;
        subtitleContainer.style.borderRightColor = borderLight;

        // Pill/Soft shape
        subtitleContainer.style.borderTopLeftRadius = 24f;
        subtitleContainer.style.borderTopRightRadius = 24f;
        subtitleContainer.style.borderBottomLeftRadius = 24f;
        subtitleContainer.style.borderBottomRightRadius = 24f;
        subtitleContainer.style.paddingLeft = 32;
        subtitleContainer.style.paddingRight = 32;
        subtitleContainer.style.paddingTop = 16;
        subtitleContainer.style.paddingBottom = 16;
        
        subtitleLabel = new Label("Chào mừng bạn đến với Y WONDER LAND...");
        subtitleLabel.style.fontSize = 18;
        subtitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        subtitleLabel.style.color = new Color(1f, 0.988f, 0.968f, 1f); // Palia White
        subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
        subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        subtitleContainer.Add(subtitleLabel);
        
        cutsceneRoot.Add(skipButton);
        cutsceneRoot.Add(subtitleContainer);
        
        cutsceneUIDocument.rootVisualElement.Add(cutsceneRoot);
    }

    private void UpdateSubtitleText()
    {
        if (subtitleLabel == null) return;
        
        if (cutsceneTimer < 5.0f)
        {
            subtitleLabel.text = "Chào mừng bạn đến với vùng đất kỳ diệu Y WONDER LAND!";
        }
        else if (cutsceneTimer < 12.0f)
        {
            subtitleLabel.text = "Nơi đây từng là một nông trại trù phú, ngập tràn tiếng cười và sức sống...";
        }
        else if (cutsceneTimer < 20.0f)
        {
            subtitleLabel.text = "Hãy cùng nhau khôi phục lại nông trại hoang sơ này và xây dựng một kỷ nguyên mới.";
        }
        else if (cutsceneTimer < 28.0f)
        {
            subtitleLabel.text = "Thuyền đang cập bến rồi. Một cuộc phiêu lưu mới đang đón chờ bạn phía trước!";
        }
        else
        {
            subtitleLabel.text = "Sẵn sàng lên bờ khám phá nào!";
        }
    }

    public void SkipCutscene()
    {
        if (!isCutscenePlaying) return;
        Debug.Log("[BoatCutscene] Skipping cutscene!");
        
        // Instant teleport boat to the final waypoint position
        if (waypoints.Count > 0)
        {
            Transform finalWaypoint = waypoints[waypoints.Count - 1];
            if (finalWaypoint != null)
            {
                transform.position = finalWaypoint.position;
                if (waypoints.Count > 1)
                {
                    Vector3 arrivalDir = (finalWaypoint.position - waypoints[waypoints.Count - 2].position).normalized;
                    if (arrivalDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(arrivalDir);
                }
                else
                {
                    transform.rotation = finalWaypoint.rotation;
                }
            }
        }
        
        // Instantly align camera to cameraPosition3 (Shore View / Ending position)
        if (cameraPosition3 != null && mainCameraTransform != null)
        {
            mainCameraTransform.position = cameraPosition3.position;
            mainCameraTransform.LookAt(transform.position + Vector3.up * 1.5f);
        }
        
        EndCutscene();
    }

    private void EndCutscene()
    {
        if (!isCutscenePlaying) return; // Prevent double trigger
        isCutscenePlaying = false;
        Debug.Log("[BoatCutscene] Boat Cutscene Completed!");
        
        // Clean up UI
        if (cutsceneUIDocument != null)
        {
            Destroy(cutsceneUIDocument.gameObject);
        }
        
        // Notify GameManager to start gameplay!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBoatArrived();
        }
        else
        {
            Debug.LogWarning("[BoatCutscene] GameManager.Instance is NULL! Attempting to find GameManager dynamically...");
            GameManager gm = GameObject.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.OnBoatArrived();
            }
            else
            {
                Debug.LogError("[BoatCutscene] Critical: Could not find GameManager in scene to end cutscene!");
            }
        }
    }
}
