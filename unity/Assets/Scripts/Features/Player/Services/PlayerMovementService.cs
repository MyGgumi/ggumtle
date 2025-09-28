using System;
using System.Collections;
using Features.MobileControls.Messages;
using Features.FieldItem.Messages;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Player.Services
{
    public class PlayerMovementService : IDisposable
    {
        #region Debug Settings

        [Header("Debug Settings")]
        public bool enableDebugLogs = false; // 입력 로그 활성화
        public bool enableInitLogs = false; // 초기화 로그

        #endregion

        #region Movement State

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _sprintInput;
        private bool _isMoving;

        private bool _analogMovement = true;
        private bool _canMove = true;

        // 스피드 배율 관련
        private float _speedMultiplier = 1.0f;
        private Coroutine _speedBoostCoroutine;

        #endregion

        #region Events

        public event Action<Vector2> MoveInputChanged;
        public event Action<Vector2> LookInputChanged;
        public event Action<bool> JumpInputChanged;
        public event Action<bool> SprintInputChanged;
        public event Action<bool> MovingStateChanged;
        public event Action<float> SpeedMultiplierChanged;

        #endregion

        #region Properties

        public Vector2 MoveInput
        {
            get => _moveInput;
            private set
            {
                if (_moveInput != value)
                {
                    _moveInput = value;
                    MoveInputChanged?.Invoke(value);
                    UpdateMovingState();
                }
            }
        }

        public Vector2 LookInput
        {
            get => _lookInput;
            private set
            {
                if (_lookInput != value)
                {
                    _lookInput = value;
                    LookInputChanged?.Invoke(value);
                }
            }
        }

        public bool JumpInput
        {
            get => _jumpInput;
            private set
            {
                if (_jumpInput != value)
                {
                    _jumpInput = value;
                    JumpInputChanged?.Invoke(value);
                }
            }
        }

        public bool SprintInput
        {
            get => _sprintInput;
            private set
            {
                if (_sprintInput != value)
                {
                    _sprintInput = value;
                    SprintInputChanged?.Invoke(value);
                }
            }
        }

        public bool IsMoving
        {
            get => _isMoving;
            private set
            {
                if (_isMoving != value)
                {
                    _isMoving = value;
                    MovingStateChanged?.Invoke(value);
                }
            }
        }

        public bool AnalogMovement
        {
            get => _analogMovement;
            set => _analogMovement = value;
        }

        public bool CanMove
        {
            get => _canMove;
            set
            {
                _canMove = value;
                if (!value)
                {
                    ResetAllInputs();
                }
            }
        }

        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            private set
            {
                if (Math.Abs(_speedMultiplier - value) > 0.01f)
                {
                    _speedMultiplier = value;
                    SpeedMultiplierChanged?.Invoke(value);

                    if (enableDebugLogs)
                        Debug.Log($"[PlayerMovementService] 속도 배율 변경: {value:F2}");
                }
            }
        }

        #endregion

        #region Dependencies & Subscriptions

        private readonly CompositeDisposable _disposables = new();

        #endregion

        #region Constructor

        [Inject]
        public PlayerMovementService(
            ISubscriber<MobileInputMessage> mobileInputSubscriber,
            ISubscriber<SpeedChangedMessage> speedChangedSubscriber)
        {
            ResetAllInputs();
            CanMove = true;
            AnalogMovement = true;
            SpeedMultiplier = 1.0f;

            mobileInputSubscriber.Subscribe(HandleMobileInput).AddTo(_disposables);
            speedChangedSubscriber.Subscribe(HandleSpeedChanged).AddTo(_disposables);

            if (enableDebugLogs)
            {
                Debug.Log("[PlayerMovementService] 초기화 및 MessagePipe 구독 완료");
            }
        }

        #endregion

        #region Input Handling

        private void HandleMobileInput(MobileInputMessage msg)
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerMovementService] 입력 수신: {msg.InputType}, 값: {(msg.InputType == MobileInputType.Move || msg.InputType == MobileInputType.Look ? msg.Vector2Value.ToString() : msg.BoolValue.ToString())}"
                );
            }

            switch (msg.InputType)
            {
                case MobileInputType.Move:
                    SetMoveInput(msg.Vector2Value);
                    break;
                case MobileInputType.Look:
                    SetLookInput(msg.Vector2Value);
                    break;
                case MobileInputType.Jump:
                    SetJumpInput(msg.BoolValue);
                    break;
                case MobileInputType.Sprint:
                    SetSprintInput(msg.BoolValue);
                    break;
            }
        }

        public void SetMoveInput(Vector2 input)
        {
            if (!CanMove)
            {
                MoveInput = Vector2.zero;
                return;
            }

            MoveInput = input;
        }

        public void SetLookInput(Vector2 input)
        {
            if (enableDebugLogs && input.magnitude > 0.01f)
                Debug.Log($"[PlayerMovementService] LookInput 설정: {input}");
            LookInput = input;
        }

        public void SetJumpInput(bool input)
        {
            if (!CanMove)
            {
                JumpInput = false;
                return;
            }

            JumpInput = input;
        }

        public void SetSprintInput(bool input)
        {
            if (!CanMove)
            {
                SprintInput = false;
                return;
            }

            SprintInput = input;
        }

        private void HandleSpeedChanged(SpeedChangedMessage msg)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerMovementService] 속도 변경 메시지 수신: UserId={msg.userId}, Multiplier={msg.speedMultiplier}, Duration={msg.duration}초");
            }

            // 기존 속도 부스트 중단
            if (_speedBoostCoroutine != null)
            {
                // 코루틴은 MonoBehaviour에서만 실행 가능하므로 직접 타이머 관리
                StopSpeedBoost();
            }

            // 새로운 속도 부스트 적용
            ApplySpeedBoost(msg.speedMultiplier, msg.duration);
        }

        #endregion

        #region State Management

        private void UpdateMovingState()
        {
            IsMoving = CanMove && MoveInput.magnitude > 0.01f;
        }

        public void ResetAllInputs()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            JumpInput = false;
            SprintInput = false;
        }

        public void ResetMovementInputs()
        {
            MoveInput = Vector2.zero;
            JumpInput = false;
            SprintInput = false;
        }

        #endregion

        #region Speed Boost Management

        private void ApplySpeedBoost(float multiplier, float duration)
        {
            SpeedMultiplier = multiplier;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerMovementService] 속도 부스트 적용: {multiplier:F2}배, {duration:F1}초 지속");
            }

            // UniTask로 타이머 관리 (코루틴 대신)
            ResetSpeedAfterDelay(duration);
        }

        private async void ResetSpeedAfterDelay(float delay)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay((int)(delay * 1000));

                SpeedMultiplier = 1.0f;

                if (enableDebugLogs)
                {
                    Debug.Log("[PlayerMovementService] 속도 부스트 종료");
                }
            }
            catch (System.Exception e)
            {
                if (enableDebugLogs)
                {
                    Debug.LogError($"[PlayerMovementService] 속도 부스트 타이머 오류: {e.Message}");
                }
            }
        }

        private void StopSpeedBoost()
        {
            SpeedMultiplier = 1.0f;

            if (enableDebugLogs)
            {
                Debug.Log("[PlayerMovementService] 속도 부스트 중단");
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables.Dispose();

            MoveInputChanged = null;
            LookInputChanged = null;
            JumpInputChanged = null;
            SprintInputChanged = null;
            MovingStateChanged = null;
            SpeedMultiplierChanged = null;

            // 속도 부스트 중단
            StopSpeedBoost();

            if (enableDebugLogs)
            {
                Debug.Log("[PlayerMovementService] Dispose 완료");
            }
        }

        #endregion
    }
}
