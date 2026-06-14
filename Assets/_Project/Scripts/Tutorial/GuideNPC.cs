using UnityEngine;
using UnityEngine.AI;
using System;
using YWonderLand.Tutorial;

[RequireComponent(typeof(NavMeshAgent))]
public class GuideNPC : MonoBehaviour
{
    [Header("Tutorial Sequence")]
    [Tooltip("Kéo thả các Trạm dừng chân (TutorialNode) vào đây theo thứ tự")]
    public TutorialNode[] tutorialNodes;

    [Header("Distance Settings")]
    public float waitDistance = 3.0f;
    public float playerReachDistance = 2.0f; // Khoảng cách để kích hoạt Giao Việc
    public float warningTime = 10.0f;
    
    [Header("NPC Identity")]
    public string npcName = "NPC Tân Thủ";

    [Header("Animation Settings")]
    [Tooltip("Tên của State hoạt ảnh đứng yên trong Animator")]
    public string idleAnimName = "Idle";
    [Tooltip("Tên của State hoạt ảnh chạy trong Animator")]
    public string runAnimName = "Walking";
    [Tooltip("Tên của State hoạt ảnh vẫy tay trong Animator")]
    public string waveAnimName = "Waving";
    [Tooltip("Tên của State hoạt ảnh chỉ trỏ trong Animator")]
    public string pointAnimName = "Pointing";
    
    private string currentAnim = "";

    // Actions/Callbacks
    public Action<string> OnDialogueTriggered; // Triggers subtitle UI
    public Action OnSequenceCompleted;         // All nodes completed

    private NavMeshAgent agent;
    private Transform playerTransform;
    private int currentNodeIndex = 0;
    
    // States
    private bool isWalkingToNode = false;
    private bool isWaitingForPlayerToReachNode = false;
    private bool isWaitingForPlayerTask = false;
    
    // Player monitoring state
    private Vector3 lastPlayerPosition;
    private float playerIdleTimer = 0f;
    private bool isPlayerIdleWarningTriggered = false;

    // Chat spam timer
    private float chatSpamTimer = 0f;
    private float CHAT_SPAM_INTERVAL = 8f; // Gọi người chơi mỗi 8 giây nếu họ đứng yên hoặc chưa tới

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

        CreateFallbackVisual();

        if (GetComponent<FloatingNameTag>() == null)
        {
            FloatingNameTag tag = gameObject.AddComponent<FloatingNameTag>();
            tag.displayName = npcName;
            tag.nameColor = FloatingNameTag.COLOR_HERO;
            tag.heightOffset = 3.0f;
            tag.tmpFontSize = 3.5f;
        }

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.speed = 2.5f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.5f;

        // Nếu chưa có Node nào, ta không tự tạo nữa vì TutorialManager sẽ lo việc này.
        // Chỉ việc chờ StartNode được gọi từ bên ngoài.
    }

    private string GetRandomString(string[] arr)
    {
        if (arr == null || arr.Length == 0) return "";
        return arr[UnityEngine.Random.Range(0, arr.Length)];
    }

    public void StartNode(int index)
    {
        if (tutorialNodes == null || tutorialNodes.Length == 0 || index >= tutorialNodes.Length)
        {
            Debug.Log("[GuideNPC] Đã hoàn thành toàn bộ tuyến hướng dẫn!");
            OnSequenceCompleted?.Invoke();
            this.enabled = false;
            return;
        }

        currentNodeIndex = index;
        TutorialNode node = tutorialNodes[currentNodeIndex];
        
        isWalkingToNode = true;
        isWaitingForPlayerToReachNode = false;
        isWaitingForPlayerTask = false;

        agent.SetDestination(node.transform.position);
        agent.isStopped = false;
        PlayAnimation(runAnimName);

        TriggerDialogue(GetRandomString(node.walkDialogues));
    }

    public void StartGreetingSequence(int firstNodeIndex = 0)
    {
        StartCoroutine(GreetingRoutine(firstNodeIndex));
    }

    private System.Collections.IEnumerator GreetingRoutine(int nodeIndex)
    {
        // Vô hiệu hóa các cờ logic bình thường để NPC đứng yên
        isWalkingToNode = false;
        isWaitingForPlayerToReachNode = false;
        isWaitingForPlayerTask = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // 1. NPC đứng vẫy tay chào
        PlayAnimation(waveAnimName);

        // 2. Chờ người chơi tới gần (khoảng cách > 5m)
        if (playerTransform != null)
        {
            while (Vector3.Distance(transform.position, playerTransform.position) > 5.0f)
            {
                LookAtPlayer(); // Liên tục quay mặt nhìn theo người chơi
                yield return null;
            }
        }

        // 3. Người chơi đã tới gần (<5m), dừng vẫy tay, chuyển sang chỉ trỏ
        PlayAnimation(pointAnimName);
        
        // Quay mặt về hướng trạm tiếp theo thay vì nhìn người chơi
        if (tutorialNodes != null && tutorialNodes.Length > nodeIndex)
        {
            Vector3 lookDirection = tutorialNodes[nodeIndex].transform.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        // Đợi cho hoạt ảnh chỉ trỏ chạy xong (khoảng 1.5 giây)
        yield return new WaitForSeconds(1.5f);

        // 4. Trở lại guồng máy bình thường: chạy tới Node số 0
        StartNode(nodeIndex);
    }

    void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                lastPlayerPosition = playerTransform.position;
            }
            return;
        }

        TutorialNode currentNode = tutorialNodes[currentNodeIndex];

        // 1. Nếu đang đi tới Node
        if (isWalkingToNode)
        {
            float distToNode = Vector3.Distance(transform.position, currentNode.transform.position);
            
            // Check Player Distance (Đợi người chơi nếu quá xa)
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            if (agent.isStopped)
            {
                // Đang đứng chờ: Phải đợi người chơi lại thật gần (cách 1 đoạn) mới chịu đi tiếp
                if (distToPlayer <= Mathf.Max(1.0f, waitDistance - 1.0f))
                {
                    agent.isStopped = false;
                    PlayAnimation(runAnimName);
                    chatSpamTimer = 0f; // Reset spam
                }
                else
                {
                    SpamChat(GetRandomString(currentNode.waitPlayerDialogues));
                    LookAtPlayer(); // NPC quay đầu lại nhìn người chơi
                }
            }
            else
            {
                // Đang đi bộ: Nếu người chơi tụt lại quá xa thì dừng lại
                if (distToPlayer > waitDistance)
                {
                    agent.isStopped = true;
                    PlayAnimation(idleAnimName);
                }
            }

            // Tới đích chưa?
            if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance || distToNode <= 0.8f))
            {
                isWalkingToNode = false;
                isWaitingForPlayerToReachNode = true;
                
                agent.isStopped = true;
                PlayAnimation(idleAnimName);
                
                // Quay mặt về hướng player
                LookAtPlayer();
                TriggerDialogue(GetRandomString(currentNode.waitPlayerDialogues));
            }
        }
        // 2. Nếu đã tới Node và đang đợi Player tới gần
        else if (isWaitingForPlayerToReachNode)
        {
            LookAtPlayer();

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer <= playerReachDistance)
            {
                // Player đã tới! Giao việc.
                isWaitingForPlayerToReachNode = false;
                isWaitingForPlayerTask = true;
                
                TriggerDialogue(GetRandomString(currentNode.actionDialogues));
                currentNode.OnPlayerArrivedAtNode?.Invoke();
            }
            else
            {
                // Nhắc nhở người chơi chạy lại gần
                SpamChat(GetRandomString(currentNode.waitPlayerDialogues));
            }
        }
        // 3. Nếu Player đang làm việc tại Node
        else if (isWaitingForPlayerTask)
        {
            LookAtPlayer();

            // Nhắc nhở
            SpamChat(GetRandomString(currentNode.actionDialogues)); // Hối thúc làm việc
            MonitorPlayerIdleDuringTask();

            // Kiểm tra xem Task xong chưa
            if (currentNode.isTaskCompleted)
            {
                isWaitingForPlayerTask = false;
                // Chuyển sang Node tiếp theo
                StartNode(currentNodeIndex + 1);
            }
        }
    }

    private void LookAtPlayer()
    {
        Vector3 lookDirection = playerTransform.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 5f);
        }
    }

    private void SpamChat(string message)
    {
        chatSpamTimer += Time.deltaTime;
        if (chatSpamTimer >= CHAT_SPAM_INTERVAL)
        {
            chatSpamTimer = 0f;
            TriggerDialogue(message);
        }
    }

    private void MonitorPlayerIdleDuringTask()
    {
        float playerMovedDist = Vector3.Distance(playerTransform.position, lastPlayerPosition);
        if (playerMovedDist < 0.1f)
        {
            playerIdleTimer += Time.deltaTime;
            if (playerIdleTimer >= warningTime && !isPlayerIdleWarningTriggered)
            {
                isPlayerIdleWarningTriggered = true;
                TutorialNode currentNode = tutorialNodes[currentNodeIndex];
                TriggerDialogue(GetRandomString(currentNode.idleWarningDialogues));
                // Player idle warning without wave
            }
        }
        else
        {
            playerIdleTimer = 0f;
            isPlayerIdleWarningTriggered = false;
            lastPlayerPosition = playerTransform.position;
        }
    }

    private void PlayAnimation(string animName)
    {
        if (string.IsNullOrEmpty(animName)) return;

        // Tìm tất cả Animator (cả ở cục Parent và Model con)
        Animator[] animators = GetComponentsInChildren<Animator>();
        Animator targetAnimator = null;

        // Ưu tiên lấy Animator nào có gắn sẵn Controller (NPCAnimator)
        foreach (var anim in animators)
        {
            if (anim.runtimeAnimatorController != null)
            {
                targetAnimator = anim;
                break;
            }
        }

        if (targetAnimator == null && animators.Length > 0)
        {
            targetAnimator = animators[0];
        }

        if (targetAnimator != null && currentAnim != animName)
        {
            Debug.Log($"[GuideNPC] Thay đổi hoạt ảnh thành -> {animName} (Trên object: {targetAnimator.gameObject.name})");
            targetAnimator.CrossFadeInFixedTime(animName, 0.2f); // Chuyển đổi an toàn theo thời gian thực (0.2s)
            currentAnim = animName;
        }
        else if (targetAnimator == null && currentAnim != animName)
        {
            Debug.LogWarning($"[GuideNPC] LỖI: Không tìm thấy Animator nào cho hoạt ảnh {animName}!");
        }
    }

    private void TriggerDialogue(string dialogueText)
    {
        Debug.Log($"[GuideNPC] {npcName}: \"{dialogueText}\"");
        OnDialogueTriggered?.Invoke(dialogueText);
    }

    private void CreateFallbackVisual()
    {
        if (GetComponentInChildren<Renderer>() == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "NPCVisualFallback";
            capsule.transform.SetParent(this.transform, false);
            capsule.transform.localPosition = new Vector3(0, 1f, 0); 
            
            Renderer r = capsule.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                r.material.color = new Color(0.6f, 0.2f, 0.8f);
            }
            
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pointer.name = "NPCDirectionPointer";
            pointer.transform.SetParent(capsule.transform, false);
            pointer.transform.localPosition = new Vector3(0, 0.5f, 0.5f); 
            pointer.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            pointer.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
            
            Renderer pr = pointer.GetComponent<Renderer>();
            if (pr != null)
            {
                pr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                pr.material.color = Color.yellow; 
            }
        }
    }
}
