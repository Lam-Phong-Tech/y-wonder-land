using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Leaderboard Popup.
/// Manages category tabs and display of ranking lists.
/// </summary>
public class LeaderboardPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument leaderboardDocument;

    private VisualElement overlay;
    private Button btnClose;
    private VisualElement listContainer;
    private Label colValueHeader;
    private Label lblMyRankValue;

    // Tabs
    private Button tabDiligence;
    private Button tabLevel;
    private Button tabFashion;
    private Button tabPet;
    private Button tabRich;

    private Button activeTab;
    private string activeCategory = "diligence";

    // Mock ranking data
    private Dictionary<string, List<LeaderboardEntry>> mockData;
    private Dictionary<string, string> myRanks;

    private struct LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public string value;
        public string guildName;
    }

    private void Awake()
    {
        if (leaderboardDocument == null)
            leaderboardDocument = GetComponent<UIDocument>();

        InitMockData();
    }

    private void OnEnable()
    {
        var root = leaderboardDocument.rootVisualElement;

        // Query components
        overlay = root.Q<VisualElement>("LeaderboardOverlay");
        btnClose = root.Q<Button>("BtnCloseLeaderboard");
        listContainer = root.Q<VisualElement>("LeaderboardList");
        colValueHeader = root.Q<Label>("ColValueHeader");
        lblMyRankValue = root.Q<Label>("LblMyRankValue");

        // Query tabs
        tabDiligence = root.Q<Button>("TabDiligence");
        tabLevel = root.Q<Button>("TabLevel");
        tabFashion = root.Q<Button>("TabFashion");
        tabPet = root.Q<Button>("TabPet");
        tabRich = root.Q<Button>("TabRich");

        // Register callbacks
        RegisterCallbacks();

        // Default tab
        activeTab = tabDiligence;
        SetActiveTab(tabDiligence, "diligence", "EXP");

        // Hide initially
        Hide();
    }

    private void RegisterCallbacks()
    {
        // Close operations
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay) Hide();
        });

        // Tab click events
        tabDiligence?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabDiligence, "diligence", "EXP"));
        tabLevel?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabLevel, "level", "LEVEL"));
        tabFashion?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabFashion, "fashion", "STYLE"));
        tabPet?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabPet, "pet", "PET"));
        tabRich?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabRich, "rich", "GOLD"));
    }

    private void SetActiveTab(Button tab, string category, string valueHeaderName)
    {
        if (activeTab != null)
        {
            activeTab.RemoveFromClassList("leaderboard-tab--active");
        }
        activeTab = tab;
        activeTab?.AddToClassList("leaderboard-tab--active");

        activeCategory = category;

        // Update column header label
        if (colValueHeader != null)
        {
            colValueHeader.text = valueHeaderName;
        }

        // Update player's own rank card
        if (lblMyRankValue != null && myRanks.TryGetValue(activeCategory, out var rank))
        {
            lblMyRankValue.text = rank;
        }

        // Render table rows
        RefreshList();

        Debug.Log($"[Leaderboard] Switched to tab: {category}");
    }

    private void RefreshList()
    {
        if (listContainer == null) return;

        listContainer.Clear();

        if (mockData.TryGetValue(activeCategory, out var entries))
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var rowElement = CreateRowElement(entry, i);
                listContainer.Add(rowElement);
            }
        }
    }

    private VisualElement CreateRowElement(LeaderboardEntry entry, int index)
    {
        var row = new VisualElement();
        row.AddToClassList("leaderboard-row");

        // Styling based on rank (Gold, Silver, Bronze, Zebra Odd/Even)
        if (entry.rank == 1)
            row.AddToClassList("row-rank-1");
        else if (entry.rank == 2)
            row.AddToClassList("row-rank-2");
        else if (entry.rank == 3)
            row.AddToClassList("row-rank-3");
        else if (index % 2 == 0)
            row.AddToClassList("row-rank-even");
        else
            row.AddToClassList("row-rank-odd");

        // Column 1: Rank Badge
        var rankCol = new VisualElement();
        rankCol.AddToClassList("col-rank");

        if (entry.rank == 1)
        {
            var medalLabel = new Label("🥇");
            medalLabel.AddToClassList("rank-medal");
            rankCol.Add(medalLabel);
        }
        else if (entry.rank == 2)
        {
            var medalLabel = new Label("🥈");
            medalLabel.AddToClassList("rank-medal");
            rankCol.Add(medalLabel);
        }
        else if (entry.rank == 3)
        {
            var medalLabel = new Label("🥉");
            medalLabel.AddToClassList("rank-medal");
            rankCol.Add(medalLabel);
        }
        else
        {
            // Diamond container for 4+
            var container = new VisualElement();
            container.AddToClassList("rank-diamond-container");

            var diamond = new VisualElement();
            diamond.AddToClassList("rank-diamond");

            var numberLabel = new Label(entry.rank.ToString());
            numberLabel.AddToClassList("rank-diamond-text");

            diamond.Add(numberLabel);
            container.Add(diamond);
            rankCol.Add(container);
        }

        // Column 2: Player Name
        var playerCol = new Label(entry.playerName);
        playerCol.AddToClassList("col-player");
        playerCol.AddToClassList("row-player-text");

        // Column 3: Stats Value
        var valueCol = new Label(entry.value);
        valueCol.AddToClassList("col-value");
        valueCol.AddToClassList("row-value-text");

        // Column 4: Guild Name
        var guildCol = new Label(entry.guildName);
        guildCol.AddToClassList("col-guild");
        guildCol.AddToClassList("row-guild-text");

        // Assemble row
        row.Add(rankCol);
        row.Add(playerCol);
        row.Add(valueCol);
        row.Add(guildCol);

        return row;
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
            RefreshList();
            Debug.Log("[Leaderboard] Popup opened");
        }
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[Leaderboard] Popup closed");
        }
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Mock Data ──

    private void InitMockData()
    {
        myRanks = new Dictionary<string, string>
        {
            ["diligence"] = "100+",
            ["level"] = "42",
            ["fashion"] = "88",
            ["pet"] = "100+",
            ["rich"] = "15"
        };

        mockData = new Dictionary<string, List<LeaderboardEntry>>
        {
            ["diligence"] = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { rank = 1, playerName = "carot6868", value = "98.540 EXP", guildName = "Unknown" },
                new LeaderboardEntry { rank = 2, playerName = "Anhbaole", value = "95.200 EXP", guildName = "Gấu Đỏ" },
                new LeaderboardEntry { rank = 3, playerName = "Haiau1982", value = "89.120 EXP", guildName = "Sài Gòn" },
                new LeaderboardEntry { rank = 4, playerName = "Meocon6868", value = "84.300 EXP", guildName = "Cà Rốt" },
                new LeaderboardEntry { rank = 5, playerName = "Khoailang_Dalat", value = "79.450 EXP", guildName = "Unknown" },
                new LeaderboardEntry { rank = 6, playerName = "NuHoangNongTrai", value = "75.000 EXP", guildName = "Đất Việt" },
                new LeaderboardEntry { rank = 7, playerName = "LamPhongFarm", value = "71.200 EXP", guildName = "Vui Vẻ" },
                new LeaderboardEntry { rank = 8, playerName = "NongDanChamChi", value = "68.900 EXP", guildName = "Unknown" },
                new LeaderboardEntry { rank = 9, playerName = "BiTraiCay", value = "64.500 EXP", guildName = "Unknown" },
                new LeaderboardEntry { rank = 10, playerName = "CuTi_Gardener", value = "59.200 EXP", guildName = "Bình Minh" }
            },
            ["level"] = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { rank = 1, playerName = "carot6868", value = "Lv. 113", guildName = "Unknown" },
                new LeaderboardEntry { rank = 2, playerName = "Anhbaole", value = "Lv. 112", guildName = "Gấu Đỏ" },
                new LeaderboardEntry { rank = 3, playerName = "Haiau1982", value = "Lv. 112", guildName = "Sài Gòn" },
                new LeaderboardEntry { rank = 4, playerName = "Meocon6868", value = "Lv. 112", guildName = "Cà Rốt" },
                new LeaderboardEntry { rank = 5, playerName = "Khoailang_Dalat", value = "Lv. 111", guildName = "Unknown" },
                new LeaderboardEntry { rank = 6, playerName = "NuHoangNongTrai", value = "Lv. 110", guildName = "Đất Việt" },
                new LeaderboardEntry { rank = 7, playerName = "LamPhongFarm", value = "Lv. 110", guildName = "Vui Vẻ" },
                new LeaderboardEntry { rank = 8, playerName = "NongDanChamChi", value = "Lv. 109", guildName = "Unknown" },
                new LeaderboardEntry { rank = 9, playerName = "BiTraiCay", value = "Lv. 108", guildName = "Unknown" },
                new LeaderboardEntry { rank = 10, playerName = "CuTi_Gardener", value = "Lv. 107", guildName = "Bình Minh" }
            },
            ["fashion"] = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { rank = 1, playerName = "Princess_Pink", value = "8.500 ★", guildName = "Hoàng Gia" },
                new LeaderboardEntry { rank = 2, playerName = "LamPhongFarm", value = "7.900 ★", guildName = "Vui Vẻ" },
                new LeaderboardEntry { rank = 3, playerName = "Meocon6868", value = "7.400 ★", guildName = "Cà Rốt" },
                new LeaderboardEntry { rank = 4, playerName = "Fashionista_Pro", value = "6.800 ★", guildName = "Unknown" },
                new LeaderboardEntry { rank = 5, playerName = "Khoailang_Dalat", value = "6.200 ★", guildName = "Unknown" },
                new LeaderboardEntry { rank = 6, playerName = "AoBaBa_Style", value = "5.900 ★", guildName = "Đất Việt" },
                new LeaderboardEntry { rank = 7, playerName = "Anhbaole", value = "5.500 ★", guildName = "Gấu Đỏ" },
                new LeaderboardEntry { rank = 8, playerName = "carot6868", value = "5.100 ★", guildName = "Unknown" },
                new LeaderboardEntry { rank = 9, playerName = "BoSua_Garden", value = "4.800 ★", guildName = "Unknown" },
                new LeaderboardEntry { rank = 10, playerName = "LuaVang", value = "4.200 ★", guildName = "Unknown" }
            },
            ["pet"] = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { rank = 1, playerName = "Haiau1982", value = "Lv. 50", guildName = "Sài Gòn" },
                new LeaderboardEntry { rank = 2, playerName = "NongDanChamChi", value = "Lv. 48", guildName = "Unknown" },
                new LeaderboardEntry { rank = 3, playerName = "carot6868", value = "Lv. 45", guildName = "Unknown" },
                new LeaderboardEntry { rank = 4, playerName = "Anhbaole", value = "Lv. 44", guildName = "Gấu Đỏ" },
                new LeaderboardEntry { rank = 5, playerName = "GauMap", value = "Lv. 42", guildName = "Unknown" },
                new LeaderboardEntry { rank = 6, playerName = "LamPhongFarm", value = "Lv. 40", guildName = "Vui Vẻ" },
                new LeaderboardEntry { rank = 7, playerName = "CúnCon_Yêu", value = "Lv. 38", guildName = "Unknown" },
                new LeaderboardEntry { rank = 8, playerName = "Meocon6868", value = "Lv. 35", guildName = "Cà Rốt" },
                new LeaderboardEntry { rank = 9, playerName = "Khoailang_Dalat", value = "Lv. 32", guildName = "Unknown" },
                new LeaderboardEntry { rank = 10, playerName = "ChimBoCau", value = "Lv. 30", guildName = "Unknown" }
            },
            ["rich"] = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { rank = 1, playerName = "LamPhongFarm", value = "980.500 ◆", guildName = "Vui Vẻ" },
                new LeaderboardEntry { rank = 2, playerName = "TrieuPhuNongThon", value = "850.200 ◆", guildName = "Unknown" },
                new LeaderboardEntry { rank = 3, playerName = "carot6868", value = "740.000 ◆", guildName = "Unknown" },
                new LeaderboardEntry { rank = 4, playerName = "Anhbaole", value = "620.100 ◆", guildName = "Gấu Đỏ" },
                new LeaderboardEntry { rank = 5, playerName = "Haiau1982", value = "590.400 ◆", guildName = "Sài Gòn" },
                new LeaderboardEntry { rank = 6, playerName = "Meocon6868", value = "480.000 ◆", guildName = "Cà Rốt" },
                new LeaderboardEntry { rank = 7, playerName = "DaiGiaVườnAo", value = "420.000 ◆", guildName = "Unknown" },
                new LeaderboardEntry { rank = 8, playerName = "Khoailang_Dalat", value = "380.900 ◆", guildName = "Unknown" },
                new LeaderboardEntry { rank = 9, playerName = "NongDanSieuGiau", value = "310.000 ◆", guildName = "Unknown" },
                new LeaderboardEntry { rank = 10, playerName = "XuVang_Store", value = "290.000 ◆", guildName = "Unknown" }
            }
        };
    }
}
