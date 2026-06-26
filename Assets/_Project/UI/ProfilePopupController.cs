using UnityEngine;
using UnityEngine.UIElements;

public class ProfilePopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument profileDocument;

    private VisualElement root;
    private VisualElement profileOverlay;
    private Button btnClose;

    // Profile Details Elements
    private VisualElement profileAvatarLarge;
    private Label profileName;
    private Label profileLevel;
    private VisualElement profileExpFill;
    private Label profileExpText;

    // Farm Stats Elements
    private Label statPlanted;
    private Label statSold;
    private Label statFriends;
    private Label statJoinedDate;

    void Awake()
    {
        if (profileDocument == null)
        {
            profileDocument = GetComponent<UIDocument>();
        }

        if (profileDocument == null)
        {
            Debug.LogError("[ProfilePopup] UIDocument component not found!");
            return;
        }

        root = profileDocument.rootVisualElement;
        QueryElements();
        RegisterCallbacks();

        // Hide popup initially
        Hide();
    }

    private void QueryElements()
    {
        profileOverlay = root.Q<VisualElement>("ProfileOverlay");
        btnClose = root.Q<Button>("BtnClose");

        // Profile details
        profileAvatarLarge = root.Q<VisualElement>("ProfileAvatarLarge");
        profileName = root.Q<Label>("ProfileName");
        profileLevel = root.Q<Label>("ProfileLevel");
        profileExpFill = root.Q<VisualElement>("ProfileExpFill");
        profileExpText = root.Q<Label>("ProfileExpText");

        // Farm stats
        statPlanted = root.Q<Label>("StatPlanted");
        statSold = root.Q<Label>("StatSold");
        statFriends = root.Q<Label>("StatFriends");
        statJoinedDate = root.Q<Label>("StatJoinedDate");
    }

    private void RegisterCallbacks()
    {
        // Close Button
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());

        // Click outside panel to close
        profileOverlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == profileOverlay)
            {
                Hide();
            }
        });
    }

    /// <summary>
    /// Show the profile popup with dynamic data passed from GameHUD.
    /// </summary>
    public void Show(string name, string levelStr, string expStr)
    {
        if (profileOverlay != null)
        {
            profileOverlay.style.display = DisplayStyle.Flex;
        }

        // Set dynamic texts
        if (profileName != null) profileName.text = name;
        if (profileLevel != null) profileLevel.text = levelStr;

        if (profileAvatarLarge != null)
        {
            profileAvatarLarge.RemoveFromClassList("avatar-male");
            profileAvatarLarge.RemoveFromClassList("avatar-female");
            
            int gender = GameManager.Instance != null ? GameManager.Instance.selectedCharacterIndex : 0;
            if (gender == 0)
                profileAvatarLarge.AddToClassList("avatar-male");
            else
                profileAvatarLarge.AddToClassList("avatar-female");
        }

        // Calculate and set EXP progress bar width
        float expVal = 0f;
        float.TryParse(expStr, out expVal);
        
        // Let's assume level up requires 100.00 EXP in this mockup
        float maxExp = 100.00f;
        float pct = (expVal / maxExp) * 100f;
        pct = Mathf.Clamp(pct, 0f, 100f);

        if (profileExpFill != null)
        {
            profileExpFill.style.width = Length.Percent(pct);
        }
        if (profileExpText != null)
        {
            profileExpText.text = $"{expVal:F2} / {maxExp:F2} ({pct:F0}%)";
        }

        // Generate or load farm stats
        UpdateFarmStatsMock();
        
        Debug.Log($"[Profile] Opened profile details for player: '{name}'");
    }

    public void Hide()
    {
        if (profileOverlay != null)
        {
            profileOverlay.style.display = DisplayStyle.None;
        }
    }

    private void UpdateFarmStatsMock()
    {
        // Seed random stats to make it look dynamic and alive
        int planted = Random.Range(100, 300);
        int sold = planted + Random.Range(50, 200);
        int friendsCount = Random.Range(3, 10);

        if (statPlanted != null) statPlanted.text = planted.ToString();
        if (statSold != null) statSold.text = sold.ToString();
        if (statFriends != null) statFriends.text = friendsCount.ToString();
        if (statJoinedDate != null) statJoinedDate.text = "02/06/2026";
    }
}

