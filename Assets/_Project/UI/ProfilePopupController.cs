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
    /// Mở hồ sơ. Chỉ nhận TÊN từ HUD; cấp/EXP/thống kê đọc thẳng từ manager để khỏi phải
    /// bóc ngược chuỗi trên label (cách cũ parse "120 / 250" luôn ra 0 nên thanh EXP đứng im).
    /// </summary>
    public void Show(string name)
    {
        if (profileOverlay != null)
        {
            profileOverlay.style.display = DisplayStyle.Flex;
        }

        var exp = YWonderLand.Managers.ExperienceManager.Instance;

        // Set dynamic texts
        if (profileName != null) profileName.text = name;
        if (profileLevel != null)
            profileLevel.text = exp != null ? $"Level: {exp.Level}" : "Level: 1";

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

        // Thanh EXP — lấy số thật của cấp hiện tại.
        float pct = exp != null ? exp.ExpPercent : 0f;
        if (profileExpFill != null)
            profileExpFill.style.width = Length.Percent(Mathf.Clamp(pct, 0f, 100f));

        if (profileExpText != null)
        {
            if (exp == null)
                profileExpText.text = "0 / 0";
            else if (exp.IsMaxLevel)
                profileExpText.text = "Cấp tối đa";
            else
                profileExpText.text = $"{exp.ExpInLevel} / {exp.ExpForNextLevel} ({pct:F0}%)";
        }

        UpdateFarmStats();

        Debug.Log($"[Profile] Opened profile details for player: '{name}'");
    }

    public void Hide()
    {
        if (profileOverlay != null)
        {
            profileOverlay.style.display = DisplayStyle.None;
        }
    }

    // Số THẬT, cộng dồn qua các phiên chơi (xem PlayerStats). Trước đây là Random nên mở
    // popup 2 lần ra 2 con số khác nhau.
    private void UpdateFarmStats()
    {
        if (statPlanted != null)
            statPlanted.text = YWonderLand.Managers.PlayerStats.Planted.ToString("N0");
        if (statSold != null)
            statSold.text = YWonderLand.Managers.PlayerStats.Sold.ToString("N0");

        // Hệ bạn bè chưa có backend -> để 0 chứ không bịa số.
        if (statFriends != null) statFriends.text = "0";

        if (statJoinedDate != null)
            statJoinedDate.text = YWonderLand.Managers.PlayerStats.JoinedDate.ToString("dd/MM/yyyy");
    }
}

