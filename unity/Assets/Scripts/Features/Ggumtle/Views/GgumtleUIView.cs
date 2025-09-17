using DI;
using Features.Ggumtle.Models;
using Features.Ggumtle.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using R3DisposableBag = R3.DisposableBag;

namespace Features.Ggumtle.Views
{
    /// <summary>
    /// 꿈틀이 상호작용 UI를 담당하는 View (UI Toolkit 기반)
    /// PlayerListView 패턴을 따라 VisualElement + R3로 구현
    /// </summary>
    public class GgumtleUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private GgumtleViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _interactionUI;
        private Label _interactionLabel;
        private VisualElement _progressBar;
        private VisualElement _progressFill;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = false;

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer에서 ViewModel 자동 해결
            ResolveViewModel();

            if (viewModel != null)
            {
                CacheUIElements();
                SubscribeToViewModel();
                InitializeUI();

                if (enableDebugLogs)
                    Debug.Log("[GgumtleUIView] 초기화 완료");
            }
            else
            {
                Debug.LogError("[GgumtleUIView] ViewModel을 해결할 수 없음");
            }
        }

        private void ResolveViewModel()
        {
            try
            {
                // VContainer에서 직접 해결 (Self-Resolving 패턴)
                var lifetimeScope = FindObjectOfType<DI.GameLifetimeScope>();
                if (lifetimeScope != null && lifetimeScope.Container != null)
                {
                    viewModel = lifetimeScope.Container.Resolve<GgumtleViewModel>();
                    if (enableDebugLogs)
                        Debug.Log($"[GgumtleUIView] ViewModel 자동 해결 성공: {viewModel != null}");
                }
                else
                {
                    Debug.LogError("[GgumtleUIView] GameLifetimeScope를 찾을 수 없음");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GgumtleUIView] ViewModel 해결 실패: {ex.Message}");
            }
        }

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[GgumtleUIView] Root VisualElement가 null입니다.");
                return;
            }

            // InteractionUI.uxml 구조에 맞게 UI 요소들 캐싱
            _interactionUI = _root.Q<VisualElement>("interactionUI");
            _interactionLabel = _root.Q<Label>("interactionLabel");
            _progressBar = _root.Q<VisualElement>("progressBar");
            _progressFill = _root.Q<VisualElement>("progressFill");

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[GgumtleUIView] UI 요소 캐싱 완료: "
                        + $"InteractionUI={(_interactionUI != null ? "OK" : "NULL")}, "
                        + $"Label={(_interactionLabel != null ? "OK" : "NULL")}, "
                        + $"ProgressBar={(_progressBar != null ? "OK" : "NULL")}, "
                        + $"ProgressFill={(_progressFill != null ? "OK" : "NULL")}"
                );
            }
        }

        private void InitializeUI()
        {
            // 초기에는 UI 숨김
            HideInteractionUI();

            // 진행바 초기화
            UpdateProgressBar(0f);

            if (enableDebugLogs)
                Debug.Log("[GgumtleUIView] UI 초기화 완료");
        }

        private R3DisposableBag _disposables = new();

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
            {
                Debug.LogError("[GgumtleUIView] GgumtleViewModel이 설정되지 않음");
                return;
            }

            // R3로 ViewModel 구독 (최신 방식)
            viewModel
                .IsInRange.Subscribe(inRange => ShowInteractionUI(inRange))
                .AddTo(ref _disposables);
            viewModel
                .InteractionText.Subscribe(text => UpdateInteractionText(text))
                .AddTo(ref _disposables);
            viewModel
                .HoldProgress.Subscribe(progress => UpdateProgressBar(progress))
                .AddTo(ref _disposables);

            if (enableDebugLogs)
                Debug.Log("[GgumtleUIView] ViewModel R3 구독 완료");
        }

        #region UI Update Methods (UI Toolkit)

        private void ShowInteractionUI(bool show)
        {
            if (_interactionUI != null)
            {
                _interactionUI.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

                if (enableDebugLogs)
                    Debug.Log($"[GgumtleUIView] 상호작용 UI {(show ? "표시" : "숨김")}");
            }
        }

        private void HideInteractionUI()
        {
            ShowInteractionUI(false);
        }

        private void UpdateInteractionText(string text)
        {
            if (_interactionLabel != null)
            {
                _interactionLabel.text = text;
            }
        }

        private void UpdateProgressBar(float progress)
        {
            if (_progressFill != null)
            {
                // 진행률을 width 스타일로 설정 (0-100%)
                _progressFill.style.width = Length.Percent(progress * 100f);

                if (enableDebugLogs)
                    Debug.Log($"[GgumtleUIView] 진행바 업데이트: {progress:P1}");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables.Dispose();

            if (enableDebugLogs)
                Debug.Log("[GgumtleUIView] OnDestroy");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 상호작용 UI 표시/숨김
        /// </summary>
        public void SetInteractionUIVisibility(bool visible)
        {
            ShowInteractionUI(visible);
        }

        /// <summary>
        /// 수동으로 홀드 취소
        /// </summary>
        public void CancelHold()
        {
            if (viewModel != null)
            {
                viewModel.CancelHold();
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            if (viewModel == null)
            {
                Debug.Log("[GgumtleUIView] ViewModel is null");
                return;
            }

            Debug.Log(
                $"[GgumtleUIView] UI State:\n"
                    + $"  UI Visible: {(_interactionUI?.style.display.value == DisplayStyle.Flex)}\n"
                    + $"  In Range: {viewModel.IsInRange.Value}\n"
                    + $"  Interaction Text: {viewModel.InteractionText.Value}\n"
                    + $"  Is Holding: {viewModel.IsHolding.Value}\n"
                    + $"  Hold Progress: {viewModel.HoldProgress.Value:P1}\n"
                    + $"  Food Progress: {viewModel.CurrentFood.Value}/{viewModel.MaxFood.Value}\n"
                    + $"  State: {viewModel.State.Value}"
            );
        }

        [ContextMenu("Test Show UI")]
        private void TestShowUI() => SetInteractionUIVisibility(true);

        [ContextMenu("Test Hide UI")]
        private void TestHideUI() => SetInteractionUIVisibility(false);

        #endregion
    }
}
