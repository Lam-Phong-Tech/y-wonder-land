using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Controller for the Splash/Loading Screen.
/// Simulates loading progress with dynamic status messages,
/// then fades out to reveal the Login Screen underneath.
/// Press P key to replay the splash animation for testing.
/// Click anywhere to skip loading instantly.
/// </summary>
public class SplashLoadingController : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField] private float minLoadDuration = 2.0f;
    [SerializeField] private float maxLoadDuration = 3.0f;

    private UIDocument uiDocument;
    private VisualElement splashRoot;
    private VisualElement progressFill;
    private Label lblStatus;
    private Label lblPercentage;

    // Loading state
    private Coroutine loadingCoroutine;
    private bool isLoading = false;
    private bool hasCompleted = false;

    // Status messages per progress milestone
    private static readonly string[] statusMessages = new string[]
    {
        "Đang tải cấu hình nông trại...",       // 0-25%
        "Đang kết nối đến máy chủ Cloud...",    // 25-55%
        "Đang đồng bộ dữ liệu thế giới 3D...",// 55-85%
        "Đang chuẩn bị giao diện...",           // 85-99%
        "Tải hoàn tất!"                          // 100%
    };

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("[SplashLoading] UIDocument component not found!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks(root);
    }

    void Start()
    {
        // Start loading simulation on game start
        StartLoading();
    }

    void Update()
    {
        // Cheat key: P to replay splash screen for testing
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
        {
            ReplaySplash();
        }
    }

    private void QueryElements(VisualElement root)
    {
        splashRoot = root.Q<VisualElement>("SplashRoot");
        progressFill = root.Q<VisualElement>("ProgressFill");
        lblStatus = root.Q<Label>("lblStatus");
        lblPercentage = root.Q<Label>("lblPercentage");
    }

    private void RegisterCallbacks(VisualElement root)
    {
        // Click anywhere to skip loading
        root.RegisterCallback<ClickEvent>(OnScreenClicked);
    }

    private void OnScreenClicked(ClickEvent evt)
    {
        if (isLoading && !hasCompleted)
        {
            Debug.Log("[SplashLoading] Skip clicked — jumping to 100%");
            SkipToComplete();
        }
    }

    // ── Loading Simulation ──

    public void StartLoading()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        // Reset state
        hasCompleted = false;
        isLoading = true;
        gameObject.SetActive(true);

        // Reset visual state
        if (splashRoot != null)
        {
            splashRoot.RemoveFromClassList("splash-fade-out");
            splashRoot.style.opacity = 1f;
        }
        if (progressFill != null)
        {
            progressFill.style.width = Length.Percent(0);
        }
        if (lblPercentage != null)
        {
            lblPercentage.text = "0%";
        }
        if (lblStatus != null)
        {
            lblStatus.text = statusMessages[0];
        }

        loadingCoroutine = StartCoroutine(SimulateLoading());
    }

    private IEnumerator SimulateLoading()
    {
        float totalDuration = Random.Range(minLoadDuration, maxLoadDuration);
        float elapsed = 0f;
        float currentProgress = 0f;

        Debug.Log($"[SplashLoading] Starting loading simulation ({totalDuration:F1}s)");

        while (currentProgress < 100f)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / totalDuration);

            // Ease-in-out curve for more natural feel
            float easedTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
            currentProgress = Mathf.Clamp(easedTime * 100f, 0f, 100f);

            UpdateProgressUI(currentProgress);

            yield return null;
        }

        // Ensure we hit exactly 100%
        UpdateProgressUI(100f);
        OnLoadingComplete();
    }

    private void UpdateProgressUI(float progress)
    {
        int percent = Mathf.RoundToInt(progress);

        if (progressFill != null)
        {
            progressFill.style.width = Length.Percent(progress);
        }

        if (lblPercentage != null)
        {
            lblPercentage.text = $"{percent}%";
        }

        if (lblStatus != null)
        {
            // Update status message based on progress milestones
            if (percent < 25)
                lblStatus.text = statusMessages[0];
            else if (percent < 55)
                lblStatus.text = statusMessages[1];
            else if (percent < 85)
                lblStatus.text = statusMessages[2];
            else if (percent < 100)
                lblStatus.text = statusMessages[3];
            else
                lblStatus.text = statusMessages[4];
        }
    }

    private void SkipToComplete()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

        UpdateProgressUI(100f);
        OnLoadingComplete();
    }

    private void OnLoadingComplete()
    {
        isLoading = false;
        hasCompleted = true;
        Debug.Log("[SplashLoading] Loading complete! Starting fade out...");

        // Start fade out
        StartCoroutine(FadeOutAndHide());
    }

    private IEnumerator FadeOutAndHide()
    {
        // Add fade-out class for CSS transition
        if (splashRoot != null)
        {
            splashRoot.AddToClassList("splash-fade-out");
        }

        // Wait for fade transition (0.5s defined in USS)
        yield return new WaitForSeconds(0.6f);

        // Deactivate game object to fully remove from render
        gameObject.SetActive(false);

        Debug.Log("[SplashLoading] Splash screen hidden. Login screen should now be visible.");
    }

    // ── Cheat: Replay Splash ──

    public void ReplaySplash()
    {
        Debug.Log("[SplashLoading] Replaying splash screen (P key pressed)");

        // Re-enable and re-query elements if needed
        gameObject.SetActive(true);

        // Need to re-query after reactivation since OnEnable runs again
        // OnEnable will handle QueryElements + RegisterCallbacks
        // Just start loading after a frame delay
        StartCoroutine(DelayedRestart());
    }

    private IEnumerator DelayedRestart()
    {
        yield return null; // Wait one frame for OnEnable to complete
        StartLoading();
    }
}
