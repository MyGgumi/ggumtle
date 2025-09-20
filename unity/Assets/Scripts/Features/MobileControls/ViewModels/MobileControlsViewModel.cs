using System;
using Features.MobileControls.Messages;
using Features.MobileControls.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.MobileControls.ViewModels
{
    /// <summary>
    /// 모바일 컨트롤 전체 상태를 관리하는 ViewModel
    /// UI 바인딩과 입력 이벤트 처리를 담당
    /// </summary>
    public class MobileControlsViewModel : IDisposable
    {
        #region Observable Properties

        // 전체 컨트롤 상태
        public readonly ReactiveProperty<bool> IsEnabled = new(true);
        public readonly ReactiveProperty<float> GlobalOpacity = new(1.0f);

        // 조이스틱 상태
        public readonly ReactiveProperty<bool> JoystickActive = new(true);
        public readonly ReactiveProperty<bool> JoystickDragging = new(false);
        public readonly ReactiveProperty<Vector2> JoystickInput = new(Vector2.zero);
        public readonly ReactiveProperty<Vector2> JoystickKnobPosition = new(Vector2.zero);

        // 점프 버튼 상태
        public readonly ReactiveProperty<bool> JumpButtonVisible = new(true);
        public readonly ReactiveProperty<bool> JumpButtonEnabled = new(true);
        public readonly ReactiveProperty<bool> JumpButtonPressed = new(false);
        public readonly ReactiveProperty<float> JumpButtonScale = new(1.0f);

        // 상호작용 버튼 상태
        public readonly ReactiveProperty<bool> InteractButtonVisible = new(false);
        public readonly ReactiveProperty<bool> InteractButtonEnabled = new(true);
        public readonly ReactiveProperty<bool> InteractButtonPressed = new(false);
        public readonly ReactiveProperty<float> InteractButtonScale = new(1.0f);

        // 전체 모바일 컨트롤 표시 여부
        public readonly ReactiveProperty<bool> ShouldShowControls = new(true);

        #endregion

        #region Dependencies

        private readonly MobileControlsData _data;

        // MessagePipe Publishers
        private readonly IPublisher<JoystickInputMessage> _joystickInputPublisher;
        private readonly IPublisher<JoystickEndMessage> _joystickEndPublisher;
        private readonly IPublisher<MobileButtonPressedMessage> _buttonPressedPublisher;
        private readonly IPublisher<MobileButtonReleasedMessage> _buttonReleasedPublisher;
        private readonly IPublisher<InteractHoldStartMessage> _interactHoldStartPublisher;
        private readonly IPublisher<InteractHoldEndMessage> _interactHoldEndPublisher;
        private readonly IPublisher<CameraTouchMessage> _cameraTouchPublisher;

        // MessagePipe Subscribers
        private readonly ISubscriber<InteractButtonVisibilityMessage> _interactVisibilitySubscriber;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private bool _isInitialized = false;

        #endregion

        #region Constructor & Initialization

        [Inject]
        public MobileControlsViewModel(
            IPublisher<JoystickInputMessage> joystickInputPublisher,
            IPublisher<JoystickEndMessage> joystickEndPublisher,
            IPublisher<MobileButtonPressedMessage> buttonPressedPublisher,
            IPublisher<MobileButtonReleasedMessage> buttonReleasedPublisher,
            IPublisher<InteractHoldStartMessage> interactHoldStartPublisher,
            IPublisher<InteractHoldEndMessage> interactHoldEndPublisher,
            IPublisher<CameraTouchMessage> cameraTouchPublisher,
            ISubscriber<InteractButtonVisibilityMessage> interactVisibilitySubscriber
        )
        {
            _data = new MobileControlsData();

            _joystickInputPublisher = joystickInputPublisher;
            _joystickEndPublisher = joystickEndPublisher;
            _buttonPressedPublisher = buttonPressedPublisher;
            _buttonReleasedPublisher = buttonReleasedPublisher;
            _interactHoldStartPublisher = interactHoldStartPublisher;
            _interactHoldEndPublisher = interactHoldEndPublisher;
            _cameraTouchPublisher = cameraTouchPublisher;
            _interactVisibilitySubscriber = interactVisibilitySubscriber;

            UnityEngine.Debug.Log($"[MobileControlsViewModel] VContainer 의존성 주입 완료 - JoystickPublisher: {joystickInputPublisher != null}");

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            // 외부 메시지 구독
            _interactVisibilitySubscriber
                .Subscribe(OnInteractVisibilityRequested)
                .AddTo(_disposables);

            // 데이터 상태를 ReactiveProperty와 동기화
            SyncDataToProperties();

            // 플랫폼에 따른 표시 여부 계산
            UpdateControlVisibility();

            _isInitialized = true;
            // Debug.Log("[MobileControlsViewModel] 초기화 완료");
        }

        #endregion

        #region Public Methods - Joystick

        /// <summary>
        /// 조이스틱 드래그 시작
        /// </summary>
        public void StartJoystickDrag(Vector2 startPosition)
        {
            _data.joystick.isDragging = true;
            JoystickDragging.Value = true;

            // Debug.Log($"[MobileControlsViewModel] 조이스틱 드래그 시작: {startPosition}");
        }

        /// <summary>
        /// 조이스틱 입력 업데이트
        /// </summary>
        public void UpdateJoystickInput(Vector2 inputValue, Vector2 knobPosition)
        {
            _data.joystick.SetInput(inputValue, knobPosition);

            JoystickInput.Value = inputValue;
            JoystickKnobPosition.Value = knobPosition;

            // MessagePipe로 이벤트 발행
            _joystickInputPublisher.Publish(
                new JoystickInputMessage(inputValue, _data.joystick.isDragging)
            );

            // 디버그 로그 비활성화
            // UnityEngine.Debug.Log($"[MobileControlsViewModel] JoystickInputMessage 발행: {inputValue}, isDragging: {_data.joystick.isDragging}");
        }

        /// <summary>
        /// 조이스틱 드래그 종료
        /// </summary>
        public void EndJoystickDrag()
        {
            _data.joystick.Reset();

            JoystickDragging.Value = false;
            JoystickInput.Value = Vector2.zero;
            JoystickKnobPosition.Value = Vector2.zero;

            // MessagePipe로 종료 이벤트 발행
            _joystickEndPublisher.Publish(JoystickEndMessage.Instance);

            // Debug.Log("[MobileControlsViewModel] 조이스틱 드래그 종료");
        }

        #endregion

        #region Public Methods - Buttons

        /// <summary>
        /// 버튼 눌림 처리
        /// </summary>
        public void OnButtonPressed(MobileButtonType buttonType)
        {
            var buttonData = _data.GetButtonData(buttonType);
            if (buttonData == null || !buttonData.isEnabled)
                return;

            buttonData.SetPressed(true);
            UpdateButtonProperties(buttonType, buttonData);

            // MessagePipe로 이벤트 발행
            _buttonPressedPublisher.Publish(new MobileButtonPressedMessage(buttonType));

            // Debug.Log($"[MobileControlsViewModel] {buttonType} 버튼 눌림");
        }

        /// <summary>
        /// 버튼 해제 처리
        /// </summary>
        public void OnButtonReleased(MobileButtonType buttonType)
        {
            var buttonData = _data.GetButtonData(buttonType);
            if (buttonData == null)
                return;

            buttonData.SetPressed(false);
            UpdateButtonProperties(buttonType, buttonData);

            // MessagePipe로 이벤트 발행
            _buttonReleasedPublisher.Publish(new MobileButtonReleasedMessage(buttonType));

            // Debug.Log($"[MobileControlsViewModel] {buttonType} 버튼 해제");
        }

        /// <summary>
        /// 상호작용 홀드 시작
        /// </summary>
        public void StartInteractHold()
        {
            if (!_data.interactButton.isVisible || !_data.interactButton.isEnabled)
                return;

            _interactHoldStartPublisher.Publish(InteractHoldStartMessage.Instance);
            // Debug.Log("[MobileControlsViewModel] 상호작용 홀드 시작");
        }

        /// <summary>
        /// 상호작용 홀드 종료
        /// </summary>
        public void EndInteractHold()
        {
            _interactHoldEndPublisher.Publish(InteractHoldEndMessage.Instance);
            // Debug.Log("[MobileControlsViewModel] 상호작용 홀드 종료");
        }

        #endregion

        #region Public Methods - Camera

        /// <summary>
        /// 카메라 터치 입력 처리
        /// </summary>
        public void OnCameraTouch(Vector2 deltaPosition, bool isActive)
        {
            _cameraTouchPublisher.Publish(new CameraTouchMessage(deltaPosition, isActive));
        }

        #endregion

        #region Public Methods - Settings

        /// <summary>
        /// 모바일 컨트롤 활성화/비활성화
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _data.isEnabled = enabled;
            IsEnabled.Value = enabled;
            UpdateControlVisibility();
        }


        /// <summary>
        /// 전체 투명도 설정
        /// </summary>
        public void SetGlobalOpacity(float opacity)
        {
            _data.globalOpacity = Mathf.Clamp01(opacity);
            GlobalOpacity.Value = _data.globalOpacity;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 상호작용 버튼 가시성 요청 처리 (GgumtleViewModel에서 요청)
        /// </summary>
        private void OnInteractVisibilityRequested(InteractButtonVisibilityMessage msg)
        {
            _data.interactButton.SetVisibility(msg.IsVisible);
            InteractButtonVisible.Value = msg.IsVisible;

            // Debug.Log(
            //     $"[MobileControlsViewModel] 상호작용 버튼 가시성 변경: {msg.IsVisible} ({msg.Reason})"
            // );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 데이터를 ReactiveProperty에 동기화
        /// </summary>
        private void SyncDataToProperties()
        {
            // 전체 설정
            IsEnabled.Value = _data.isEnabled;
            GlobalOpacity.Value = _data.globalOpacity;

            // 조이스틱
            JoystickActive.Value = _data.joystick.isActive;
            JoystickDragging.Value = _data.joystick.isDragging;
            JoystickInput.Value = _data.joystick.inputValue;
            JoystickKnobPosition.Value = _data.joystick.knobPosition;

            // 버튼들
            UpdateButtonProperties(MobileButtonType.Jump, _data.jumpButton);
            UpdateButtonProperties(MobileButtonType.Interact, _data.interactButton);
        }

        /// <summary>
        /// 버튼별 ReactiveProperty 업데이트
        /// </summary>
        private void UpdateButtonProperties(MobileButtonType buttonType, MobileButtonData data)
        {
            switch (buttonType)
            {
                case MobileButtonType.Jump:
                    JumpButtonVisible.Value = data.isVisible;
                    JumpButtonEnabled.Value = data.isEnabled;
                    JumpButtonPressed.Value = data.isPressed;
                    JumpButtonScale.Value = data.pressScale;
                    break;

                case MobileButtonType.Interact:
                    InteractButtonVisible.Value = data.isVisible;
                    InteractButtonEnabled.Value = data.isEnabled;
                    InteractButtonPressed.Value = data.isPressed;
                    InteractButtonScale.Value = data.pressScale;
                    break;
            }
        }

        /// <summary>
        /// 플랫폼에 따른 컨트롤 표시 여부 업데이트
        /// </summary>
        private void UpdateControlVisibility()
        {
            bool shouldShow = _data.ShouldShowControls();
            ShouldShowControls.Value = shouldShow;

            // Debug.Log($"[MobileControlsViewModel] 컨트롤 표시 여부: {shouldShow} (isEnabled: {_data.isEnabled})");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables.Dispose();
            // Debug.Log("[MobileControlsViewModel] Dispose 완료");
        }

        #endregion
    }
}
