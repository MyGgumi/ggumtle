using System;
using System.Collections.Generic;
using Features.Notification.Messages;
using Features.Notification.Models;
using Features.Notification.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Notification.ViewModels
{
    /// <summary>
    /// 알림 시스템 ViewModel
    /// R3 + MessagePipe 기반의 반응형 ViewModel
    /// </summary>
    public class NotificationViewModel : IDisposable
    {
        #region Observable Properties

        // 알림 상태
        public readonly ReadOnlyReactiveProperty<bool> IsShowing;
        public readonly ReadOnlyReactiveProperty<string> CurrentMessage;
        public readonly ReadOnlyReactiveProperty<int> QueueCount;
        public readonly ReadOnlyReactiveProperty<bool> HasQueuedNotifications;

        // 애니메이션 설정
        public readonly ReadOnlyReactiveProperty<float> DefaultDisplayDuration;
        public readonly ReadOnlyReactiveProperty<float> FadeInDuration;
        public readonly ReadOnlyReactiveProperty<float> FadeOutDuration;

        // UI 상태 (계산된 속성들)
        public readonly ReadOnlyReactiveProperty<bool> ShouldShowBanner;
        public readonly ReadOnlyReactiveProperty<NotificationType> CurrentNotificationType;

        #endregion

        #region Dependencies

        private readonly INotificationService _notificationService;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly ReactiveProperty<NotificationType> _currentNotificationType = new(NotificationType.Info);
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public NotificationViewModel(
            INotificationService notificationService,
            ISubscriber<NotificationShowMessage> showSubscriber,
            ISubscriber<NotificationQueuedMessage> queuedSubscriber,
            ISubscriber<NotificationHideMessage> hideSubscriber,
            ISubscriber<NotificationVisibilityChangedMessage> visibilityChangedSubscriber,
            ISubscriber<NotificationQueueProcessMessage> queueProcessSubscriber,
            ISubscriber<NotificationQueueClearedMessage> queueClearedSubscriber,
            ISubscriber<NotificationAnimationCompleteMessage> animationCompleteSubscriber,
            ISubscriber<GameEventNotificationMessage> gameEventSubscriber,
            ISubscriber<PlayerEventNotificationMessage> playerEventSubscriber,
            ISubscriber<WarningNotificationMessage> warningSubscriber,
            ISubscriber<SuccessNotificationMessage> successSubscriber
        )
        {
            _notificationService = notificationService;

            // Service의 Observable 속성들을 직접 연결
            IsShowing = _notificationService.IsShowing;
            CurrentMessage = _notificationService.CurrentMessage;
            QueueCount = _notificationService.QueueCount;
            HasQueuedNotifications = _notificationService.HasQueuedNotifications;
            DefaultDisplayDuration = _notificationService.DefaultDisplayDuration;
            FadeInDuration = _notificationService.FadeInDuration;
            FadeOutDuration = _notificationService.FadeOutDuration;

            // 계산된 속성들
            ShouldShowBanner = IsShowing.CombineLatest(CurrentMessage, (isShowing, message) =>
                isShowing && !string.IsNullOrEmpty(message))
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            CurrentNotificationType = _currentNotificationType.ToReadOnlyReactiveProperty().AddTo(_disposables);

            // 메시지 구독
            showSubscriber.Subscribe(OnNotificationShow).AddTo(_disposables);
            queuedSubscriber.Subscribe(OnNotificationQueued).AddTo(_disposables);
            hideSubscriber.Subscribe(OnNotificationHide).AddTo(_disposables);
            visibilityChangedSubscriber.Subscribe(OnVisibilityChanged).AddTo(_disposables);
            queueProcessSubscriber.Subscribe(OnQueueProcess).AddTo(_disposables);
            queueClearedSubscriber.Subscribe(OnQueueCleared).AddTo(_disposables);
            animationCompleteSubscriber.Subscribe(OnAnimationComplete).AddTo(_disposables);
            gameEventSubscriber.Subscribe(OnGameEvent).AddTo(_disposables);
            playerEventSubscriber.Subscribe(OnPlayerEvent).AddTo(_disposables);
            warningSubscriber.Subscribe(OnWarning).AddTo(_disposables);
            successSubscriber.Subscribe(OnSuccess).AddTo(_disposables);

            DebugLog("초기화 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 알림 즉시 표시
        /// </summary>
        public void ShowNotification(string message, float duration = -1f, NotificationType type = NotificationType.Info)
        {
            _notificationService.ShowNotification(message, duration, type);
        }

        /// <summary>
        /// 알림 큐에 추가
        /// </summary>
        public void QueueNotification(string message, float duration = -1f, NotificationType type = NotificationType.Info)
        {
            _notificationService.QueueNotification(message, duration, type);
        }

        /// <summary>
        /// 알림 숨김
        /// </summary>
        public void HideNotification(bool immediate = false)
        {
            _notificationService.HideNotification(immediate);
        }

        /// <summary>
        /// 알림 큐 처리
        /// </summary>
        public void ProcessNotificationQueue()
        {
            _notificationService.ProcessNotificationQueue();
        }

        /// <summary>
        /// 알림 큐 초기화
        /// </summary>
        public void ClearNotificationQueue()
        {
            _notificationService.ClearNotificationQueue();
        }

        /// <summary>
        /// 애니메이션 시간 설정
        /// </summary>
        public void SetAnimationDurations(float fadeIn, float fadeOut)
        {
            _notificationService.SetAnimationDurations(fadeIn, fadeOut);
        }

        /// <summary>
        /// 기본 표시 시간 설정
        /// </summary>
        public void SetDefaultDisplayDuration(float duration)
        {
            _notificationService.SetDefaultDisplayDuration(duration);
        }

        // 미리 정의된 알림들
        public void ShowGameStartNotification()
        {
            _notificationService.ShowGameStartNotification();
        }

        public void ShowGameEndNotification()
        {
            _notificationService.ShowGameEndNotification();
        }

        public void ShowPlayerJoinedNotification(string playerName)
        {
            _notificationService.ShowPlayerJoinedNotification(playerName);
        }

        public void ShowPlayerLeftNotification(string playerName)
        {
            _notificationService.ShowPlayerLeftNotification(playerName);
        }

        public void ShowPlayerDiedNotification(string playerName)
        {
            _notificationService.ShowPlayerDiedNotification(playerName);
        }

        public void ShowPlayerEscapedNotification(string playerName)
        {
            _notificationService.ShowPlayerEscapedNotification(playerName);
        }

        public void ShowObjectiveNotification(string objective)
        {
            _notificationService.ShowObjectiveNotification(objective);
        }

        public void ShowWarningNotification(string warning)
        {
            _notificationService.ShowWarningNotification(warning);
        }

        public void ShowSuccessNotification(string success)
        {
            _notificationService.ShowSuccessNotification(success);
        }

        // 유틸리티 메서드들
        public List<NotificationData> GetQueuedNotifications()
        {
            return _notificationService.GetQueuedNotifications();
        }

        public NotificationData PeekNextNotification()
        {
            return _notificationService.PeekNextNotification();
        }

        public NotificationData GetCurrentNotification()
        {
            return _notificationService.GetCurrentNotification();
        }

        #endregion

        #region Message Handlers

        private void OnNotificationShow(NotificationShowMessage message)
        {
            _currentNotificationType.Value = message.type;
            DebugLog($"알림 표시: {message.message} ({message.duration}s, {message.type})");
        }

        private void OnNotificationQueued(NotificationQueuedMessage message)
        {
            DebugLog($"알림 큐 추가: {message.notification.message} (큐 크기: {message.queueCount})");
        }

        private void OnNotificationHide(NotificationHideMessage message)
        {
            DebugLog($"알림 숨김: {(message.immediate ? "즉시" : "애니메이션")}");
        }

        private void OnVisibilityChanged(NotificationVisibilityChangedMessage message)
        {
            DebugLog($"가시성 변경: {message.isShowing} - {message.currentMessage}");
        }

        private void OnQueueProcess(NotificationQueueProcessMessage message)
        {
            DebugLog($"큐 처리: {message.processedNotification.message} (남은 개수: {message.remainingCount})");
        }

        private void OnQueueCleared(NotificationQueueClearedMessage message)
        {
            DebugLog($"큐 초기화: {message.clearedCount}개 제거");
        }

        private void OnAnimationComplete(NotificationAnimationCompleteMessage message)
        {
            DebugLog($"애니메이션 완료: {message.wasShowing} - {message.message}");

            // 애니메이션 완료 후 큐에 다음 알림이 있으면 처리
            if (!message.wasShowing)
            {
                ProcessNotificationQueue();
            }
        }

        private void OnGameEvent(GameEventNotificationMessage message)
        {
            DebugLog($"게임 이벤트 알림: {message.eventType} - {message.message}");
        }

        private void OnPlayerEvent(PlayerEventNotificationMessage message)
        {
            DebugLog($"플레이어 이벤트 알림: {message.playerName} {message.eventType} - {message.message}");
        }

        private void OnWarning(WarningNotificationMessage message)
        {
            DebugLog($"경고 알림: {message.warning} (긴급: {message.isUrgent})");
        }

        private void OnSuccess(SuccessNotificationMessage message)
        {
            DebugLog($"성공 알림: {message.success}");
        }

        #endregion

        #region Private Methods

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[NotificationViewModel] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _currentNotificationType?.Dispose();
            DebugLog("Dispose 완료");
        }

        #endregion
    }
}