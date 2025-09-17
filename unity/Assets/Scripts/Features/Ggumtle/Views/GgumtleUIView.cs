using DI;
using Features.Ggumtle.Models;
using Features.Ggumtle.ViewModels;
using InputSystem.Actions;
using InputSystem.Core;
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

        [Header("Mobile Controls")]
        private ActionButtonController actionButtonController;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _interactionUI;
        private Label _interactionLabel;
        private VisualElement _progressBar;
        private VisualElement _progressFill;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer에서 ViewModel 자동 해결
            ResolveViewModel();

            // ActionButtonController 찾기
            ResolveActionButtonController();

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
                var lifetimeScope = FindFirstObjectByType<GameLifetimeScope>();
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

        private void ResolveActionButtonController()
        {
            try
            {
                // UniversalHUDController에서 ActionButtonController 찾기
                var hudController = FindFirstObjectByType<UniversalHUDController>();
                if (hudController != null)
                {
                    var inputCoordinator = hudController.GetComponent<InputCoordinator>();
                    if (inputCoordinator != null)
                    {
                        actionButtonController = inputCoordinator.GetLayer<ActionButtonController>();
                        if (enableDebugLogs)
                            Debug.Log($"[GgumtleUIView] ActionButtonController 해결 성공: {actionButtonController != null}");
                    }
                    else
                    {
                        // InputCoordinator가 없으면 직접 컴포넌트에서 찾기
                        actionButtonController = hudController.GetComponent<ActionButtonController>();
                        if (enableDebugLogs)
                            Debug.Log($"[GgumtleUIView] ActionButtonController 직접 해결: {actionButtonController != null}");
                    }
                }

                if (actionButtonController == null)
                {
                    Debug.LogWarning("[GgumtleUIView] ActionButtonController를 찾을 수 없음 - 모바일 상호작용 버튼이 작동하지 않을 수 있습니다");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GgumtleUIView] ActionButtonController 해결 실패: {ex.Message}");
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
                .IsInRange.Subscribe(inRange => {
                    ShowInteractionUI(inRange);
                    UpdateMobileInteractionButton(inRange);
                })
                .AddTo(ref _disposables);
            viewModel
                .InteractionText.Subscribe(text => UpdateInteractionText(text))
                .AddTo(ref _disposables);
            viewModel
                .HoldProgress.Subscribe(progress => UpdateProgressBar(progress))
                .AddTo(ref _disposables);

            // 모바일 상호작용 버튼 이벤트 연결
            ConnectMobileInteractionEvents();

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
                {
                    Debug.Log($"[GgumtleUIView] 상호작용 UI {(show ? "표시" : "숨김")}");
                    Debug.Log($"[GgumtleUIView] UI 요소 상태 - Display: {_interactionUI.style.display.value}, Position: {_interactionUI.worldBound}, Visible: {_interactionUI.visible}");

                    if (show)
                    {
                        Debug.Log($"[GgumtleUIView] InteractionText: '{_interactionLabel?.text}', Label Visible: {_interactionLabel?.visible}");
                    }
                }
            }
            else
            {
                Debug.LogError("[GgumtleUIView] _interactionUI가 null입니다!");
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

        #region Mobile Interaction Integration

        /// <summary>
        /// 모바일 상호작용 버튼 업데이트
        /// </summary>
        private void UpdateMobileInteractionButton(bool show)
        {
            if (actionButtonController != null)
            {
                actionButtonController.UpdateInteractionButtonVisibility(show);

                if (enableDebugLogs)
                    Debug.Log($"[GgumtleUIView] 모바일 상호작용 버튼 {(show ? "표시" : "숨김")}");
            }
            else if (show)
            {
                // 버튼이 필요한데 ActionButtonController가 없으면 경고
                Debug.LogWarning("[GgumtleUIView] ActionButtonController가 없어 모바일 상호작용 버튼을 표시할 수 없음");
            }
        }

        /// <summary>
        /// 모바일 상호작용 버튼 이벤트 연결
        /// </summary>
        private void ConnectMobileInteractionEvents()
        {
            if (actionButtonController == null || viewModel == null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[GgumtleUIView] 모바일 이벤트 연결 건너뛰기 - ActionButton: {actionButtonController != null}, ViewModel: {viewModel != null}");
                return;
            }

            // 상호작용 버튼 이벤트 연결
            actionButtonController.OnInteractPressed += OnMobileInteractPressed;
            actionButtonController.OnInteractReleased += OnMobileInteractReleased;
            actionButtonController.OnInteractHoldStart += OnMobileInteractHoldStart;
            actionButtonController.OnInteractHoldEnd += OnMobileInteractHoldEnd;

            if (enableDebugLogs)
                Debug.Log("[GgumtleUIView] 모바일 상호작용 이벤트 연결 완료");
        }

        /// <summary>
        /// 모바일 상호작용 버튼 이벤트 해제
        /// </summary>
        private void DisconnectMobileInteractionEvents()
        {
            if (actionButtonController != null)
            {
                actionButtonController.OnInteractPressed -= OnMobileInteractPressed;
                actionButtonController.OnInteractReleased -= OnMobileInteractReleased;
                actionButtonController.OnInteractHoldStart -= OnMobileInteractHoldStart;
                actionButtonController.OnInteractHoldEnd -= OnMobileInteractHoldEnd;

                if (enableDebugLogs)
                    Debug.Log("[GgumtleUIView] 모바일 상호작용 이벤트 해제 완료");
            }
        }

        private void OnMobileInteractPressed()
        {
            if (enableDebugLogs)
                Debug.Log("[GgumtleUIView] 모바일 상호작용 버튼 누름");
        }

        private void OnMobileInteractReleased()
        {
            if (enableDebugLogs)
                Debug.Log("[GgumtleUIView] 모바일 상호작용 버튼 떴");
        }

        private async void OnMobileInteractHoldStart()
        {
            if (viewModel != null && viewModel.IsInRange.Value && viewModel.CanInteract.Value)
            {
                if (enableDebugLogs)
                    Debug.Log("[GgumtleUIView] 모바일 홀드 시작");

                // ViewModel의 StartHold 메서드 호출
                await viewModel.StartHold();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[GgumtleUIView] 홀드 시작 실패 - InRange: {viewModel?.IsInRange.Value}, CanInteract: {viewModel?.CanInteract.Value}");
            }
        }

        private void OnMobileInteractHoldEnd()
        {
            if (viewModel != null)
            {
                if (enableDebugLogs)
                    Debug.Log("[GgumtleUIView] 모바일 홀드 종료");

                // ViewModel의 CancelHold 메서드 호출
                viewModel.CancelHold();
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // 모바일 이벤트 해제
            DisconnectMobileInteractionEvents();

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
