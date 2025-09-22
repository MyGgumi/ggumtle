using Features.Chat.Models;

namespace Features.Chat.Messages
{
    /// <summary>
    /// 채팅 열림/닫힘 토글 메시지
    /// </summary>
    public readonly struct ChatToggledMessage
    {
        public readonly bool isOpen;

        public ChatToggledMessage(bool isOpen)
        {
            this.isOpen = isOpen;
        }
    }

    /// <summary>
    /// 메시지 추가 메시지
    /// </summary>
    public readonly struct MessageAddedMessage
    {
        public readonly ChatMessage message;
        public readonly int totalMessageCount;

        public MessageAddedMessage(ChatMessage message, int totalMessageCount)
        {
            this.message = message;
            this.totalMessageCount = totalMessageCount;
        }
    }

    /// <summary>
    /// 메시지 전송 메시지
    /// </summary>
    public readonly struct MessageSentMessage
    {
        public readonly string playerId;
        public readonly string playerName;
        public readonly string message;
        public readonly ChatMessageType type;

        public MessageSentMessage(string playerId, string playerName, string message, ChatMessageType type)
        {
            this.playerId = playerId;
            this.playerName = playerName;
            this.message = message;
            this.type = type;
        }
    }

    /// <summary>
    /// 메시지 수신 메시지
    /// </summary>
    public readonly struct MessageReceivedMessage
    {
        public readonly ChatMessage message;
        public readonly bool isFromNetwork;

        public MessageReceivedMessage(ChatMessage message, bool isFromNetwork)
        {
            this.message = message;
            this.isFromNetwork = isFromNetwork;
        }
    }

    /// <summary>
    /// 퀵 채팅 전송 메시지
    /// </summary>
    public readonly struct QuickChatSentMessage
    {
        public readonly string playerId;
        public readonly string playerName;
        public readonly string quickMessage;

        public QuickChatSentMessage(string playerId, string playerName, string quickMessage)
        {
            this.playerId = playerId;
            this.playerName = playerName;
            this.quickMessage = quickMessage;
        }
    }

    /// <summary>
    /// 채팅 기록 초기화 메시지
    /// </summary>
    public readonly struct ChatHistoryClearedMessage
    {
        public readonly int clearedMessageCount;

        public ChatHistoryClearedMessage(int clearedMessageCount)
        {
            this.clearedMessageCount = clearedMessageCount;
        }
    }

    /// <summary>
    /// 퀵 채팅 메시지 변경 메시지
    /// </summary>
    public readonly struct QuickChatMessagesChangedMessage
    {
        public readonly string[] quickMessages;

        public QuickChatMessagesChangedMessage(string[] quickMessages)
        {
            this.quickMessages = quickMessages;
        }
    }

    /// <summary>
    /// 시스템 메시지 추가 메시지
    /// </summary>
    public readonly struct SystemMessageAddedMessage
    {
        public readonly string message;
        public readonly ChatMessageType type;

        public SystemMessageAddedMessage(string message, ChatMessageType type)
        {
            this.message = message;
            this.type = type;
        }
    }

    /// <summary>
    /// 게임 이벤트 메시지
    /// </summary>
    public readonly struct GameEventChatMessage
    {
        public readonly string eventType;
        public readonly string playerName;
        public readonly string message;
        public readonly ChatMessageType messageType;

        public GameEventChatMessage(string eventType, string playerName, string message, ChatMessageType messageType)
        {
            this.eventType = eventType;
            this.playerName = playerName;
            this.message = message;
            this.messageType = messageType;
        }
    }

    /// <summary>
    /// 플레이어 채팅 이벤트 메시지
    /// </summary>
    public readonly struct PlayerChatEventMessage
    {
        public readonly int playerId;
        public readonly string playerName;
        public readonly string eventType;
        public readonly string message;

        public PlayerChatEventMessage(int playerId, string playerName, string eventType, string message)
        {
            this.playerId = playerId;
            this.playerName = playerName;
            this.eventType = eventType;
            this.message = message;
        }
    }

    /// <summary>
    /// 채팅 설정 변경 메시지
    /// </summary>
    public readonly struct ChatSettingsChangedMessage
    {
        public readonly int maxChatMessages;
        public readonly float messageDisplayDuration;
        public readonly bool enableQuickChat;

        public ChatSettingsChangedMessage(int maxChatMessages, float messageDisplayDuration, bool enableQuickChat)
        {
            this.maxChatMessages = maxChatMessages;
            this.messageDisplayDuration = messageDisplayDuration;
            this.enableQuickChat = enableQuickChat;
        }
    }

    /// <summary>
    /// 채팅 입력 상태 변경 메시지
    /// </summary>
    public readonly struct ChatInputStateChangedMessage
    {
        public readonly string currentInput;
        public readonly bool isFocused;

        public ChatInputStateChangedMessage(string currentInput, bool isFocused)
        {
            this.currentInput = currentInput;
            this.isFocused = isFocused;
        }
    }

    /// <summary>
    /// 채팅 가시성 변경 메시지
    /// </summary>
    public readonly struct ChatVisibilityChangedMessage
    {
        public readonly bool isVisible;
        public readonly bool showChatIcon;

        public ChatVisibilityChangedMessage(bool isVisible, bool showChatIcon)
        {
            this.isVisible = isVisible;
            this.showChatIcon = showChatIcon;
        }
    }
}