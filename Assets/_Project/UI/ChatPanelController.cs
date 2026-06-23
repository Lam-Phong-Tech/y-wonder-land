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

    // Emote Popup Elements
    private VisualElement emotePopup;
    private Button btnEmoteWave;
    private Button btnEmotePoint;
    private Button btnEmoteLaugh;
    private Button btnEmoteDance;

    // State Variables
    private bool isExpanded = false;
    private bool isChatVisible = true;
    private bool wasPopupOpen = false; // theo dõi trạng thái popup để ẩn/hiện chat
    private bool keyboardShifted = false; // mobile: chat đang được đẩy lên tránh bàn phím mềm
    private float? buildModeBottom = null; // !=null khi build mode đẩy chat lên (để khôi phục sau khi gõ xong)
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

        // Query Emote Elements
        emotePopup = root.Q<VisualElement>("EmotePopup");
        btnEmoteWave = root.Q<Button>("BtnEmoteWave");
        btnEmotePoint = root.Q<Button>("BtnEmotePoint");
        btnEmoteLaugh = root.Q<Button>("BtnEmoteLaugh");
        btnEmoteDance = root.Q<Button>("BtnEmoteDance");

        RegisterCallbacks();
        
        // Initial state
        ToggleExpand(false);

        // Add initial system message
        ReceiveMessage("Hệ thống", "Chào mừng bạn đến với Y WONDER GREEN FARM! Hãy bắt đầu cuộc hành trình trồng trọt nào.", "#E67E22");
    }

    private void RegisterCallbacks()
    {
        // Collapse/Expand toggles
        btnExpand?.RegisterCallback<ClickEvent>(evt => ToggleExpand(true));
        btnCollapse?.RegisterCallback<ClickEvent>(evt => ToggleExpand(false));

        // Click on the collapsed message text to expand and type immediately
        lblLastMessage?.RegisterCallback<ClickEvent>(evt => 
        {
            ToggleExpand(true);
            inputMessage?.Focus();
        });

        // Send actions
        btnSend?.RegisterCallback<ClickEvent>(evt => SendMessageFromInput());

        // Emoji buttons
        btnQuickEmoji?.RegisterCallback<ClickEvent>(evt => ToggleEmotePopup());
        btnEmojiExpanded?.RegisterCallback<ClickEvent>(evt => ToggleEmotePopup());

        // Emote actions
        btnEmoteWave?.RegisterCallback<ClickEvent>(evt => PlayEmote("Waving", 2.0f));
        btnEmotePoint?.RegisterCallback<ClickEvent>(evt => PlayEmote("Pointing", 2.0f));
        btnEmoteLaugh?.RegisterCallback<ClickEvent>(evt => PlayEmote("Laughing", 2.0f));
        btnEmoteDance?.RegisterCallback<ClickEvent>(evt => PlayEmote("Dancing", 2.0f));

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
        // Đang mở popup (Settings/Túi/Shop...) → ẨN chat để không đè lên nhau; đóng popup → hiện lại.
        bool popupOpen = UIPopupTracker.AnyOpen;
        if (popupOpen != wasPopupOpen)
        {
            wasPopupOpen = popupOpen;
            if (root != null) root.style.display = popupOpen ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (popupOpen) return; // chat đang ẩn → bỏ qua phím Enter

        UpdateKeyboardAvoidance(); // mobile: đẩy chat LÊN khi bàn phím mềm che ô nhập

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
            if (emotePopup != null) emotePopup.style.display = DisplayStyle.None; // Hide emote popup when collapsing
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
    /// Check if the chat input field is currently focused by the user.
    /// </summary>
    public bool IsTyping()
    {
        if (inputMessage == null || inputMessage.focusController == null) return false;
        return inputMessage.focusController.focusedElement == inputMessage;
    }

    /// <summary>
    /// Toggle the visibility of the Emote Popup grid.
    /// </summary>
    private void ToggleEmotePopup()
    {
        if (emotePopup == null) return;
        bool isCurrentlyVisible = emotePopup.style.display == DisplayStyle.Flex;
        emotePopup.style.display = isCurrentlyVisible ? DisplayStyle.None : DisplayStyle.Flex;
    }

    /// <summary>
    /// Play a social animation via PlayerController.
    /// </summary>
    private void PlayEmote(string animName, float duration)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.PlayActionAnimation(animName, duration);
        }
        else
        {
            Debug.LogWarning($"[ChatPanel] Không tìm thấy PlayerController để thực hiện hành động: {animName}");
        }

        // Tự động đóng popup sau khi chọn
        if (emotePopup != null)
        {
            emotePopup.style.display = DisplayStyle.None;
        }
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
            aiMsg = "Chào bạn! Chúc bạn một ngày tốt lành tại Y WONDER GREEN FARM! 🌻";
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
                "Lên cấp sẽ nhận được nhiều quà tặng độc quyền tại Y WONDER GREEN FARM đó."
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
    /// Shift the chat panel up when Build Mode is active to avoid overlapping build items.
    /// </summary>
    public void ShiftForBuildMode(bool isBuildMode)
    {
        buildModeBottom = isBuildMode ? 136f : (float?)null; // 136 = trên thanh vật liệu build 120px
        if (!keyboardShifted) ApplyBaseBottom(); // đang gõ (bàn phím đẩy lên) thì giữ, khôi phục sau
    }

    // Trả chat về đáy "nền" hiện tại: build mode (136) hoặc mặc định USS (16px).
    private void ApplyBaseBottom()
    {
        if (root == null) return;
        if (buildModeBottom.HasValue) root.style.bottom = buildModeBottom.Value;
        else root.style.bottom = StyleKeyword.Null;
    }

    // Mobile: khi focus ô chat, bàn phím mềm bật lên che ô nhập → đẩy panel chat LÊN trên bàn phím.
    // PC không có bàn phím mềm nên không kích hoạt. CẦN TEST TRÊN MÁY THẬT: TouchScreenKeyboard.area
    // chỉ có giá trị trên thiết bị (Editor/Simulator luôn = 0 → fallback ước lượng 45% chiều cao màn).
    private void UpdateKeyboardAvoidance()
    {
        if (root == null) return;

        bool typing = IsTyping();
        bool softKeyboard = TouchScreenKeyboard.visible || Application.isMobilePlatform;

        if (typing && softKeyboard)
        {
            float panelH = (root.panel != null) ? root.panel.visualTree.layout.height : 720f;
            float kbScreenH = TouchScreenKeyboard.area.height;
            if (kbScreenH < 1f) kbScreenH = Screen.height * 0.45f; // .area chưa trả → ước lượng ~45% màn
            float kbPanel = kbScreenH / Mathf.Max(1f, Screen.height) * panelH;
            root.style.bottom = kbPanel + 16f; // ngồi ngay trên bàn phím + lề
            keyboardShifted = true;
        }
        else if (keyboardShifted)
        {
            keyboardShifted = false;
            ApplyBaseBottom(); // bàn phím đóng → về nền (build mode hoặc mặc định)
        }
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
