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
        public string itemId;
        public string iconClass;

        public RewardItem(string name, string emoji, int amt, string itemId = null, string iconClass = null)
        {
            rewardName = name;
            rewardEmoji = emoji;
            amount = amt;
            this.itemId = itemId;
            this.iconClass = iconClass;
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
    private YWonderLand.Data.ItemDatabase itemDatabase;

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
        itemDatabase = Resources.Load<YWonderLand.Data.ItemDatabase>("ItemDatabase");
        QueryElements();
        RegisterCallbacks();

        // Danh sách nhiệm vụ bắt đầu TRỐNG (anh chốt 31/07). Trước đây nạp sẵn 5 nhiệm vụ bịa thời
        // dựng giao diện — "Tìm Ngôi Nhà Đầu Tiên", "Gieo Hạt Giống Cà Rốt" (còn hiện SẴN 10/10 chờ
        // nhận thưởng), chăm sóc thú, kết bạn... Khách mở ra là tưởng hệ nhiệm vụ đã chạy thật.
        // Hệ nhiệm vụ thật chưa làm; khi làm thì nạp vào questList rồi gọi RenderQuestList().

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

        // Chưa có nhiệm vụ nào thì phải NÓI RA. Bỏ mock đi mà không có dòng này thì popup mở ra
        // trống trơn, trông như hỏng. (Hòm thư vốn đã có sẵn dòng tương tự.)
        if (questList.Count == 0)
        {
            Label emptyText = new Label("Chưa có nhiệm vụ nào!");
            emptyText.style.color = new Color(0.54f, 0.49f, 0.43f);
            emptyText.style.fontSize = 14;
            emptyText.style.unityFontStyleAndWeight = FontStyle.Bold;
            emptyText.style.marginTop = 20;
            emptyText.style.alignSelf = Align.Center;
            questListScroll.Add(emptyText);
            return;
        }

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
            if (quest.isRewardClaimed)
            {
                iconContainer.AddToClassList("claimed");
                iconContainer.Add(CreateQuestCheckMark("quest-claimed-check"));
            }
            else
            {
                VisualElement icon = new VisualElement();
                icon.AddToClassList("quest-list-icon");
                icon.AddToClassList(quest.isCompleted ? "quest-icon-reward" : "quest-icon-mission");

                if (quest.isCompleted)
                {
                    iconContainer.AddToClassList("ready");
                }

                iconContainer.Add(icon);
            }
            card.Add(iconContainer);

            // 2. Info Content
            VisualElement info = new VisualElement();
            info.AddToClassList("quest-card-content");
            
            Label title = new Label(quest.title);
            title.AddToClassList("quest-card-title");
            
            Label progress = new Label($"Tiáº¿n trÃ¬nh: {quest.currentProgress}/{quest.targetProgress}");
            progress.AddToClassList("quest-card-progress-brief");

            info.Add(title);
            info.Add(progress);
            card.Add(info);

            // 3. Status Badge right
            VisualElement badge = new VisualElement();
            badge.AddToClassList("quest-card-status-badge");
            
            string statusTxt = "Äang lÃ m";
            if (quest.isRewardClaimed)
            {
                statusTxt = "ÄÃ£ nháº­n";
                badge.AddToClassList("claimed");
            }
            else if (quest.isCompleted)
            {
                statusTxt = "Nháº­n quÃ ";
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
        detailGiver.text = $"NgÆ°á»i giao: {quest.giver}";
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

                VisualElement icon = CreateRewardIcon(reward);

                Label amount = new Label($"x{reward.amount}");
                amount.AddToClassList("reward-amount");

                slot.Add(icon);
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
                btnClaimReward.text = "ÄÃ£ hoÃ n thÃ nh";
                btnClaimReward.SetEnabled(false);
            }
            else if (quest.isCompleted)
            {
                btnClaimReward.text = "Nháº­n thÆ°á»Ÿng";
                btnClaimReward.SetEnabled(true);
            }
            else
            {
                btnClaimReward.text = "Äang thá»±c hiá»‡n...";
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

        Debug.Log($"[Quest] Nháº­n pháº§n thÆ°á»Ÿng nhiá»‡m vá»¥ '{selectedQuest.title}' thÃ nh cÃ´ng: {summary}");

        SelectQuest(selectedQuest);
    }

    private VisualElement CreateRewardIcon(RewardItem reward)
    {
        if (!string.IsNullOrEmpty(reward.iconClass))
        {
            var classIcon = new VisualElement();
            classIcon.AddToClassList("reward-icon");
            classIcon.AddToClassList(reward.iconClass);
            return classIcon;
        }

        var itemDef = !string.IsNullOrEmpty(reward.itemId) && itemDatabase != null
            ? itemDatabase.GetItem(reward.itemId)
            : null;

        if (itemDef != null && (itemDef.iconTexture != null || itemDef.iconSprite != null))
        {
            var imageIcon = new Image { scaleMode = ScaleMode.ScaleToFit };
            imageIcon.AddToClassList("reward-icon");
            imageIcon.AddToClassList("reward-icon-image");

            if (itemDef.iconTexture != null)
                imageIcon.image = itemDef.iconTexture;
            else
                imageIcon.sprite = itemDef.iconSprite;

            return imageIcon;
        }

        var fallback = new VisualElement();
        fallback.AddToClassList("reward-icon");
        fallback.AddToClassList(GetFallbackRewardIconClass(reward));
        return fallback;
    }

    private string GetFallbackRewardIconClass(RewardItem reward)
    {
        switch (reward.rewardEmoji)
        {
            case "\U0001FA99":
                return "quest-reward-pos";
            case "\u2605":
                return "quest-reward-exp";
            case "\u2B50":
                return "quest-reward-exp";
            case "\U0001F48E":
                return "quest-reward-diamond";
            case "\U0001F4E6":
                return "quest-reward-gift";
            default:
                return "quest-reward-gift";
        }
    }

    private VisualElement CreateQuestCheckMark(string className)
    {
        var check = new VisualElement();
        check.AddToClassList("quest-check-mark");
        check.AddToClassList(className);
        return check;
    }
}
