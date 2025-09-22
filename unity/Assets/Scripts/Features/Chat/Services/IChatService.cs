using System.Collections.Generic;
using Features.Chat.Models;
using R3;
using UnityEngine;

namespace Features.Chat.Services
{
    /// <summary>
    /// 채팅 시스템 관리 서비스 인터페이스
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 채팅 열림 상태 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsChatOpen { get; }

        /// <summary>
        /// 현재 입력 메시지 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<string> CurrentMessage { get; }

        /// <summary>
        /// 채팅 메시지 개수 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<int> MessageCount { get; }

        /// <summary>
        /// 채팅 기록 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<List<ChatMessage>> ChatHistory { get; }

        /// <summary>
        /// 퀵 채팅 활성화 상태 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<bool> EnableQuickChat { get; }

        /// <summary>
        /// 퀵 채팅 메시지 목록 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<string[]> QuickChatMessages { get; }

        /// <summary>
        /// 최대 채팅 메시지 수 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<int> MaxChatMessages { get; }

        /// <summary>
        /// 메시지 표시 시간 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<float> MessageDisplayDuration { get; }

        /// <summary>
        /// 채팅 열림/닫힘 토글
        /// </summary>
        void ToggleChat();

        /// <summary>
        /// 채팅 열림 상태 설정
        /// </summary>
        void SetChatOpen(bool isOpen);

        /// <summary>
        /// 메시지 전송
        /// </summary>
        void SendMessage(string message, string playerId = "", string playerName = "");

        /// <summary>
        /// 퀵 채팅 메시지 전송
        /// </summary>
        void SendQuickChatMessage(string message, string playerId = "", string playerName = "");

        /// <summary>
        /// 메시지 수신 (네트워크에서)
        /// </summary>
        void ReceiveMessage(string playerId, string playerName, string message, ChatMessageType type = ChatMessageType.Normal);

        /// <summary>
        /// 메시지 추가 (로컬)
        /// </summary>
        void AddMessage(ChatMessage message);

        /// <summary>
        /// 시스템 메시지 추가
        /// </summary>
        void AddSystemMessage(string message);

        /// <summary>
        /// 게임 이벤트 메시지 추가
        /// </summary>
        void AddGameEventMessage(string eventType, string playerName = "");

        /// <summary>
        /// 채팅 기록 초기화
        /// </summary>
        void ClearChatHistory();

        /// <summary>
        /// 현재 입력 메시지 설정
        /// </summary>
        void SetCurrentMessage(string message);

        /// <summary>
        /// 채팅 설정 변경
        /// </summary>
        void SetChatSettings(int maxMessages, float displayDuration, bool enableQuickChat);

        /// <summary>
        /// 퀵 채팅 메시지 설정
        /// </summary>
        void SetQuickChatMessages(string[] messages);

        /// <summary>
        /// 퀵 채팅 메시지 추가
        /// </summary>
        void AddQuickChatMessage(string message);

        /// <summary>
        /// 퀵 채팅 메시지 제거
        /// </summary>
        void RemoveQuickChatMessage(string message);

        // 유틸리티 메서드들
        /// <summary>
        /// 최근 메시지 가져오기
        /// </summary>
        List<ChatMessage> GetRecentMessages(int count);

        /// <summary>
        /// 타입별 메시지 가져오기
        /// </summary>
        List<ChatMessage> GetMessagesByType(ChatMessageType type);

        /// <summary>
        /// 메시지 타입에 따른 색상 가져오기
        /// </summary>
        Color GetMessageColor(ChatMessageType type);

        /// <summary>
        /// 만료된 메시지들 정리
        /// </summary>
        void CleanupExpiredMessages();

        /// <summary>
        /// 특정 플레이어의 메시지 가져오기
        /// </summary>
        List<ChatMessage> GetMessagesByPlayer(string playerId);

        /// <summary>
        /// 채팅 통계 정보 가져오기
        /// </summary>
        (int total, int system, int normal, int quickChat) GetChatStatistics();
    }
}