using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class GuideNPC : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform[] waypoints;
    public float waitDistance = 3.0f;
    public float warningTime = 10.0f;
    
    [Header("NPC Identity")]
    public string npcName = "Lâm Hướng Dẫn";

    // Actions/Callbacks
    public Action<string> OnDialogueTriggered; // Triggers subtitle UI
    public Action OnDestinationReached;      // Triggered when NPC finishes the tutorial path

    private NavMeshAgent agent;
    private Transform playerTransform;
    private int currentWaypointIndex = 0;
    private bool isWaitingForPlayer = false;

    // Player monitoring state
    private Vector3 lastPlayerPosition;
    private float playerIdleTimer = 0f;
    private bool isPlayerIdleWarningTriggered = false;

    // Dialogue configuration per waypoint
    private string[] waypointDialogues = new string[]
    {
        "Chào mừng bạn đến với đảo hoang! Hãy đi theo tôi để khám phá nhé!",
        "Đây là khu vực đất canh tác màu mỡ của chúng ta.",
        "Hãy dừng chân tại đây. Tôi sẽ hướng dẫn bạn cách cuốc đất canh tác."
    };

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Try to auto-discover player by Tag
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            lastPlayerPosition = playerTransform.position;
        }

        // Create fallback visual capsule if no renderer exists
        CreateFallbackVisual();

        // Auto-attach floating name tag above NPC head
        if (GetComponent<FloatingNameTag>() == null)
        {
            FloatingNameTag tag = gameObject.AddComponent<FloatingNameTag>();
            tag.displayName = npcName;
            tag.nameColor = FloatingNameTag.COLOR_HERO; // #5B42F3 — Hero purple
            tag.heightOffset = 3.0f;
            tag.tmpFontSize = 3.5f;
        }

        // Initially configure NavMeshAgent parameters
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.speed = 2.5f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.5f;

        // If waypoints are not configured, dynamically generate them using targetFarmTile position
        if ((waypoints == null || waypoints.Length == 0) && TutorialManager.Instance != null && TutorialManager.Instance.targetFarmTile != null)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = TutorialManager.Instance.targetFarmTile.transform.position;
            
            // Generate 3 waypoints leading to the tile
            GameObject wp1 = new GameObject("WP_Dynamic_1");
            wp1.transform.position = Vector3.Lerp(startPos, endPos, 0.33f);
            
            GameObject wp2 = new GameObject("WP_Dynamic_2");
            wp2.transform.position = Vector3.Lerp(startPos, endPos, 0.66f);
            
            GameObject wp3 = new GameObject("WP_Dynamic_3");
            wp3.transform.position = endPos - (endPos - startPos).normalized * 1.5f; // Stop 1.5m away from tile
            
            waypoints = new Transform[] { wp1.transform, wp2.transform, wp3.transform };
            Debug.Log($"[GuideNPC] Dynamically generated {waypoints.Length} waypoints leading to targetFarmTile.");
        }

        // Move to the first waypoint
        MoveToNextWaypoint();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            // Try to find player again if it spawned late
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                lastPlayerPosition = playerTransform.position;
            }
            return;
        }

        CheckPlayerDistance();
        MonitorPlayerIdle();
        CheckWaypointProgress();
    }

    private void CheckPlayerDistance()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > waitDistance)
        {
            if (!isWaitingForPlayer)
            {
                isWaitingForPlayer = true;
                agent.isStopped = true;
                
                // Trigger waiting dialogue
                TriggerDialogue("Hãy đi nhanh lên nào, tôi đang đợi bạn đấy!");
                
                // Play idle animation (mockup using Animator if available)
                var animator = GetComponent<Animator>();
                if (animator != null) animator.SetFloat("Speed", 0f);
            }
        }
        else if (distanceToPlayer <= waitDistance * 0.7f)
        {
            if (isWaitingForPlayer)
            {
                isWaitingForPlayer = false;
                agent.isStopped = false;
                
                // Trigger resuming dialogue
                TriggerDialogue("Tốt lắm! Đi tiếp thôi nào!");
                
                var animator = GetComponent<Animator>();
                if (animator != null) animator.SetFloat("Speed", agent.speed);
            }
        }
    }

    private void MonitorPlayerIdle()
    {
        // Check if player has moved
        float playerMovedDist = Vector3.Distance(playerTransform.position, lastPlayerPosition);
        if (playerMovedDist < 0.1f)
        {
            playerIdleTimer += Time.deltaTime;
            if (playerIdleTimer >= warningTime && !isPlayerIdleWarningTriggered)
            {
                isPlayerIdleWarningTriggered = true;
                
                // NPC turns around to look at player
                Vector3 lookDirection = playerTransform.position - transform.position;
                lookDirection.y = 0; // Keep horizontal orientation
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }

                // Trigger idle warning dialogue
                TriggerDialogue("Này bạn ơi! Nhấn phím di chuyển W A S D hoặc Joystick để đi theo tôi nhé!");
                
                // Trigger custom warning animation (Mockup wave)
                var animator = GetComponent<Animator>();
                if (animator != null) animator.SetTrigger("Wave");
            }
        }
        else
        {
            // Reset timer since player moved
            playerIdleTimer = 0f;
            isPlayerIdleWarningTriggered = false;
            lastPlayerPosition = playerTransform.position;
        }
    }

    private void CheckWaypointProgress()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (isWaitingForPlayer) return;

        // Check if agent arrived at destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex < waypoints.Length)
            {
                MoveToNextWaypoint();
            }
            else
            {
                // Finished path, trigger event
                agent.isStopped = true;
                
                var animator = GetComponent<Animator>();
                if (animator != null) animator.SetFloat("Speed", 0f);

                OnDestinationReached?.Invoke();
                this.enabled = false; // Disable update loop
            }
        }
    }

    private void MoveToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0 || currentWaypointIndex >= waypoints.Length) return;

        Transform target = waypoints[currentWaypointIndex];
        if (target != null)
        {
            agent.SetDestination(target.position);
            
            var animator = GetComponent<Animator>();
            if (animator != null) animator.SetFloat("Speed", agent.speed);

            // Trigger waypoint dialogue if configured
            if (currentWaypointIndex < waypointDialogues.Length)
            {
                TriggerDialogue(waypointDialogues[currentWaypointIndex]);
            }
        }
    }

    private void TriggerDialogue(string dialogueText)
    {
        Debug.Log($"[GuideNPC] {npcName}: \"{dialogueText}\"");
        OnDialogueTriggered?.Invoke(dialogueText);
    }

    private void CreateFallbackVisual()
    {
        // If there's no renderer in children, create a fallback capsule
        if (GetComponentInChildren<Renderer>() == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "NPCVisualFallback";
            capsule.transform.SetParent(this.transform, false);
            capsule.transform.localPosition = new Vector3(0, 1f, 0); // Center at height 1m
            capsule.transform.localScale = new Vector3(1f, 1f, 1f);
            
            // Set capsule color to bright purple (stands out)
            Renderer r = capsule.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = new Color(0.6f, 0.2f, 0.8f);
            }
            
            // Add a pointer cylinder representing face direction
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pointer.name = "NPCDirectionPointer";
            pointer.transform.SetParent(capsule.transform, false);
            pointer.transform.localPosition = new Vector3(0, 0.5f, 0.5f); // Nose height, facing forward Z
            pointer.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            pointer.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
            
            Renderer pr = pointer.GetComponent<Renderer>();
            if (pr != null)
            {
                pr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                pr.material.color = Color.yellow; // Yellow nose
            }
        }
    }
}
