using Features.Revival.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Features.Revival.Views
{
    /// <summary>
    /// 부활 UI를 담당하는 View (UI Toolkit 기반)
    /// GgumtleUIView 패턴을 따라 VisualElement + R3로 구현
    /// </summary>
    public class RevivalUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private RevivalViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _interactionUI;
        private Label _interactionLabel;
        private VisualElement _progressBar;
        private VisualElement _progressFill;
        private Label _revivalStatusLabel;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        [Inject]
        public void Construct(RevivalViewModel revivalViewModel)
        {
            viewModel = revivalViewModel;
            if (enableDebugLogs)
                Debug.Log($"[RevivalUIView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError("[RevivalUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            InitializeUI();

            if (enableDebugLogs)
                Debug.Log("[RevivalUIView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[RevivalUIView] Root VisualElement가 null입니다.");
                return;
            }

            // InteractionUI.uxml 구조에 맞게 UI 요소들 캐싱
            _interactionUI = _root.Q<VisualElement>("interactionUI");
            _interactionLabel = _root.Q<Label>("interactionLabel");
            _progressBar = _root.Q<VisualElement>("progressBar");
            _progressFill = _root.Q<VisualElement>("progressFill");

            // 부활 상태 텍스트용 라벨 (추가 생성 또는 기존 활용)
            _revivalStatusLabel = _root.Q<Label>("revivalStatusLabel");
            if (_revivalStatusLabel == null)
            {
                // 없으면 interactionLabel을 상태 표시용으로도 활용
                _revivalStatusLabel = _interactionLabel;
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[RevivalUIView] UI 요소 캐싱 완료: "
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
                Debug.Log("[RevivalUIView] UI 초기화 완료");
        }

        private CompositeDisposable _disposables = new();

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
            {
                Debug.LogError("[RevivalUIView] RevivalViewModel이 설정되지 않음");
                return;
            }

            // R3로 ViewModel 구독
            viewModel.IsInRange
                .Subscribe(inRange => {
                    if (enableDebugLogs)
                        Debug.Log($"[RevivalUIView] IsInRange 변경됨: {inRange}");
                    ShowInteractionUI(inRange);

                    if (inRange)
                    {
                        // 범위에 들어오면 즉시 진행바 표시 (0% 상태)
                        UpdateProgressBarVisibility(true);
                        UpdateProgressBar(0f);
                    }
                    else
                    {
                        // 범위에서 나가면 진행바 숨김
                        UpdateProgressBarVisibility(false);
                    }
                })
                .AddTo(_disposables);

            viewModel.InteractionText
                .Subscribe(text => {
                    if (enableDebugLogs)
                        Debug.Log($"[RevivalUIView] InteractionText 변경됨: '{text}'");
                    UpdateInteractionText(text);
                })
                .AddTo(_disposables);

            viewModel.IsHolding
                .Subscribe(isHolding => {
                    if (enableDebugLogs)
                        Debug.Log($"[RevivalUIView] IsHolding 변경됨: {isHolding}");
                    UpdateProgressBarVisibility(isHolding || viewModel.IsSelfDefibReviving.CurrentValue);
                })
                .AddTo(_disposables);

            viewModel.HoldProgress
                .Subscribe(progress => {
                    if (viewModel.IsHolding.CurrentValue)
                    {
                        UpdateProgressBar(progress);
                    }
                })
                .AddTo(_disposables);

            viewModel.IsSelfDefibReviving
                .Subscribe(isReviving => {
                    if (enableDebugLogs)
                        Debug.Log($"[RevivalUIView] IsSelfDefibReviving 변경됨: {isReviving}");
                    UpdateProgressBarVisibility(isReviving || viewModel.IsHolding.CurrentValue);

                    if (isReviving)
                    {
                        ShowSelfDefibUI();
                    }
                    else
                    {
                        HideSelfDefibUI();
                    }
                })
                .AddTo(_disposables);

            viewModel.SelfDefibProgress
                .Subscribe(progress => {
                    if (viewModel.IsSelfDefibReviving.CurrentValue)
                    {
                        UpdateProgressBar(progress);
                    }
                })
                .AddTo(_disposables);

            viewModel.RevivalStatusText
                .Subscribe(statusText => {
                    if (enableDebugLogs)
                        Debug.Log($"[RevivalUIView] RevivalStatusText 변경됨: '{statusText}'");
                    UpdateRevivalStatusText(statusText);
                })
                .AddTo(_disposables);

            Debug.Log($"[RevivalUIView] ViewModel R3 구독 완료");
        }

        #region UI Update Methods

        private void ShowInteractionUI(bool show)
        {
            if (_interactionUI != null)
            {
                _interactionUI.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

                if (enableDebugLogs)
                {
                    Debug.Log($"[RevivalUIView] 상호작용 UI {(show ? "표시" : "숨김")}");
                }
            }
            else
            {
                Debug.LogError("[RevivalUIView] _interactionUI가 null입니다!");
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

        private void UpdateRevivalStatusText(string text)
        {
            if (_revivalStatusLabel != null && _revivalStatusLabel != _interactionLabel)
            {
                _revivalStatusLabel.text = text;
            }
            else if (!string.IsNullOrEmpty(text))
            {
                // 상태 텍스트가 있으면 상호작용 텍스트 대신 표시
                UpdateInteractionText(text);
            }
        }

        private void UpdateProgressBar(float progress)
        {
            if (_progressFill != null)
            {
                // 진행률을 width 스타일로 설정 (0-100%)
                _progressFill.style.width = Length.Percent(progress * 100f);

                if (enableDebugLogs)
                    Debug.Log($"[RevivalUIView] 진행바 업데이트: {progress:P1}");
            }
        }

        private void UpdateProgressBarVisibility(bool show)
        {
            if (_progressBar != null)
            {
                _progressBar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

                if (enableDebugLogs)
                    Debug.Log($"[RevivalUIView] 진행바 {(show ? "표시" : "숨김")}");
            }
        }

        private void ShowSelfDefibUI()
        {
            // 자가제세동기 진행 중에는 전체 UI 표시
            ShowInteractionUI(true);
            UpdateProgressBarVisibility(true);
        }

        private void HideSelfDefibUI()
        {
            // 자가제세동기 완료 후에는 원래 상호작용 상태로 복원
            bool shouldShowInteraction = viewModel != null && viewModel.IsInRange.CurrentValue;
            ShowInteractionUI(shouldShowInteraction);

            if (!viewModel.IsHolding.CurrentValue)
            {
                UpdateProgressBarVisibility(false);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables.Dispose();

            if (enableDebugLogs)
                Debug.Log("[RevivalUIView] OnDestroy");
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
        public async void CancelHold()
        {
            if (viewModel != null)
            {
                await viewModel.CancelDirectRevivalAsync();
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            if (viewModel == null)
            {
                Debug.Log("[RevivalUIView] ViewModel is null");
                return;
            }

            Debug.Log(
                $"[RevivalUIView] UI State:\n"
                    + $"  UI Visible: {(_interactionUI?.style.display.value == DisplayStyle.Flex)}\n"
                    + $"  In Range: {viewModel.IsInRange.CurrentValue}\n"
                    + $"  Can Revive: {viewModel.CanRevive.CurrentValue}\n"
                    + $"  Interaction Text: {viewModel.InteractionText.CurrentValue}\n"
                    + $"  Is Holding: {viewModel.IsHolding.CurrentValue}\n"
                    + $"  Hold Progress: {viewModel.HoldProgress.CurrentValue:P1}\n"
                    + $"  Self Defib Reviving: {viewModel.IsSelfDefibReviving.CurrentValue}\n"
                    + $"  Self Defib Progress: {viewModel.SelfDefibProgress.CurrentValue:P1}\n"
                    + $"  Revival Status: {viewModel.RevivalStatusText.CurrentValue}"
            );
        }

        [ContextMenu("Test Show UI")]
        private void TestShowUI() => SetInteractionUIVisibility(true);

        [ContextMenu("Test Hide UI")]
        private void TestHideUI() => SetInteractionUIVisibility(false);

        #endregion
    }
}