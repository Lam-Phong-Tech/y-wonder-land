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

        /// <summary>Khác rỗng = thư báo CON VẬT CHẾT, sinh từ FarmActivityLog (không phải thư hệ thống).
        /// Đọc/xoá thư này phải ghi ngược lại nhật ký, kẻo mở lại hòm thư nó hiện lên như mới.</summary>
        public string deathLogId;

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

        // Hòm thư bắt đầu TRỐNG (anh chốt 31/07). Trước đây nạp sẵn 5 thư mẫu — quà tân thủ, báo
        // bảo trì, đền bù, đua top, thư hàng xóm — toàn là chữ bịa thời dựng giao diện, khách xem
        // build là tưởng tính năng thật. Giờ chỉ còn thư THẬT do game sinh ra: báo cây/thú chết
        // (xem SyncDeathMails). Có thư hệ thống thật thì nối vào cùng chỗ đó.

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

    public void Show()
    {
        if (mailboxOverlay != null)
        {
            mailboxOverlay.style.display = DisplayStyle.Flex;
        }

        SyncDeathMails();  // nạp thư báo con vật chết trước khi vẽ danh sách
        SelectMail(null); // Clear selected state on open
        RenderMailList();
        UpdateFooterButtons();
    }

    /// <summary>
    /// Dựng lại danh sách thư báo CON VẬT CHẾT từ nhật ký nông trại (khách chốt 30/07).
    /// Con chết thì không bấm vào đâu mà xem được nữa, nên báo về đây. Xếp MỚI NHẤT lên đầu.
    /// </summary>
    private void SyncDeathMails()
    {
        // Bỏ hết thư chết cũ rồi dựng lại — tránh nhân đôi khi mở hòm thư nhiều lần.
        mailList.RemoveAll(m => !string.IsNullOrEmpty(m.deathLogId));

        var deaths = YWonderLand.Environment.FarmActivityLog.GetDeaths(); // mới nhất trước
        for (int i = 0; i < deaths.Count; i++)
        {
            var d = deaths[i];
            string reason = string.IsNullOrEmpty(d.reason) ? "đã chết" : $"đã {d.reason}";
            string when = YWonderLand.Environment.FarmActivityLog.FormatWhen(d.unixTime);
            // Nhiều cái chết cùng loại sát giờ nhau đã được gộp thành 1 mục (xem FarmActivityLog.RecordDeath).
            string howMany = d.count > 1 ? $" ({d.count} cái)" : "";

            var mail = new MailData(
                d.id,
                $"{d.subjectName} {reason}{howMany}",
                "Nông trại",
                when,
                $"{d.subjectName} của bạn {reason} lúc {when}{howMany}.\n\n" +
                "Nhớ cho thú ăn và tưới cây trước khi thanh máu cạn để khỏi mất nhé!",
                false
            );
            mail.isRead = d.isRead;
            mail.deathLogId = d.id;
            mailList.Insert(i, mail); // chèn lên đầu, giữ đúng thứ tự mới -> cũ
        }
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
            // Thư báo chết phải ghi "đã đọc" xuống nhật ký, không thì lần sau mở lại vẫn là thư mới.
            if (!string.IsNullOrEmpty(mail.deathLogId))
                YWonderLand.Environment.FarmActivityLog.MarkDeathRead(mail.deathLogId);
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
        // Xoá thư báo chết = xoá luôn khỏi nhật ký, không thì mở lại hòm thư nó hiện lại.
        if (!string.IsNullOrEmpty(selectedMail.deathLogId))
            YWonderLand.Environment.FarmActivityLog.RemoveDeath(selectedMail.deathLogId);
        mailList.Remove(selectedMail);
        SelectMail(null);
        RenderMailList();
        UpdateFooterButtons();
    }

    private void DeleteAllReadMails()
    {
        // Delete mails that are read AND (have no rewards OR rewards are already claimed)
        int removedCount = mailList.RemoveAll(mail =>
        {
            if (!mail.isRead || (mail.hasReward && !mail.isRewardClaimed)) return false;
            // Dọn thư báo chết thì xoá luôn khỏi nhật ký cho khớp.
            if (!string.IsNullOrEmpty(mail.deathLogId))
                YWonderLand.Environment.FarmActivityLog.RemoveDeath(mail.deathLogId);
            return true;
        });
        
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
