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
        private bool _isHolding = false;

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
                _actionButton.RegisterCallback<PointerDownEvent>(OnActionButtonPointerDown);
                _actionButton.RegisterCallback<PointerUpEvent>(OnActionButtonPointerUp);
                // PointerLeave 이벤트는 너무 민감해서 일시적으로 비활성화
                // _actionButton.RegisterCallback<PointerLeaveEvent>(OnActionButtonPointerLeave);
                Debug.Log("[InteractionView] ✅ 상호작용 버튼 이벤트 등록 완료");
            }
            else
            {
                Debug.LogError($"[InteractionView] ❌ {_actionButtonName} 버튼을 찾을 수 없습니다!");
            }

            // 초기에는 상호작용 버튼 및 프로그레스바 숨김
            UpdateInteractionButtonVisibility(false);
            SetProgressBarVisibility(false);

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
                _viewModel.OnProgressBarVisibilityChanged += HandleProgressBarVisibilityChanged;
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
                _viewModel.OnProgressBarVisibilityChanged -= HandleProgressBarVisibilityChanged;
                _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }
        }

        void OnDestroy()
        {
            if (_actionButton != null)
            {
                _actionButton.UnregisterCallback<ClickEvent>(OnActionButtonClicked);
                _actionButton.UnregisterCallback<PointerDownEvent>(OnActionButtonPointerDown);
                _actionButton.UnregisterCallback<PointerUpEvent>(OnActionButtonPointerUp);
                // PointerLeave 이벤트는 사용하지 않음
            }

            // ActionButtonController 이벤트 해제 불필요 (구독하지 않음)
        }

        #region Event Handlers

        private void HandleInteractionStarted(InteractionType type, string text, float duration)
        {
            UpdateInteractionText(text);

            // 홀드 진행 중일 때는 UI 재표시로 인한 레이아웃 변경 방지
            if (!_isHolding)
            {
                ShowInteractionUI();
            }

            Debug.Log($"[InteractionView] Interaction started: {type} - {text}, 홀드 중: {_isHolding}");
        }

        private void HandleInteractionProgress(float progress)
        {
            UpdateProgressBar(progress);

            // 홀드 상호작용인 경우 핸들러에게 진행도 위임
            if (_viewModel != null && _viewModel.HasNearbyInteractions)
            {
                var nearbyInteraction = _viewModel.GetBestNearbyInteraction();
                if (nearbyInteraction != null && nearbyInteraction.targetObject != null)
                {
                    var handler = Interaction.Handlers.InteractionHandlerFactory.GetHandler(nearbyInteraction.type);
                    var interactable = nearbyInteraction.targetObject.GetComponent<IInteractable>();

                    if (handler.RequiresHold() && interactable != null)
                    {
                        handler.OnHoldProgress(interactable, progress);
                    }
                }
            }
        }

        private void HandleInteractionCompleted(InteractionType type)
        {
            Debug.Log($"[InteractionView] Interaction completed: {type}");

            // 홀드 상호작용인 경우 핸들러에게 완료 위임
            if (_viewModel != null && _viewModel.HasNearbyInteractions)
            {
                var nearbyInteraction = _viewModel.GetBestNearbyInteraction();
                if (nearbyInteraction != null && nearbyInteraction.targetObject != null)
                {
                    var handler = Interaction.Handlers.InteractionHandlerFactory.GetHandler(nearbyInteraction.type);
                    var interactable = nearbyInteraction.targetObject.GetComponent<IInteractable>();

                    if (handler.RequiresHold() && interactable != null)
                    {
                        handler.OnHoldComplete(interactable);
                    }
                }
            }

            // Feeding 타입은 UI를 유지하고 텍스트만 업데이트
            if (type != Models.InteractionType.Feeding)
            {
                HideInteractionUI();
            }
            else
            {
                // 먹이주기는 프로그레스바만 리셋하고 UI 유지
                UpdateProgressBar(0f);
                Debug.Log("[InteractionView] 먹이주기 완료 - UI 유지, 프로그레스바 리셋");
            }
        }

        private void HandleInteractionCancelled()
        {
            Debug.Log("[InteractionView] Interaction cancelled");

            // 홀드 중이었다면 홀드 상태만 초기화
            if (_isHolding)
            {
                _isHolding = false;
                Debug.Log("[InteractionView] 홀드 상태 초기화됨");
            }

            // 진행바만 0으로 초기화 (가시성은 RefreshUI에서 결정)
            UpdateProgressBar(0f);

            // 근처 상호작용 여부에 따라 UI 상태 결정 (텍스트와 진행바 함께)
            RefreshUI();
        }

        private void HandleNearbyInteractionAdded(
            InteractionType type,
            string text,
            GameObject target
        )
        {
            // 상자가 열려있는 경우 UI 표시하지 않음
            if (type == InteractionType.Chest && target != null)
            {
                var chest = target.GetComponent<InteractableChest>();
                if (chest != null && chest.isOpen)
                {
                    return;
                }
            }

            if (!_isUIVisible)
            {
                UpdateInteractionText(text);

                // 핸들러를 사용해서 프로그레스바 가시성 결정
                var handler = Interaction.Handlers.InteractionHandlerFactory.GetHandler(type);
                SetProgressBarVisibility(handler.ShouldShowProgressBar());

                ShowInteractionUI();
            }
        }

        private void HandleNearbyInteractionRemoved(InteractionType type, GameObject target)
        {
            Debug.Log($"[InteractionView] 근처 상호작용 제거됨: {type}, 남은 상호작용: {_viewModel.HasNearbyInteractions}, 활성 상호작용: {_viewModel.IsActive}");

            // 근처에 상호작용이 없으면 UI 숨김 (활성 상태와 관계없이)
            if (!_viewModel.HasNearbyInteractions)
            {
                HideInteractionUI();
                Debug.Log("[InteractionView] 근처 상호작용 없음 - UI 숨김");
            }
            else
            {
                // 남은 상호작용이 있다면 UI 새로고침
                RefreshUI();
                Debug.Log("[InteractionView] 다른 상호작용 남아있음 - UI 새로고침");
            }
        }

        private void HandleUIVisibilityChanged(bool visible)
        {
            if (visible)
                ShowInteractionUI();
            else
                HideInteractionUI();
        }

        private void HandleProgressBarVisibilityChanged(bool visible)
        {
            SetProgressBarVisibility(visible);
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
                var targetDisplay = visible ? DisplayStyle.Flex : DisplayStyle.None;

                // 현재 상태와 동일하면 불필요한 레이아웃 변경 방지
                if (_progressBar.style.display.value == targetDisplay)
                {
                    return; // 로그 제거 - 너무 자주 호출됨
                }

                _progressBar.style.display = targetDisplay;
                Debug.Log($"[InteractionView] 📊 프로그레스바 가시성 변경: {(visible ? "표시" : "숨김")}");
            }
        }

        private void ShowInteractionUI()
        {
            if (_interactionContainer != null && !_isUIVisible)
            {
                _isUIVisible = true;
                _interactionContainer.style.display = DisplayStyle.Flex;
                _interactionContainer.style.opacity = 1f;

                Debug.Log("[InteractionView] Showing interaction UI");
            }
        }

        private void HideInteractionUI()
        {
            if (_interactionContainer != null && _isUIVisible)
            {
                _isUIVisible = false;

                // fade 애니메이션 대신 opacity 트랜지션 사용
                _interactionContainer.style.opacity = 0f;

                // 프로그레스바도 컨테이너와 함께 사라지도록 (별도로 숨기지 않음)

                _interactionContainer
                    .schedule.Execute(() =>
                    {
                        if (_interactionContainer != null)
                        {
                            _interactionContainer.style.display = DisplayStyle.None;
                            _interactionContainer.style.opacity = 1f; // 다음 표시를 위해 리셋
                        }
                    })
                    .ExecuteLater((long)(_fadeOutDuration * 1000));

                Debug.Log("[InteractionView] Hiding interaction UI (텍스트와 진행바 함께)");
            }
        }

        private void RefreshUI()
        {
            if (_viewModel == null)
                return;

            UpdateInteractionText(_viewModel.CurrentText);
            UpdateProgressBar(_viewModel.CurrentProgress);

            // 근처 상호작용 확인
            if (_viewModel.HasNearbyInteractions)
            {
                var nearbyInteraction = _viewModel.GetBestNearbyInteraction();
                if (nearbyInteraction != null)
                {
                    // 상자가 열려있는 경우 UI 숨김
                    if (nearbyInteraction.type == InteractionType.Chest && nearbyInteraction.targetObject != null)
                    {
                        var chest = nearbyInteraction.targetObject.GetComponent<InteractableChest>();
                        if (chest != null && chest.isOpen)
                        {
                            HideInteractionUI();
                            return;
                        }
                    }

                    // 텍스트와 진행바 업데이트
                    UpdateInteractionText(nearbyInteraction.displayText);
                    var handler = Interaction.Handlers.InteractionHandlerFactory.GetHandler(nearbyInteraction.type);
                    SetProgressBarVisibility(handler.ShouldShowProgressBar());

                    // UI 표시
                    ShowInteractionUI();
                    return;
                }
            }

            // 근처 상호작용 없으면 UI 숨김
            HideInteractionUI();
        }

        #endregion

        #region User Interactions

        private void OnActionButtonPointerDown(PointerDownEvent evt)
        {
            Debug.Log("[InteractionView] 🖱️ Action button pointer down!");
            _isHolding = true;

            // 마우스 캡처 - 이제 PointerUp까지 모든 마우스 이벤트를 받음
            if (_actionButton != null)
            {
                _actionButton.CaptureMouse();
                Debug.Log("[InteractionView] 🎯 마우스 캡처 완료");
            }

            StartHoldInteraction();
        }

        private void OnActionButtonPointerUp(PointerUpEvent evt)
        {
            Debug.Log("[InteractionView] 🖱️ Action button pointer up!");

            // 마우스 캡처 해제
            if (_actionButton != null)
            {
                _actionButton.ReleaseMouse();
                Debug.Log("[InteractionView] 🎯 마우스 캡처 해제");
            }

            StopHoldInteraction();
        }


        private void StartHoldInteraction()
        {
            if (_viewModel == null || !_isHolding) return;

            // 근처에 상호작용이 있는지 확인
            if (_viewModel.HasNearbyInteractions)
            {
                var nearbyInteraction = _viewModel.GetBestNearbyInteraction();
                if (nearbyInteraction != null)
                {
                    Debug.Log($"[InteractionView] 홀드 시작 시도 - 타입: {nearbyInteraction.type}, 텍스트: '{nearbyInteraction.displayText}'");
                    var handler = Interaction.Handlers.InteractionHandlerFactory.GetHandler(nearbyInteraction.type);

                    if (handler.RequiresHold())
                    {
                        // 홀드가 필요한 상호작용: 핸들러에게 홀드 시작 위임
                        if (nearbyInteraction.targetObject != null)
                        {
                            var interactable = nearbyInteraction.targetObject.GetComponent<IInteractable>();
                            if (interactable != null)
                            {
                                Debug.Log($"[InteractionView] 홀드 상호작용 시작: {nearbyInteraction.type}");
                                handler.OnHoldStart(interactable);

                                // ViewModel에게 홀드 상호작용 시작 알림
                                _viewModel.StartInteraction(
                                    nearbyInteraction.type,
                                    nearbyInteraction.displayText,
                                    interactable.GetHoldDuration(),
                                    true, // requiresHold = true
                                    nearbyInteraction.targetObject
                                );
                            }
                        }
                    }
                    else
                    {
                        // 즉시 실행 타입은 상호작용 객체의 Interact 메서드 호출
                        if (nearbyInteraction.targetObject != null)
                        {
                            var interactable = nearbyInteraction.targetObject.GetComponent<IInteractable>();
                            if (interactable != null)
                            {
                                Debug.Log($"[InteractionView] ⚡ {nearbyInteraction.targetObject.name}.Interact() 호출!");
                                interactable.Interact();
                            }
                        }
                    }
                }
            }
        }

        private void StopHoldInteraction()
        {
            if (!_isHolding) return;
            _isHolding = false;

            // 마우스 캡처 해제 (PointerUp이 호출되지 않을 수 있으므로)
            if (_actionButton != null)
            {
                _actionButton.ReleaseMouse();
                Debug.Log("[InteractionView] 🎯 마우스 캡처 해제 (StopHoldInteraction)");
            }

            // 홀드가 필요한 진행 중인 상호작용이 있으면 취소
            if (_viewModel != null && _viewModel.IsInProgress)
            {
                // 핸들러에게 홀드 취소 위임
                if (_viewModel.HasNearbyInteractions)
                {
                    var nearbyInteraction = _viewModel.GetBestNearbyInteraction();
                    if (nearbyInteraction != null && nearbyInteraction.targetObject != null)
                    {
                        var handler = Interaction.Handlers.InteractionHandlerFactory.GetHandler(nearbyInteraction.type);
                        var interactable = nearbyInteraction.targetObject.GetComponent<IInteractable>();

                        if (handler.RequiresHold() && interactable != null)
                        {
                            Debug.Log("[InteractionView] 홀드 중단으로 상호작용 취소");
                            handler.OnHoldCancelled(interactable);
                        }
                    }
                }

                _viewModel.CancelInteraction();
                // CancelInteraction에서 HandleInteractionCancelled가 호출되어 RefreshUI 실행됨
            }
            else
            {
                // 진행 중인 상호작용이 없는 경우에만 직접 RefreshUI 호출
                RefreshUI();
            }
        }

        private void OnActionButtonClicked(ClickEvent evt)
        {
            // 클릭 이벤트는 홀드 처리로 대체됨
            // 홀드가 필요하지 않은 상호작용(상자, 탈출)은 PointerDown에서 처리됨
            Debug.Log("[InteractionView] 🖱️ Action button clicked! (홀드 처리로 대체됨)");
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
                var targetDisplay = visible ? DisplayStyle.Flex : DisplayStyle.None;

                // 현재 상태와 동일하면 불필요한 재렌더링 방지
                if (_actionButton.style.display.value == targetDisplay)
                {
                    return; // 로그 제거 - 너무 자주 호출됨
                }

                _actionButton.style.display = targetDisplay;
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
