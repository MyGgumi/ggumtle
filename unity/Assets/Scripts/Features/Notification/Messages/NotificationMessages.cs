using Features.Notification.Models;

namespace Features.Notification.Messages
{
    /// <summary>
    /// MessagePipe용 기본 알림 메시지
    /// </summary>
    public readonly struct NotificationMessage
    {
        public readonly string message;
        public readonly float duration;
        public readonly NotificationType type;

        public NotificationMessage(string message, float duration, NotificationType type = NotificationType.Info)
        {
            this.message = message;
            this.duration = duration;
            this.type = type;
        }
    }
    /// <summary>
    /// 알림 표시 요청 메시지
    /// </summary>
    public readonly struct NotificationShowMessage
    {
        public readonly string message;
        public readonly float duration;
        public readonly NotificationType type;

        public NotificationShowMessage(string message, float duration, NotificationType type)
        {
            this.message = message;
            this.duration = duration;
            this.type = type;
        }
    }

    /// <summary>
    /// 알림 큐 추가 메시지
    /// </summary>
    public readonly struct NotificationQueuedMessage
    {
        public readonly NotificationData notification;
        public readonly int queueCount;

        public NotificationQueuedMessage(NotificationData notification, int queueCount)
        {
            this.notification = notification;
            this.queueCount = queueCount;
        }
    }

    /// <summary>
    /// 알림 숨김 요청 메시지
    /// </summary>
    public readonly struct NotificationHideMessage
    {
        public readonly bool immediate;

        public NotificationHideMessage(bool immediate)
        {
            this.immediate = immediate;
        }
    }

    /// <summary>
    /// 알림 가시성 변경 메시지
    /// </summary>
    public readonly struct NotificationVisibilityChangedMessage
    {
        public readonly bool isShowing;
        public readonly string currentMessage;

        public NotificationVisibilityChangedMessage(bool isShowing, string currentMessage)
        {
            this.isShowing = isShowing;
            this.currentMessage = currentMessage;
        }
    }

    /// <summary>
    /// 알림 큐 처리 메시지
    /// </summary>
    public readonly struct NotificationQueueProcessMessage
    {
        public readonly NotificationData processedNotification;
        public readonly int remainingCount;

        public NotificationQueueProcessMessage(NotificationData processedNotification, int remainingCount)
        {
            this.processedNotification = processedNotification;
            this.remainingCount = remainingCount;
        }
    }

    /// <summary>
    /// 알림 큐 초기화 메시지
    /// </summary>
    public readonly struct NotificationQueueClearedMessage
    {
        public readonly int clearedCount;

        public NotificationQueueClearedMessage(int clearedCount)
        {
            this.clearedCount = clearedCount;
        }
    }

    /// <summary>
    /// 알림 애니메이션 완료 메시지
    /// </summary>
    public readonly struct NotificationAnimationCompleteMessage
    {
        public readonly bool wasShowing;
        public readonly string message;

        public NotificationAnimationCompleteMessage(bool wasShowing, string message)
        {
            this.wasShowing = wasShowing;
            this.message = message;
        }
    }

    /// <summary>
    /// 게임 이벤트 알림 메시지
    /// </summary>
    public readonly struct GameEventNotificationMessage
    {
        public readonly string eventType;
        public readonly string message;
        public readonly float duration;

        public GameEventNotificationMessage(string eventType, string message, float duration)
        {
            this.eventType = eventType;
            this.message = message;
            this.duration = duration;
        }
    }

    /// <summary>
    /// 플레이어 이벤트 알림 메시지
    /// </summary>
    public readonly struct PlayerEventNotificationMessage
    {
        public readonly int playerId;
        public readonly string playerName;
        public readonly string eventType;
        public readonly string message;
        public readonly float duration;

        public PlayerEventNotificationMessage(int playerId, string playerName, string eventType, string message, float duration)
        {
            this.playerId = playerId;
            this.playerName = playerName;
            this.eventType = eventType;
            this.message = message;
            this.duration = duration;
        }
    }

    /// <summary>
    /// 경고 알림 메시지
    /// </summary>
    public readonly struct WarningNotificationMessage
    {
        public readonly string warning;
        public readonly float duration;
        public readonly bool isUrgent;

        public WarningNotificationMessage(string warning, float duration, bool isUrgent)
        {
            this.warning = warning;
            this.duration = duration;
            this.isUrgent = isUrgent;
        }
    }

    /// <summary>
    /// 성공 알림 메시지
    /// </summary>
    public readonly struct SuccessNotificationMessage
    {
        public readonly string success;
        public readonly float duration;

        public SuccessNotificationMessage(string success, float duration)
        {
            this.success = success;
            this.duration = duration;
        }
    }

    /// <summary>
    /// 알림 만료 메시지 (MainLifetimeScope 호환)
    /// </summary>
    public readonly struct NotificationExpiredMessage
    {
        public readonly string expiredMessage;
        public readonly float expiredDuration;

        public NotificationExpiredMessage(string expiredMessage, float expiredDuration)
        {
            this.expiredMessage = expiredMessage;
            this.expiredDuration = expiredDuration;
        }
    }

    /// <summary>
    /// 알림 초기화 메시지 (MainLifetimeScope 호환)
    /// </summary>
    public readonly struct NotificationClearedMessage
    {
        public readonly int clearedCount;
        public readonly bool wasForced;

        public NotificationClearedMessage(int clearedCount, bool wasForced = false)
        {
            this.clearedCount = clearedCount;
            this.wasForced = wasForced;
        }
    }

    /// <summary>
    /// 알림 설정 변경 메시지 (MainLifetimeScope 호환)
    /// </summary>
    public readonly struct NotificationSettingsChangedMessage
    {
        public readonly float defaultDuration;
        public readonly float fadeInDuration;
        public readonly float fadeOutDuration;
        public readonly int maxQueueSize;

        public NotificationSettingsChangedMessage(float defaultDuration, float fadeInDuration, float fadeOutDuration, int maxQueueSize)
        {
            this.defaultDuration = defaultDuration;
            this.fadeInDuration = fadeInDuration;
            this.fadeOutDuration = fadeOutDuration;
            this.maxQueueSize = maxQueueSize;
        }
    }
}