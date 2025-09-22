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

        [Header("Sprites")]
        [SerializeField]
        private Sprite ggumtleSprite;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        [Inject]
        public void Construct(GgumtleViewModel ggumtleViewModel)
        {
            viewModel = ggumtleViewModel;
            if (enableDebugLogs)
                Debug.Log($"[GgumtleUIView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError("[GgumtleUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            InitializeUI();
            InitializeSprites();

            if (enableDebugLogs)
                Debug.Log("[GgumtleUIView] 초기화 완료");
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

        private CompositeDisposable _disposables = new();

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
                    Debug.Log($"[GgumtleUIView] IsInRange 변경됨: {inRange}");
                    ShowInteractionUI(inRange);
                })
                .AddTo(_disposables);
            viewModel
                .InteractionText.Subscribe(text => {
                    Debug.Log($"[GgumtleUIView] InteractionText 변경됨: '{text}'");
                    UpdateInteractionText(text);
                })
                .AddTo(_disposables);
            viewModel
                .HoldProgress.Subscribe(progress => {
                    Debug.Log($"[GgumtleUIView] HoldProgress 변경됨: {progress:F2}");
                    UpdateProgressBar(progress);
                })
                .AddTo(_disposables);

            // 새로운 상태들 구독
            viewModel
                .IsDiggingInProgress.Subscribe(isDigging => {
                    if (enableDebugLogs)
                        Debug.Log($"[GgumtleUIView] IsDiggingInProgress 변경됨: {isDigging}");
                    // 파기 진행 중일 때 UI 스타일 변경 등 가능
                })
                .AddTo(_disposables);

            viewModel
                .IsCancelRequested.Subscribe(isCancelRequested => {
                    if (enableDebugLogs)
                        Debug.Log($"[GgumtleUIView] IsCancelRequested 변경됨: {isCancelRequested}");
                    // 취소 요청 시 UI 피드백 가능
                })
                .AddTo(_disposables);

            Debug.Log($"[GgumtleUIView] ViewModel R3 구독 완료 - 현재 상태: IsInRange={viewModel.IsInRange.Value}, InteractionText='{viewModel.InteractionText.Value}'");
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

        #region Sprite Initialization

        private void InitializeSprites()
        {
            if (ggumtleSprite != null)
            {
                var ggumtleElement = _root?.Q<VisualElement>("ggumtle");
                if (ggumtleElement != null)
                {
                    ggumtleElement.style.backgroundImage = new StyleBackground(ggumtleSprite);
                    ggumtleElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

                    if (enableDebugLogs)
                        Debug.Log("[GgumtleUIView] 꿈틀이 스프라이트 설정 완료");
                }
            }
        }

        public void SetGgumtleSprite(Sprite sprite)
        {
            ggumtleSprite = sprite;
            InitializeSprites();
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
