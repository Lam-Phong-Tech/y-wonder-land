using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }

    [SerializeField] private VisualTreeAsset loadingScreenAsset;
    
    private UIDocument uiDocument;
    private VisualElement rootContainer;
    private VisualElement progressBarFill;
    private Label loadingText;

    private bool isVisible = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (Instance == this)
        {
            _ = InitializeUIAsync();
        }
    }

    private async Task InitializeUIAsync()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;
        
        // Chỉ gán file uxml nếu người dùng quên chưa gán trong Inspector
        if (uiDocument.visualTreeAsset == null)
        {
            if (loadingScreenAsset == null)
            {
                loadingScreenAsset = Resources.Load<VisualTreeAsset>("UI/LoadingScreen");
#if UNITY_EDITOR
                if (loadingScreenAsset == null)
                {
                    loadingScreenAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/UI/LoadingScreen.uxml");
                }
#endif
            }
            uiDocument.visualTreeAsset = loadingScreenAsset;
        }

        uiDocument.sortingOrder = 999; 

        // Chờ UI Toolkit khởi tạo Visual Tree
        while (uiDocument.rootVisualElement == null)
        {
            await Awaitable.NextFrameAsync();
        }

        // Chờ tìm thấy LoadingContainer
        int timeout = 0;
        while (rootContainer == null && timeout < 100)
        {
            rootContainer = uiDocument.rootVisualElement.Q<VisualElement>("LoadingContainer");
            if (rootContainer == null)
            {
                await Awaitable.NextFrameAsync();
                timeout++;
            }
        }

        if (rootContainer != null)
        {
            progressBarFill = rootContainer.Q<VisualElement>("ProgressBarFill");
            loadingText = rootContainer.Q<Label>("LoadingText");

            // TẮT HIỂN THỊ ĐỂ KHÔNG CHẶN CLICK
            rootContainer.style.display = DisplayStyle.None;
            
            // Xóa picking mode cho mọi thành phần con
            rootContainer.pickingMode = PickingMode.Ignore;
        }
        else
        {
            Debug.LogError("[LoadingScreen] Lỗi nghiêm trọng: Không tìm thấy 'LoadingContainer' trong file UXML!");
        }
    }

    void Update()
    {
        // Giả lập thanh Progress chạy liên tục khi đang load
        if (isVisible && progressBarFill != null)
        {
            float pingPong = Mathf.PingPong(Time.time * 50f, 100f);
            progressBarFill.style.width = Length.Percent(pingPong);
        }
    }

    /// <summary>
    /// Hiển thị màn hình chờ (Fade In)
    /// </summary>
    public async Task ShowLoadingAsync(string message = "Đang đồng bộ thế giới...")
    {
        if (rootContainer == null) return;

        if (loadingText != null)
            loadingText.text = message;

        rootContainer.style.display = DisplayStyle.Flex;
        
        // Đợi 1 frame để UI Element kịp render trước khi đổi opacity
        await Awaitable.NextFrameAsync();
        
        rootContainer.AddToClassList("loading-screen-container--visible");
        isVisible = true;

        // Đợi transition chạy xong (1.0s) để màn hình đen hoàn toàn trước khi game bị lag do LoadScene
        await Awaitable.WaitForSecondsAsync(1.0f);
    }

    /// <summary>
    /// Ẩn màn hình chờ (Fade Out)
    /// </summary>
    public async Task HideLoadingAsync()
    {
        if (rootContainer == null) return;

        rootContainer.RemoveFromClassList("loading-screen-container--visible");
        
        // Đợi transition chạy xong (1.0s)
        await Awaitable.WaitForSecondsAsync(1.0f);
        
        rootContainer.style.display = DisplayStyle.None;
        isVisible = false;
    }
}
