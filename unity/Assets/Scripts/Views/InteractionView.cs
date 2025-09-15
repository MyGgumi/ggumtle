using Models;
using UnityEngine;
using UnityEngine.UIElements;
using ViewModels.UI;
using Views.Core;
using InputSystem.Actions;

namespace Views
{
    public class InteractionView : BaseView<InteractionViewModel>
    {
        [Header("Interaction UI Elements")]
        [SerializeField]
        private string _interactionContainerName = "interactionUI";

        [SerializeField]
        private string _interactionTextName = "interactionLabel";

        [SerializeField]
        private string _progressBarName = "progressBar";

        [SerializeField]
        private string _actionButtonName = "interactButton";

        private VisualElement _interactionContainer;
        private Label _interactionText;
        private VisualElement _progressBar;
        private VisualElement _progressFill;
        private VisualElement _actionButton;

        [Header("Animation Settings")]
        [SerializeField]
        private float _fadeInDuration = 0.3f;

        [SerializeField]
        private float _fadeOutDuration = 0.2f;

        private bool _isUIVisible = false;
        private ActionButtonController _actionButtonController;

        protected override void InitializeUIElements()
        {
            base.InitializeUIElements();

            _interactionContainer = GetUIElement<VisualElement>(_interactionContainerName);
            _interactionText = GetUIElement<Label>(_interactionTextName);
            _progressBar = GetUIElement<VisualElement>(_progressBarName);
            _progressFill = _progressBar?.Q<VisualElement>("progressFill");
            _actionButton = GetUIElement<VisualElement>(_actionButtonName);

            Debug.Log($"[InteractionView] UI 요소 초기화 결과:");
            Debug.Log($"  - Container ({_interactionContainerName}): {(_interactionContainer != null ? "✅ 찾음" : "❌ 없음")}");
            Debug.Log($"  - Text ({_interactionTextName}): {(_interactionText != null ? "✅ 찾음" : "❌ 없음")}");
            Debug.Log($"  - ProgressBar ({_progressBarName}): {(_progressBar != null ? "✅ 찾음" : "❌ 없음")}");
            Debug.Log($"  - ActionButton ({_actionButtonName}): {(_actionButton != null ? "✅ 찾음" : "❌ 없음")}");

            if (_actionButton != null)
            {
                _actionButton.RegisterCallback<ClickEvent>(OnActionButtonClicked);
                Debug.Log("[InteractionView] ✅ 상호작용 버튼 이벤트 등록 완료");
            }
            else
            {
                Debug.LogError($"[InteractionView] ❌ {_actionButtonName} 버튼을 찾을 수 없습니다!");
            }

            // 초기에는 상호작용 버튼 숨김
            UpdateInteractionButtonVisibility(false);

            HideInteractionUI();
        }

        protected override void SubscribeToViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.OnInteractionStarted += HandleInteractionStarted;
                _viewModel.OnInteractionProgress += HandleInteractionProgress;
                _viewModel.OnInteractionCompleted += HandleInteractionCompleted;
                _viewModel.OnInteractionCancelled += HandleInteractionCancelled;
                _viewModel.OnNearbyInteractionAdded += HandleNearbyInteractionAdded;
                _viewModel.OnNearbyInteractionRemoved += HandleNearbyInteractionRemoved;
                _viewModel.OnUIVisibilityChanged += HandleUIVisibilityChanged;
                _viewModel.PropertyChanged += HandleViewModelPropertyChanged;

                RefreshUI();
            }
        }

        protected override void UnsubscribeFromViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.OnInteractionStarted -= HandleInteractionStarted;
                _viewModel.OnInteractionProgress -= HandleInteractionProgress;
                _viewModel.OnInteractionCompleted -= HandleInteractionCompleted;
                _viewModel.OnInteractionCancelled -= HandleInteractionCancelled;
                _viewModel.OnNearbyInteractionAdded -= HandleNearbyInteractionAdded;
                _viewModel.OnNearbyInteractionRemoved -= HandleNearbyInteractionRemoved;
                _viewModel.OnUIVisibilityChanged -= HandleUIVisibilityChanged;
                _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }
        }

        void OnDestroy()
        {
            if (_actionButton != null)
            {
                _actionButton.UnregisterCallback<ClickEvent>(OnActionButtonClicked);
            }

            // ActionButtonController 이벤트 해제 불필요 (구독하지 않음)
        }

        #region Event Handlers

        private void HandleInteractionStarted(InteractionType type, string text, float duration)
        {
            // 상자 타입은 InteractionView UI를 사용하지 않음 (ChestView가 처리)
            if (type == InteractionType.Chest)
            {
                Debug.Log($"[InteractionView] Chest interaction started - skipping UI");
                return;
            }

            UpdateInteractionText(text);
            SetProgressBarVisibility(duration > 0);
            ShowInteractionUI();

            Debug.Log($"[InteractionView] Interaction started: {type} - {text}");
        }

        private void HandleInteractionProgress(float progress)
        {
            UpdateProgressBar(progress);
        }

        private void HandleInteractionCompleted(InteractionType type)
        {
            Debug.Log($"[InteractionView] Interaction completed: {type}");

            // 상자 타입은 UI를 숨기지 않음 (반복 가능한 상호작용)
            if (type != InteractionType.Chest)
            {
                HideInteractionUI();
            }
        }

        private void HandleInteractionCancelled()
        {
            Debug.Log("[InteractionView] Interaction cancelled");
            HideInteractionUI();
        }

        private void HandleNearbyInteractionAdded(
            InteractionType type,
            string text,
            GameObject target
        )
        {
            // 상자 타입은 InteractionView UI를 사용하지 않음
            if (type == InteractionType.Chest)
            {
                return;
            }

            if (!_isUIVisible && !_viewModel.IsActive)
            {
                UpdateInteractionText(text);
                ShowInteractionUI();
            }
        }

        private void HandleNearbyInteractionRemoved(InteractionType type, GameObject target)
        {
            // 상자 타입은 InteractionView UI를 사용하지 않음
            if (type == InteractionType.Chest)
            {
                return;
            }

            if (!_viewModel.IsActive && !_viewModel.HasNearbyInteractions)
            {
                HideInteractionUI();
            }
        }

        private void HandleUIVisibilityChanged(bool visible)
        {
            if (visible)
                ShowInteractionUI();
            else
                HideInteractionUI();
        }

        private void HandleViewModelPropertyChanged(string propertyName)
        {
            RefreshUI();
        }

        #endregion

        #region UI Updates

        private void UpdateInteractionText(string text)
        {
            if (_interactionText != null)
            {
                _interactionText.text = text;
            }
        }

        private void UpdateProgressBar(float progress)
        {
            if (_progressFill != null)
            {
                _progressFill.style.width = new Length(progress * 100f, LengthUnit.Percent);
            }
        }

        private void SetProgressBarVisibility(bool visible)
        {
            if (_progressBar != null)
            {
                _progressBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ShowInteractionUI()
        {
            if (_interactionContainer != null && !_isUIVisible)
            {
                _isUIVisible = true;
                _interactionContainer.style.display = DisplayStyle.Flex;

                _interactionContainer.RemoveFromClassList("fade-out");
                _interactionContainer.AddToClassList("fade-in");

                Debug.Log("[InteractionView] Showing interaction UI");
            }
        }

        private void HideInteractionUI()
        {
            if (_interactionContainer != null && _isUIVisible)
            {
                _isUIVisible = false;

                _interactionContainer.RemoveFromClassList("fade-in");
                _interactionContainer.AddToClassList("fade-out");

                _interactionContainer
                    .schedule.Execute(() =>
                    {
                        if (_interactionContainer != null)
                        {
                            _interactionContainer.style.display = DisplayStyle.None;
                        }
                    })
                    .ExecuteLater((long)(_fadeOutDuration * 1000));

                Debug.Log("[InteractionView] Hiding interaction UI");
            }
        }

        private void RefreshUI()
        {
            if (_viewModel == null)
                return;

            // 상자 타입은 InteractionView UI를 사용하지 않음
            if (_viewModel.CurrentType == InteractionType.Chest)
            {
                HideInteractionUI();
                return;
            }

            // 근처에 상자만 있는 경우에도 UI 숨김
            if (_viewModel.HasNearbyInteractions)
            {
                var nearbyInteraction = _viewModel.GetBestNearbyInteraction();
                if (nearbyInteraction != null && nearbyInteraction.type == InteractionType.Chest)
                {
                    HideInteractionUI();
                    return;
                }
            }

            UpdateInteractionText(_viewModel.CurrentText);
            UpdateProgressBar(_viewModel.CurrentProgress);

            if (_viewModel.ShowUI || _viewModel.IsActive || _viewModel.HasNearbyInteractions)
            {
                ShowInteractionUI();
            }
            else
            {
                HideInteractionUI();
            }
        }

        #endregion

        #region User Interactions

        private void OnActionButtonClicked(ClickEvent evt)
        {
            Debug.Log("[InteractionView] 🖱️ Action button clicked!");

            if (_viewModel == null)
            {
                Debug.LogError("[InteractionView] _viewModel이 null입니다!");
                return;
            }

            // 근처에 상호작용이 있으면 우선 처리 (상자 포함)
            if (_viewModel.HasNearbyInteractions)
            {
                Debug.Log("[InteractionView] 근처 상호작용 있음, 실행 시도");
                var nearbyInteraction = _viewModel.GetBestNearbyInteraction();
                if (nearbyInteraction != null)
                {
                    Debug.Log($"[InteractionView] 발견된 상호작용: {nearbyInteraction.type}, 대상: {nearbyInteraction.targetObject?.name}");

                    // 실제 상호작용 객체의 Interact 메서드 호출
                    if (nearbyInteraction.targetObject != null)
                    {
                        var interactable = nearbyInteraction.targetObject.GetComponent<IInteractable>();
                        if (interactable != null)
                        {
                            Debug.Log($"[InteractionView] ⚡ {nearbyInteraction.targetObject.name}.Interact() 호출!");
                            interactable.Interact();
                        }
                    }

                    // 상자가 아닌 경우에만 상호작용 시작
                    if (nearbyInteraction.type != InteractionType.Chest)
                    {
                        _viewModel.StartInteraction(
                            nearbyInteraction.type,
                            nearbyInteraction.displayText,
                            3f,
                            false,
                            nearbyInteraction.targetObject
                        );
                    }
                }
            }
            else if (_viewModel.IsActive)
            {
                // 활성 상호작용이 있을 때 (상자는 이미 처리됨)
                Debug.Log("[InteractionView] 활성 상호작용 처리");

                if (_viewModel.IsInProgress)
                {
                    Debug.Log("[InteractionView] 진행 중인 상호작용 취소");
                    _viewModel.CancelInteraction();
                }
                else
                {
                    Debug.Log("[InteractionView] 상호작용 완료");
                    _viewModel.CompleteInteraction();
                }
            }
            else
            {
                Debug.LogWarning("[InteractionView] 근처에 상호작용 가능한 객체가 없습니다!");
            }
        }


        #endregion

        #region Public API

        public void SetInteractionText(string text)
        {
            UpdateInteractionText(text);
        }

        public void ForceShowUI()
        {
            ShowInteractionUI();
        }

        public void ForceHideUI()
        {
            HideInteractionUI();
        }

        /// <summary>
        /// 상호작용 버튼 가시성 업데이트 (ActionButtonController 대체)
        /// </summary>
        public void UpdateInteractionButtonVisibility(bool visible)
        {
            if (_actionButton != null)
            {
                _actionButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                Debug.Log($"[InteractionView] 🔘 상호작용 버튼 {(visible ? "표시" : "숨김")} - 버튼 상태: {_actionButton.style.display.value}");
            }
            else
            {
                Debug.LogError("[InteractionView] ❌ _actionButton이 null입니다! 버튼 가시성을 변경할 수 없습니다.");
            }
        }

        /// <summary>
        /// 상호작용 버튼이 표시되어 있는지 확인
        /// </summary>
        public bool IsInteractionButtonVisible()
        {
            if (_actionButton == null) return false;
            return _actionButton.style.display == DisplayStyle.Flex;
        }

        public bool IsUIVisible => _isUIVisible;

        #endregion
    }
}
