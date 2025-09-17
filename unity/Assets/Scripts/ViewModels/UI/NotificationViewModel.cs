using System;
using System.Collections;
using System.Collections.Generic;
using MVVM.Core;
using UnityEngine;

namespace MVVM.UI
{
    [System.Serializable]
    public class NotificationData
    {
        public string message;
        public float duration;
        public DateTime timestamp;

        public NotificationData(string message, float duration = -1f)
        {
            this.message = message;
            this.duration = duration;
            this.timestamp = DateTime.Now;
        }
    }

    public class NotificationViewModel : BaseViewModel
    {
        [Header("Notification State")]
        [SerializeField]
        private bool _isShowing = false;

        [SerializeField]
        private string _currentMessage = "";

        [SerializeField]
        private Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();

        [Header("Settings")]
        [SerializeField]
        private float _defaultDisplayDuration = 2.2f;

        [SerializeField]
        private float _fadeInDuration = 0.3f;

        [SerializeField]
        private float _fadeOutDuration = 0.5f;

        [SerializeField]
        private int _maxQueueSize = 10;

        public event Action<string, float> NotificationRequested; // message, duration
        public event Action NotificationHideRequested;
        public event Action<bool> NotificationVisibilityChanged; // isShowing
        public event Action<string> CurrentMessageChanged;
        public event Action QueueCleared;

        #region Properties

        public bool IsShowing
        {
            get => _isShowing;
            private set
            {
                if (SetProperty(ref _isShowing, value))
                {
                    NotificationVisibilityChanged?.Invoke(_isShowing);
                }
            }
        }

        public string CurrentMessage
        {
            get => _currentMessage;
            private set
            {
                if (SetProperty(ref _currentMessage, value ?? ""))
                {
                    CurrentMessageChanged?.Invoke(_currentMessage);
                }
            }
        }

        public float DefaultDisplayDuration
        {
            get => _defaultDisplayDuration;
            set => SetProperty(ref _defaultDisplayDuration, Mathf.Max(0f, value));
        }

        public float FadeInDuration
        {
            get => _fadeInDuration;
            set => SetProperty(ref _fadeInDuration, Mathf.Max(0f, value));
        }

        public float FadeOutDuration
        {
            get => _fadeOutDuration;
            set => SetProperty(ref _fadeOutDuration, Mathf.Max(0f, value));
        }

        public int MaxQueueSize
        {
            get => _maxQueueSize;
            set => SetProperty(ref _maxQueueSize, Mathf.Max(1, value));
        }

        public int QueueCount => _notificationQueue.Count;
        public bool HasQueuedNotifications => _notificationQueue.Count > 0;

        #endregion

        #region Notification Methods

        public void ShowNotification(string message, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message))
                return;

            float displayDuration = duration > 0 ? duration : _defaultDisplayDuration;
            CurrentMessage = message;
            IsShowing = true;

            NotificationRequested?.Invoke(message, displayDuration);

            if (EnableDebugLogs)
            {
                Debug.Log($"[NotificationViewModel] 알림 표시: {message} ({displayDuration}초)");
            }
        }

        public void HideNotification()
        {
            IsShowing = false;
            CurrentMessage = "";
            NotificationHideRequested?.Invoke();

            if (EnableDebugLogs)
            {
                Debug.Log("[NotificationViewModel] 알림 숨김 요청");
            }
        }

        public void QueueNotification(string message, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message))
                return;

            float displayDuration = duration > 0 ? duration : _defaultDisplayDuration;
            var notification = new NotificationData(message, displayDuration);

            // 큐가 가득 찬 경우 가장 오래된 항목 제거
            if (_notificationQueue.Count >= _maxQueueSize)
            {
                _notificationQueue.Dequeue();

                if (EnableDebugLogs)
                {
                    Debug.LogWarning($"[NotificationViewModel] 큐가 가득 참. 오래된 알림 제거.");
                }
            }

            _notificationQueue.Enqueue(notification);
            OnPropertyChanged(nameof(QueueCount));
            OnPropertyChanged(nameof(HasQueuedNotifications));

            if (!_isShowing)
            {
                ProcessNotificationQueue();
            }

            if (EnableDebugLogs)
            {
                Debug.Log(
                    $"[NotificationViewModel] 알림 큐에 추가: {message} (큐 크기: {_notificationQueue.Count})"
                );
            }
        }

        public void ProcessNotificationQueue()
        {
            if (_notificationQueue.Count > 0 && !_isShowing)
            {
                var notification = _notificationQueue.Dequeue();
                OnPropertyChanged(nameof(QueueCount));
                OnPropertyChanged(nameof(HasQueuedNotifications));

                ShowNotification(notification.message, notification.duration);

                if (EnableDebugLogs)
                {
                    Debug.Log($"[NotificationViewModel] 큐에서 알림 처리: {notification.message}");
                }
            }
        }

        public void ClearNotificationQueue()
        {
            _notificationQueue.Clear();
            OnPropertyChanged(nameof(QueueCount));
            OnPropertyChanged(nameof(HasQueuedNotifications));
            QueueCleared?.Invoke();

            if (EnableDebugLogs)
            {
                Debug.Log("[NotificationViewModel] 알림 큐 초기화");
            }
        }

        #endregion

        #region Predefined Notifications

        public void ShowGameStartNotification()
        {
            ShowNotification("게임이 시작되었습니다!", 3f);
        }

        public void ShowGameEndNotification()
        {
            ShowNotification("게임이 종료되었습니다.", 3f);
        }

        public void ShowPlayerJoinedNotification(string playerName)
        {
            ShowNotification($"{playerName}님이 게임에 참가했습니다.", 2.5f);
        }

        public void ShowPlayerLeftNotification(string playerName)
        {
            ShowNotification($"{playerName}님이 게임을 떠났습니다.", 2.5f);
        }

        public void ShowPlayerDiedNotification(string playerName)
        {
            ShowNotification($"{playerName}님이 사망했습니다.", 3f);
        }

        public void ShowPlayerEscapedNotification(string playerName)
        {
            ShowNotification($"{playerName}님이 탈출했습니다!", 3f);
        }

        public void ShowObjectiveNotification(string objective)
        {
            ShowNotification($"목표: {objective}", 4f);
        }

        public void ShowWarningNotification(string warning)
        {
            ShowNotification($"⚠️ {warning}", 3f);
        }

        public void ShowSuccessNotification(string success)
        {
            ShowNotification($"✅ {success}", 2.5f);
        }

        #endregion

        #region Utility Methods

        public List<NotificationData> GetQueuedNotifications()
        {
            return new List<NotificationData>(_notificationQueue);
        }

        public NotificationData PeekNextNotification()
        {
            return _notificationQueue.Count > 0 ? _notificationQueue.Peek() : null;
        }

        public void SetAnimationDurations(float fadeIn, float fadeOut)
        {
            FadeInDuration = fadeIn;
            FadeOutDuration = fadeOut;
        }

        #endregion

        #region BaseViewModel Override

        protected override void InitializeViewModel()
        {
            base.InitializeViewModel();

            _isShowing = false;
            _currentMessage = "";
            _notificationQueue = new Queue<NotificationData>();
            _defaultDisplayDuration = 2.2f;
            _fadeInDuration = 0.3f;
            _fadeOutDuration = 0.5f;
            _maxQueueSize = 10;

            if (EnableDebugLogs)
            {
                Debug.Log("[NotificationViewModel] 초기화 완료");
            }
        }

        protected override void CleanupViewModel()
        {
            base.CleanupViewModel();

            ClearNotificationQueue();

            NotificationRequested = null;
            NotificationHideRequested = null;
            NotificationVisibilityChanged = null;
            CurrentMessageChanged = null;
            QueueCleared = null;

            if (EnableDebugLogs)
            {
                Debug.Log("[NotificationViewModel] 정리 완료");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log(
                $"[NotificationViewModel] State:\n"
                    + $"  IsShowing: {IsShowing}\n"
                    + $"  CurrentMessage: '{CurrentMessage}'\n"
                    + $"  QueueCount: {QueueCount}/{MaxQueueSize}\n"
                    + $"  DefaultDuration: {DefaultDisplayDuration}s\n"
                    + $"  FadeIn/Out: {FadeInDuration}s / {FadeOutDuration}s"
            );
        }

        [ContextMenu("Show Test Notification")]
        private void ShowTestNotification()
        {
            ShowNotification("테스트 알림 메시지입니다.", 2f);
        }

        [ContextMenu("Queue Multiple Test Notifications")]
        private void QueueTestNotifications()
        {
            QueueNotification("첫 번째 알림", 1.5f);
            QueueNotification("두 번째 알림", 2f);
            QueueNotification("세 번째 알림", 1.5f);
        }

        [ContextMenu("Show Game Start")]
        private void DebugGameStart() => ShowGameStartNotification();

        [ContextMenu("Show Player Joined")]
        private void DebugPlayerJoined() => ShowPlayerJoinedNotification("테스트플레이어");

        #endregion
    }
}
