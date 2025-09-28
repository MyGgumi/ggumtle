using System;
using Features.Chest.Messages;
using Features.Ggumtle.Messages;
using Features.MobileControls.Messages;
using Features.MobileControls.Models;
using Features.Player.Services;
using Features.PlayerList.Models;
using Features.Revival.Messages;
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
        private readonly PlayerManagerService _playerManagerService;

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
        private readonly ISubscriber<GgumtleDetectedMessage> _ggumtleDetectedSubscriber;
        private readonly ISubscriber<GgumtleLeftMessage> _ggumtleLeftSubscriber;
        private readonly ISubscriber<ChestDetectedMessage> _chestDetectedSubscriber;
        private readonly ISubscriber<ChestLeftMessage> _chestLeftSubscriber;
        private readonly ISubscriber<ChestClosedMessage> _chestClosedSubscriber;
        private readonly ISubscriber<FaintedMonggingDetectedMessage> _faintedMonggingDetectedSubscriber;
        private readonly ISubscriber<FaintedMonggingLeftMessage> _faintedMonggingLeftSubscriber;
        private readonly ISubscriber<Features.EscapeGate.Messages.EscapeGateDetectedMessage> _escapeGateDetectedSubscriber;
        private readonly ISubscriber<Features.EscapeGate.Messages.EscapeGateLeftMessage> _escapeGateLeftSubscriber;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private bool _isInitialized = false;

        // 상호작용 상태 추적
        private bool _isGgumtleNearby = false;
        private bool _isChestNearby = false;
        private bool _isFaintedMonggingNearby = false;
        private bool _isEscapeGateNearby = false;
        private string _currentGgumtleId = null;
        private int _currentChestId = -1;
        private long _currentFaintedMonggingId = -1;
        private int _currentEscapeGateId = -1;

        #endregion

        #region Constructor & Initialization

        [Inject]
        public MobileControlsViewModel(
            PlayerManagerService playerManagerService,
            IPublisher<JoystickInputMessage> joystickInputPublisher,
            IPublisher<JoystickEndMessage> joystickEndPublisher,
            IPublisher<MobileButtonPressedMessage> buttonPressedPublisher,
            IPublisher<MobileButtonReleasedMessage> buttonReleasedPublisher,
            IPublisher<InteractHoldStartMessage> interactHoldStartPublisher,
            IPublisher<InteractHoldEndMessage> interactHoldEndPublisher,
            IPublisher<CameraTouchMessage> cameraTouchPublisher,
            ISubscriber<InteractButtonVisibilityMessage> interactVisibilitySubscriber,
            ISubscriber<GgumtleDetectedMessage> ggumtleDetectedSubscriber,
            ISubscriber<GgumtleLeftMessage> ggumtleLeftSubscriber,
            ISubscriber<ChestDetectedMessage> chestDetectedSubscriber,
            ISubscriber<ChestLeftMessage> chestLeftSubscriber,
            ISubscriber<ChestClosedMessage> chestClosedSubscriber,
            ISubscriber<FaintedMonggingDetectedMessage> faintedMonggingDetectedSubscriber,
            ISubscriber<FaintedMonggingLeftMessage> faintedMonggingLeftSubscriber,
            ISubscriber<Features.EscapeGate.Messages.EscapeGateDetectedMessage> escapeGateDetectedSubscriber,
            ISubscriber<Features.EscapeGate.Messages.EscapeGateLeftMessage> escapeGateLeftSubscriber
        )
        {
            _data = new MobileControlsData();
            _playerManagerService = playerManagerService;

            _joystickInputPublisher = joystickInputPublisher;
            _joystickEndPublisher = joystickEndPublisher;
            _buttonPressedPublisher = buttonPressedPublisher;
            _buttonReleasedPublisher = buttonReleasedPublisher;
            _interactHoldStartPublisher = interactHoldStartPublisher;
            _interactHoldEndPublisher = interactHoldEndPublisher;
            _cameraTouchPublisher = cameraTouchPublisher;
            _interactVisibilitySubscriber = interactVisibilitySubscriber;
            _ggumtleDetectedSubscriber = ggumtleDetectedSubscriber;
            _ggumtleLeftSubscriber = ggumtleLeftSubscriber;
            _chestDetectedSubscriber = chestDetectedSubscriber;
            _chestLeftSubscriber = chestLeftSubscriber;
            _chestClosedSubscriber = chestClosedSubscriber;
            _faintedMonggingDetectedSubscriber = faintedMonggingDetectedSubscriber;
            _faintedMonggingLeftSubscriber = faintedMonggingLeftSubscriber;
            _escapeGateDetectedSubscriber = escapeGateDetectedSubscriber;
            _escapeGateLeftSubscriber = escapeGateLeftSubscriber;

            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] VContainer 의존성 주입 완료 - JoystickPublisher: {joystickInputPublisher != null}"
            );

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

            // 꿈틀이 관련 메시지 구독
            _ggumtleDetectedSubscriber.Subscribe(OnGgumtleDetected).AddTo(_disposables);

            _ggumtleLeftSubscriber.Subscribe(OnGgumtleLeft).AddTo(_disposables);

            // 상자 관련 메시지 구독
            _chestDetectedSubscriber.Subscribe(OnChestDetected).AddTo(_disposables);

            _chestLeftSubscriber.Subscribe(OnChestLeft).AddTo(_disposables);

            _chestClosedSubscriber.Subscribe(OnChestClosed).AddTo(_disposables);

            // 기절한 몽깅이 관련 메시지 구독
            _faintedMonggingDetectedSubscriber
                .Subscribe(OnFaintedMonggingDetected)
                .AddTo(_disposables);

            _faintedMonggingLeftSubscriber.Subscribe(OnFaintedMonggingLeft).AddTo(_disposables);

            // 탈출 게이트 관련 메시지 구독
            _escapeGateDetectedSubscriber.Subscribe(OnEscapeGateDetected).AddTo(_disposables);

            _escapeGateLeftSubscriber.Subscribe(OnEscapeGateLeft).AddTo(_disposables);

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

        // 홀드 시간 측정용
        private float _holdStartTime = 0f;

        /// <summary>
        /// 상호작용 홀드 시작
        /// </summary>
        public void StartInteractHold()
        {
            if (!_data.interactButton.isVisible || !_data.interactButton.isEnabled)
                return;

            _holdStartTime = Time.time;
            _interactHoldStartPublisher.Publish(InteractHoldStartMessage.Instance);
            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] 상호작용 홀드 시작 - 시간: {_holdStartTime:F2}"
            );
        }

        /// <summary>
        /// 상호작용 홀드 종료
        /// </summary>
        public void EndInteractHold()
        {
            float holdDuration = Time.time - _holdStartTime;
            _interactHoldEndPublisher.Publish(InteractHoldEndMessage.Instance);
            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] 상호작용 홀드 종료 - 홀드 시간: {holdDuration:F2}초"
            );
        }

        #endregion

        #region Public Methods - Camera

        /// <summary>
        /// 카메라 터치 입력 처리
        /// </summary>
        public void OnCameraTouch(Vector2 deltaPosition, bool isActive)
        {
            // UnityEngine.Debug.Log($"[MobileControlsViewModel] 카메라 터치 메시지 발행: 델타={deltaPosition}, 활성={isActive}");
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

            Debug.Log(
                $"[MobileControlsViewModel] 상호작용 버튼 가시성 변경: {msg.IsVisible} ({msg.Reason})"
            );
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

        #region Message Handlers

        /// <summary>
        /// 꿈틀이 감지 메시지 처리
        /// </summary>
        private void OnGgumtleDetected(GgumtleDetectedMessage message)
        {
            _isGgumtleNearby = true;
            _currentGgumtleId = message.GgumtleId;

            UpdateInteractButtonVisibility();

            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] 꿈틀이 감지: {message.GgumtleId}, 상호작용 버튼 표시"
            );
        }

        /// <summary>
        /// 꿈틀이 벗어남 메시지 처리
        /// </summary>
        private void OnGgumtleLeft(GgumtleLeftMessage message)
        {
            if (_currentGgumtleId == message.GgumtleId)
            {
                _isGgumtleNearby = false;
                _currentGgumtleId = null;

                UpdateInteractButtonVisibility();

                UnityEngine.Debug.Log(
                    $"[MobileControlsViewModel] 꿈틀이 벗어남: {message.GgumtleId}"
                );
            }
        }

        /// <summary>
        /// 상자 감지 메시지 처리
        /// </summary>
        private void OnChestDetected(ChestDetectedMessage message)
        {
            _isChestNearby = true;
            _currentChestId = message.ChestId;

            UpdateInteractButtonVisibility();

            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] 상자 감지: {message.ChestId}, 상호작용 버튼 표시"
            );
        }

        /// <summary>
        /// 상자 벗어남 메시지 처리
        /// </summary>
        private void OnChestLeft(ChestLeftMessage message)
        {
            if (_currentChestId == message.ChestId)
            {
                _isChestNearby = false;
                _currentChestId = -1;

                UpdateInteractButtonVisibility();

                UnityEngine.Debug.Log($"[MobileControlsViewModel] 상자 벗어남: {message.ChestId}");
            }
        }

        /// <summary>
        /// 상자 닫힘 메시지 처리
        /// </summary>
        private void OnChestClosed(ChestClosedMessage message)
        {
            if (_currentChestId == message.ChestId)
            {
                // 상자가 닫혔지만 아직 근처에 있을 수 있으므로 상태만 초기화
                UnityEngine.Debug.Log(
                    $"[MobileControlsViewModel] 상자 닫힘 처리: {message.ChestId} (현재 근처: {_isChestNearby})"
                );
            }
        }

        /// <summary>
        /// 기절한 몽깅이 감지 메시지 처리
        /// </summary>
        private void OnFaintedMonggingDetected(FaintedMonggingDetectedMessage message)
        {
            _isFaintedMonggingNearby = true;
            _currentFaintedMonggingId = message.PlayerId;

            UpdateInteractButtonVisibility();

            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] 기절한 몽깅이 감지: {message.PlayerId} ({message.PlayerName}), 상호작용 버튼 표시"
            );
        }

        /// <summary>
        /// 기절한 몽깅이 벗어남 메시지 처리
        /// </summary>
        private void OnFaintedMonggingLeft(FaintedMonggingLeftMessage message)
        {
            if (_currentFaintedMonggingId == message.PlayerId)
            {
                _isFaintedMonggingNearby = false;
                _currentFaintedMonggingId = -1;

                UpdateInteractButtonVisibility();

                UnityEngine.Debug.Log(
                    $"[MobileControlsViewModel] 기절한 몽깅이 벗어남: {message.PlayerId}"
                );
            }
        }

        /// <summary>
        /// 상호작용 버튼 가시성 업데이트 - 로컬 몽깅이 플레이어만 표시
        /// </summary>
        private void UpdateInteractButtonVisibility()
        {
            // 로컬 플레이어 역할 확인
            var localPlayerRole = _playerManagerService.GetLocalPlayerRole();
            bool isLocalMongging = localPlayerRole == PlayerRole.Mongging;

            // 로컬 몽깅이이고 상호작용 객체가 근처에 있을 때만 표시
            if (!isLocalMongging)
                return;
            bool shouldShow =
                _isGgumtleNearby
                || _isChestNearby
                || _isFaintedMonggingNearby
                || _isEscapeGateNearby;

            var interactData = _data.GetButtonData(MobileButtonType.Interact);
            if (interactData != null)
            {
                interactData.SetVisibility(shouldShow);
                UpdateButtonProperties(MobileButtonType.Interact, interactData);
            }

            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] 상호작용 버튼 가시성 업데이트: {shouldShow} (isLocalMongging: {isLocalMongging}, 꿈틀이: {_isGgumtleNearby}, 상자: {_isChestNearby}, 기절몽깅이: {_isFaintedMonggingNearby}, 탈출구: {_isEscapeGateNearby})"
            );
        }

        /// <summary>
        /// 탈출 게이트 감지 메시지 처리
        /// </summary>
        private void OnEscapeGateDetected(
            Features.EscapeGate.Messages.EscapeGateDetectedMessage message
        )
        {
            _isEscapeGateNearby = true;
            _currentEscapeGateId = message.GateId;

            UpdateInteractButtonVisibility();

            UnityEngine.Debug.Log(
                $"[MobileControlsViewModel] 탈출 게이트 감지: {message.GateId}, 상호작용 버튼 표시"
            );
        }

        /// <summary>
        /// 탈출 게이트 벗어남 메시지 처리
        /// </summary>
        private void OnEscapeGateLeft(Features.EscapeGate.Messages.EscapeGateLeftMessage message)
        {
            if (_currentEscapeGateId == message.GateId)
            {
                _isEscapeGateNearby = false;
                _currentEscapeGateId = -1;

                UpdateInteractButtonVisibility();

                UnityEngine.Debug.Log(
                    $"[MobileControlsViewModel] 탈출 게이트 벗어남: {message.GateId}"
                );
            }
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
