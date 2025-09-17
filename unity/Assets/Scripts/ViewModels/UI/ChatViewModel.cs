using System;
using System.Collections.Generic;
using MVVM.Core;
using UnityEngine;

namespace MVVM.UI
{
    [System.Serializable]
    public class ChatMessage
    {
        public string playerId;
        public string playerName;
        public string message;
        public System.DateTime timestamp;
        public ChatMessageType type;

        public ChatMessage(
            string id,
            string name,
            string msg,
            ChatMessageType msgType = ChatMessageType.Normal
        )
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
        Escape,
    }

    public class ChatViewModel : BaseViewModel
    {
        [Header("Chat State")]
        [SerializeField]
        private bool _isChatOpen = false;

        [SerializeField]
        private string _currentMessage = "";

        [Header("Settings")]
        [SerializeField]
        private int _maxChatMessages = 50;

        [SerializeField]
        private float _messageDisplayDuration = 10f;

        [SerializeField]
        private bool _enableQuickChat = true;

        [SerializeField]
        private string[] _quickChatMessages =
        {
            "도와줘!",
            "근처야",
            "위험!",
            "따라와!",
            "가는중!",
            "해결됨",
        };

        [Header("Current State")]
        [SerializeField]
        private List<ChatMessage> _chatHistory = new List<ChatMessage>();

        public event Action<bool> ChatToggled;
        public event Action<ChatMessage> MessageAdded;
        public event Action<ChatMessage> MessageReceived;
        public event Action<string, string> MessageSent; // playerId, message
        public event Action ChatHistoryCleared;
        public event Action<string[]> QuickChatMessagesChanged;

        #region Properties

        public bool IsChatOpen
        {
            get => _isChatOpen;
            set
            {
                if (SetProperty(ref _isChatOpen, value))
                {
                    ChatToggled?.Invoke(_isChatOpen);
                }
            }
        }

        public string CurrentMessage
        {
            get => _currentMessage;
            set => SetProperty(ref _currentMessage, value ?? "");
        }

        public int MaxChatMessages
        {
            get => _maxChatMessages;
            set => SetProperty(ref _maxChatMessages, Mathf.Max(1, value));
        }

        public float MessageDisplayDuration
        {
            get => _messageDisplayDuration;
            set => SetProperty(ref _messageDisplayDuration, Mathf.Max(0f, value));
        }

        public bool EnableQuickChat
        {
            get => _enableQuickChat;
            set => SetProperty(ref _enableQuickChat, value);
        }

        public string[] QuickChatMessages
        {
            get => _quickChatMessages;
            set
            {
                if (SetProperty(ref _quickChatMessages, value ?? new string[0]))
                {
                    QuickChatMessagesChanged?.Invoke(_quickChatMessages);
                }
            }
        }

        public List<ChatMessage> ChatHistory => new List<ChatMessage>(_chatHistory);
        public int MessageCount => _chatHistory.Count;

        #endregion

        #region Chat Methods

        public void ToggleChat()
        {
            IsChatOpen = !_isChatOpen;

            if (EnableDebugLogs)
            {
                Debug.Log($"[ChatViewModel] 채팅 토글: {_isChatOpen}");
            }
        }

        public new void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message?.Trim()))
                return;

            // 로컬 플레이어 메시지 전송
            string playerId = "1"; // 임시 플레이어 ID
            string playerName = "나"; // 임시 플레이어 이름

            var chatMessage = new ChatMessage(
                playerId,
                playerName,
                message.Trim(),
                ChatMessageType.Normal
            );
            AddMessage(chatMessage);

            MessageSent?.Invoke(playerId, message.Trim());
            CurrentMessage = ""; // 입력 필드 클리어

            if (EnableDebugLogs)
            {
                Debug.Log($"[ChatViewModel] 채팅 전송: {message}");
            }
        }

        public void SendQuickChatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            string playerId = "1"; // 임시 플레이어 ID
            string playerName = "나"; // 임시 플레이어 이름

            var chatMessage = new ChatMessage(
                playerId,
                playerName,
                message,
                ChatMessageType.QuickChat
            );
            AddMessage(chatMessage);

            MessageSent?.Invoke(playerId, message);

            if (EnableDebugLogs)
            {
                Debug.Log($"[ChatViewModel] 퀵 채팅 전송: {message}");
            }
        }

        public void ReceiveMessage(
            string playerId,
            string playerName,
            string message,
            ChatMessageType type = ChatMessageType.Normal
        )
        {
            var chatMessage = new ChatMessage(playerId, playerName, message, type);
            AddMessage(chatMessage);
            MessageReceived?.Invoke(chatMessage);
        }

        public void AddMessage(ChatMessage message)
        {
            if (message == null)
                return;

            _chatHistory.Add(message);

            // 최대 메시지 수 제한
            if (_chatHistory.Count > _maxChatMessages)
            {
                _chatHistory.RemoveAt(0);
            }

            MessageAdded?.Invoke(message);
            OnPropertyChanged(nameof(ChatHistory));
            OnPropertyChanged(nameof(MessageCount));

            if (EnableDebugLogs)
            {
                Debug.Log($"[ChatViewModel] 메시지 추가: [{message.playerName}] {message.message}");
            }
        }

        public void AddSystemMessage(string message)
        {
            var systemMessage = new ChatMessage(
                "SYSTEM",
                "시스템",
                message,
                ChatMessageType.System
            );
            AddMessage(systemMessage);
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
                _ => eventType,
            };

            ChatMessageType messageType = eventType switch
            {
                "player_died" => ChatMessageType.Death,
                "player_escaped" => ChatMessageType.Escape,
                _ => ChatMessageType.System,
            };

            var gameMessage = new ChatMessage("SYSTEM", "게임", message, messageType);
            AddMessage(gameMessage);
        }

        public void ClearChatHistory()
        {
            _chatHistory.Clear();
            ChatHistoryCleared?.Invoke();
            OnPropertyChanged(nameof(ChatHistory));
            OnPropertyChanged(nameof(MessageCount));

            if (EnableDebugLogs)
            {
                Debug.Log("[ChatViewModel] 채팅 기록 초기화");
            }
        }

        #endregion

        #region Quick Chat Management

        public void SetQuickChatMessages(string[] messages)
        {
            QuickChatMessages = messages;
        }

        public void AddQuickChatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var newMessages = new List<string>(_quickChatMessages) { message };
            QuickChatMessages = newMessages.ToArray();
        }

        public void RemoveQuickChatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var newMessages = new List<string>(_quickChatMessages);
            newMessages.Remove(message);
            QuickChatMessages = newMessages.ToArray();
        }

        #endregion

        #region Utility Methods

        public Color GetMessageColor(ChatMessageType type)
        {
            return type switch
            {
                ChatMessageType.System => Color.yellow,
                ChatMessageType.QuickChat => Color.cyan,
                ChatMessageType.Warning => Color.red,
                ChatMessageType.Death => Color.red,
                ChatMessageType.Escape => Color.green,
                _ => Color.white,
            };
        }

        public List<ChatMessage> GetRecentMessages(int count)
        {
            if (count <= 0)
                return new List<ChatMessage>();

            int startIndex = Mathf.Max(0, _chatHistory.Count - count);
            int actualCount = Mathf.Min(count, _chatHistory.Count);

            return _chatHistory.GetRange(startIndex, actualCount);
        }

        public List<ChatMessage> GetMessagesByType(ChatMessageType type)
        {
            return _chatHistory.FindAll(msg => msg.type == type);
        }

        #endregion

        #region BaseViewModel Override

        protected override void InitializeViewModel()
        {
            base.InitializeViewModel();

            _isChatOpen = false;
            _currentMessage = "";
            _maxChatMessages = 50;
            _messageDisplayDuration = 10f;
            _enableQuickChat = true;
            _chatHistory = new List<ChatMessage>();

            if (EnableDebugLogs)
            {
                Debug.Log("[ChatViewModel] 초기화 완료");
            }
        }

        protected override void CleanupViewModel()
        {
            base.CleanupViewModel();

            ChatToggled = null;
            MessageAdded = null;
            MessageReceived = null;
            MessageSent = null;
            ChatHistoryCleared = null;
            QuickChatMessagesChanged = null;

            if (EnableDebugLogs)
            {
                Debug.Log("[ChatViewModel] 정리 완료");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log(
                $"[ChatViewModel] State:\n"
                    + $"  IsChatOpen: {IsChatOpen}\n"
                    + $"  CurrentMessage: '{CurrentMessage}'\n"
                    + $"  MessageCount: {MessageCount}/{MaxChatMessages}\n"
                    + $"  EnableQuickChat: {EnableQuickChat}\n"
                    + $"  QuickChatMessages: {QuickChatMessages.Length}개"
            );
        }

        [ContextMenu("Add Test Message")]
        private void AddTestMessage()
        {
            AddSystemMessage("테스트 메시지입니다.");
        }

        [ContextMenu("Send Test Quick Chat")]
        private void SendTestQuickChat()
        {
            if (QuickChatMessages.Length > 0)
            {
                SendQuickChatMessage(QuickChatMessages[0]);
            }
        }

        [ContextMenu("Toggle Chat")]
        private void DebugToggleChat() => ToggleChat();

        #endregion
    }
}
