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

        Transform targetCamTransform = cameraPosition1;

        // Determine camera target based on progress
        if (progress < 0.35f)
        {
            // Stage 1: Wide Angle Intro
            targetCamTransform = cameraPosition1;
        }
        else if (progress < 0.70f)
        {
            // Stage 2: Character Close-up
            targetCamTransform = cameraPosition2;
        }
        else
        {
            // Stage 3: Island Welcoming View
            targetCamTransform = cameraPosition3;
        }

        // Interpolate camera position and rotation smoothly
        if (targetCamTransform != null)
        {
            mainCameraTransform.position = Vector3.Lerp(mainCameraTransform.position, targetCamTransform.position, camLerpSpeed * Time.deltaTime);
            mainCameraTransform.rotation = Quaternion.Slerp(mainCameraTransform.rotation, targetCamTransform.rotation, camLerpSpeed * Time.deltaTime);
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
        skipButton.style.paddingLeft = 16;
        skipButton.style.paddingRight = 16;
        skipButton.style.paddingTop = 8;
        skipButton.style.paddingBottom = 8;
        skipButton.style.fontSize = 14;
        skipButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        skipButton.style.backgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f); // #EFEFEF
        skipButton.style.borderWidth = 3;
        skipButton.style.borderColor = new Color(0.24f, 0.21f, 0.21f, 1f);     // #3D3535
        skipButton.style.borderRadius = 8;
        skipButton.style.color = new Color(0.24f, 0.21f, 0.21f, 1f);
        
        // Solid shadow style via border offset concept
        skipButton.style.marginRight = 4;
        skipButton.style.marginBottom = 4;
        
        // Skip button start hidden (first 3s)
        skipButton.style.display = DisplayStyle.None;
        
        // Register skip click callback
        skipButton.clicked += SkipCutscene;
        
        // Style callbacks for button hover/click states (The Tangible Playground)
        skipButton.RegisterCallback<PointerOverEvent>(evt => {
            skipButton.style.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        });
        skipButton.RegisterCallback<PointerOutEvent>(evt => {
            skipButton.style.backgroundColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        });
        skipButton.RegisterCallback<PointerDownEvent>(evt => {
            skipButton.style.backgroundColor = new Color(0.81f, 0.81f, 0.81f, 1f);
            skipButton.style.borderLeftWidth = 4;
            skipButton.style.borderTopWidth = 4;
        });
        skipButton.RegisterCallback<PointerUpEvent>(evt => {
            skipButton.style.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            skipButton.style.borderLeftWidth = 3;
            skipButton.style.borderTopWidth = 3;
        });
        
        // Add Subtitle Panel (Bottom Center)
        VisualElement subtitleContainer = new VisualElement();
        subtitleContainer.style.position = Position.Absolute;
        subtitleContainer.style.bottom = 40;
        subtitleContainer.style.alignSelf = Align.Center;
        subtitleContainer.style.width = 500;
        subtitleContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f); // Semi-transparent black
        subtitleContainer.style.borderWidth = 2;
        subtitleContainer.style.borderColor = new Color(0.93f, 0.93f, 0.93f, 0.9f);
        subtitleContainer.style.borderRadius = 12;
        subtitleContainer.style.paddingLeft = 20;
        subtitleContainer.style.paddingRight = 20;
        subtitleContainer.style.paddingTop = 12;
        subtitleContainer.style.paddingBottom = 12;
        
        subtitleLabel = new Label("Chào mừng bạn đến với Y WONDER LAND...");
        subtitleLabel.style.fontSize = 15;
        subtitleLabel.style.color = Color.white;
        subtitleLabel.style.whiteSpace = WhiteSpace.Normal;
        subtitleLabel.style.unityParagraphLayout = ParagraphLayout.Enabled;
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
                transform.rotation = finalWaypoint.rotation;
            }
        }
        
        // Instantly align camera to cameraPosition3 (Shore View / Ending position)
        if (cameraPosition3 != null && mainCameraTransform != null)
        {
            mainCameraTransform.position = cameraPosition3.position;
            mainCameraTransform.rotation = cameraPosition3.rotation;
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
