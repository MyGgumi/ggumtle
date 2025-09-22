using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Notification.Models
{
    /// <summary>
    /// 알림 타입 정의
    /// </summary>
    public enum NotificationType
    {
        Info,       // 일반 정보
        Warning,    // 경고
        Success,    // 성공
        Error,      // 오류
        Game,       // 게임 이벤트
        Player      // 플레이어 이벤트
    }

    /// <summary>
    /// 개별 알림 데이터
    /// </summary>
    [Serializable]
    public class NotificationData
    {
        [Header("Notification Info")]
        public string message;
        public float duration;
        public NotificationType type;
        public DateTime timestamp;

        [Header("Animation Settings")]
        public float fadeInDuration;
        public float fadeOutDuration;

        public NotificationData(string message, float duration = -1f, NotificationType type = NotificationType.Info)
        {
            this.message = message;
            this.duration = duration;
            this.type = type;
            this.timestamp = DateTime.Now;
            this.fadeInDuration = 0.3f;
            this.fadeOutDuration = 0.5f;
        }

        public NotificationData(string message, float duration, NotificationType type, float fadeIn, float fadeOut)
        {
            this.message = message;
            this.duration = duration;
            this.type = type;
            this.timestamp = DateTime.Now;
            this.fadeInDuration = fadeIn;
            this.fadeOutDuration = fadeOut;
        }

        /// <summary>
        /// 알림 데이터 복사
        /// </summary>
        public NotificationData Clone()
        {
            return new NotificationData(message, duration, type, fadeInDuration, fadeOutDuration)
            {
                timestamp = this.timestamp
            };
        }

        /// <summary>
        /// 만료 여부 확인 (일정 시간 후 자동 제거용)
        /// </summary>
        public bool IsExpired(TimeSpan maxAge)
        {
            return DateTime.Now - timestamp > maxAge;
        }
    }

    /// <summary>
    /// 알림 시스템 전체 데이터 관리
    /// </summary>
    [Serializable]
    public class NotificationModel
    {
        [Header("Notification State")]
        public bool isShowing = false;
        public string currentMessage = "";
        public NotificationData currentNotification = null;

        [Header("Queue Management")]
        public Queue<NotificationData> notificationQueue = new();
        public int maxQueueSize = 10;

        [Header("Default Settings")]
        public float defaultDisplayDuration = 2.2f;
        public float defaultFadeInDuration = 0.3f;
        public float defaultFadeOutDuration = 0.5f;

        /// <summary>
        /// 큐에 있는 알림 개수
        /// </summary>
        public int QueueCount => notificationQueue.Count;

        /// <summary>
        /// 큐에 대기 중인 알림이 있는지 여부
        /// </summary>
        public bool HasQueuedNotifications => notificationQueue.Count > 0;

        /// <summary>
        /// 알림 큐에 추가
        /// </summary>
        public void QueueNotification(NotificationData notification)
        {
            // 큐가 가득 찬 경우 가장 오래된 항목 제거
            if (notificationQueue.Count >= maxQueueSize)
            {
                notificationQueue.Dequeue();
            }

            notificationQueue.Enqueue(notification);
        }

        /// <summary>
        /// 다음 알림 가져오기
        /// </summary>
        public NotificationData DequeueNotification()
        {
            return notificationQueue.Count > 0 ? notificationQueue.Dequeue() : null;
        }

        /// <summary>
        /// 다음 알림 확인 (제거하지 않음)
        /// </summary>
        public NotificationData PeekNextNotification()
        {
            return notificationQueue.Count > 0 ? notificationQueue.Peek() : null;
        }

        /// <summary>
        /// 모든 큐 초기화
        /// </summary>
        public void ClearQueue()
        {
            notificationQueue.Clear();
        }

        /// <summary>
        /// 큐의 모든 알림 리스트로 반환
        /// </summary>
        public List<NotificationData> GetQueuedNotifications()
        {
            return new List<NotificationData>(notificationQueue);
        }

        /// <summary>
        /// 현재 표시 중인 알림 설정
        /// </summary>
        public void SetCurrentNotification(NotificationData notification)
        {
            currentNotification = notification;
            currentMessage = notification?.message ?? "";
            isShowing = notification != null;
        }

        /// <summary>
        /// 현재 알림 숨김
        /// </summary>
        public void HideCurrentNotification()
        {
            currentNotification = null;
            currentMessage = "";
            isShowing = false;
        }

        /// <summary>
        /// 유효한 지속 시간 반환 (기본값 적용)
        /// </summary>
        public float GetValidDuration(float duration)
        {
            return duration > 0 ? duration : defaultDisplayDuration;
        }

        /// <summary>
        /// 만료된 큐 항목들 정리
        /// </summary>
        public void CleanupExpiredNotifications(TimeSpan maxAge)
        {
            var tempQueue = new Queue<NotificationData>();
            while (notificationQueue.Count > 0)
            {
                var notification = notificationQueue.Dequeue();
                if (!notification.IsExpired(maxAge))
                {
                    tempQueue.Enqueue(notification);
                }
            }
            notificationQueue = tempQueue;
        }
    }
}