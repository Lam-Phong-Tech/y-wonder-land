using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class QuestPopupController : MonoBehaviour
{
    [System.Serializable]
    public class RewardItem
    {
        public string rewardName;
        public string rewardEmoji;
        public int amount;

        public RewardItem(string name, string emoji, int amt)
        {
            rewardName = name;
            rewardEmoji = emoji;
            amount = amt;
        }
    }

    [System.Serializable]
    public class QuestData
    {
        public string id;
        public string title;
        public string giver;
        public string description;
        public int currentProgress;
        public int targetProgress;
        public bool isCompleted;
        public bool isRewardClaimed;
        public List<RewardItem> rewards;

        public QuestData(string id, string title, string giver, string desc, int current, int target, List<RewardItem> rewards = null)
        {
            this.id = id;
            this.title = title;
            this.giver = giver;
            this.description = desc;
            this.currentProgress = current;
            this.targetProgress = target;
            this.isCompleted = current >= target;
            this.isRewardClaimed = false;
            this.rewards = rewards ?? new List<RewardItem>();
        }

        public void AddProgress(int amount)
        {
            currentProgress = Mathf.Clamp(currentProgress + amount, 0, targetProgress);
            isCompleted = currentProgress >= targetProgress;
        }
    }

    [Header("References")]
    [SerializeField] private UIDocument questDocument;

    private VisualElement root;
    private VisualElement questOverlay;
    private Button btnClose;

    // Left Column
    private ScrollView questListScroll;

    // Right Column
    private VisualElement detailEmptyState;
    private VisualElement detailContentState;
    private Label detailTitle;
    private Label detailGiver;
    private Label detailDescription;
    
    // Progress
    private VisualElement questProgressFill;
    private Label questProgressText;

    // Rewards
    private VisualElement questRewardSection;
    private VisualElement rewardGrid;
    private Button btnClaimReward;

    // Data Storage
    private List<QuestData> questList = new List<QuestData>();
    private QuestData selectedQuest = null;

    void Awake()
    {
        if (questDocument == null)
        {
            questDocument = GetComponent<UIDocument>();
        }

        if (questDocument == null)
        {
            Debug.LogError("[QuestPopup] UIDocument component not found!");
            return;
        }

        root = questDocument.rootVisualElement;
        QueryElements();
        RegisterCallbacks();

        // Load Mock Data
        InitializeMockData();

        // Hide initially
        Hide();
    }

    private void QueryElements()
    {
        questOverlay = root.Q<VisualElement>("QuestOverlay");
        btnClose = root.Q<Button>("BtnClose");

        // Left Column
        questListScroll = root.Q<ScrollView>("QuestListScroll");

        // Right Column
        detailEmptyState = root.Q<VisualElement>("DetailEmptyState");
        detailContentState = root.Q<VisualElement>("DetailContentState");
        detailTitle = root.Q<Label>("DetailTitle");
        detailGiver = root.Q<Label>("DetailGiver");
        detailDescription = root.Q<Label>("DetailDescription");

        // Progress
        questProgressFill = root.Q<VisualElement>("QuestProgressFill");
        questProgressText = root.Q<Label>("QuestProgressText");

        // Rewards
        questRewardSection = root.Q<VisualElement>("QuestRewardSection");
        rewardGrid = root.Q<VisualElement>("RewardGrid");
        btnClaimReward = root.Q<Button>("BtnClaimReward");
    }

    private void RegisterCallbacks()
    {
        // Close Button
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());

        // Click outside overlay to close
        questOverlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == questOverlay)
            {
                Hide();
            }
        });

        // Claim Reward
        btnClaimReward?.RegisterCallback<ClickEvent>(evt => ClaimSelectedQuestReward());
    }

    private void InitializeMockData()
    {
        questList.Clear();

        // 1. Village Discovery (In Progress)
        var welcomeReward = new List<RewardItem>
        {
            new RewardItem("Cá vàng", "🪙", 500),
            new RewardItem("Kinh nghiệm", "★", 100)
        };
        questList.Add(new QuestData(
            "quest_discovery",
            "Tìm Ngôi Nhà Đầu Tiên",
            "Trưởng Làng",
            "Khám phá các ngóc ngách của đảo hoang và tìm kiếm ngôi nhà cổ kính đầu tiên để mở khóa các tính năng chế tạo công cụ.",
            0,
            1,
            welcomeReward
        ));

        // 2. Carrot Planting (Completed, Ready to Claim)
        var carrotReward = new List<RewardItem>
        {
            new RewardItem("Cá vàng", "🪙", 300),
            new RewardItem("Hộp gỗ", "📦", 1)
        };
        questList.Add(new QuestData(
            "quest_carrot",
            "Gieo Hạt Giống Cà Rốt",
            "Lâm Nông Dân",
            "Lâm đang rất cần cà rốt để nấu món súp cho lễ hội. Hãy mua hạt giống tại cửa hàng, gieo 10 hạt giống cà rốt lên các ô đất trống trong vườn.",
            10,
            10,
            carrotReward
        ));

        // 3. Pet Care (In Progress)
        var petReward = new List<RewardItem>
        {
            new RewardItem("Cá vàng", "🪙", 200),
            new RewardItem("Quả táo", "🍎", 2)
        };
        questList.Add(new QuestData(
            "quest_pet",
            "Chăm sóc Rùa con",
            "Vy Vy",
            "Hãy cho Rùa con ăn đầy đủ các bữa để tăng điểm thân mật. Rùa con khi vui vẻ sẽ giúp bạn tìm kiếm các vật phẩm ẩn dưới lòng đất.",
            1,
            3,
            petReward
        ));

        // 4. Connect with Neighbors (Claimed)
        var connectReward = new List<RewardItem>
        {
            new RewardItem("Cá vàng", "🪙", 100),
            new RewardItem("Kim cương", "💎", 1)
        };
        var finishedQuest = new QuestData(
            "quest_neighbors",
            "Giao lưu kết bạn",
            "Hệ Thống",
            "Mở danh sách Bạn bè và gửi lời mời kết bạn với ít nhất một người chơi khác trên bản đồ để bắt đầu hoạt động giao thương nông sản.",
            1,
            1,
            connectReward
        );
        finishedQuest.isRewardClaimed = true;
        questList.Add(finishedQuest);
    }

    public void Show()
    {
        if (questOverlay != null)
        {
            questOverlay.style.display = DisplayStyle.Flex;
        }

        SelectQuest(null);
        RenderQuestList();
    }

    public void Hide()
    {
        if (questOverlay != null)
        {
            questOverlay.style.display = DisplayStyle.None;
        }
    }

    private void RenderQuestList()
    {
        if (questListScroll == null) return;
        questListScroll.Clear();

        foreach (var quest in questList)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("quest-card");

            if (selectedQuest != null && selectedQuest.id == quest.id)
            {
                card.AddToClassList("selected");
            }
            if (quest.isCompleted && !quest.isRewardClaimed)
            {
                card.AddToClassList("completed-ready");
            }

            // 1. Icon Container
            VisualElement iconContainer = new VisualElement();
            iconContainer.AddToClassList("quest-icon-container");
            string iconStr = "⚔️";
            if (quest.isRewardClaimed) iconStr = "🎁";
            else if (quest.isCompleted) iconStr = "✔️";
            Label iconLabel = new Label(iconStr);
            iconLabel.AddToClassList("quest-icon-label");
            if (quest.isCompleted && !quest.isRewardClaimed)
            {
                iconLabel.AddToClassList("completed");
            }
            iconContainer.Add(iconLabel);
            card.Add(iconContainer);

            // 2. Info Content
            VisualElement info = new VisualElement();
            info.AddToClassList("quest-card-content");
            
            Label title = new Label(quest.title);
            title.AddToClassList("quest-card-title");
            
            Label progress = new Label($"Tiến trình: {quest.currentProgress}/{quest.targetProgress}");
            progress.AddToClassList("quest-card-progress-brief");

            info.Add(title);
            info.Add(progress);
            card.Add(info);

            // 3. Status Badge right
            VisualElement badge = new VisualElement();
            badge.AddToClassList("quest-card-status-badge");
            
            string statusTxt = "Đang làm";
            if (quest.isRewardClaimed)
            {
                statusTxt = "Đã nhận";
                badge.AddToClassList("claimed");
            }
            else if (quest.isCompleted)
            {
                statusTxt = "Nhận quà";
                badge.AddToClassList("ready");
            }
            
            Label statusLabel = new Label(statusTxt);
            statusLabel.AddToClassList("quest-card-status-text");
            badge.Add(statusLabel);
            card.Add(badge);

            // Register Click Event
            card.RegisterCallback<ClickEvent>(evt => SelectQuest(quest));

            questListScroll.Add(card);
        }
    }

    private void SelectQuest(QuestData quest)
    {
        selectedQuest = quest;
        RenderQuestList();

        if (quest == null)
        {
            detailEmptyState.style.display = DisplayStyle.Flex;
            detailContentState.style.display = DisplayStyle.None;
            return;
        }

        detailEmptyState.style.display = DisplayStyle.None;
        detailContentState.style.display = DisplayStyle.Flex;

        detailTitle.text = quest.title;
        detailGiver.text = $"Người giao: {quest.giver}";
        detailDescription.text = quest.description;

        // Progress bar calculation
        float pct = ((float)quest.currentProgress / quest.targetProgress) * 100f;
        if (questProgressFill != null) questProgressFill.style.width = Length.Percent(pct);
        if (questProgressText != null) questProgressText.text = $"{quest.currentProgress} / {quest.targetProgress}";

        // Rewards grid
        if (quest.rewards != null && quest.rewards.Count > 0)
        {
            questRewardSection.style.display = DisplayStyle.Flex;
            rewardGrid.Clear();

            foreach (var reward in quest.rewards)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList("reward-slot");

                Label emoji = new Label(reward.rewardEmoji);
                emoji.AddToClassList("reward-emoji");

                Label amount = new Label($"x{reward.amount}");
                amount.AddToClassList("reward-amount");

                slot.Add(emoji);
                slot.Add(amount);
                rewardGrid.Add(slot);
            }
        }
        else
        {
            questRewardSection.style.display = DisplayStyle.None;
        }

        // Action button config
        if (btnClaimReward != null)
        {
            if (quest.isRewardClaimed)
            {
                btnClaimReward.text = "Đã hoàn thành";
                btnClaimReward.SetEnabled(false);
            }
            else if (quest.isCompleted)
            {
                btnClaimReward.text = "Nhận thưởng";
                btnClaimReward.SetEnabled(true);
            }
            else
            {
                btnClaimReward.text = "Đang thực hiện...";
                btnClaimReward.SetEnabled(false);
            }
        }
    }

    private void ClaimSelectedQuestReward()
    {
        if (selectedQuest == null || !selectedQuest.isCompleted || selectedQuest.isRewardClaimed) return;

        selectedQuest.isRewardClaimed = true;

        string summary = "";
        foreach (var item in selectedQuest.rewards)
        {
            summary += $"{item.rewardEmoji} {item.rewardName} x{item.amount}, ";
        }
        if (summary.Length > 2) summary = summary.Substring(0, summary.Length - 2);

        Debug.Log($"[Quest] Nhận phần thưởng nhiệm vụ '{selectedQuest.title}' thành công: {summary}");

        SelectQuest(selectedQuest);
    }
}
