using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Chat.Models
{
    /// <summary>
    /// 채팅 메시지 타입 정의
    /// </summary>
    public enum ChatMessageType
    {
        Normal,     // 일반 메시지
        System,     // 시스템 메시지
        QuickChat,  // 퀵 채팅
        Warning,    // 경고
        Death,      // 사망 관련
        Escape,     // 탈출 관련
        Game        // 게임 이벤트
    }

    /// <summary>
    /// 개별 채팅 메시지 데이터
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        [Header("Message Info")]
        public string playerId;
        public string playerName;
        public string message;
        public DateTime timestamp;
        public ChatMessageType type;

        [Header("Display Settings")]
        public bool isVisible = true;
        public float displayDuration = 10f;

        public ChatMessage()
        {
            playerId = "";
            playerName = "";
            message = "";
            timestamp = DateTime.Now;
            type = ChatMessageType.Normal;
        }

        public ChatMessage(string id, string name, string msg, ChatMessageType msgType = ChatMessageType.Normal)
        {
            playerId = id;
            playerName = name;
            message = msg;
            timestamp = DateTime.Now;
            type = msgType;
            isVisible = true;
            displayDuration = 10f;
        }

        /// <summary>
        /// 메시지 데이터 복사
        /// </summary>
        public ChatMessage Clone()
        {
            return new ChatMessage(playerId, playerName, message, type)
            {
                timestamp = this.timestamp,
                isVisible = this.isVisible,
                displayDuration = this.displayDuration
            };
        }

        /// <summary>
        /// 메시지 만료 여부 확인
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.Now - timestamp > TimeSpan.FromSeconds(displayDuration);
        }

        /// <summary>
        /// 시간 문자열 포맷
        /// </summary>
        public string GetTimeString()
        {
            return timestamp.ToString("HH:mm");
        }
    }

    /// <summary>
    /// 채팅 시스템 전체 데이터 관리
    /// </summary>
    [Serializable]
    public class ChatModel
    {
        [Header("Chat State")]
        public bool isChatOpen = false;
        public string currentMessage = "";

        [Header("Message History")]
        public List<ChatMessage> chatHistory = new();
        public int maxChatMessages = 50;

        [Header("Settings")]
        public float messageDisplayDuration = 10f;
        public bool enableQuickChat = true;
        public string[] quickChatMessages = {
            "도와줘!",
            "근처야",
            "위험!",
            "따라와!",
            "가는중!",
            "해결됨"
        };

        /// <summary>
        /// 메시지 개수
        /// </summary>
        public int MessageCount => chatHistory.Count;

        /// <summary>
        /// 채팅 열림/닫힘 토글
        /// </summary>
        public void ToggleChat()
        {
            isChatOpen = !isChatOpen;
        }

        /// <summary>
        /// 메시지 추가
        /// </summary>
        public void AddMessage(ChatMessage message)
        {
            if (message == null)
                return;

            chatHistory.Add(message);

            // 최대 메시지 수 제한
            if (chatHistory.Count > maxChatMessages)
            {
                chatHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 시스템 메시지 추가
        /// </summary>
        public void AddSystemMessage(string message)
        {
            var systemMessage = new ChatMessage("SYSTEM", "시스템", message, ChatMessageType.System);
            AddMessage(systemMessage);
        }

        /// <summary>
        /// 게임 이벤트 메시지 추가
        /// </summary>
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

        /// <summary>
        /// 채팅 기록 초기화
        /// </summary>
        public void ClearChatHistory()
        {
            chatHistory.Clear();
        }

        /// <summary>
        /// 최근 메시지 가져오기
        /// </summary>
        public List<ChatMessage> GetRecentMessages(int count)
        {
            if (count <= 0)
                return new List<ChatMessage>();

            int startIndex = Mathf.Max(0, chatHistory.Count - count);
            int actualCount = Mathf.Min(count, chatHistory.Count);

            return chatHistory.GetRange(startIndex, actualCount);
        }

        /// <summary>
        /// 타입별 메시지 가져오기
        /// </summary>
        public List<ChatMessage> GetMessagesByType(ChatMessageType type)
        {
            return chatHistory.FindAll(msg => msg.type == type);
        }

        /// <summary>
        /// 메시지 타입에 따른 색상 반환
        /// </summary>
        public Color GetMessageColor(ChatMessageType type)
        {
            return type switch
            {
                ChatMessageType.System => Color.yellow,
                ChatMessageType.QuickChat => Color.cyan,
                ChatMessageType.Warning => Color.red,
                ChatMessageType.Death => Color.red,
                ChatMessageType.Escape => Color.green,
                ChatMessageType.Game => Color.magenta,
                _ => Color.white,
            };
        }

        /// <summary>
        /// 퀵 채팅 메시지 설정
        /// </summary>
        public void SetQuickChatMessages(string[] messages)
        {
            quickChatMessages = messages ?? new string[0];
        }

        /// <summary>
        /// 퀵 채팅 메시지 추가
        /// </summary>
        public void AddQuickChatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var newMessages = new List<string>(quickChatMessages) { message };
            quickChatMessages = newMessages.ToArray();
        }

        /// <summary>
        /// 퀵 채팅 메시지 제거
        /// </summary>
        public void RemoveQuickChatMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var newMessages = new List<string>(quickChatMessages);
            newMessages.Remove(message);
            quickChatMessages = newMessages.ToArray();
        }

        /// <summary>
        /// 만료된 메시지들 정리
        /// </summary>
        public void CleanupExpiredMessages()
        {
            chatHistory.RemoveAll(msg => msg.IsExpired());
        }
    }
}