using System;
using System.Collections.Generic;
using System.Linq;
using Features.Chat.Messages;
using Features.Chat.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Chat.Services
{
    /// <summary>
    /// 채팅 시스템 관리 서비스 구현체
    /// </summary>
    public class ChatServiceImpl : IChatService, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<bool> IsChatOpen => _isChatOpen;
        public ReadOnlyReactiveProperty<string> CurrentMessage => _currentMessage;
        public ReadOnlyReactiveProperty<int> MessageCount => _messageCount;
        public ReadOnlyReactiveProperty<List<ChatMessage>> ChatHistory => _chatHistory;
        public ReadOnlyReactiveProperty<bool> EnableQuickChat => _enableQuickChat;
        public ReadOnlyReactiveProperty<string[]> QuickChatMessages => _quickChatMessages;
        public ReadOnlyReactiveProperty<int> MaxChatMessages => _maxChatMessages;
        public ReadOnlyReactiveProperty<float> MessageDisplayDuration => _messageDisplayDuration;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<bool> _isChatOpen = new(false);
        private readonly ReactiveProperty<string> _currentMessage = new("");
        private readonly ReactiveProperty<int> _messageCount = new(0);
        private readonly ReactiveProperty<List<ChatMessage>> _chatHistory = new(new());
        private readonly ReactiveProperty<bool> _enableQuickChat = new(true);
        private readonly ReactiveProperty<string[]> _quickChatMessages = new(new string[0]);
        private readonly ReactiveProperty<int> _maxChatMessages = new(50);
        private readonly ReactiveProperty<float> _messageDisplayDuration = new(10f);

        private readonly ChatModel _chatModel = new();
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Dependencies

        private readonly IPublisher<ChatToggledMessage> _chatToggledPublisher;
        private readonly IPublisher<MessageAddedMessage> _messageAddedPublisher;
        private readonly IPublisher<MessageSentMessage> _messageSentPublisher;
        private readonly IPublisher<MessageReceivedMessage> _messageReceivedPublisher;
        private readonly IPublisher<QuickChatSentMessage> _quickChatSentPublisher;
        private readonly IPublisher<ChatHistoryClearedMessage> _historyClearedPublisher;
        private readonly IPublisher<QuickChatMessagesChangedMessage> _quickChatChangedPublisher;
        private readonly IPublisher<SystemMessageAddedMessage> _systemMessagePublisher;
        private readonly IPublisher<GameEventChatMessage> _gameEventPublisher;
        private readonly IPublisher<PlayerChatEventMessage> _playerEventPublisher;
        private readonly IPublisher<ChatSettingsChangedMessage> _settingsChangedPublisher;
        private readonly IPublisher<ChatInputStateChangedMessage> _inputStatePublisher;
        private readonly IPublisher<ChatVisibilityChangedMessage> _visibilityChangedPublisher;

        #endregion

        #region Constructor

        [Inject]
        public ChatServiceImpl(
            IPublisher<ChatToggledMessage> chatToggledPublisher,
            IPublisher<MessageAddedMessage> messageAddedPublisher,
            IPublisher<MessageSentMessage> messageSentPublisher,
            IPublisher<MessageReceivedMessage> messageReceivedPublisher,
            IPublisher<QuickChatSentMessage> quickChatSentPublisher,
            IPublisher<ChatHistoryClearedMessage> historyClearedPublisher,
            IPublisher<QuickChatMessagesChangedMessage> quickChatChangedPublisher,
            IPublisher<SystemMessageAddedMessage> systemMessagePublisher,
            IPublisher<GameEventChatMessage> gameEventPublisher,
            IPublisher<PlayerChatEventMessage> playerEventPublisher,
            IPublisher<ChatSettingsChangedMessage> settingsChangedPublisher,
            IPublisher<ChatInputStateChangedMessage> inputStatePublisher,
            IPublisher<ChatVisibilityChangedMessage> visibilityChangedPublisher
        )
        {
            _chatToggledPublisher = chatToggledPublisher;
            _messageAddedPublisher = messageAddedPublisher;
            _messageSentPublisher = messageSentPublisher;
            _messageReceivedPublisher = messageReceivedPublisher;
            _quickChatSentPublisher = quickChatSentPublisher;
            _historyClearedPublisher = historyClearedPublisher;
            _quickChatChangedPublisher = quickChatChangedPublisher;
            _systemMessagePublisher = systemMessagePublisher;
            _gameEventPublisher = gameEventPublisher;
            _playerEventPublisher = playerEventPublisher;
            _settingsChangedPublisher = settingsChangedPublisher;
            _inputStatePublisher = inputStatePublisher;
            _visibilityChangedPublisher = visibilityChangedPublisher;

            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // 초기 상태 설정
            UpdateObservables();

            if (_enableDebugLogs)
            {
                Debug.Log("[ChatServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void ToggleChat()
        {
            _chatModel.ToggleChat();
            UpdateObservables();

            _chatToggledPublisher.Publish(new ChatToggledMessage(_chatModel.isChatOpen));
            _visibilityChangedPublisher.Publish(new ChatVisibilityChangedMessage(_chatModel.isChatOpen, true));

            DebugLog($"채팅 토글: {_chatModel.isChatOpen}");
        }

        public void SetChatOpen(bool isOpen)
        {
            if (_chatModel.isChatOpen != isOpen)
            {
                _chatModel.isChatOpen = isOpen;
                UpdateObservables();

                _chatToggledPublisher.Publish(new ChatToggledMessage(isOpen));
                _visibilityChangedPublisher.Publish(new ChatVisibilityChangedMessage(isOpen, true));

                DebugLog($"채팅 상태 설정: {isOpen}");
            }
        }

        public void SendMessage(string message, string playerId = "", string playerName = "")
        {
            if (string.IsNullOrEmpty(message?.Trim()))
                return;

            // 기본값 설정
            string actualPlayerId = string.IsNullOrEmpty(playerId) ? "1" : playerId;
            string actualPlayerName = string.IsNullOrEmpty(playerName) ? "나" : playerName;

            var chatMessage = new ChatMessage(actualPlayerId, actualPlayerName, message.Trim(), ChatMessageType.Normal);
            AddMessage(chatMessage);

            _messageSentPublisher.Publish(new MessageSentMessage(actualPlayerId, actualPlayerName, message.Trim(), ChatMessageType.Normal));
            _playerEventPublisher.Publish(new PlayerChatEventMessage(int.Parse(actualPlayerId), actualPlayerName, "message_sent", message.Trim()));

            // 입력 필드 클리어
            SetCurrentMessage("");

            DebugLog($"메시지 전송: [{actualPlayerName}] {message}");
        }

        public void SendQuickChatMessage(string message, string playerId = "", string playerName = "")
        {
            if (string.IsNullOrEmpty(message))
                return;

            // 기본값 설정
            string actualPlayerId = string.IsNullOrEmpty(playerId) ? "1" : playerId;
            string actualPlayerName = string.IsNullOrEmpty(playerName) ? "나" : playerName;

            var chatMessage = new ChatMessage(actualPlayerId, actualPlayerName, message, ChatMessageType.QuickChat);
            AddMessage(chatMessage);

            _quickChatSentPublisher.Publish(new QuickChatSentMessage(actualPlayerId, actualPlayerName, message));
            _messageSentPublisher.Publish(new MessageSentMessage(actualPlayerId, actualPlayerName, message, ChatMessageType.QuickChat));

            DebugLog($"퀵 채팅 전송: [{actualPlayerName}] {message}");
        }

        public void ReceiveMessage(string playerId, string playerName, string message, ChatMessageType type = ChatMessageType.Normal)
        {
            var chatMessage = new ChatMessage(playerId, playerName, message, type);
            AddMessage(chatMessage);

            _messageReceivedPublisher.Publish(new MessageReceivedMessage(chatMessage, true));

            DebugLog($"메시지 수신: [{playerName}] {message}");
        }

        public void AddMessage(ChatMessage message)
        {
            if (message == null)
                return;

            _chatModel.AddMessage(message);
            UpdateObservables();

            _messageAddedPublisher.Publish(new MessageAddedMessage(message, _chatModel.MessageCount));

            DebugLog($"메시지 추가: [{message.playerName}] {message.message}");
        }

        public void AddSystemMessage(string message)
        {
            var systemMessage = new ChatMessage("SYSTEM", "시스템", message, ChatMessageType.System);
            AddMessage(systemMessage);

            _systemMessagePublisher.Publish(new SystemMessageAddedMessage(message, ChatMessageType.System));

            DebugLog($"시스템 메시지 추가: {message}");
        }

        public void AddGameEventMessage(string eventType, string playerName = "")
        {
            _chatModel.AddGameEventMessage(eventType, playerName);
            UpdateObservables();

            var lastMessage = _chatModel.chatHistory.LastOrDefault();
            if (lastMessage != null)
            {
                _messageAddedPublisher.Publish(new MessageAddedMessage(lastMessage, _chatModel.MessageCount));
                _gameEventPublisher.Publish(new GameEventChatMessage(eventType, playerName, lastMessage.message, lastMessage.type));
            }

            DebugLog($"게임 이벤트 메시지 추가: {eventType} - {playerName}");
        }

        public void ClearChatHistory()
        {
            int clearedCount = _chatModel.MessageCount;
            _chatModel.ClearChatHistory();
            UpdateObservables();

            _historyClearedPublisher.Publish(new ChatHistoryClearedMessage(clearedCount));

            DebugLog($"채팅 기록 초기화: {clearedCount}개 메시지 제거");
        }

        public void SetCurrentMessage(string message)
        {
            _chatModel.currentMessage = message ?? "";
            _currentMessage.Value = _chatModel.currentMessage;

            _inputStatePublisher.Publish(new ChatInputStateChangedMessage(_chatModel.currentMessage, !string.IsNullOrEmpty(_chatModel.currentMessage)));
        }

        public void SetChatSettings(int maxMessages, float displayDuration, bool enableQuickChat)
        {
            _chatModel.maxChatMessages = Mathf.Max(1, maxMessages);
            _chatModel.messageDisplayDuration = Mathf.Max(0f, displayDuration);
            _chatModel.enableQuickChat = enableQuickChat;

            UpdateObservables();

            _settingsChangedPublisher.Publish(new ChatSettingsChangedMessage(maxMessages, displayDuration, enableQuickChat));

            DebugLog($"채팅 설정 변경: Max={maxMessages}, Duration={displayDuration}s, QuickChat={enableQuickChat}");
        }

        public void SetQuickChatMessages(string[] messages)
        {
            _chatModel.SetQuickChatMessages(messages);
            _quickChatMessages.Value = _chatModel.quickChatMessages;

            _quickChatChangedPublisher.Publish(new QuickChatMessagesChangedMessage(_chatModel.quickChatMessages));

            DebugLog($"퀵 채팅 메시지 설정: {messages?.Length ?? 0}개");
        }

        public void AddQuickChatMessage(string message)
        {
            _chatModel.AddQuickChatMessage(message);
            _quickChatMessages.Value = _chatModel.quickChatMessages;

            _quickChatChangedPublisher.Publish(new QuickChatMessagesChangedMessage(_chatModel.quickChatMessages));

            DebugLog($"퀵 채팅 메시지 추가: {message}");
        }

        public void RemoveQuickChatMessage(string message)
        {
            _chatModel.RemoveQuickChatMessage(message);
            _quickChatMessages.Value = _chatModel.quickChatMessages;

            _quickChatChangedPublisher.Publish(new QuickChatMessagesChangedMessage(_chatModel.quickChatMessages));

            DebugLog($"퀵 채팅 메시지 제거: {message}");
        }

        // 유틸리티 메서드들
        public List<ChatMessage> GetRecentMessages(int count)
        {
            return _chatModel.GetRecentMessages(count);
        }

        public List<ChatMessage> GetMessagesByType(ChatMessageType type)
        {
            return _chatModel.GetMessagesByType(type);
        }

        public Color GetMessageColor(ChatMessageType type)
        {
            return _chatModel.GetMessageColor(type);
        }

        public void CleanupExpiredMessages()
        {
            int beforeCount = _chatModel.MessageCount;
            _chatModel.CleanupExpiredMessages();
            int afterCount = _chatModel.MessageCount;

            if (beforeCount != afterCount)
            {
                UpdateObservables();
                DebugLog($"만료된 메시지 정리: {beforeCount - afterCount}개 제거");
            }
        }

        public List<ChatMessage> GetMessagesByPlayer(string playerId)
        {
            return _chatModel.chatHistory.FindAll(msg => msg.playerId == playerId);
        }

        public (int total, int system, int normal, int quickChat) GetChatStatistics()
        {
            var total = _chatModel.MessageCount;
            var system = _chatModel.GetMessagesByType(ChatMessageType.System).Count;
            var normal = _chatModel.GetMessagesByType(ChatMessageType.Normal).Count;
            var quickChat = _chatModel.GetMessagesByType(ChatMessageType.QuickChat).Count;

            return (total, system, normal, quickChat);
        }

        #endregion

        #region Private Methods

        private void UpdateObservables()
        {
            _isChatOpen.Value = _chatModel.isChatOpen;
            _currentMessage.Value = _chatModel.currentMessage;
            _messageCount.Value = _chatModel.MessageCount;
            _chatHistory.Value = new List<ChatMessage>(_chatModel.chatHistory);
            _enableQuickChat.Value = _chatModel.enableQuickChat;
            _quickChatMessages.Value = _chatModel.quickChatMessages;
            _maxChatMessages.Value = _chatModel.maxChatMessages;
            _messageDisplayDuration.Value = _chatModel.messageDisplayDuration;
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[ChatServiceImpl] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _isChatOpen?.Dispose();
            _currentMessage?.Dispose();
            _messageCount?.Dispose();
            _chatHistory?.Dispose();
            _enableQuickChat?.Dispose();
            _quickChatMessages?.Dispose();
            _maxChatMessages?.Dispose();
            _messageDisplayDuration?.Dispose();

            DebugLog("Dispose 완료");
        }

        #endregion
    }
}