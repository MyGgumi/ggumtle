using System;
using System.Collections.Generic;
using Features.Chat.Messages;
using Features.Chat.Models;
using Features.Chat.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Chat.ViewModels
{
    /// <summary>
    /// 채팅 시스템 ViewModel
    /// R3 + MessagePipe 기반의 반응형 ViewModel
    /// </summary>
    public class ChatViewModel : IDisposable
    {
        #region Observable Properties

        // 채팅 상태
        public readonly ReadOnlyReactiveProperty<bool> IsChatOpen;
        public readonly ReadOnlyReactiveProperty<string> CurrentMessage;
        public readonly ReadOnlyReactiveProperty<int> MessageCount;
        public readonly ReadOnlyReactiveProperty<List<ChatMessage>> ChatHistory;

        // 설정
        public readonly ReadOnlyReactiveProperty<bool> EnableQuickChat;
        public readonly ReadOnlyReactiveProperty<string[]> QuickChatMessages;
        public readonly ReadOnlyReactiveProperty<int> MaxChatMessages;
        public readonly ReadOnlyReactiveProperty<float> MessageDisplayDuration;

        // UI 상태 (계산된 속성들)
        public readonly ReadOnlyReactiveProperty<bool> ShouldShowChatPanel;
        public readonly ReadOnlyReactiveProperty<bool> HasMessages;
        public readonly ReadOnlyReactiveProperty<ChatMessage> LatestMessage;
        public readonly ReadOnlyReactiveProperty<bool> CanSendMessage;

        #endregion

        #region Dependencies

        private readonly IChatService _chatService;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public ChatViewModel(
            IChatService chatService,
            ISubscriber<ChatToggledMessage> chatToggledSubscriber,
            ISubscriber<MessageAddedMessage> messageAddedSubscriber,
            ISubscriber<MessageSentMessage> messageSentSubscriber,
            ISubscriber<MessageReceivedMessage> messageReceivedSubscriber,
            ISubscriber<QuickChatSentMessage> quickChatSentSubscriber,
            ISubscriber<ChatHistoryClearedMessage> historyClearedSubscriber,
            ISubscriber<QuickChatMessagesChangedMessage> quickChatChangedSubscriber,
            ISubscriber<SystemMessageAddedMessage> systemMessageSubscriber,
            ISubscriber<GameEventChatMessage> gameEventSubscriber,
            ISubscriber<PlayerChatEventMessage> playerEventSubscriber,
            ISubscriber<ChatSettingsChangedMessage> settingsChangedSubscriber,
            ISubscriber<ChatInputStateChangedMessage> inputStateSubscriber,
            ISubscriber<ChatVisibilityChangedMessage> visibilityChangedSubscriber
        )
        {
            _chatService = chatService;

            // Service의 Observable 속성들을 직접 연결
            IsChatOpen = _chatService.IsChatOpen;
            CurrentMessage = _chatService.CurrentMessage;
            MessageCount = _chatService.MessageCount;
            ChatHistory = _chatService.ChatHistory;
            EnableQuickChat = _chatService.EnableQuickChat;
            QuickChatMessages = _chatService.QuickChatMessages;
            MaxChatMessages = _chatService.MaxChatMessages;
            MessageDisplayDuration = _chatService.MessageDisplayDuration;

            // 계산된 속성들
            ShouldShowChatPanel = IsChatOpen
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            HasMessages = MessageCount
                .Select(count => count > 0)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            LatestMessage = ChatHistory
                .Select(history => history?.Count > 0 ? history[history.Count - 1] : null)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            CanSendMessage = CurrentMessage
                .Select(message => !string.IsNullOrEmpty(message?.Trim()))
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            // 메시지 구독
            chatToggledSubscriber.Subscribe(OnChatToggled).AddTo(_disposables);
            messageAddedSubscriber.Subscribe(OnMessageAdded).AddTo(_disposables);
            messageSentSubscriber.Subscribe(OnMessageSent).AddTo(_disposables);
            messageReceivedSubscriber.Subscribe(OnMessageReceived).AddTo(_disposables);
            quickChatSentSubscriber.Subscribe(OnQuickChatSent).AddTo(_disposables);
            historyClearedSubscriber.Subscribe(OnHistoryCleared).AddTo(_disposables);
            quickChatChangedSubscriber.Subscribe(OnQuickChatChanged).AddTo(_disposables);
            systemMessageSubscriber.Subscribe(OnSystemMessage).AddTo(_disposables);
            gameEventSubscriber.Subscribe(OnGameEvent).AddTo(_disposables);
            playerEventSubscriber.Subscribe(OnPlayerEvent).AddTo(_disposables);
            settingsChangedSubscriber.Subscribe(OnSettingsChanged).AddTo(_disposables);
            inputStateSubscriber.Subscribe(OnInputStateChanged).AddTo(_disposables);
            visibilityChangedSubscriber.Subscribe(OnVisibilityChanged).AddTo(_disposables);

            DebugLog("초기화 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 채팅 열림/닫힘 토글
        /// </summary>
        public void ToggleChat()
        {
            _chatService.ToggleChat();
        }

        /// <summary>
        /// 채팅 열림 상태 설정
        /// </summary>
        public void SetChatOpen(bool isOpen)
        {
            _chatService.SetChatOpen(isOpen);
        }

        /// <summary>
        /// 메시지 전송
        /// </summary>
        public void SendMessage(string message)
        {
            _chatService.SendMessage(message);
        }

        /// <summary>
        /// 퀵 채팅 메시지 전송
        /// </summary>
        public void SendQuickChatMessage(string message)
        {
            _chatService.SendQuickChatMessage(message);
        }

        /// <summary>
        /// 메시지 수신 (네트워크에서)
        /// </summary>
        public void ReceiveMessage(string playerId, string playerName, string message, ChatMessageType type = ChatMessageType.Normal)
        {
            _chatService.ReceiveMessage(playerId, playerName, message, type);
        }

        /// <summary>
        /// 시스템 메시지 추가
        /// </summary>
        public void AddSystemMessage(string message)
        {
            _chatService.AddSystemMessage(message);
        }

        /// <summary>
        /// 게임 이벤트 메시지 추가
        /// </summary>
        public void AddGameEventMessage(string eventType, string playerName = "")
        {
            _chatService.AddGameEventMessage(eventType, playerName);
        }

        /// <summary>
        /// 채팅 기록 초기화
        /// </summary>
        public void ClearChatHistory()
        {
            _chatService.ClearChatHistory();
        }

        /// <summary>
        /// 현재 입력 메시지 설정
        /// </summary>
        public void SetCurrentMessage(string message)
        {
            _chatService.SetCurrentMessage(message);
        }

        /// <summary>
        /// 채팅 설정 변경
        /// </summary>
        public void SetChatSettings(int maxMessages, float displayDuration, bool enableQuickChat)
        {
            _chatService.SetChatSettings(maxMessages, displayDuration, enableQuickChat);
        }

        /// <summary>
        /// 퀵 채팅 메시지 설정
        /// </summary>
        public void SetQuickChatMessages(string[] messages)
        {
            _chatService.SetQuickChatMessages(messages);
        }

        /// <summary>
        /// 퀵 채팅 메시지 추가
        /// </summary>
        public void AddQuickChatMessage(string message)
        {
            _chatService.AddQuickChatMessage(message);
        }

        /// <summary>
        /// 퀵 채팅 메시지 제거
        /// </summary>
        public void RemoveQuickChatMessage(string message)
        {
            _chatService.RemoveQuickChatMessage(message);
        }

        // 유틸리티 메서드들
        public List<ChatMessage> GetRecentMessages(int count)
        {
            return _chatService.GetRecentMessages(count);
        }

        public List<ChatMessage> GetMessagesByType(ChatMessageType type)
        {
            return _chatService.GetMessagesByType(type);
        }

        public Color GetMessageColor(ChatMessageType type)
        {
            return _chatService.GetMessageColor(type);
        }

        public List<ChatMessage> GetMessagesByPlayer(string playerId)
        {
            return _chatService.GetMessagesByPlayer(playerId);
        }

        public (int total, int system, int normal, int quickChat) GetChatStatistics()
        {
            return _chatService.GetChatStatistics();
        }

        #endregion

        #region Message Handlers

        private void OnChatToggled(ChatToggledMessage message)
        {
            DebugLog($"채팅 토글: {message.isOpen}");
        }

        private void OnMessageAdded(MessageAddedMessage message)
        {
            DebugLog($"메시지 추가: [{message.message.playerName}] {message.message.message} (총 {message.totalMessageCount}개)");
        }

        private void OnMessageSent(MessageSentMessage message)
        {
            DebugLog($"메시지 전송: [{message.playerName}] {message.message} ({message.type})");
        }

        private void OnMessageReceived(MessageReceivedMessage message)
        {
            DebugLog($"메시지 수신: [{message.message.playerName}] {message.message.message} (네트워크: {message.isFromNetwork})");
        }

        private void OnQuickChatSent(QuickChatSentMessage message)
        {
            DebugLog($"퀵 채팅 전송: [{message.playerName}] {message.quickMessage}");
        }

        private void OnHistoryCleared(ChatHistoryClearedMessage message)
        {
            DebugLog($"채팅 기록 초기화: {message.clearedMessageCount}개 메시지 제거");
        }

        private void OnQuickChatChanged(QuickChatMessagesChangedMessage message)
        {
            DebugLog($"퀵 채팅 메시지 변경: {message.quickMessages?.Length ?? 0}개");
        }

        private void OnSystemMessage(SystemMessageAddedMessage message)
        {
            DebugLog($"시스템 메시지: {message.message} ({message.type})");
        }

        private void OnGameEvent(GameEventChatMessage message)
        {
            DebugLog($"게임 이벤트: {message.eventType} - {message.playerName} - {message.message}");
        }

        private void OnPlayerEvent(PlayerChatEventMessage message)
        {
            DebugLog($"플레이어 이벤트: {message.playerName} {message.eventType} - {message.message}");
        }

        private void OnSettingsChanged(ChatSettingsChangedMessage message)
        {
            DebugLog($"채팅 설정 변경: Max={message.maxChatMessages}, Duration={message.messageDisplayDuration}s, QuickChat={message.enableQuickChat}");
        }

        private void OnInputStateChanged(ChatInputStateChangedMessage message)
        {
            DebugLog($"입력 상태 변경: '{message.currentInput}' (포커스: {message.isFocused})");
        }

        private void OnVisibilityChanged(ChatVisibilityChangedMessage message)
        {
            DebugLog($"가시성 변경: 채팅={message.isVisible}, 아이콘={message.showChatIcon}");
        }

        #endregion

        #region Private Methods

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[ChatViewModel] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            DebugLog("Dispose 완료");
        }

        #endregion
    }
}