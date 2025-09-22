using System;
using Features.Feeding.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Features.Feeding.Views
{
    /// <summary>
    /// 꿈틀이 먹이 UI View
    /// </summary>
    public class FeedingUIView : MonoBehaviour
    {
        // ViewModel 의존성
        [Inject] private FeedingViewModel _viewModel;

        // UI 요소들
        private VisualElement _root;
        private VisualElement _lightArea;
        private Label _feedingCountLabel;
        private VisualElement _feedingIcon;

        // 구독 관리
        private readonly CompositeDisposable _disposables = new();

        [Header("디버그 설정")]
        [SerializeField] private bool enableDebugLogs = true;

        #region Lifecycle

        public void Initialize(VisualElement root)
        {
            _root = root;

            CacheUIElements();
            SubscribeToViewModel();
            UpdateAllUI();

            if (enableDebugLogs)
                Debug.Log("[FeedingUIView] 초기화 완료");
        }

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables.Dispose();

            if (enableDebugLogs)
                Debug.Log("[FeedingUIView] OnDestroy");
        }

        #endregion

        #region UI Setup

        private void CacheUIElements()
        {
            _lightArea = _root?.Q<VisualElement>("LightArea");
            _feedingCountLabel = _lightArea?.Q<Label>();
            _feedingIcon = _root?.Q<VisualElement>("lightIcon");

            // UXML의 기본 텍스트 초기화
            if (_feedingCountLabel != null)
            {
                _feedingCountLabel.text = "";
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[FeedingUIView] UI 요소 캐싱 완료: "
                    + $"먹이영역={(_lightArea != null ? "OK" : "NULL")}, "
                    + $"먹이라벨={(_feedingCountLabel != null ? "OK" : "NULL")}, "
                    + $"먹이아이콘={(_feedingIcon != null ? "OK" : "NULL")}"
                );
            }
        }

        private void SubscribeToViewModel()
        {
            if (_viewModel == null) return;

            // 먹이 개수 변경 구독
            _viewModel.FeedingCount
                .Subscribe(count => UpdateFeedingDisplay(count))
                .AddTo(_disposables);

            // 먹이 스프라이트 변경 구독
            _viewModel.FeedingSprite
                .Subscribe(sprite => UpdateFeedingSprite(sprite))
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[FeedingUIView] ViewModel 이벤트 구독 완료");
        }

        #endregion

        #region UI Update Methods

        private void UpdateFeedingDisplay(int count)
        {
            if (_feedingCountLabel != null)
            {
                _feedingCountLabel.text = count.ToString();
            }

            if (enableDebugLogs)
                Debug.Log($"[FeedingUIView] 먹이 개수 UI 업데이트: {count}");
        }

        private void UpdateFeedingSprite(Sprite sprite)
        {
            if (_feedingIcon != null && sprite != null)
            {
                _feedingIcon.style.backgroundImage = new StyleBackground(sprite);
            }

            if (enableDebugLogs)
                Debug.Log($"[FeedingUIView] 먹이 스프라이트 업데이트: {sprite?.name ?? "null"}");
        }

        private void UpdateAllUI()
        {
            if (_viewModel == null) return;

            UpdateFeedingDisplay(_viewModel.FeedingCount.Value);
            UpdateFeedingSprite(_viewModel.FeedingSprite.Value);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 먹이 영역 표시/숨김
        /// </summary>
        public void SetFeedingAreaVisibility(bool visible)
        {
            if (_lightArea != null)
            {
                _lightArea.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

                if (enableDebugLogs)
                    Debug.Log($"[FeedingUIView] 먹이 영역 표시: {visible}");
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning($"[FeedingUIView] 아직 초기화되지 않음 - 먹이 영역 표시 요청 무시");
            }
        }

        /// <summary>
        /// 먹이 스프라이트 설정
        /// </summary>
        public void SetFeedingSprite(Sprite sprite)
        {
            _viewModel?.SetFeedingSprite(sprite);
        }

        /// <summary>
        /// 먹이 개수 강제 업데이트
        /// </summary>
        public void RefreshFeedingDisplay()
        {
            if (_viewModel != null)
            {
                UpdateFeedingDisplay(_viewModel.FeedingCount.Value);
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log(
                $"[FeedingUIView] UI 상태:\n"
                + $"  FeedingCount: {_feedingCountLabel?.text ?? "NULL"}\n"
                + $"  LightArea Visible: {_lightArea?.style.display.value == DisplayStyle.Flex}\n"
                + $"  ViewModel: {(_viewModel != null ? "Connected" : "NULL")}"
            );
        }

        [ContextMenu("Debug Add Feeding")]
        private void DebugAddFeeding()
        {
            _viewModel?.DebugAddFeeding(10);
        }

        [ContextMenu("Debug Feed To Ggumtle")]
        private void DebugFeedToGgumtle()
        {
            _viewModel?.DebugFeedToGgumtle(1);
        }

        #endregion
    }
}