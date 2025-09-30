using System;
using System.Collections.Generic;
using Features.Notification.Messages;
using Features.Notification.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Notification.Services
{
    /// <summary>
    /// 알림 시스템 관리 서비스 구현체
    /// </summary>
    public class NotificationServiceImpl : INotificationService, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<bool> IsShowing => _isShowing;
        public ReadOnlyReactiveProperty<string> CurrentMessage => _currentMessage;
        public ReadOnlyReactiveProperty<int> QueueCount => _queueCount;
        public ReadOnlyReactiveProperty<bool> HasQueuedNotifications => _hasQueuedNotifications;
        public ReadOnlyReactiveProperty<float> DefaultDisplayDuration => _defaultDisplayDuration;
        public ReadOnlyReactiveProperty<float> FadeInDuration => _fadeInDuration;
        public ReadOnlyReactiveProperty<float> FadeOutDuration => _fadeOutDuration;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<bool> _isShowing = new(false);
        private readonly ReactiveProperty<string> _currentMessage = new("");
        private readonly ReactiveProperty<int> _queueCount = new(0);
        private readonly ReactiveProperty<bool> _hasQueuedNotifications = new(false);
        private readonly ReactiveProperty<float> _defaultDisplayDuration = new(2.2f);
        private readonly ReactiveProperty<float> _fadeInDuration = new(0.3f);
        private readonly ReactiveProperty<float> _fadeOutDuration = new(0.5f);

        private readonly NotificationModel _notificationModel = new();
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = false;

        #endregion

        #region Dependencies

        private readonly IPublisher<NotificationShowMessage> _showPublisher;
        private readonly IPublisher<NotificationQueuedMessage> _queuedPublisher;
        private readonly IPublisher<NotificationHideMessage> _hidePublisher;
        private readonly IPublisher<NotificationVisibilityChangedMessage> _visibilityChangedPublisher;
        private readonly IPublisher<NotificationQueueProcessMessage> _queueProcessPublisher;
        private readonly IPublisher<NotificationQueueClearedMessage> _queueClearedPublisher;
        private readonly IPublisher<NotificationAnimationCompleteMessage> _animationCompletePublisher;
        private readonly IPublisher<GameEventNotificationMessage> _gameEventPublisher;
        private readonly IPublisher<PlayerEventNotificationMessage> _playerEventPublisher;
        private readonly IPublisher<WarningNotificationMessage> _warningPublisher;
        private readonly IPublisher<SuccessNotificationMessage> _successPublisher;

        #endregion

        #region Constructor

        [Inject]
        public NotificationServiceImpl(
            IPublisher<NotificationShowMessage> showPublisher,
            IPublisher<NotificationQueuedMessage> queuedPublisher,
            IPublisher<NotificationHideMessage> hidePublisher,
            IPublisher<NotificationVisibilityChangedMessage> visibilityChangedPublisher,
            IPublisher<NotificationQueueProcessMessage> queueProcessPublisher,
            IPublisher<NotificationQueueClearedMessage> queueClearedPublisher,
            IPublisher<NotificationAnimationCompleteMessage> animationCompletePublisher,
            IPublisher<GameEventNotificationMessage> gameEventPublisher,
            IPublisher<PlayerEventNotificationMessage> playerEventPublisher,
            IPublisher<WarningNotificationMessage> warningPublisher,
            IPublisher<SuccessNotificationMessage> successPublisher
        )
        {
            _showPublisher = showPublisher;
            _queuedPublisher = queuedPublisher;
            _hidePublisher = hidePublisher;
            _visibilityChangedPublisher = visibilityChangedPublisher;
            _queueProcessPublisher = queueProcessPublisher;
            _queueClearedPublisher = queueClearedPublisher;
            _animationCompletePublisher = animationCompletePublisher;
            _gameEventPublisher = gameEventPublisher;
            _playerEventPublisher = playerEventPublisher;
            _warningPublisher = warningPublisher;
            _successPublisher = successPublisher;

            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_enableDebugLogs)
            {
                Debug.Log("[NotificationServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void ShowNotification(string message, float duration = -1f, NotificationType type = NotificationType.Info)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // 이미 알림이 표시 중이면 큐에 추가
            if (_notificationModel.isShowing)
            {
                DebugLog($"알림 표시 중 - 큐에 추가: {message}");
                QueueNotification(message, duration, type);
                return;
            }

            // 표시 중이 아니면 즉시 표시
            float displayDuration = _notificationModel.GetValidDuration(duration);
            var notification = new NotificationData(message, displayDuration, type);

            _notificationModel.SetCurrentNotification(notification);
            UpdateObservables();

            _showPublisher.Publish(new NotificationShowMessage(message, displayDuration, type));
            _visibilityChangedPublisher.Publish(new NotificationVisibilityChangedMessage(true, message));

            DebugLog($"알림 표시: {message} ({displayDuration}초, {type})");
        }

        public void QueueNotification(string message, float duration = -1f, NotificationType type = NotificationType.Info)
        {
            if (string.IsNullOrEmpty(message))
                return;

            float displayDuration = _notificationModel.GetValidDuration(duration);
            var notification = new NotificationData(message, displayDuration, type);

            _notificationModel.QueueNotification(notification);
            UpdateObservables();

            _queuedPublisher.Publish(new NotificationQueuedMessage(notification, _notificationModel.QueueCount));

            if (!_notificationModel.isShowing)
            {
                ProcessNotificationQueue();
            }

            DebugLog($"알림 큐에 추가: {message} (큐 크기: {_notificationModel.QueueCount})");
        }

        public void HideNotification(bool immediate = false)
        {
            _notificationModel.HideCurrentNotification();
            UpdateObservables();

            _hidePublisher.Publish(new NotificationHideMessage(immediate));
            _visibilityChangedPublisher.Publish(new NotificationVisibilityChangedMessage(false, ""));

            DebugLog($"알림 숨김 {(immediate ? "(즉시)" : "(애니메이션)")}");
        }

        public void ProcessNotificationQueue()
        {
            if (_notificationModel.HasQueuedNotifications && !_notificationModel.isShowing)
            {
                var notification = _notificationModel.DequeueNotification();
                if (notification != null)
                {
                    _notificationModel.SetCurrentNotification(notification);
                    UpdateObservables();

                    _showPublisher.Publish(new NotificationShowMessage(notification.message, notification.duration, notification.type));
                    _queueProcessPublisher.Publish(new NotificationQueueProcessMessage(notification, _notificationModel.QueueCount));

                    DebugLog($"큐에서 알림 처리: {notification.message}");
                }
            }
        }

        public void ClearNotificationQueue()
        {
            int clearedCount = _notificationModel.QueueCount;
            _notificationModel.ClearQueue();
            UpdateObservables();

            _queueClearedPublisher.Publish(new NotificationQueueClearedMessage(clearedCount));

            DebugLog($"알림 큐 초기화: {clearedCount}개 제거");
        }

        public void SetAnimationDurations(float fadeIn, float fadeOut)
        {
            _notificationModel.defaultFadeInDuration = Mathf.Max(0f, fadeIn);
            _notificationModel.defaultFadeOutDuration = Mathf.Max(0f, fadeOut);

            _fadeInDuration.Value = _notificationModel.defaultFadeInDuration;
            _fadeOutDuration.Value = _notificationModel.defaultFadeOutDuration;

            DebugLog($"애니메이션 시간 설정: FadeIn={fadeIn}s, FadeOut={fadeOut}s");
        }

        public void SetDefaultDisplayDuration(float duration)
        {
            _notificationModel.defaultDisplayDuration = Mathf.Max(0f, duration);
            _defaultDisplayDuration.Value = _notificationModel.defaultDisplayDuration;

            DebugLog($"기본 표시 시간 설정: {duration}s");
        }

        public void SetMaxQueueSize(int maxSize)
        {
            _notificationModel.maxQueueSize = Mathf.Max(1, maxSize);
            DebugLog($"최대 큐 크기 설정: {maxSize}");
        }

        // 미리 정의된 알림들
        public void ShowGameStartNotification()
        {
            ShowNotification("게임이 시작되었습니다!", 3f, NotificationType.Game);
            _gameEventPublisher.Publish(new GameEventNotificationMessage("game_start", "게임이 시작되었습니다!", 3f));
        }

        public void ShowGameEndNotification()
        {
            ShowNotification("게임이 종료되었습니다.", 3f, NotificationType.Game);
            _gameEventPublisher.Publish(new GameEventNotificationMessage("game_end", "게임이 종료되었습니다.", 3f));
        }

        public void ShowPlayerJoinedNotification(string playerName)
        {
            string message = $"{playerName}님이 게임에 참가했습니다.";
            ShowNotification(message, 2.5f, NotificationType.Player);
            _playerEventPublisher.Publish(new PlayerEventNotificationMessage(0, playerName, "joined", message, 2.5f));
        }

        public void ShowPlayerLeftNotification(string playerName)
        {
            string message = $"{playerName}님이 게임을 떠났습니다.";
            ShowNotification(message, 2.5f, NotificationType.Player);
            _playerEventPublisher.Publish(new PlayerEventNotificationMessage(0, playerName, "left", message, 2.5f));
        }

        public void ShowPlayerDiedNotification(string playerName)
        {
            string message = $"{playerName}님이 사망했습니다.";
            ShowNotification(message, 3f, NotificationType.Player);
            _playerEventPublisher.Publish(new PlayerEventNotificationMessage(0, playerName, "died", message, 3f));
        }

        public void ShowPlayerEscapedNotification(string playerName)
        {
            string message = $"{playerName}님이 탈출했습니다!";
            ShowNotification(message, 3f, NotificationType.Player);
            _playerEventPublisher.Publish(new PlayerEventNotificationMessage(0, playerName, "escaped", message, 3f));
        }

        public void ShowObjectiveNotification(string objective)
        {
            string message = $"목표: {objective}";
            ShowNotification(message, 4f, NotificationType.Info);
        }

        public void ShowWarningNotification(string warning)
        {
            string message = $"⚠️ {warning}";
            ShowNotification(message, 3f, NotificationType.Warning);
            _warningPublisher.Publish(new WarningNotificationMessage(warning, 3f, false));
        }

        public void ShowSuccessNotification(string success)
        {
            string message = $"✅ {success}";
            ShowNotification(message, 2.5f, NotificationType.Success);
            _successPublisher.Publish(new SuccessNotificationMessage(success, 2.5f));
        }

        public void ShowItemCannotBeUsedNotification(string itemName, string reason)
        {
            string message = $"⚠️ {itemName}을(를) 사용할 수 없습니다: {reason}";
            ShowNotification(message, 2.5f, NotificationType.Warning);
            _warningPublisher.Publish(new WarningNotificationMessage(message, 2.5f, false));
        }

        // 유틸리티 메서드들
        public List<NotificationData> GetQueuedNotifications()
        {
            return _notificationModel.GetQueuedNotifications();
        }

        public NotificationData PeekNextNotification()
        {
            return _notificationModel.PeekNextNotification();
        }

        public NotificationData GetCurrentNotification()
        {
            return _notificationModel.currentNotification;
        }

        public void CleanupExpiredNotifications()
        {
            _notificationModel.CleanupExpiredNotifications(TimeSpan.FromMinutes(5));
            UpdateObservables();
            DebugLog("만료된 알림들 정리 완료");
        }

        #endregion

        #region Private Methods

        private void UpdateObservables()
        {
            _isShowing.Value = _notificationModel.isShowing;
            _currentMessage.Value = _notificationModel.currentMessage;
            _queueCount.Value = _notificationModel.QueueCount;
            _hasQueuedNotifications.Value = _notificationModel.HasQueuedNotifications;
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[NotificationServiceImpl] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _isShowing?.Dispose();
            _currentMessage?.Dispose();
            _queueCount?.Dispose();
            _hasQueuedNotifications?.Dispose();
            _defaultDisplayDuration?.Dispose();
            _fadeInDuration?.Dispose();
            _fadeOutDuration?.Dispose();

            DebugLog("Dispose 완료");
        }

        #endregion
    }
}