using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Friends Popup.
/// Manages friends list, pending requests, searching and adding friends.
/// </summary>
public class FriendsPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument friendsDocument;

    private VisualElement overlay;
    private Button btnClose;
    private VisualElement listContainer;
    private Label lblStateTitle;
    private VisualElement searchGroup;
    private TextField txtSearchName;
    private Button btnSearch;
    private Button btnRefresh;

    // Tabs
    private Button tabFriends;
    private Button tabRequests;
    private Button tabSearch;

    private Button activeTab;
    private TabType activeTabType = TabType.Friends;

    // Mock Data
    private List<FriendData> friendsList = new List<FriendData>();
    private List<FriendData> pendingRequests = new List<FriendData>();
    private List<FriendData> searchPool = new List<FriendData>();
    private List<FriendData> currentDisplayedList = new List<FriendData>();

    public enum TabType
    {
        Friends,
        Requests,
        Search
    }

    [System.Serializable]
    public class FriendData
    {
        public string name;
        public int level;
        public bool isMale;
        public bool isOnline;
        public string avatarEmoji;

        public FriendData(string name, int level, bool isMale, bool isOnline, string avatarEmoji)
        {
            this.name = name;
            this.level = level;
            this.isMale = isMale;
            this.isOnline = isOnline;
            this.avatarEmoji = avatarEmoji;
        }
    }

    private void Awake()
    {
        if (friendsDocument == null)
        {
            friendsDocument = GetComponent<UIDocument>();
        }

        if (friendsDocument != null)
        {
            friendsDocument.sortingOrder = 100; // Force popup to render on top of HUD
        }

        InitMockData();
    }

    private void OnEnable()
    {
        if (friendsDocument == null) return;

        var root = friendsDocument.rootVisualElement;

        // Query components
        overlay = root.Q<VisualElement>("FriendsOverlay");
        btnClose = root.Q<Button>("BtnCloseFriends");
        listContainer = root.Q<VisualElement>("FriendsList");
        lblStateTitle = root.Q<Label>("LblStateTitle");
        searchGroup = root.Q<VisualElement>("SearchGroup");
        txtSearchName = root.Q<TextField>("TxtSearchName");
        btnSearch = root.Q<Button>("BtnSearch");
        btnRefresh = root.Q<Button>("BtnRefresh");

        // Query tabs
        tabFriends = root.Q<Button>("TabFriends");
        tabRequests = root.Q<Button>("TabRequests");
        tabSearch = root.Q<Button>("TabSearch");

        // Register callbacks
        RegisterCallbacks();
        SetupPlaceholders();

        // Default tab
        activeTab = tabFriends;
        SetActiveTab(tabFriends, TabType.Friends);

        // Hide initially
        Hide();
    }

    private void RegisterCallbacks()
    {
        // Close operations
        if (btnClose != null)
        {
            btnClose.clicked -= Hide;
            btnClose.clicked += Hide;
            // Force close on pointer down to bypass any UI Toolkit event consumption issues
            btnClose.RegisterCallback<PointerDownEvent>(evt => Hide());
        }
        overlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == overlay)
            {
                Hide();
            }
        });

        // Tab click events
        tabFriends?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabFriends, TabType.Friends));
        tabRequests?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabRequests, TabType.Requests));
        tabSearch?.RegisterCallback<ClickEvent>(evt => SetActiveTab(tabSearch, TabType.Search));

        // Search & Refresh action events
        btnSearch?.RegisterCallback<ClickEvent>(evt => ExecuteSearch());
        btnRefresh?.RegisterCallback<ClickEvent>(evt => ExecuteRefreshSearchPool());

        // Focus styling for search textfield
        if (txtSearchName != null)
        {
            txtSearchName.RegisterCallback<FocusInEvent>(evt => searchGroup?.AddToClassList("friends-search-focus"));
            txtSearchName.RegisterCallback<FocusOutEvent>(evt => searchGroup?.RemoveFromClassList("friends-search-focus"));
        }
    }

    private void SetupPlaceholders()
    {
        if (txtSearchName != null && txtSearchName.textEdition != null)
        {
            txtSearchName.textEdition.placeholder = "Nhập tên nhân vật...";
        }
    }

    private void SetActiveTab(Button tab, TabType tabType)
    {
        if (activeTab != null)
        {
            activeTab.RemoveFromClassList("friends-tab--active");
        }
        activeTab = tab;
        activeTab?.AddToClassList("friends-tab--active");

        activeTabType = tabType;

        // Reset search field text on tab change
        if (txtSearchName != null)
        {
            txtSearchName.value = "";
        }

        // Toggle state elements based on active tab
        UpdateLayoutForTab();

        // Refresh list
        RefreshList();

        Debug.Log($"[Friends] Switched to tab: {tabType}");
    }

    private void UpdateLayoutForTab()
    {
        if (lblStateTitle == null) return;

        switch (activeTabType)
        {
            case TabType.Friends:
                lblStateTitle.text = "Danh sách bạn bè";
                if (searchGroup != null) searchGroup.style.display = DisplayStyle.None;
                break;

            case TabType.Requests:
                lblStateTitle.text = "Lời mời kết bạn";
                if (searchGroup != null) searchGroup.style.display = DisplayStyle.None;
                break;

            case TabType.Search:
                lblStateTitle.text = "Người chơi ngẫu nhiên";
                if (searchGroup != null) searchGroup.style.display = DisplayStyle.Flex;
                // Make search textfield and buttons visible
                if (btnSearch != null) btnSearch.style.display = DisplayStyle.Flex;
                if (btnRefresh != null) btnRefresh.style.display = DisplayStyle.Flex;
                break;
        }
    }

    private void RefreshList()
    {
        if (listContainer == null) return;

        listContainer.Clear();
        GetListDataForActiveTab();

        for (int i = 0; i < currentDisplayedList.Count; i++)
        {
            var data = currentDisplayedList[i];
            var card = CreatePlayerCard(data);
            listContainer.Add(card);
        }
    }

    private void GetListDataForActiveTab()
    {
        currentDisplayedList.Clear();
        switch (activeTabType)
        {
            case TabType.Friends:
                currentDisplayedList.AddRange(friendsList);
                break;
            case TabType.Requests:
                currentDisplayedList.AddRange(pendingRequests);
                break;
            case TabType.Search:
                currentDisplayedList.AddRange(searchPool);
                break;
        }
    }

    private VisualElement CreatePlayerCard(FriendData data)
    {
        var card = new VisualElement();
        card.AddToClassList("friend-card");

        // Card Left Area: Avatar + Details
        var cardLeft = new VisualElement();
        cardLeft.AddToClassList("friend-card-left");

        // Avatar Wrap
        var avatarWrap = new VisualElement();
        avatarWrap.AddToClassList("friend-avatar-wrap");

        var avatarImage = new VisualElement();
        avatarImage.AddToClassList("friend-avatar-image");
        avatarImage.AddToClassList(data.isMale ? "friend-avatar-male" : "friend-avatar-female");
        avatarWrap.Add(avatarImage);

        // Status Dot (Only relevant for Friends list, but can show for all)
        if (activeTabType == TabType.Friends)
        {
            var statusDot = new VisualElement();
            statusDot.AddToClassList("friend-status-dot");
            statusDot.AddToClassList(data.isOnline ? "status-online" : "status-offline");
            avatarWrap.Add(statusDot);
        }

        cardLeft.Add(avatarWrap);

        // Player Info Text
        var infoWrap = new VisualElement();
        infoWrap.AddToClassList("friend-info");

        var nameRow = new VisualElement();
        nameRow.AddToClassList("friend-name-row");

        var nameLabel = new Label(data.name);
        nameLabel.AddToClassList("friend-name");
        nameRow.Add(nameLabel);

        var genderLabel = new Label(data.isMale ? "♂" : "♀");
        genderLabel.AddToClassList("friend-gender");
        genderLabel.AddToClassList(data.isMale ? "gender-male" : "gender-female");
        nameRow.Add(genderLabel);

        infoWrap.Add(nameRow);

        var levelLabel = new Label($"Lv. {data.level}");
        levelLabel.AddToClassList("friend-level");
        infoWrap.Add(levelLabel);

        cardLeft.Add(infoWrap);
        card.Add(cardLeft);

        // Card Right Area: Actions Buttons
        var actionsWrap = new VisualElement();
        actionsWrap.AddToClassList("friend-actions");

        switch (activeTabType)
        {
            case TabType.Friends:
                var btnRemove = new Button();
                btnRemove.text = "Xóa bạn";
                btnRemove.AddToClassList("btn-action");
                btnRemove.AddToClassList("btn-remove");
                btnRemove.RegisterCallback<ClickEvent>(evt => HandleRemoveFriend(data));
                actionsWrap.Add(btnRemove);
                break;

            case TabType.Requests:
                var btnAccept = new Button();
                btnAccept.text = "Đồng ý";
                btnAccept.AddToClassList("btn-action");
                btnAccept.AddToClassList("btn-accept");
                btnAccept.RegisterCallback<ClickEvent>(evt => HandleAcceptRequest(data));
                actionsWrap.Add(btnAccept);

                var btnDecline = new Button();
                btnDecline.text = "Từ chối";
                btnDecline.AddToClassList("btn-action");
                btnDecline.AddToClassList("btn-decline");
                btnDecline.RegisterCallback<ClickEvent>(evt => HandleDeclineRequest(data));
                actionsWrap.Add(btnDecline);
                break;

            case TabType.Search:
                var btnAdd = new Button();
                btnAdd.text = "Kết bạn";
                btnAdd.AddToClassList("btn-action");
                btnAdd.AddToClassList("btn-add");
                btnAdd.RegisterCallback<ClickEvent>(evt => HandleAddFriendRequest(data));
                actionsWrap.Add(btnAdd);
                break;
        }

        card.Add(actionsWrap);
        return card;
    }

    // ── Button Logic Handlers ──

    private void HandleRemoveFriend(FriendData friend)
    {
        Debug.Log($"[Friends] Removing friend: {friend.name}");
        friendsList.Remove(friend);
        RefreshList();
    }

    private void HandleAcceptRequest(FriendData requester)
    {
        Debug.Log($"[Friends] Accepted friend request from: {requester.name}");
        pendingRequests.Remove(requester);
        // Add to friends list, mark as online for demo
        requester.isOnline = true;
        friendsList.Add(requester);
        RefreshList();
    }

    private void HandleDeclineRequest(FriendData requester)
    {
        Debug.Log($"[Friends] Declined friend request from: {requester.name}");
        pendingRequests.Remove(requester);
        RefreshList();
    }

    private void HandleAddFriendRequest(FriendData targetPlayer)
    {
        Debug.Log($"[Friends] Sent friend request to: {targetPlayer.name}");
        searchPool.Remove(targetPlayer);
        // Simulating: remove from search list since request is sent
        RefreshList();
    }

    private void ExecuteSearch()
    {
        if (txtSearchName == null) return;

        string searchName = txtSearchName.value.Trim();
        Debug.Log($"[Friends] Searching for user: '{searchName}'");

        if (string.IsNullOrEmpty(searchName))
        {
            // Reset to default search pool if search is empty
            GetListDataForActiveTab();
            RefreshList();
            return;
        }

        // Search within our search pool first
        List<FriendData> results = new List<FriendData>();
        foreach (var p in searchPool)
        {
            if (p.name.ToLower().Contains(searchName.ToLower()))
            {
                results.Add(p);
            }
        }

        // If no results, dynamically generate a matching mock player to test the UX
        if (results.Count == 0)
        {
            var isMale = Random.value > 0.5f;
            var emoji = isMale ? "🧑" : "👩";
            var mockUser = new FriendData(searchName, Random.Range(10, 80), isMale, Random.value > 0.3f, emoji);
            // Temporarily add to searchPool so it remains if searched again
            searchPool.Add(mockUser);
            results.Add(mockUser);
        }

        currentDisplayedList.Clear();
        currentDisplayedList.AddRange(results);

        // Rebuild list with search results
        if (listContainer != null)
        {
            listContainer.Clear();
            for (int i = 0; i < currentDisplayedList.Count; i++)
            {
                var card = CreatePlayerCard(currentDisplayedList[i]);
                listContainer.Add(card);
            }
        }
    }

    private void ExecuteRefreshSearchPool()
    {
        Debug.Log("[Friends] Refreshing random search pool...");
        // Re-generate random search pool
        GenerateNewSearchPool();
        // Reset text
        if (txtSearchName != null) txtSearchName.value = "";
        
        RefreshList();
    }

    private void GenerateNewSearchPool()
    {
        searchPool.Clear();
        string[] names = { "AnhDepTrai", "CoBeMuaDong", "NongDanTapSu", "GauMapU", "KiemSiHuyenThoai", "RongBay", "LamVuonVuiVe", "LuffyFarm" };
        string[] emojis = { "🧑", "👩", "🤠", "👦", "👧", "👨", "👩‍🦰", "👨‍🦳" };

        for (int i = 0; i < 5; i++)
        {
            var name = names[Random.Range(0, names.Length)] + Random.Range(10, 99);
            var emoji = emojis[Random.Range(0, emojis.Length)];
            var isMale = Random.value > 0.5f;
            var isOnline = Random.value > 0.4f;
            var level = Random.Range(5, 90);
            searchPool.Add(new FriendData(name, level, isMale, isOnline, emoji));
        }
    }

    // ── Public API ──

    public void Show()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
            SetActiveTab(tabFriends, TabType.Friends);
            Debug.Log("[Friends] Popup opened");
        }
    }

    public void Hide()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
            Debug.Log("[Friends] Popup closed");
        }
    }

    public bool IsVisible()
    {
        return overlay != null && overlay.style.display == DisplayStyle.Flex;
    }

    // ── Mock Data Generation ──

    private void InitMockData()
    {
        // 1. Friends list
        friendsList.Add(new FriendData("LamFarming", 32, true, true, "🧑‍🌾"));
        friendsList.Add(new FriendData("NgocVy123", 15, false, false, "👩‍🌾"));
        friendsList.Add(new FriendData("HaiLua", 44, true, true, "🤠"));
        friendsList.Add(new FriendData("MinhMap", 28, true, false, "🧑"));
        friendsList.Add(new FriendData("ThaoFarm", 21, false, true, "👩"));

        // 2. Pending requests
        pendingRequests.Add(new FriendData("SuperPlow", 18, true, true, "🚜"));
        pendingRequests.Add(new FriendData("PrincessFarming", 25, false, true, "👸"));
        pendingRequests.Add(new FriendData("TraiCayNgon", 12, true, false, "🍇"));

        // 3. Search pool (random players suggestion)
        searchPool.Add(new FriendData("GIRLChekvani59", 22, false, true, "👱‍♀️"));
        searchPool.Add(new FriendData("Hoa200288", 67, true, true, "👨"));
        searchPool.Add(new FriendData("Saramax68", 27, true, false, "👦"));
        searchPool.Add(new FriendData("Tucluong", 70, true, true, "👨‍🦰"));
        searchPool.Add(new FriendData("vantoan12", 10, true, true, "👨‍🦳"));
    }
}
