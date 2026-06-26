using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MailboxPopupController : MonoBehaviour
{
    [System.Serializable]
    public class AttachmentItem
    {
        public string itemName;
        public string itemEmoji;
        public string itemId;
        public string iconClass;
        public int amount;

        public AttachmentItem(string name, string emoji, int amt, string itemId = null, string iconClass = null)
        {
            itemName = name;
            itemEmoji = emoji;
            this.itemId = itemId;
            this.iconClass = iconClass;
            amount = amt;
        }
    }

    [System.Serializable]
    public class MailData
    {
        public string id;
        public string title;
        public string sender;
        public string date;
        public string content;
        public bool isRead;
        public bool hasReward;
        public bool isRewardClaimed;
        public List<AttachmentItem> attachments;

        public MailData(string id, string title, string sender, string date, string content, bool hasReward, List<AttachmentItem> attachments = null)
        {
            this.id = id;
            this.title = title;
            this.sender = sender;
            this.date = date;
            this.content = content;
            this.isRead = false;
            this.hasReward = hasReward;
            this.isRewardClaimed = false;
            this.attachments = attachments ?? new List<AttachmentItem>();
        }
    }

    [Header("References")]
    [SerializeField] private UIDocument mailboxDocument;
    private YWonderLand.Data.ItemDatabase itemDatabase;
    private VisualElement root;
    private VisualElement mailboxOverlay;
    private VisualElement mailboxPanel;
    
    // Left Column
    private ScrollView mailListScroll;
    private Button btnClaimAll;
    private Button btnDeleteRead;

    // Right Column
    private VisualElement detailEmptyState;
    private VisualElement detailContentState;
    private Label detailTitle;
    private Label detailMeta;
    private ScrollView detailBodyScroll;
    private Label detailBodyText;
    private VisualElement detailAttachmentSection;
    private VisualElement attachmentGrid;
    private Button btnClaimReward;
    private Button btnDeleteMail;
    private Button btnClose;

    // Data Storage
    private List<MailData> mailList = new List<MailData>();
    private MailData selectedMail = null;

    void Awake()
    {
        if (mailboxDocument == null)
        {
            mailboxDocument = GetComponent<UIDocument>();
        }

        if (mailboxDocument == null)
        {
            Debug.LogError("[MailboxPopup] UIDocument component not found!");
            return;
        }

        root = mailboxDocument.rootVisualElement;
        itemDatabase = Resources.Load<YWonderLand.Data.ItemDatabase>("ItemDatabase");
        QueryElements();
        RegisterCallbacks();
        
        // Load Initial Mock Data
        InitializeMockData();
        
        // Hide popup initially
        Hide();
    }

    private void QueryElements()
    {
        mailboxOverlay = root.Q<VisualElement>("MailboxOverlay");
        mailboxPanel = root.Q<VisualElement>("MailboxPanel");
        btnClose = root.Q<Button>("BtnClose");

        // Left Column
        mailListScroll = root.Q<ScrollView>("MailListScroll");
        btnClaimAll = root.Q<Button>("BtnClaimAll");
        btnDeleteRead = root.Q<Button>("BtnDeleteRead");

        // Right Column
        detailEmptyState = root.Q<VisualElement>("DetailEmptyState");
        detailContentState = root.Q<VisualElement>("DetailContentState");
        detailTitle = root.Q<Label>("DetailTitle");
        detailMeta = root.Q<Label>("DetailMeta");
        detailBodyScroll = root.Q<ScrollView>("DetailBodyScroll");
        detailBodyText = root.Q<Label>("DetailBodyText");
        detailAttachmentSection = root.Q<VisualElement>("DetailAttachmentSection");
        attachmentGrid = root.Q<VisualElement>("AttachmentGrid");
        btnClaimReward = root.Q<Button>("BtnClaimReward");
        btnDeleteMail = root.Q<Button>("BtnDeleteMail");
    }

    private void RegisterCallbacks()
    {
        // Close Button
        btnClose?.RegisterCallback<ClickEvent>(evt => Hide());

        // Click outside panel to close
        mailboxOverlay?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == mailboxOverlay)
            {
                Hide();
            }
        });

        // Bottom Controls
        btnClaimAll?.RegisterCallback<ClickEvent>(evt => ClaimAllRewards());
        btnDeleteRead?.RegisterCallback<ClickEvent>(evt => DeleteAllReadMails());

        // Mail Detail controls
        btnClaimReward?.RegisterCallback<ClickEvent>(evt => ClaimSelectedReward());
        btnDeleteMail?.RegisterCallback<ClickEvent>(evt => DeleteSelectedMail());
    }

    private void InitializeMockData()
    {
        mailList.Clear();

        // 1. Welcome Mail (With reward, unread)
        var welcomeGift = new List<AttachmentItem>
        {
            new AttachmentItem("Cá vàng", "🪙", 1500, iconClass: "mail-reward-pos"),
            new AttachmentItem("Hạt giống Cà rốt", "🥕", 10, itemId: "carrot_seed_01"),
            new AttachmentItem("Rùa con", "🐢", 1, itemId: "turtle_01")
        };
        mailList.Add(new MailData(
            "mail_welcome",
            "Quà Chào Mừng Tân Thủ!",
            "Hệ Thống",
            "10:00 02/06",
            "Chào mừng bạn đến với Y WONDER GREEN FARM! Đây là món quà nhỏ từ Ban Quản Trị để giúp bạn khởi nghiệp nông trại của mình thuận lợi hơn. Hãy chăm sóc cây trồng và kết bạn thật nhiều nhé!",
            true,
            welcomeGift
        ));

        // 2. System Maintenance (No reward, read)
        var maintenanceMail = new MailData(
            "mail_maintenance",
            "Báo Cáo Bảo Trì Định Kỳ",
            "Kỹ Thuật",
            "Hôm qua",
            "Hệ thống đã hoàn tất bảo trì định kỳ lúc 05:00 sáng. Chúng tôi đã nâng cấp server để đảm bảo game chạy mượt mà hơn và sửa một số lỗi hiển thị chữ nút bấm. Cảm ơn bạn đã đồng hành cùng chúng tôi!",
            false
        );
        maintenanceMail.isRead = true; // Mark as read initially
        mailList.Add(maintenanceMail);

        // 3. Compensation Mail (With reward, read & claimed)
        var compGift = new List<AttachmentItem>
        {
            new AttachmentItem("Cá vàng", "🪙", 500, iconClass: "mail-reward-pos"),
            new AttachmentItem("Phân bón siêu tốc", "🧪", 3, itemId: "fertilizer_01")
        };
        var compMail = new MailData(
            "mail_compensation",
            "Đền Bù Sự Cố Kết Nối",
            "Ban Quản Trị",
            "30/05/2026",
            "Chúng tôi chân thành xin lỗi vì sự cố mất kết nối mạng vào tối ngày 29/05. Gửi kèm theo đây là gói đền bù sự cố. Chúc bạn chơi game vui vẻ!",
            true,
            compGift
        );
        compMail.isRead = true;
        compMail.isRewardClaimed = true;
        mailList.Add(compMail);

        // 4. Weekly Ranking Event (With reward, unread)
        var rankGift = new List<AttachmentItem>
        {
            new AttachmentItem("Kim cương", "💎", 5, iconClass: "mail-reward-diamond")
        };
        mailList.Add(new MailData(
            "mail_weekly_rank",
            "Sự Kiện Đua Top Tuần",
            "Sự Kiện",
            "28/05/2026",
            "Chúc mừng bạn đã lọt vào Top 100 nông dân chăm chỉ tuần qua! Hãy nhận phần quà đính kèm để tiếp tục nỗ lực trong tuần mới.",
            true,
            rankGift
        ));

        // 5. Letter from Neighbor (No reward, unread)
        mailList.Add(new MailData(
            "mail_neighbor",
            "Lời chào từ người hàng xóm",
            "Lâm Farming",
            "25/05/2026",
            "Chào đằng ấy! Tớ thấy nông trại của đằng ấy rất đẹp. Khi nào rảnh hãy ghé thăm nông trại của tớ chơi nhé! Tớ có trồng rất nhiều dưa hấu chín mọng ngon lắm.",
            false
        ));
    }

    public void Show()
    {
        if (mailboxOverlay != null)
        {
            mailboxOverlay.style.display = DisplayStyle.Flex;
        }
        
        SelectMail(null); // Clear selected state on open
        RenderMailList();
        UpdateFooterButtons();
    }

    public void Hide()
    {
        if (mailboxOverlay != null)
        {
            mailboxOverlay.style.display = DisplayStyle.None;
        }
    }

    private void RenderMailList()
    {
        if (mailListScroll == null) return;
        mailListScroll.Clear();

        if (mailList.Count == 0)
        {
            Label emptyText = new Label("Hòm thư trống trơn!");
            emptyText.style.color = new Color(0.54f, 0.49f, 0.43f);
            emptyText.style.fontSize = 14;
            emptyText.style.unityFontStyleAndWeight = FontStyle.Bold;
            emptyText.style.marginTop = 20;
            emptyText.style.alignSelf = Align.Center;
            mailListScroll.Add(emptyText);
            return;
        }

        foreach (var mail in mailList)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("mail-card");
            
            // Apply selection/read states styling
            if (selectedMail != null && selectedMail.id == mail.id)
            {
                card.AddToClassList("selected");
            }
            if (!mail.isRead)
            {
                card.AddToClassList("unread");
                
                // Add unread dot badge
                VisualElement dot = new VisualElement();
                dot.AddToClassList("unread-badge");
                card.Add(dot);
            }

            // 1. Icon Container
            VisualElement iconContainer = new VisualElement();
            iconContainer.AddToClassList("mail-icon-container");
            iconContainer.AddToClassList(mail.isRead ? "read" : "unread");
            if (mail.isRead)
            {
                iconContainer.Add(CreateCheckMark("mail-read-check"));
            }
            card.Add(iconContainer);

            // 2. Content info text
            VisualElement contentContainer = new VisualElement();
            contentContainer.AddToClassList("mail-card-content");
            
            Label titleLabel = new Label(mail.title);
            titleLabel.AddToClassList("mail-card-title");
            if (!mail.isRead)
            {
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            
            Label senderLabel = new Label($"Từ: {mail.sender}");
            senderLabel.AddToClassList("mail-card-sender");
            
            contentContainer.Add(titleLabel);
            contentContainer.Add(senderLabel);
            card.Add(contentContainer);

            // 3. Right Panel: Date and Gift status badge
            VisualElement rightContainer = new VisualElement();
            rightContainer.AddToClassList("mail-card-right");
            
            Label dateLabel = new Label(mail.date);
            dateLabel.AddToClassList("mail-card-date");
            rightContainer.Add(dateLabel);

            if (mail.hasReward)
            {
                VisualElement giftBadge = new VisualElement();
                giftBadge.AddToClassList("mail-gift-badge");
                if (mail.isRewardClaimed)
                {
                    giftBadge.AddToClassList("claimed");
                }
                VisualElement giftIcon = mail.isRewardClaimed
                    ? CreateCheckMark("mail-gift-claimed-check")
                    : new VisualElement();
                giftIcon.AddToClassList("mail-gift-badge-icon");
                if (mail.isRewardClaimed)
                {
                    giftIcon.AddToClassList("claimed");
                }
                else
                {
                    giftIcon.AddToClassList("mail-reward-gift-icon");
                }
                giftBadge.Add(giftIcon);
                rightContainer.Add(giftBadge);
            }
            
            card.Add(rightContainer);

            // Register Card Click Callback
            card.RegisterCallback<ClickEvent>(evt => SelectMail(mail));

            mailListScroll.Add(card);
        }
    }

    private void SelectMail(MailData mail)
    {
        selectedMail = mail;
        
        // Render mail selection outlines again
        RenderMailList();

        if (mail == null)
        {
            detailEmptyState.style.display = DisplayStyle.Flex;
            detailContentState.style.display = DisplayStyle.None;
            return;
        }

        // Mark as read immediately when selected
        if (!mail.isRead)
        {
            mail.isRead = true;
            RenderMailList(); // re-render to update the envelope state icon
            UpdateFooterButtons();
        }

        detailEmptyState.style.display = DisplayStyle.None;
        detailContentState.style.display = DisplayStyle.Flex;

        // Set text content
        detailTitle.text = mail.title;
        detailMeta.text = $"Từ: {mail.sender}  |  {mail.date}";
        detailBodyText.text = mail.content;

        // Render attachments section
        if (mail.hasReward && mail.attachments != null && mail.attachments.Count > 0)
        {
            detailAttachmentSection.style.display = DisplayStyle.Flex;
            attachmentGrid.Clear();

            foreach (var item in mail.attachments)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList("attachment-slot");

                VisualElement icon = CreateAttachmentIcon(item);
                
                Label amount = new Label($"x{item.amount}");
                amount.AddToClassList("attachment-amount");

                slot.Add(icon);
                slot.Add(amount);
                attachmentGrid.Add(slot);
            }

            // Configure Claim Button
            btnClaimReward.style.display = DisplayStyle.Flex;
            if (mail.isRewardClaimed)
            {
                btnClaimReward.text = "Đã nhận";
                btnClaimReward.SetEnabled(false);
            }
            else
            {
                btnClaimReward.text = "Nhận quà";
                btnClaimReward.SetEnabled(true);
            }
        }
        else
        {
            // Hide attachments and claim button if not applicable
            detailAttachmentSection.style.display = DisplayStyle.None;
            btnClaimReward.style.display = DisplayStyle.None;
        }
    }

    private void ClaimSelectedReward()
    {
        if (selectedMail == null || !selectedMail.hasReward || selectedMail.isRewardClaimed) return;

        selectedMail.isRewardClaimed = true;
        
        // Simulated items payout notification
        string rewardsSummary = "";
        foreach (var item in selectedMail.attachments)
        {
            rewardsSummary += $"{item.itemEmoji} {item.itemName} x{item.amount}, ";
        }
        if (rewardsSummary.Length > 2) rewardsSummary = rewardsSummary.Substring(0, rewardsSummary.Length - 2);

        Debug.Log($"[Mailbox] Đã nhận quà thành công từ '{selectedMail.title}': {rewardsSummary}");
        
        // Refresh UI state
        SelectMail(selectedMail);
        UpdateFooterButtons();
    }

    private VisualElement CreateAttachmentIcon(AttachmentItem item)
    {
        if (!string.IsNullOrEmpty(item.iconClass))
        {
            var icon = new VisualElement();
            icon.AddToClassList("attachment-icon");
            icon.AddToClassList(item.iconClass);
            return icon;
        }

        var itemDef = !string.IsNullOrEmpty(item.itemId) && itemDatabase != null
            ? itemDatabase.GetItem(item.itemId)
            : null;

        if (itemDef != null && (itemDef.iconTexture != null || itemDef.iconSprite != null))
        {
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("attachment-icon");
            icon.AddToClassList("attachment-icon-image");

            if (itemDef.iconTexture != null)
                icon.image = itemDef.iconTexture;
            else
                icon.sprite = itemDef.iconSprite;

            return icon;
        }

        var fallback = new VisualElement();
        fallback.AddToClassList("attachment-icon");
        fallback.AddToClassList("mail-reward-gift-icon");
        return fallback;
    }

    private VisualElement CreateCheckMark(string className)
    {
        var check = new VisualElement();
        check.AddToClassList("mail-check-mark");
        check.AddToClassList(className);
        return check;
    }

    private void ClaimAllRewards()
    {
        int claimedCount = 0;
        string summary = "";
        
        foreach (var mail in mailList)
        {
            if (mail.hasReward && !mail.isRewardClaimed)
            {
                mail.isRewardClaimed = true;
                mail.isRead = true;
                claimedCount++;

                foreach (var item in mail.attachments)
                {
                    summary += $"{item.itemEmoji} {item.itemName} x{item.amount}\n";
                }
            }
        }

        if (claimedCount > 0)
        {
            Debug.Log($"[Mailbox] Nhận nhanh quà từ {claimedCount} hộp thư thành công!\nPhần thưởng thu hoạch:\n{summary}");
            
            // Refresh states
            if (selectedMail != null)
            {
                SelectMail(selectedMail);
            }
            else
            {
                RenderMailList();
            }
            UpdateFooterButtons();
        }
        else
        {
            Debug.Log("[Mailbox] Không có quà tặng nào mới để nhận!");
        }
    }

    private void DeleteSelectedMail()
    {
        if (selectedMail == null) return;

        Debug.Log($"[Mailbox] Đã xóa thư: '{selectedMail.title}'");
        mailList.Remove(selectedMail);
        SelectMail(null);
        RenderMailList();
        UpdateFooterButtons();
    }

    private void DeleteAllReadMails()
    {
        // Delete mails that are read AND (have no rewards OR rewards are already claimed)
        int removedCount = mailList.RemoveAll(mail => mail.isRead && (!mail.hasReward || mail.isRewardClaimed));
        
        if (removedCount > 0)
        {
            Debug.Log($"[Mailbox] Đã dọn dẹp {removedCount} thư đã đọc/đã nhận quà.");
            
            // Check if selected mail was removed
            if (selectedMail != null && !mailList.Contains(selectedMail))
            {
                SelectMail(null);
            }
            else
            {
                RenderMailList();
            }
            UpdateFooterButtons();
        }
        else
        {
            Debug.Log("[Mailbox] Không có thư đã đọc/nhận quà nào để dọn dẹp.");
        }
    }

    private void UpdateFooterButtons()
    {
        // Enable Claim All button only if there is at least one unclaimed reward
        bool hasUnclaimed = false;
        foreach (var mail in mailList)
        {
            if (mail.hasReward && !mail.isRewardClaimed)
            {
                hasUnclaimed = true;
                break;
            }
        }
        btnClaimAll?.SetEnabled(hasUnclaimed);

        // Enable Delete Read button only if there is at least one read mail that has no unclaimed reward
        bool hasDeletable = false;
        foreach (var mail in mailList)
        {
            if (mail.isRead && (!mail.hasReward || mail.isRewardClaimed))
            {
                hasDeletable = true;
                break;
            }
        }
        btnDeleteRead?.SetEnabled(hasDeletable);
    }
}
