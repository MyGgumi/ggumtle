using Features.MobileControls.Messages;
using Features.MobileControls.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.MobileControls.Services
{
    /// <summary>
    /// 모바일 입력 처리 서비스
    /// 모바일 컨트롤(조이스틱, 버튼, 터치) 입력을 처리하고 MessagePipe로 발행
    /// </summary>
    public class MobileInputService : System.IDisposable, IStartable
    {
        #region Debug Settings

        [Header("Debug Settings")]
        public bool enableDebugLogs = true; // 이동 관련 로그 활성화
        public bool enableInitLogs = true; // 초기화 로그

        #endregion

        #region Dependencies & Publishers

        private readonly CompositeDisposable _disposables = new();
        private readonly IPublisher<MobileInputMessage> _mobileInputPublisher;

        // MessagePipe 구독자들
        private readonly ISubscriber<JoystickInputMessage> _joystickInputSubscriber;
        private readonly ISubscriber<JoystickEndMessage> _joystickEndSubscriber;
        private readonly ISubscriber<MobileButtonPressedMessage> _buttonPressedSubscriber;
        private readonly ISubscriber<MobileButtonReleasedMessage> _buttonReleasedSubscriber;
        private readonly ISubscriber<CameraTouchMessage> _cameraTouchSubscriber;

        #endregion

        #region Constructor

        [Inject]
        public MobileInputService(
            ISubscriber<JoystickInputMessage> joystickInputSubscriber,
            ISubscriber<JoystickEndMessage> joystickEndSubscriber,
            ISubscriber<MobileButtonPressedMessage> buttonPressedSubscriber,
            ISubscriber<MobileButtonReleasedMessage> buttonReleasedSubscriber,
            ISubscriber<CameraTouchMessage> cameraTouchSubscriber,
            IPublisher<MobileInputMessage> mobileInputPublisher
        )
        {
            if (enableInitLogs)
                UnityEngine.Debug.Log("[MobileInputService] 생성자 호출됨 - VContainer 의존성 주입 시작");

            _mobileInputPublisher = mobileInputPublisher;
            _joystickInputSubscriber = joystickInputSubscriber;
            _joystickEndSubscriber = joystickEndSubscriber;
            _buttonPressedSubscriber = buttonPressedSubscriber;
            _buttonReleasedSubscriber = buttonReleasedSubscriber;
            _cameraTouchSubscriber = cameraTouchSubscriber;

            if (enableInitLogs)
                UnityEngine.Debug.Log($"[MobileInputService] VContainer 의존성 주입 완료 - JoystickSubscriber: {joystickInputSubscriber != null}, Publisher: {mobileInputPublisher != null}");
        }

        public void Start()
        {
            if (enableInitLogs)
            {
                UnityEngine.Debug.Log("[MobileInputService] Entry Point Start() 호출됨");
                UnityEngine.Debug.Log($"[MobileInputService] MessagePipe Subscribers - Joystick: {_joystickInputSubscriber != null}, Publisher: {_mobileInputPublisher != null}");
            }

            // MessagePipe 구독 - 모바일 컨트롤 입력
            _joystickInputSubscriber
                .Subscribe(msg => {
                    if (enableDebugLogs)
                        UnityEngine.Debug.Log($"[MobileInputService] ✅ JoystickInputMessage 수신: {msg.InputValue}");
                    PublishMoveInput(msg.InputValue);
                })
                .AddTo(_disposables);

            _joystickEndSubscriber.Subscribe(_ => PublishMoveInput(Vector2.zero)).AddTo(_disposables);

            _buttonPressedSubscriber
                .Subscribe(msg =>
                {
                    if (msg.ButtonType == MobileButtonType.Jump)
                        PublishJumpInput(true);
                })
                .AddTo(_disposables);

            _buttonReleasedSubscriber
                .Subscribe(msg =>
                {
                    if (msg.ButtonType == MobileButtonType.Jump)
                        PublishJumpInput(false);
                })
                .AddTo(_disposables);

            _cameraTouchSubscriber
                .Subscribe(msg =>
                {
                    if (enableDebugLogs)
                        UnityEngine.Debug.Log($"[MobileInputService] ✅ CameraTouchMessage 수신: 델타={msg.DeltaPosition}, 활성={msg.IsActive}");

                    if (msg.IsActive)
                        PublishLookInput(msg.DeltaPosition);
                    else
                        PublishLookInput(Vector2.zero); // 터치 종료 시 리셋
                })
                .AddTo(_disposables);

            if (enableDebugLogs)
            {
                Debug.Log("[MobileInputService] 초기화 및 MessagePipe 구독 완료");
            }
        }

        #endregion

        #region Input Publishing

        private void PublishMoveInput(Vector2 input)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MobileInputService] Move 입력 발행: {input}");
            }
            _mobileInputPublisher.Publish(MobileInputMessage.Move(input));
        }

        private void PublishLookInput(Vector2 input)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MobileInputService] Look 입력 발행: {input}");
            }
            _mobileInputPublisher.Publish(MobileInputMessage.Look(input));
        }

        private void PublishJumpInput(bool input)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MobileInputService] Jump 입력 발행: {input}");
            }
            _mobileInputPublisher.Publish(MobileInputMessage.Jump(input));
        }

        private void PublishSprintInput(bool input)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MobileInputService] Sprint 입력 발행: {input}");
            }
            _mobileInputPublisher.Publish(MobileInputMessage.Sprint(input));
        }

        #endregion


        #region Dispose

        public void Dispose()
        {
            _disposables.Dispose();
            if (enableDebugLogs)
            {
                Debug.Log("[MobileInputService] Dispose 완룼");
            }
        }

        #endregion
    }
}
