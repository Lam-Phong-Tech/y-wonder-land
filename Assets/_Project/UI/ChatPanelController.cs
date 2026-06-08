using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

/// <summary>
/// Controller for the Collapsible Bottom-Center Chat UI.
/// Supports rate limiting, profanity filtering, and mock AI responses.
/// </summary>
public class ChatPanelController : MonoBehaviour
{
    public static ChatPanelController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIDocument chatDocument;

    // UI Elements
    private VisualElement root;
    private VisualElement chatCollapsed;
    private VisualElement chatExpandedShadow;
    private VisualElement chatExpanded;
    private Label lblLastMessage;
    private Button btnQuickEmoji;
    private Button btnExpand;
    private Button btnCollapse;
    private ScrollView scrollerHistory;
    private Button btnEmojiExpanded;
    private TextField inputMessage;
    private Button btnSend;

    // State Variables
    private bool isExpanded = false;
    private bool isChatVisible = true;
    private List<float> messageTimestamps = new List<float>();

    // Mock quick emojis
    private readonly string[] quickEmojis = { "🌾", "🐔", "🐟", "❤️", "👍", "🔥", "🐷", "🌻", "⭐" };

    // Profanity Filter words list
    private readonly List<string> badWords = new List<string> 
    { 
        "ngu", "fuck", "chửi", "dcm", "đm", "vl", "chó", "đách", "súc sinh" 
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (chatDocument == null)
            chatDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (chatDocument == null) return;

        root = chatDocument.rootVisualElement;

        // Query Collapsed Elements
        chatCollapsed = root.Q<VisualElement>("ChatCollapsed");
        lblLastMessage = root.Q<Label>("LblLastMessage");
        btnQuickEmoji = root.Q<Button>("BtnQuickEmoji");
        btnExpand = root.Q<Button>("BtnExpand");

        // Query Expanded Elements
        chatExpandedShadow = root.Q<VisualElement>("ChatExpandedShadow");
        chatExpanded = root.Q<VisualElement>("ChatExpanded");
        btnCollapse = root.Q<Button>("BtnCollapse");
        scrollerHistory = root.Q<ScrollView>("ScrollerHistory");
        btnEmojiExpanded = root.Q<Button>("BtnEmojiExpanded");
        inputMessage = root.Q<TextField>("InputMessage");
        btnSend = root.Q<Button>("BtnSend");

        RegisterCallbacks();
        
        // Initial state
        ToggleExpand(false);

        // Add initial system message
        ReceiveMessage("Hệ thống", "Chào mừng bạn đến với Y WONDER LAND! Hãy bắt đầu cuộc hành trình trồng trọt nào.", "#E67E22");
    }

    private void RegisterCallbacks()
    {
        // Collapse/Expand toggles
        btnExpand?.RegisterCallback<ClickEvent>(evt => ToggleExpand(true));
        btnCollapse?.RegisterCallback<ClickEvent>(evt => ToggleExpand(false));

        // Send actions
        btnSend?.RegisterCallback<ClickEvent>(evt => SendMessageFromInput());

        // Emoji buttons
        btnQuickEmoji?.RegisterCallback<ClickEvent>(evt => SendRandomEmoji());
        btnEmojiExpanded?.RegisterCallback<ClickEvent>(evt => SendRandomEmoji());

        // Text field enter key submit callback (UI Toolkit standard)
        inputMessage?.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                SendMessageFromInput();
                evt.StopPropagation();
            }
        });
    }

    /// <summary>
    /// Update logic to listen to global Keyboard Enter key.
    /// </summary>
    private void Update()
    {
        if (!isChatVisible) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.enterKey.wasPressedThisFrame)
        {
            if (!isExpanded)
            {
                // Expand and focus
                ToggleExpand(true);
                inputMessage?.Focus();
            }
            else
            {
                // If expanded but input is not focused, focus it
                var rootFocus = root.focusController.focusedElement;
                if (rootFocus != inputMessage)
                {
                    inputMessage?.Focus();
                }
                else
                {
                    // Input is focused. If empty, blur and collapse, else SendMessageFromInput handles it
                    if (string.IsNullOrEmpty(inputMessage.value))
                    {
                        inputMessage?.Blur();
                        ToggleExpand(false);
                    }
                    else
                    {
                        SendMessageFromInput();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Expand or Collapse the Chat panel.
    /// </summary>
    public void ToggleExpand(bool expand)
    {
        isExpanded = expand;

        if (!isChatVisible)
        {
            if (chatCollapsed != null) chatCollapsed.style.display = DisplayStyle.None;
            if (chatExpandedShadow != null) chatExpandedShadow.style.display = DisplayStyle.None;
            return;
        }

        if (expand)
        {
            if (chatCollapsed != null) chatCollapsed.style.display = DisplayStyle.None;
            if (chatExpandedShadow != null) chatExpandedShadow.style.display = DisplayStyle.Flex;
            ScrollToBottom();
        }
        else
        {
            if (chatCollapsed != null) chatCollapsed.style.display = DisplayStyle.Flex;
            if (chatExpandedShadow != null) chatExpandedShadow.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Show or hide the entire Chat UI completely.
    /// </summary>
    public void SetChatVisible(bool visible)
    {
        isChatVisible = visible;
        
        if (visible)
        {
            ToggleExpand(isExpanded);
        }
        else
        {
            if (chatCollapsed != null) chatCollapsed.style.display = DisplayStyle.None;
            if (chatExpandedShadow != null) chatExpandedShadow.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Send a message typed in the text input field.
    /// </summary>
    private void SendMessageFromInput()
    {
        if (inputMessage == null) return;
        string rawText = inputMessage.value;
        if (string.IsNullOrEmpty(rawText)) return;

        // Reset input field
        inputMessage.value = "";
        
        // Focus again for continuous typing
        inputMessage.Focus();

        // 1. Rate Limit check (max 5 messages in 30 seconds)
        float now = Time.time;
        messageTimestamps.RemoveAll(t => now - t > 30f); // Clean old timestamps
        
        if (messageTimestamps.Count >= 5)
        {
            ReceiveMessage("Hệ thống", "Bạn đang gửi tin nhắn quá nhanh. Vui lòng đợi 30 giây để tiếp tục!", "#FF4B4B");
            return;
        }

        messageTimestamps.Add(now);

        // 2. Profanity Filter
        string cleanText = ApplyProfanityFilter(rawText);

        // 3. Post Message
        ReceiveMessage("Bạn", cleanText, "#E5A93C");
        
        // 4. Update collapsed preview
        if (lblLastMessage != null)
        {
            lblLastMessage.text = $"Bạn: {cleanText}";
        }

        // 5. Trigger Mock AI Response
        StartCoroutine(HandleAIResponseCheck(cleanText.ToLower()));
    }

    /// <summary>
    /// Send a random fun emoji to chat.
    /// </summary>
    private void SendRandomEmoji()
    {
        string emoji = quickEmojis[Random.Range(0, quickEmojis.Length)];
        ReceiveMessage("Bạn", emoji, "#E5A93C");
        
        if (lblLastMessage != null)
        {
            lblLastMessage.text = $"Bạn: {emoji}";
        }

        StartCoroutine(HandleAIResponseCheck(emoji));
    }

    /// <summary>
    /// Append a message bubble to the history scrollview.
    /// </summary>
    public void ReceiveMessage(string sender, string message, string senderColorHex)
    {
        if (scrollerHistory == null) return;

        // Container row
        VisualElement msgRow = new VisualElement();
        msgRow.AddToClassList("chat-msg-row");

        // Sender Label
        Label senderLabel = new Label($"{sender}: ");
        senderLabel.AddToClassList("chat-msg-sender");
        senderLabel.style.color = ParseColor(senderColorHex);

        // Message Label
        Label textLabel = new Label(message);
        textLabel.AddToClassList("chat-msg-text");

        msgRow.Add(senderLabel);
        msgRow.Add(textLabel);

        scrollerHistory.Add(msgRow);

        // Update Collapsed text if not sent by player
        if (sender != "Bạn" && lblLastMessage != null)
        {
            lblLastMessage.text = $"{sender}: {message}";
        }

        ScrollToBottom();
    }

    /// <summary>
    /// Filters bad words case-insensitively.
    /// </summary>
    private string ApplyProfanityFilter(string text)
    {
        string filtered = text;
        foreach (string word in badWords)
        {
            // Case-insensitive replacement
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(
                @"\b" + System.Text.RegularExpressions.Regex.Escape(word) + @"\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            filtered = regex.Replace(filtered, "***");
        }
        return filtered;
    }

    /// <summary>
    /// Coroutine to simulate network/AI response delay.
    /// </summary>
    private IEnumerator HandleAIResponseCheck(string triggerText)
    {
        yield return new WaitForSeconds(2.0f); // 2 seconds delay

        string aiMsg = "";
        
        if (triggerText.Contains("hello") || triggerText.Contains("xin chào") || triggerText.Contains("hi"))
        {
            aiMsg = "Chào bạn! Chúc bạn một ngày tốt lành tại Y WONDER LAND! 🌻";
        }
        else if (triggerText.Contains("nông trại") || triggerText.Contains("farm"))
        {
            aiMsg = "Nông trại của bạn là nơi tuyệt vời để trồng lúa nước và nuôi heo đất đấy! 🌾";
        }
        else if (triggerText.Contains("shop") || triggerText.Contains("mua"))
        {
            aiMsg = "Bạn có thể nhấp vào biểu tượng 🛒 ở thanh bên trái để mua các hạt giống xịn sò nhé!";
        }
        else if (triggerText.Contains("bản đồ") || triggerText.Contains("map"))
        {
            aiMsg = "Hãy mở Bản đồ 🗺️ trên thanh bên trái để đi khai mỏ hoặc tham quan các hòn đảo kỳ bí nhé!";
        }
        else if (triggerText.Contains("heo đất") || triggerText.Contains("piggy"))
        {
            aiMsg = "Heo Đất 🐷 giúp bạn tiết kiệm và đẻ lãi vàng. Hãy đầu tư gói dài hạn để lời nhiều nhé!";
        }
        else if (triggerText.Contains("🌾") || triggerText.Contains("🐔") || triggerText.Contains("🐟"))
        {
            aiMsg = "Haha! Tôi cũng thích các hoạt động nông nghiệp như bạn vậy! 👍";
        }
        else if (triggerText.Contains("***"))
        {
            aiMsg = "Hãy văn minh trong kênh chat thế giới nha bạn ơi! ❤️";
        }
        else
        {
            // Random general tip
            string[] tips = {
                "Mẹo nhỏ: Hãy nâng cấp công cụ để khai thác đá nhanh hơn!",
                "Bạn có biết: Thú cưng sẽ tự động nhặt tài nguyên giúp bạn không?",
                "Kết bạn thật nhiều để trao đổi nông sản qua Hòm thư nhé!",
                "Lên cấp sẽ nhận được nhiều quà tặng độc quyền tại Y WONDER LAND đó."
            };
            aiMsg = tips[Random.Range(0, tips.Length)];
        }

        ReceiveMessage("AI Hướng Dẫn", aiMsg, "#2ECC71");
    }

    /// <summary>
    /// Force scrollview to scroll to bottom after layout updates.
    /// </summary>
    private void ScrollToBottom()
    {
        if (scrollerHistory == null) return;
        scrollerHistory.RegisterCallback<GeometryChangedEvent>(OnScrollHistoryGeometryChanged);
    }

    private void OnScrollHistoryGeometryChanged(GeometryChangedEvent evt)
    {
        scrollerHistory.UnregisterCallback<GeometryChangedEvent>(OnScrollHistoryGeometryChanged);
        scrollerHistory.scrollOffset = new Vector2(0, scrollerHistory.contentContainer.layout.height);
    }

    /// <summary>
    /// Helper to parse color hex strings.
    /// </summary>
    private Color ParseColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        return Color.white;
    }
}
