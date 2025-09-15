using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private VisualElement _chatIconArea;
    private VisualElement _chatPanel;
    private VisualElement _chatMessages;
    private TextField _chatInput;
    private Button _sendButton;
    
    [Header("Settings")]
    public int maxChatMessages = 50;
    public float messageDisplayDuration = 10f;
    public bool enableQuickChat = true;
    
    [Header("Quick Chat Messages")]
    public string[] quickChatMessages = {
        "도와줘!",
        "근처야",
        "위험!",
        "따라와!",
        "가는중!",
        "해결됨"
    };
    
    [Header("Current State")]
    public bool isChatOpen = false;
    public List<ChatMessage> chatHistory = new List<ChatMessage>();
    
    // UI 이벤트 콜백 저장 변수들
    private EventCallback<ClickEvent> _chatIconAreaCallback;
    private EventCallback<KeyDownEvent> _chatInputKeyDownCallback;
    
    public static System.Action<string, string> OnChatMessageSent; // playerId, message
    public static System.Action<ChatMessage> OnChatMessageReceived;
    public static System.Action<bool> OnChatToggled;
    
    [System.Serializable]
    public class ChatMessage
    {
        public string playerId;
        public string playerName;
        public string message;
        public System.DateTime timestamp;
        public ChatMessageType type;
        
        public ChatMessage(string id, string name, string msg, ChatMessageType msgType = ChatMessageType.Normal)
        {
            playerId = id;
            playerName = name;
            message = msg;
            timestamp = System.DateTime.Now;
            type = msgType;
        }
    }
    
    public enum ChatMessageType
    {
        Normal,
        System,
        QuickChat,
        Warning,
        Death,
        Escape
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        SetupChatEvents();
        // CreateChatPanel(); // 나중에 구현
        // SetChatVisibility(false); // 나중에 구현
        Debug.Log("[ChatManager] 초기화 완료 - 터치 감지만 활성화");
    }
    
    private void CacheUIElements()
    {
        _chatIconArea = _root.Q<VisualElement>("chatIconArea");
    }
    
    private void SetupChatEvents()
    {
        if (_chatIconArea != null)
        {
            // 콜백 인스턴스 생성 및 저장
            _chatIconAreaCallback = evt => ToggleChat();
            
            // 저장된 콜백으로 등록
            _chatIconArea.RegisterCallback(_chatIconAreaCallback);
        }
    }
    
    private void CreateChatPanel()
    {
        // 채팅 패널을 동적으로 생성
        _chatPanel = new VisualElement();
        _chatPanel.name = "chatPanel";
        _chatPanel.AddToClassList("chat-panel");
        
        // 채팅 패널 스타일 설정
        _chatPanel.style.position = Position.Absolute;
        _chatPanel.style.right = 30;
        _chatPanel.style.top = 300;
        _chatPanel.style.width = 400;
        _chatPanel.style.height = 300;
        _chatPanel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        _chatPanel.style.borderTopWidth = 2;
        _chatPanel.style.borderBottomWidth = 2;
        _chatPanel.style.borderLeftWidth = 2;
        _chatPanel.style.borderRightWidth = 2;
        _chatPanel.style.borderTopColor = Color.white;
        _chatPanel.style.borderBottomColor = Color.white;
        _chatPanel.style.borderLeftColor = Color.white;
        _chatPanel.style.borderRightColor = Color.white;
        _chatPanel.style.borderTopLeftRadius = 10;
        _chatPanel.style.borderTopRightRadius = 10;
        _chatPanel.style.borderBottomLeftRadius = 10;
        _chatPanel.style.borderBottomRightRadius = 10;
        _chatPanel.style.paddingTop = 10;
        _chatPanel.style.paddingBottom = 10;
        _chatPanel.style.paddingLeft = 10;
        _chatPanel.style.paddingRight = 10;
        
        // 채팅 메시지 영역
        _chatMessages = new ScrollView();
        _chatMessages.name = "chatMessages";
        _chatMessages.style.flexGrow = 1;
        _chatMessages.style.marginBottom = 10;
        
        // 입력 영역
        var inputArea = new VisualElement();
        inputArea.style.flexDirection = FlexDirection.Row;
        inputArea.style.alignItems = Align.Center;
        
        _chatInput = new TextField();
        _chatInput.style.flexGrow = 1;
        _chatInput.style.marginRight = 5;
        // 콜백 인스턴스 생성 및 저장
        _chatInputKeyDownCallback = OnChatInputKeyDown;
        _chatInput.RegisterCallback(_chatInputKeyDownCallback);
        
        _sendButton = new Button(() => SendChatMessage());
        _sendButton.text = "전송";
        _sendButton.style.width = 60;
        
        inputArea.Add(_chatInput);
        inputArea.Add(_sendButton);
        
        _chatPanel.Add(_chatMessages);
        _chatPanel.Add(inputArea);
        
        // 퀵 채팅 버튼들 추가
        if (enableQuickChat)
        {
            CreateQuickChatButtons();
        }
        
        _root.Add(_chatPanel);
    }
    
    private void CreateQuickChatButtons()
    {
        var quickChatArea = new VisualElement();
        quickChatArea.name = "quickChatArea";
        quickChatArea.style.flexDirection = FlexDirection.Row;
        quickChatArea.style.flexWrap = Wrap.Wrap;
        quickChatArea.style.marginTop = 5;
        quickChatArea.style.marginBottom = 5;
        
        foreach (string message in quickChatMessages)
        {
            var quickButton = new Button(() => SendQuickChatMessage(message));
            quickButton.text = message;
            quickButton.AddToClassList("quick-chat-button");
            quickButton.style.fontSize = 12;
            quickButton.style.marginTop = 2;
            quickButton.style.marginBottom = 2;
            quickButton.style.marginLeft = 2;
            quickButton.style.marginRight = 2;
            quickButton.style.paddingTop = 2;
            quickButton.style.paddingBottom = 2;
            quickButton.style.paddingLeft = 5;
            quickButton.style.paddingRight = 5;
            
            quickChatArea.Add(quickButton);
        }
        
        _chatPanel.Insert(_chatPanel.childCount - 1, quickChatArea);
    }
    
    public void ToggleChat()
    {
        Debug.Log("챗이 터치되었습니다!");
        // 실제 채팅 로직은 나중에 구현
    }
    
    public void SetChatVisibility(bool visible)
    {
        isChatOpen = visible;
        if (_chatPanel != null)
        {
            _chatPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    
    public void SetChatIconVisibility(bool visible)
    {
        if (_chatIconArea != null)
        {
            _chatIconArea.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
    
    private void OnChatInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            SendChatMessage();
            evt.StopPropagation();
        }
        else if (evt.keyCode == KeyCode.Escape)
        {
            SetChatVisibility(false);
            evt.StopPropagation();
        }
    }
    
    private void SendChatMessage()
    {
        string message = _chatInput.text.Trim();
        if (string.IsNullOrEmpty(message)) return;
        
        // 로컬 플레이어 메시지 전송
        string playerId = "1"; // 임시 플레이어 ID
        string playerName = "나"; // 임시 플레이어 이름
        
        var chatMessage = new ChatMessage(playerId, playerName, message, ChatMessageType.Normal);
        AddChatMessage(chatMessage);
        
        OnChatMessageSent?.Invoke(playerId, message);
        
        _chatInput.value = "";
        Debug.Log($"[ChatManager] 채팅 전송: {message}");
    }
    
    private void SendQuickChatMessage(string message)
    {
        string playerId = "1"; // 임시 플레이어 ID
        string playerName = "나"; // 임시 플레이어 이름
        
        var chatMessage = new ChatMessage(playerId, playerName, message, ChatMessageType.QuickChat);
        AddChatMessage(chatMessage);
        
        OnChatMessageSent?.Invoke(playerId, message);
        
        Debug.Log($"[ChatManager] 퀵 채팅 전송: {message}");
    }
    
    public void AddChatMessage(ChatMessage message)
    {
        chatHistory.Add(message);
        
        // 최대 메시지 수 제한
        if (chatHistory.Count > maxChatMessages)
        {
            chatHistory.RemoveAt(0);
        }
        
        // UI에 메시지 추가
        CreateMessageElement(message);
        
        OnChatMessageReceived?.Invoke(message);
        
        // // 스크롤을 맨 아래로
        // if (_chatMessages != null)
        // {
        //     // UI Toolkit에서 스크롤을 맨 아래로 이동
        //     _chatMessages.schedule.Execute(() => 
        //     {
        //         _chatMessages.verticalScroller.value = _chatMessages.verticalScroller.highValue;
        //     }).ExecuteLater(10); // 다음 프레임에 실행
        // }
    }
    
    public void ReceiveChatMessage(string playerId, string playerName, string message, ChatMessageType type = ChatMessageType.Normal)
    {
        var chatMessage = new ChatMessage(playerId, playerName, message, type);
        AddChatMessage(chatMessage);
    }
    
    private void CreateMessageElement(ChatMessage message)
    {
        if (_chatMessages == null) return;
        
        var messageElement = new VisualElement();
        messageElement.AddToClassList("chat-message");
        messageElement.style.marginBottom = 2;
        messageElement.style.paddingTop = 2;
        messageElement.style.paddingBottom = 2;
        
        // 메시지 타입에 따른 스타일
        Color messageColor = GetMessageColor(message.type);
        
        // 플레이어 이름과 시간
        var headerLabel = new Label($"[{message.timestamp.ToString("HH:mm")}] {message.playerName}:");
        headerLabel.style.color = messageColor;
        headerLabel.style.fontSize = 12;
        headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        // 메시지 내용
        var contentLabel = new Label(message.message);
        contentLabel.style.color = Color.white;
        contentLabel.style.fontSize = 12;
        contentLabel.style.whiteSpace = WhiteSpace.Normal;
        contentLabel.style.marginLeft = 10;
        
        messageElement.Add(headerLabel);
        messageElement.Add(contentLabel);
        
        _chatMessages.Add(messageElement);
    }
    
    private Color GetMessageColor(ChatMessageType type)
    {
        return type switch
        {
            ChatMessageType.System => Color.yellow,
            ChatMessageType.QuickChat => Color.cyan,
            ChatMessageType.Warning => Color.red,
            ChatMessageType.Death => Color.red,
            ChatMessageType.Escape => Color.green,
            _ => Color.white
        };
    }
    
    public void AddSystemMessage(string message)
    {
        var systemMessage = new ChatMessage("SYSTEM", "시스템", message, ChatMessageType.System);
        AddChatMessage(systemMessage);
    }
    
    public void AddGameEventMessage(string eventType, string playerName = "")
    {
        string message = eventType switch
        {
            "player_joined" => $"{playerName}님이 게임에 참가했습니다.",
            "player_left" => $"{playerName}님이 게임을 떠났습니다.",
            "player_died" => $"{playerName}님이 사망했습니다.",
            "player_escaped" => $"{playerName}님이 탈출했습니다!",
            "game_started" => "게임이 시작되었습니다!",
            "game_ended" => "게임이 종료되었습니다.",
            _ => eventType // message 대신 eventType 사용
        };
        
        ChatMessageType messageType = eventType switch
        {
            "player_died" => ChatMessageType.Death,
            "player_escaped" => ChatMessageType.Escape,
            _ => ChatMessageType.System
        };
        
        var gameMessage = new ChatMessage("SYSTEM", "게임", message, messageType);
        AddChatMessage(gameMessage);
    }
    
    public void ClearChatHistory()
    {
        chatHistory.Clear();
        if (_chatMessages != null)
        {
            _chatMessages.Clear();
        }
        Debug.Log("[ChatManager] 채팅 기록 초기화");
    }
    
    public List<ChatMessage> GetChatHistory() => new List<ChatMessage>(chatHistory);
    
    public void SetQuickChatMessages(string[] messages)
    {
        quickChatMessages = messages;
        // 퀵 채팅 버튼 다시 생성
        if (enableQuickChat && _chatPanel != null)
        {
            var existingQuickChat = _chatPanel.Q("quickChatArea");
            if (existingQuickChat != null)
            {
                _chatPanel.Remove(existingQuickChat);
                CreateQuickChatButtons();
            }
        }
    }
    
    public bool IsChatOpen() => isChatOpen;
    public int GetMessageCount() => chatHistory.Count;
    
    #region Unity Lifecycle
    
    private void OnDestroy()
    {
        UnregisterUIEvents();
    }
    
    private void UnregisterUIEvents()
    {
        // 채팅 아이콘 이벤트 해제
        if (_chatIconArea != null && _chatIconAreaCallback != null)
        {
            _chatIconArea.UnregisterCallback(_chatIconAreaCallback);
        }
        
        // 입력 필드 이벤트 해제
        if (_chatInput != null && _chatInputKeyDownCallback != null)
        {
            _chatInput.UnregisterCallback(_chatInputKeyDownCallback);
        }
        
        Debug.Log("[ChatManager] UI 이벤트 구독 해제 완료");
    }
    
    #endregion
}