using DI;
using Features.MobileControls.Models;
using Features.MobileControls.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using R3DisposableBag = R3.DisposableBag;

namespace Features.MobileControls.Views
{
    /// <summary>
    /// Mobile Controls UI View - MVVM pattern
    /// Handles UI binding and input events for mobile controls
    /// </summary>
    public class MobileControlsView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private MobileControlsViewModel viewModel;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = false;

        [Header("Joystick Settings")]
        [SerializeField]
        private float joystickRadius = 100f;

        [SerializeField]
        private float deadZone = 0.1f;

        [SerializeField]
        [Tooltip("터치 감지 범위 배율 (1.0 = 기본, 1.5 = 1.5배 확장)")]
        private float touchAreaMultiplier = 1.5f;

        // UI References
        private VisualElement _root;
        private VisualElement _mobileControls;

        // Joystick Elements
        private VisualElement _joystickArea;
        private VisualElement _joystickBase;
        private VisualElement _joystickBackground;
        private VisualElement _joystickKnob;

        // Action Buttons
        private VisualElement _actionButtons;
        private VisualElement _jumpButton;
        private VisualElement _interactButton;

        // Camera Touch Zone
        private VisualElement _cameraTouchZone;

        // Touch state
        private bool _isJoystickDragging = false;
        private bool _isCameraTouching = false;
        private Vector2 _joystickCenter;
        private Vector2 _lastCameraTouchPosition;

        // Pointer tracking for proper event isolation
        private int _joystickPointerId = -1;
        private int _cameraPointerId = -1;

        [Inject]
        public void Construct(MobileControlsViewModel mobileControlsViewModel)
        {
            viewModel = mobileControlsViewModel;
            if (enableDebugLogs)
                Debug.Log($"[MobileControlsView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError("[MobileControlsView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            CacheUIElements();
            SetupUIEvents();
            SubscribeToViewModel();
            InitializeUI();

            if (enableDebugLogs)
                Debug.Log("[MobileControlsView] Initialized successfully");
        }


        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[MobileControlsView] Root VisualElement is null");
                return;
            }

            // Find mobile controls root
            _mobileControls = _root.Q<VisualElement>("mobileControls");

            // Joystick elements
            _joystickArea = _root.Q<VisualElement>("joystickArea");
            _joystickBase = _root.Q<VisualElement>("joystickBase");
            _joystickBackground = _root.Q<VisualElement>("joystickBackground");
            _joystickKnob = _root.Q<VisualElement>("joystickKnob");

            // Action buttons
            _actionButtons = _root.Q<VisualElement>("actionButtons");
            _jumpButton = _root.Q<VisualElement>("jumpButton");
            _interactButton = _root.Q<VisualElement>("interactButton");

            // Camera touch zone
            _cameraTouchZone = _root.Q<VisualElement>("cameraТouchZone");

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[MobileControlsView] UI elements cached - "
                        + $"MobileControls: {_mobileControls != null}, "
                        + $"Joystick: {_joystickArea != null}, "
                        + $"JumpButton: {_jumpButton != null}, "
                        + $"InteractButton: {_interactButton != null}"
                );
            }
        }

        private void SetupUIEvents()
        {
            // Setup joystick events
            if (_joystickArea != null)
            {
                _joystickArea.RegisterCallback<PointerDownEvent>(OnJoystickPointerDown);
                _joystickArea.RegisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
                _joystickArea.RegisterCallback<PointerUpEvent>(OnJoystickPointerUp);
                _joystickArea.RegisterCallback<PointerLeaveEvent>(OnJoystickPointerLeave);
            }

            // Setup jump button events
            if (_jumpButton != null)
            {
                _jumpButton.RegisterCallback<PointerDownEvent>(OnJumpButtonDown);
                _jumpButton.RegisterCallback<PointerUpEvent>(OnJumpButtonUp);
                _jumpButton.RegisterCallback<PointerLeaveEvent>(OnJumpButtonLeave);
            }

            // Setup interaction button events
            if (_interactButton != null)
            {
                _interactButton.RegisterCallback<PointerDownEvent>(OnInteractButtonDown);
                _interactButton.RegisterCallback<PointerUpEvent>(OnInteractButtonUp);
                _interactButton.RegisterCallback<PointerLeaveEvent>(OnInteractButtonLeave);
            }

            // Setup camera touch events
            if (_cameraTouchZone != null)
            {
                _cameraTouchZone.RegisterCallback<PointerDownEvent>(OnCameraTouchDown);
                _cameraTouchZone.RegisterCallback<PointerMoveEvent>(OnCameraTouchMove);
                _cameraTouchZone.RegisterCallback<PointerUpEvent>(OnCameraTouchUp);
                _cameraTouchZone.RegisterCallback<PointerLeaveEvent>(OnCameraTouchLeave);
            }

            if (enableDebugLogs)
                Debug.Log("[MobileControlsView] UI events setup completed");
        }

        private R3DisposableBag _disposables = new();

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
            {
                Debug.LogError("[MobileControlsView] ViewModel not set");
                return;
            }

            // Subscribe to control visibility
            viewModel
                .ShouldShowControls.Subscribe(show => UpdateControlsVisibility(show))
                .AddTo(ref _disposables);

            // Joystick knob position is now handled directly in OnJoystickPointerMove

            // Subscribe to button states
            viewModel
                .JumpButtonVisible.Subscribe(visible =>
                    UpdateButtonVisibility(_jumpButton, visible)
                )
                .AddTo(ref _disposables);

            viewModel
                .JumpButtonScale.Subscribe(scale => UpdateButtonScale(_jumpButton, scale))
                .AddTo(ref _disposables);

            viewModel
                .InteractButtonVisible.Subscribe(visible =>
                    UpdateButtonVisibility(_interactButton, visible)
                )
                .AddTo(ref _disposables);

            viewModel
                .InteractButtonScale.Subscribe(scale => UpdateButtonScale(_interactButton, scale))
                .AddTo(ref _disposables);

            // Subscribe to opacity
            viewModel
                .GlobalOpacity.Subscribe(opacity => UpdateGlobalOpacity(opacity))
                .AddTo(ref _disposables);

            if (enableDebugLogs)
                Debug.Log("[MobileControlsView] ViewModel subscriptions completed");
        }

        private void InitializeUI()
        {
            // Calculate joystick center
            if (_joystickBase != null)
            {
                var rect = _joystickBase.worldBound;
                _joystickCenter = new Vector2(rect.width / 2, rect.height / 2);
            }

            // Initial UI state will be updated by ViewModel subscription

            if (enableDebugLogs)
                Debug.Log("[MobileControlsView] UI initialized");
        }

        #region Joystick Input Handlers

        private void OnJoystickPointerDown(PointerDownEvent evt)
        {
            if (viewModel == null || _isJoystickDragging)
                return;

            _isJoystickDragging = true;
            _joystickPointerId = evt.pointerId;
            _joystickArea.CapturePointer(evt.pointerId);

            var localPos = _joystickArea.WorldToLocal(evt.position);
            viewModel.StartJoystickDrag(localPos);

            if (enableDebugLogs)
                Debug.Log(
                    $"[MobileControlsView] Joystick drag start: {localPos}, pointerId: {evt.pointerId}"
                );

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnJoystickPointerMove(PointerMoveEvent evt)
        {
            if (!_isJoystickDragging || viewModel == null || evt.pointerId != _joystickPointerId)
                return;

            var localPos = _joystickArea.WorldToLocal(evt.position);
            var joystickCenter = new Vector2(_joystickArea.resolvedStyle.width / 2, _joystickArea.resolvedStyle.height / 2);
            var offset = localPos - joystickCenter;

            // 확장된 터치 범위 계산 (입력값 계산용)
            var extendedRadius = joystickRadius * touchAreaMultiplier;
            var distance = offset.magnitude;

            // 시각적 핸들 위치: joystickRadius로 제한
            var visualOffset = Vector2.ClampMagnitude(offset, joystickRadius);

            // 입력값 계산: 확장된 범위 사용, 하지만 최대값은 1.0으로 제한
            var inputMagnitude = Mathf.Min(distance / extendedRadius, 1.0f);
            var inputDirection = offset.normalized;
            var fullInputOffset = inputDirection * inputMagnitude * joystickRadius;

            // Update knob position using translate (시각적으로는 joystickRadius 내에만 표시)
            if (_joystickKnob != null)
            {
                _joystickKnob.style.translate = new Translate(
                    new Length(visualOffset.x, LengthUnit.Pixel),
                    new Length(visualOffset.y, LengthUnit.Pixel)
                );
            }

            // Calculate input value (-1 to 1) with Y-axis inversion
            var normalizedInput = Vector2.zero;
            if (joystickRadius > 0)
            {
                // 확장된 범위에서 계산하되 최대 1.0으로 제한
                normalizedInput = fullInputOffset / joystickRadius;
                normalizedInput.y = -normalizedInput.y; // Y축 반전 (Unity 좌표계)

                // 최대값 1.0으로 클램프
                normalizedInput = Vector2.ClampMagnitude(normalizedInput, 1.0f);
            }

            // Apply dead zone
            var magnitude = normalizedInput.magnitude;
            var inputValue = magnitude > deadZone ? normalizedInput : Vector2.zero;

            // Pass the offset for ViewModel
            var knobPosition = joystickCenter + visualOffset;
            viewModel.UpdateJoystickInput(inputValue, knobPosition);

            if (enableDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[MobileControlsView] Distance: {distance:F1}, Visual: {visualOffset.magnitude:F1}, Input: {inputValue.magnitude:F2}");
            }

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnJoystickPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == _joystickPointerId)
            {
                EndJoystickDrag();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void OnJoystickPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.pointerId == _joystickPointerId)
            {
                EndJoystickDrag();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void EndJoystickDrag()
        {
            if (!_isJoystickDragging)
                return;

            _isJoystickDragging = false;

            // Reset knob to center using translate
            if (_joystickKnob != null)
            {
                _joystickKnob.style.translate = new Translate(
                    new Length(0, LengthUnit.Pixel),
                    new Length(0, LengthUnit.Pixel)
                );
            }

            if (_joystickPointerId >= 0)
            {
                _joystickArea.ReleasePointer(_joystickPointerId);
                _joystickPointerId = -1;
            }

            if (viewModel != null)
                viewModel.EndJoystickDrag();

            if (enableDebugLogs)
                Debug.Log("[MobileControlsView] Joystick drag ended and pointer released");
        }

        #endregion

        #region Button Input Handlers

        private void OnJumpButtonDown(PointerDownEvent evt)
        {
            viewModel?.OnButtonPressed(MobileButtonType.Jump);
            evt.StopPropagation();
            evt.PreventDefault();

            if (enableDebugLogs)
                Debug.Log($"[MobileControlsView] Jump button pressed, pointerId: {evt.pointerId}");
        }

        private void OnJumpButtonUp(PointerUpEvent evt)
        {
            viewModel?.OnButtonReleased(MobileButtonType.Jump);
            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnJumpButtonLeave(PointerLeaveEvent evt)
        {
            viewModel?.OnButtonReleased(MobileButtonType.Jump);
            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnInteractButtonDown(PointerDownEvent evt)
        {
            viewModel?.OnButtonPressed(MobileButtonType.Interact);
            viewModel?.StartInteractHold();
            evt.StopPropagation();
            evt.PreventDefault();

            if (enableDebugLogs)
                Debug.Log(
                    $"[MobileControlsView] Interact button pressed, pointerId: {evt.pointerId}"
                );
        }

        private void OnInteractButtonUp(PointerUpEvent evt)
        {
            viewModel?.OnButtonReleased(MobileButtonType.Interact);
            viewModel?.EndInteractHold();
            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnInteractButtonLeave(PointerLeaveEvent evt)
        {
            viewModel?.OnButtonReleased(MobileButtonType.Interact);
            viewModel?.EndInteractHold();
            evt.StopPropagation();
            evt.PreventDefault();
        }

        #endregion

        #region Camera Touch Handlers

        private void OnCameraTouchDown(PointerDownEvent evt)
        {
            // 조이스틱이나 버튼이 이미 활성화된 경우 카메라 터치를 무시
            if (_isJoystickDragging || _isCameraTouching)
                return;

            _isCameraTouching = true;
            _cameraPointerId = evt.pointerId;
            _lastCameraTouchPosition = evt.position;
            _cameraTouchZone.CapturePointer(evt.pointerId);

            if (enableDebugLogs)
                Debug.Log($"[MobileControlsView] Camera touch start, pointerId: {evt.pointerId}");

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnCameraTouchMove(PointerMoveEvent evt)
        {
            if (!_isCameraTouching || viewModel == null || evt.pointerId != _cameraPointerId)
                return;

            var deltaPosition = (Vector2)evt.position - _lastCameraTouchPosition;
            _lastCameraTouchPosition = evt.position;


            viewModel.OnCameraTouch(deltaPosition, true);
            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnCameraTouchUp(PointerUpEvent evt)
        {
            if (evt.pointerId == _cameraPointerId)
            {
                EndCameraTouch();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void OnCameraTouchLeave(PointerLeaveEvent evt)
        {
            if (evt.pointerId == _cameraPointerId)
            {
                EndCameraTouch();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private void EndCameraTouch()
        {
            if (!_isCameraTouching)
                return;

            _isCameraTouching = false;

            if (_cameraPointerId >= 0)
            {
                _cameraTouchZone.ReleasePointer(_cameraPointerId);
                _cameraPointerId = -1;
            }

            viewModel?.OnCameraTouch(Vector2.zero, false);

        }

        #endregion

        #region UI Update Methods

        private void UpdateControlsVisibility(bool show)
        {
            if (_mobileControls != null)
            {
                _mobileControls.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

                if (enableDebugLogs)
                    Debug.Log($"[MobileControlsView] Controls visibility: {show}");
            }
        }

        private void UpdateJoystickKnobPosition(Vector2 position)
        {
            if (_joystickKnob != null)
            {
                _joystickKnob.style.left = position.x - (_joystickKnob.resolvedStyle.width / 2);
                _joystickKnob.style.top = position.y - (_joystickKnob.resolvedStyle.height / 2);
            }
        }

        private void UpdateButtonVisibility(VisualElement button, bool visible)
        {
            if (button != null)
            {
                button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateButtonScale(VisualElement button, float scale)
        {
            if (button != null)
            {
                button.style.scale = new Scale(Vector3.one * scale);
            }
        }

        private void UpdateGlobalOpacity(float opacity)
        {
            if (_mobileControls != null)
            {
                _mobileControls.style.opacity = opacity;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            _disposables.Dispose();

            if (enableDebugLogs)
                Debug.Log("[MobileControlsView] OnDestroy");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set mobile controls visibility manually
        /// </summary>
        public void SetControlsVisibility(bool visible)
        {
            UpdateControlsVisibility(visible);
        }

        /// <summary>
        /// Enable/disable debug logs
        /// </summary>
        public void SetDebugLogging(bool enable)
        {
            enableDebugLogs = enable;
        }

        #endregion
    }
}
