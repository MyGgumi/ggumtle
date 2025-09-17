using Features.MobileControls.Messages;
using Features.MobileControls.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.MobileControls.Services
{
    /// <summary>
    /// 모바일 입력 처리 서비스
    /// 모바일 컨트롤(조이스틱, 버튼, 터치) 입력을 처리하고 MessagePipe로 발행
    /// </summary>
    public class MobileInputService : System.IDisposable
    {
        #region Debug Settings

        [Header("Debug Settings")]
        public bool enableDebugLogs = false;

        #endregion

        #region Dependencies & Publishers

        private readonly CompositeDisposable _disposables = new();
        private readonly IPublisher<MobileInputMessage> _mobileInputPublisher;

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
            _mobileInputPublisher = mobileInputPublisher;

            // MessagePipe 구독 - 모바일 컨트롤 입력
            joystickInputSubscriber
                .Subscribe(msg => PublishMoveInput(msg.InputValue))
                .AddTo(_disposables);

            joystickEndSubscriber.Subscribe(_ => PublishMoveInput(Vector2.zero)).AddTo(_disposables);

            buttonPressedSubscriber
                .Subscribe(msg =>
                {
                    if (msg.ButtonType == MobileButtonType.Jump)
                        PublishJumpInput(true);
                })
                .AddTo(_disposables);

            buttonReleasedSubscriber
                .Subscribe(msg =>
                {
                    if (msg.ButtonType == MobileButtonType.Jump)
                        PublishJumpInput(false);
                })
                .AddTo(_disposables);

            cameraTouchSubscriber
                .Subscribe(msg =>
                {
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
