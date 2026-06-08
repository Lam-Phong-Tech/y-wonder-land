using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Level Up fullscreen overlay.
/// Shows golden VFX with level info and optional unlock notification.
/// Triggered when player gains a level.
/// </summary>
public class LevelUpOverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument levelUpDocument;

    // Elements
    private VisualElement overlay;
    private Label lblLevelUp;
    private Label lblNewLevel;
    private VisualElement unlockSection;
    private Label lblUnlockItem;
    private Button btnOk;

    // State
    private bool isShowing = false;

    // Unlock data per level (mock)
    private static readonly System.Collections.Generic.Dictionary<int, string> unlockData =
        new System.Collections.Generic.Dictionary<int, string>
    {
        { 5,  "🎣 Câu cá" },
        { 10, "⛏️ Khai thác mỏ" },
        { 15, "🐄 Mua bò sữa" },
        { 20, "🏗️ Build Mode nâng cao" },
        { 30, "🎁 Gift Box" },
        { 40, "🏝️ Đảo Hải Phú" },
        { 50, "🐑 Vật nuôi đặc biệt" },
        { 60, "🌲 Đảo Mộc Nhi" },
    };

    private void Awake()
    {
        if (levelUpDocument == null)
            levelUpDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        var root = levelUpDocument.rootVisualElement;
        QueryElements(root);
        RegisterCallbacks();
        Hide();
    }

    private void QueryElements(VisualElement root)
    {
        overlay = root.Q<VisualElement>("LevelUpOverlay");
        lblLevelUp = root.Q<Label>("LblLevelUp");
        lblNewLevel = root.Q<Label>("LblNewLevel");
        unlockSection = root.Q<VisualElement>("LevelUpUnlock");
        lblUnlockItem = root.Q<Label>("LblUnlockItem");
        btnOk = root.Q<Button>("BtnLevelUpOk");
    }

    private void RegisterCallbacks()
    {
        btnOk?.RegisterCallback<ClickEvent>(evt => Hide());
    }

    // ── Public API ──

    /// <summary>
    /// Show the Level Up overlay for a given new level.
    /// </summary>
    public void Show(int newLevel)
    {
        if (overlay == null || isShowing) return;

        isShowing = true;

        // Set level text
        if (lblNewLevel != null)
            lblNewLevel.text = $"Level {newLevel}";

        // Check for unlocks
        if (unlockSection != null)
        {
            if (unlockData.ContainsKey(newLevel))
            {
                unlockSection.style.display = DisplayStyle.Flex;
                if (lblUnlockItem != null)
                    lblUnlockItem.text = unlockData[newLevel];
            }
            else
            {
                unlockSection.style.display = DisplayStyle.None;
            }
        }

        // Show overlay then trigger animation
        overlay.style.display = DisplayStyle.Flex;
        overlay.RemoveFromClassList("levelup-overlay--visible");

        // Delay 1 frame to trigger CSS transition
        overlay.schedule.Execute(() =>
        {
            overlay.AddToClassList("levelup-overlay--visible");
        }).ExecuteLater(50);

        Debug.Log($"[LevelUp] ⬆ Level UP! → Level {newLevel}" +
            (unlockData.ContainsKey(newLevel) ? $" | Mở khóa: {unlockData[newLevel]}" : ""));
    }

    /// <summary>
    /// Hide the overlay with fade out.
    /// </summary>
    public void Hide()
    {
        if (overlay == null) return;

        overlay.RemoveFromClassList("levelup-overlay--visible");

        // Wait for fade out then hide
        overlay.schedule.Execute(() =>
        {
            overlay.style.display = DisplayStyle.None;
            isShowing = false;
        }).ExecuteLater(400);
    }

    public bool IsVisible()
    {
        return isShowing;
    }

    // ── Test helper (can be called from HUD cheat or elsewhere) ──

    private int testLevel = 1;

    /// <summary>
    /// Cycle through test levels to preview unlock messages.
    /// </summary>
    public void TestLevelUp()
    {
        testLevel++;
        Show(testLevel);
    }
}
