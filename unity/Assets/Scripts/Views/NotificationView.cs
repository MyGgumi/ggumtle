using System;
using System.Collections;
using MVVM.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Views
{
    public class NotificationView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private NotificationViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _banner;
        private Label _bannerLabel;

        [Header("Animation State")]
        private Coroutine _currentBannerCoroutine;
        private bool _isInitialized = false;

        public void Initialize(VisualElement root, NotificationViewModel viewModel)
        {
            _root = root;
            this.viewModel = viewModel;

            CacheUIElements();
            SubscribeToViewModel();
            HideBanner(immediate: true);
            _isInitialized = true;

            Debug.Log("[NotificationView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _banner = _root.Q<VisualElement>("topBanner");
            _bannerLabel = _banner?.Q<Label>();

            // UXML의 하드코딩된 배너 텍스트 즉시 제거
            if (_bannerLabel != null)
            {
                _bannerLabel.text = "";
            }

            Debug.Log(
                $"[NotificationView] UI 요소 캐싱 완료: "
                    + $"배너={(_banner != null ? "OK" : "NULL")}, "
                    + $"배너라벨={(_bannerLabel != null ? "OK" : "NULL")}"
            );
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
                return;

            viewModel.NotificationRequested += OnNotificationRequested;
            viewModel.NotificationHideRequested += OnNotificationHideRequested;
            viewModel.NotificationVisibilityChanged += OnNotificationVisibilityChanged;
            viewModel.CurrentMessageChanged += OnCurrentMessageChanged;
            viewModel.QueueCleared += OnQueueCleared;

            Debug.Log("[NotificationView] ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (viewModel == null)
                return;

            viewModel.NotificationRequested -= OnNotificationRequested;
            viewModel.NotificationHideRequested -= OnNotificationHideRequested;
            viewModel.NotificationVisibilityChanged -= OnNotificationVisibilityChanged;
            viewModel.CurrentMessageChanged -= OnCurrentMessageChanged;
            viewModel.QueueCleared -= OnQueueCleared;

            Debug.Log("[NotificationView] ViewModel 이벤트 구독 해제 완료");
        }

        #region ViewModel Event Handlers

        private void OnNotificationRequested(string message, float duration)
        {
            ShowBanner(message, duration);
        }

        private void OnNotificationHideRequested()
        {
            HideBanner();
        }

        private void OnNotificationVisibilityChanged(bool isShowing)
        {
            // 추가적인 UI 상태 변경이 필요하면 여기서 처리
        }

        private void OnCurrentMessageChanged(string message)
        {
            UpdateBannerText(message);
        }

        private void OnQueueCleared()
        {
            // 큐가 클리어되었을 때 추가 처리가 필요하면 여기서
        }

        #endregion

        #region Banner Display Methods

        public void ShowBanner(string message, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (!_isInitialized || _root == null)
            {
                Debug.LogWarning(
                    $"[NotificationView] 아직 초기화되지 않았습니다. 메시지를 표시할 수 없습니다: {message}"
                );
                return;
            }

            float displayDuration =
                duration > 0 ? duration : (viewModel?.DefaultDisplayDuration ?? 2.2f);

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
                Debug.LogError("[NotificationView] 배너 레이블을 찾을 수 없습니다.");
                return;
            }

            // 이전 코루틴 중단
            if (_currentBannerCoroutine != null)
            {
                StopCoroutine(_currentBannerCoroutine);
            }

            _currentBannerCoroutine = StartCoroutine(ShowBannerCoroutine(displayDuration));

            Debug.Log($"[NotificationView] 배너 표시: {message} ({displayDuration}초)");
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

            Debug.Log($"[NotificationView] 배너 숨김 {(immediate ? "(즉시)" : "(애니메이션)")}");
        }

        private void UpdateBannerText(string message)
        {
            if (_bannerLabel != null)
            {
                _bannerLabel.text = message;
            }
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
            if (viewModel != null && viewModel.HasQueuedNotifications)
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

            float fadeInDuration = viewModel?.FadeInDuration ?? 0.3f;
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

            float fadeOutDuration = viewModel?.FadeOutDuration ?? 0.5f;
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

        #region Public API

        public void SetBannerVisibility(bool visible)
        {
            if (visible)
            {
                if (viewModel != null && !string.IsNullOrEmpty(viewModel.CurrentMessage))
                {
                    ShowBanner(viewModel.CurrentMessage);
                }
            }
            else
            {
                HideBanner(immediate: true);
            }
        }

        public bool IsBannerShowing()
        {
            return viewModel?.IsShowing ?? false;
        }

        public string GetCurrentMessage()
        {
            return viewModel?.CurrentMessage ?? "";
        }

        public int GetQueueCount()
        {
            return viewModel?.QueueCount ?? 0;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewModel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();

            // 현재 실행 중인 코루틴 정리
            if (_currentBannerCoroutine != null)
            {
                StopCoroutine(_currentBannerCoroutine);
                _currentBannerCoroutine = null;
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log(
                $"[NotificationView] UI State:\n"
                    + $"  Banner Visible: {(_banner?.style.display.value == DisplayStyle.Flex)}\n"
                    + $"  Banner Opacity: {_banner?.style.opacity.value ?? 0f}\n"
                    + $"  Banner Text: '{_bannerLabel?.text ?? "NULL"}'\n"
                    + $"  Is Showing: {IsBannerShowing()}\n"
                    + $"  Queue Count: {GetQueueCount()}\n"
                    + $"  Current Coroutine: {(_currentBannerCoroutine != null ? "Active" : "None")}\n"
                    + $"  ViewModel: {(viewModel != null ? "Connected" : "NULL")}"
            );
        }

        [ContextMenu("Test Show Banner")]
        private void TestShowBanner()
        {
            ShowBanner("테스트 배너 메시지입니다!", 3f);
        }

        [ContextMenu("Test Hide Banner")]
        private void TestHideBanner()
        {
            HideBanner();
        }

        #endregion
    }
}
