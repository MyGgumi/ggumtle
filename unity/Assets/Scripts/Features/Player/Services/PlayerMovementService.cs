using System;
using Features.MobileControls.Messages;
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
        public bool enableDebugLogs = false;

        #endregion

        #region Movement State

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpInput;
        private bool _sprintInput;
        private bool _isMoving;

        private bool _analogMovement = true;
        private bool _canMove = true;

        #endregion

        #region Events

        public event Action<Vector2> MoveInputChanged;
        public event Action<Vector2> LookInputChanged;
        public event Action<bool> JumpInputChanged;
        public event Action<bool> SprintInputChanged;
        public event Action<bool> MovingStateChanged;

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

        #endregion

        #region Dependencies & Subscriptions

        private readonly CompositeDisposable _disposables = new();

        #endregion

        #region Constructor

        [Inject]
        public PlayerMovementService(ISubscriber<MobileInputMessage> mobileInputSubscriber)
        {
            ResetAllInputs();
            CanMove = true;
            AnalogMovement = true;

            mobileInputSubscriber.Subscribe(HandleMobileInput).AddTo(_disposables);

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

        #region Dispose

        public void Dispose()
        {
            _disposables.Dispose();

            MoveInputChanged = null;
            LookInputChanged = null;
            JumpInputChanged = null;
            SprintInputChanged = null;
            MovingStateChanged = null;

            if (enableDebugLogs)
            {
                Debug.Log("[PlayerMovementService] Dispose 완료");
            }
        }

        #endregion
    }
}
