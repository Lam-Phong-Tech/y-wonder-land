using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AttendancePopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument attendanceDocument;

    private VisualElement root;
    private VisualElement attendanceOverlay;
    private Button btnClose;
    private Button btnClaimAttendance;
    private VisualElement attendanceGrid;

    // List of day elements for querying
    private List<VisualElement> daySlots = new List<VisualElement>();

    // Mock Data State
    private int claimedDays = 2; // Days 1 & 2 claimed initially
    private bool hasClaimedToday = false; // Player can claim for Day 3 today

    // Rewards info dictionary for log mockup
    private Dictionary<int, string> dayRewards = new Dictionary<int, string>
    {
        { 1, "🪙 500 Cá vàng" },
        { 2, "🥕 5 Hạt giống Cà rốt" },
        { 3, "🪙 1000 Cá vàng" },
        { 4, "🧪 2 Phân bón siêu tốc" },
        { 5, "🍎 5 Quả Táo" },
        { 6, "💎 2 Kim cương" },
        { 7, "🐢 1 Rùa con quý hiếm!" }
    };

    void Awake()
    {
        if (attendanceDocument == null)
        {
            attendanceDocument = GetComponent<UIDocument>();
        }

        if (attendanceDocument == null)
        {
            Debug.LogError("[AttendancePopup] UIDocument component not found!");
            return;
        }

        root = attendanceDocument.rootVisualElement;
        QueryElements();
        RegisterCallbacks();

        // Hide initially
        Hide();
    }

    private void QueryElements()
    {
        attendanceOverlay = root.Q<VisualElement>("AttendanceOverlay");
        btnClose = root.Q<Button>("BtnClose");
        btnClaimAttendance = root.Q<Button>("BtnClaimAttendance");
        attendanceGrid = root.Q<VisualElement>("AttendanceGrid");

        // Query 7 slots
        daySlots.Clear();
        for (int i = 1; i <= 7; i++)
        {
            var slot = root.Q<VisualElement>($"DaySlot{i}");
            if (slot != null)
            {
                daySlots.Add(slot);
            }
        }
    }

    private void RegisterCallbacks()
    {
        // Close Popup
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());

        // Click outside overlay to close
        attendanceOverlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == attendanceOverlay)
            {
                Hide();
            }
        });

        // Claim Button
        btnClaimAttendance?.RegisterCallback<ClickEvent>(evt => ClaimDailyReward());
    }

    public void Show()
    {
        if (attendanceOverlay != null)
        {
            attendanceOverlay.style.display = DisplayStyle.Flex;
        }

        UpdateAttendanceGridUI();
    }

    public void Hide()
    {
        if (attendanceOverlay != null)
        {
            attendanceOverlay.style.display = DisplayStyle.None;
        }
    }

    private void UpdateAttendanceGridUI()
    {
        // Update each slot state
        for (int i = 0; i < daySlots.Count; i++)
        {
            var slot = daySlots[i];
            int dayNumber = i + 1;

            // Reset states
            slot.RemoveFromClassList("claimed");
            slot.RemoveFromClassList("current");

            if (dayNumber <= claimedDays)
            {
                // Day is claimed
                slot.AddToClassList("claimed");
            }
            else if (dayNumber == claimedDays + 1 && !hasClaimedToday)
            {
                // Current day to claim
                slot.AddToClassList("current");
            }
        }

        // Update main claim button text & enabled status
        if (btnClaimAttendance != null)
        {
            if (hasClaimedToday)
            {
                btnClaimAttendance.text = "Đã điểm danh";
                btnClaimAttendance.SetEnabled(false);
            }
            else
            {
                int nextDay = claimedDays + 1;
                btnClaimAttendance.text = nextDay <= 7 ? $"Điểm danh (Ngày {nextDay})" : "Đã hoàn thành!";
                btnClaimAttendance.SetEnabled(nextDay <= 7);
            }
        }
    }

    private void ClaimDailyReward()
    {
        if (hasClaimedToday || claimedDays >= 7) return;

        claimedDays++;
        hasClaimedToday = true;

        string reward = dayRewards.ContainsKey(claimedDays) ? dayRewards[claimedDays] : "Quà ngẫu nhiên";
        Debug.Log($"[Attendance] Điểm danh Ngày {claimedDays} thành công! Phần thưởng nhận được: {reward}");

        // Update UI
        UpdateAttendanceGridUI();
    }
}
