using System.Collections;
using Features.Notification.Models;
using Features.Notification.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using R3DisposableBag = R3.CompositeDisposable;

namespace Features.Notification.Views
{
    /// <summary>
    /// 알림 시스템 UI를 담당하는 View (UI Toolkit 기반)
    /// GgumtleUIView 패턴을 따라 VisualElement + R3로 구현
    /// </summary>
    public class NotificationUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private NotificationViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _banner;
        private Label _bannerLabel;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        [Header("Animation State")]
        private Coroutine _currentBannerCoroutine;
        private bool _isInitialized = false;

        private CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(NotificationViewModel notificationViewModel)
        {
            viewModel = notificationViewModel;
            if (enableDebugLogs)
                Debug.Log($"[NotificationUIView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError("[NotificationUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            InitializeUI();
            _isInitialized = true;

            if (enableDebugLogs)
                Debug.Log("[NotificationUIView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[NotificationUIView] Root VisualElement가 null입니다.");
                return;
            }

            _banner = _root.Q<VisualElement>("topBanner");
            _bannerLabel = _banner?.Q<Label>();

            // UXML의 하드코딩된 배너 텍스트 즉시 제거
            if (_bannerLabel != null)
            {
                _bannerLabel.text = "";
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[NotificationUIView] UI 요소 캐싱 완료: "
                        + $"배너={(_banner != null ? "OK" : "NULL")}, "
                        + $"배너라벨={(_bannerLabel != null ? "OK" : "NULL")}"
                );
            }
        }

        private void InitializeUI()
        {
            // 초기에는 배너 숨김
            HideBanner(immediate: true);

            if (enableDebugLogs)
                Debug.Log("[NotificationUIView] UI 초기화 완료");
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
                return;

            // R3 Observable 구독
            viewModel.ShouldShowBanner
                .Subscribe(shouldShow =>
                {
                    if (shouldShow && !string.IsNullOrEmpty(viewModel.CurrentMessage.CurrentValue))
                    {
                        ShowBanner(viewModel.CurrentMessage.CurrentValue, viewModel.DefaultDisplayDuration.CurrentValue);
                    }
                    else if (!shouldShow)
                    {
                        HideBanner();
                    }
                })
                .AddTo(_disposables);

            viewModel.CurrentMessage
                .Subscribe(message => UpdateBannerText(message))
                .AddTo(_disposables);

            viewModel.CurrentNotificationType
                .Subscribe(type => UpdateBannerStyle(type))
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[NotificationUIView] ViewModel 구독 완료");
        }

        #region Banner Display Methods

        public void ShowBanner(string message, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (!_isInitialized || _root == null)
            {
                Debug.LogWarning($"[NotificationUIView] 아직 초기화되지 않았습니다. 메시지를 표시할 수 없습니다: {message}");
                return;
            }

            float displayDuration = duration > 0 ? duration : (viewModel?.DefaultDisplayDuration.CurrentValue ?? 2.2f);

            // 배너 요소 다시 확인
            if (_banner == null || _bannerLabel == null)
            {
                CacheUIElements();
            }

            // 메시지 설정
            if (_bannerLabel != null)
            {
                _bannerLabel.text = message;
            }
            else
            {
                Debug.LogError("[NotificationUIView] 배너 레이블을 찾을 수 없습니다.");
                return;
            }

            // 이전 코루틴 중단
            if (_currentBannerCoroutine != null)
            {
                StopCoroutine(_currentBannerCoroutine);
            }

            _currentBannerCoroutine = StartCoroutine(ShowBannerCoroutine(displayDuration));

            if (enableDebugLogs)
                Debug.Log($"[NotificationUIView] 배너 표시: {message} ({displayDuration}초)");
        }

        public void HideBanner(bool immediate = false)
        {
            if (_currentBannerCoroutine != null)
            {
                StopCoroutine(_currentBannerCoroutine);
                _currentBannerCoroutine = null;
            }

            if (immediate)
            {
                SetBannerDisplayState(false);
            }
            else
            {
                _currentBannerCoroutine = StartCoroutine(HideBannerCoroutine());
            }

            if (enableDebugLogs)
                Debug.Log($"[NotificationUIView] 배너 숨김 {(immediate ? "(즉시)" : "(애니메이션)")}");
        }

        private void UpdateBannerText(string message)
        {
            if (_bannerLabel != null)
            {
                _bannerLabel.text = message;
            }
        }

        private void UpdateBannerStyle(NotificationType type)
        {
            if (_banner == null)
                return;

            // 기존 타입 클래스 제거
            _banner.RemoveFromClassList("notification-info");
            _banner.RemoveFromClassList("notification-warning");
            _banner.RemoveFromClassList("notification-success");
            _banner.RemoveFromClassList("notification-error");
            _banner.RemoveFromClassList("notification-game");
            _banner.RemoveFromClassList("notification-player");

            // 새 타입 클래스 적용
            string typeClass = type switch
            {
                NotificationType.Info => "notification-info",
                NotificationType.Warning => "notification-warning",
                NotificationType.Success => "notification-success",
                NotificationType.Error => "notification-error",
                NotificationType.Game => "notification-game",
                NotificationType.Player => "notification-player",
                _ => "notification-info"
            };

            _banner.AddToClassList(typeClass);
        }

        #endregion

        #region Animation Coroutines

        private IEnumerator ShowBannerCoroutine(float duration)
        {
            // 페이드 인
            yield return StartCoroutine(FadeInBanner());

            // 표시 대기
            yield return new WaitForSeconds(duration);

            // 페이드 아웃
            yield return StartCoroutine(FadeOutBanner());

            // ViewModel에 완료 알림
            if (viewModel != null && viewModel.HasQueuedNotifications.CurrentValue)
            {
                viewModel.ProcessNotificationQueue();
            }

            _currentBannerCoroutine = null;
        }

        private IEnumerator HideBannerCoroutine()
        {
            yield return StartCoroutine(FadeOutBanner());
            _currentBannerCoroutine = null;
        }

        private IEnumerator FadeInBanner()
        {
            if (_banner == null)
                yield break;

            SetBannerDisplayState(true);

            float fadeInDuration = viewModel?.FadeInDuration.CurrentValue ?? 0.3f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                _banner.style.opacity = alpha;
                yield return null;
            }

            _banner.style.opacity = 1f;
        }

        private IEnumerator FadeOutBanner()
        {
            if (_banner == null)
                yield break;

            float fadeOutDuration = viewModel?.FadeOutDuration.CurrentValue ?? 0.5f;
            float elapsed = 0f;
            float startOpacity = _banner.resolvedStyle.opacity;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startOpacity, 0f, elapsed / fadeOutDuration);
                _banner.style.opacity = alpha;
                yield return null;
            }

            SetBannerDisplayState(false);
        }

        #endregion

        #region UI Helper Methods

        private void SetBannerDisplayState(bool visible)
        {
            if (_banner != null)
            {
                if (visible)
                {
                    _banner.RemoveFromClassList("hide");
                    _banner.AddToClassList("show");
                    _banner.style.opacity = 1f;
                }
                else
                {
                    _banner.RemoveFromClassList("show");
                    _banner.AddToClassList("hide");
                    _banner.style.opacity = 0f;
                }
            }
        }

        #endregion

        #region Public API (레거시 호환성)

        public void SetBannerVisibility(bool visible)
        {
            if (visible)
            {
                if (viewModel != null && !string.IsNullOrEmpty(viewModel.CurrentMessage.CurrentValue))
                {
                    ShowBanner(viewModel.CurrentMessage.CurrentValue);
                }
            }
            else
            {
                HideBanner(immediate: true);
            }
        }

        public bool IsBannerShowing()
        {
            return viewModel?.IsShowing.CurrentValue ?? false;
        }

        public string GetCurrentMessage()
        {
            return viewModel?.CurrentMessage.CurrentValue ?? "";
        }

        public int GetQueueCount()
        {
            return viewModel?.QueueCount.CurrentValue ?? 0;
        }

        // 레거시 호환성 메서드들
        public void ShowNotification(string message, float duration = -1f)
        {
            if (viewModel != null)
            {
                viewModel.ShowNotification(message, duration);
            }
        }

        public void QueueNotification(string message, float duration = -1f)
        {
            if (viewModel != null)
            {
                viewModel.QueueNotification(message, duration);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            _disposables?.Dispose();

            // 현재 실행 중인 코루틴 정리
            if (_currentBannerCoroutine != null)
            {
                StopCoroutine(_currentBannerCoroutine);
                _currentBannerCoroutine = null;
            }

            if (enableDebugLogs)
                Debug.Log("[NotificationUIView] OnDestroy - Dispose 완료");
        }

        #endregion
    }
}