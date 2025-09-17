using System;
using MVVM.Core;
using UnityEngine;

namespace MVVM.Movement
{
    /// <summary>
    /// 플레이어 이동 관련 ViewModel (싱글톤)
    /// StarterAssetsInputs를 대체하여 입력과 이동 로직을 분리
    /// </summary>
    public class PlayerMovementViewModel : BaseViewModel
    {
        // 싱글톤 인스턴스
        private static PlayerMovementViewModel _instance;
        public static PlayerMovementViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에서 기존 인스턴스 찾기
                    _instance = FindObjectOfType<PlayerMovementViewModel>();

                    if (_instance == null)
                    {
                        // 없으면 플레이어 GameObject에 생성
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player == null)
                        {
                            // 플레이어도 없으면 새 GameObject 생성
                            player = new GameObject("PlayerMovementViewModel");
                            Debug.LogWarning(
                                "[PlayerMovementViewModel] Player GameObject가 없어서 새로 생성"
                            );
                        }

                        _instance = player.AddComponent<PlayerMovementViewModel>();
                        Debug.Log("[PlayerMovementViewModel] 싱글톤 인스턴스 생성");
                    }
                }
                return _instance;
            }
        }

        [Header("Movement State")]
        [SerializeField]
        private Vector2 _moveInput;

        [SerializeField]
        private Vector2 _lookInput;

        [SerializeField]
        private bool _jumpInput;

        [SerializeField]
        private bool _sprintInput;

        [SerializeField]
        private bool _isMoving;

        [Header("Movement Settings")]
        [SerializeField]
        private bool _analogMovement = true;

        [SerializeField]
        private bool _canMove = true;

        // 이벤트들
        public event Action<Vector2> MoveInputChanged;
        public event Action<Vector2> LookInputChanged;
        public event Action<bool> JumpInputChanged;
        public event Action<bool> SprintInputChanged;
        public event Action<bool> MovingStateChanged;

        #region Properties

        /// <summary>
        /// 현재 이동 입력 값 (-1 ~ 1 범위)
        /// </summary>
        public Vector2 MoveInput
        {
            get => _moveInput;
            private set
            {
                if (SetProperty(ref _moveInput, value))
                {
                    MoveInputChanged?.Invoke(value);
                    UpdateMovingState();
                }
            }
        }

        /// <summary>
        /// 현재 시선 입력 값 (카메라 회전용)
        /// </summary>
        public Vector2 LookInput
        {
            get => _lookInput;
            private set
            {
                if (SetProperty(ref _lookInput, value))
                {
                    LookInputChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// 점프 입력 상태
        /// </summary>
        public bool JumpInput
        {
            get => _jumpInput;
            private set
            {
                if (SetProperty(ref _jumpInput, value))
                {
                    JumpInputChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// 달리기 입력 상태
        /// </summary>
        public bool SprintInput
        {
            get => _sprintInput;
            private set
            {
                if (SetProperty(ref _sprintInput, value))
                {
                    SprintInputChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// 현재 이동 중인지 여부
        /// </summary>
        public bool IsMoving
        {
            get => _isMoving;
            private set
            {
                if (SetProperty(ref _isMoving, value))
                {
                    MovingStateChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// 아날로그 이동 사용 여부
        /// </summary>
        public bool AnalogMovement
        {
            get => _analogMovement;
            set => SetProperty(ref _analogMovement, value);
        }

        /// <summary>
        /// 이동 가능 여부 (상호작용 중일 때 false)
        /// </summary>
        public bool CanMove
        {
            get => _canMove;
            set
            {
                if (SetProperty(ref _canMove, value))
                {
                    if (!value)
                    {
                        // 이동 불가능할 때 모든 입력 리셋
                        ResetAllInputs();
                    }
                }
            }
        }

        #endregion

        #region Input Methods (InputControllers에서 호출)

        /// <summary>
        /// 이동 입력 설정 (JoystickController에서 호출)
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            if (!CanMove)
            {
                MoveInput = Vector2.zero;
                return;
            }

            MoveInput = input;
        }

        /// <summary>
        /// 시선 입력 설정 (CameraRotationController에서 호출)
        /// </summary>
        public void SetLookInput(Vector2 input)
        {
            LookInput = input;
        }

        /// <summary>
        /// 점프 입력 설정 (ActionButtonController에서 호출)
        /// </summary>
        public void SetJumpInput(bool input)
        {
            if (!CanMove)
            {
                JumpInput = false;
                return;
            }

            JumpInput = input;
        }

        /// <summary>
        /// 달리기 입력 설정
        /// </summary>
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

        #region Movement State Management

        /// <summary>
        /// 이동 상태 업데이트
        /// </summary>
        private void UpdateMovingState()
        {
            bool wasMoving = IsMoving;
            IsMoving = CanMove && MoveInput.magnitude > 0.01f;

            if (wasMoving != IsMoving && EnableDebugLogs)
            {
                Debug.Log($"[PlayerMovementViewModel] Moving state changed: {IsMoving}");
            }
        }

        /// <summary>
        /// 모든 입력 리셋
        /// </summary>
        public void ResetAllInputs()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            JumpInput = false;
            SprintInput = false;
        }

        /// <summary>
        /// 이동 입력만 리셋 (카메라는 유지)
        /// </summary>
        public void ResetMovementInputs()
        {
            MoveInput = Vector2.zero;
            JumpInput = false;
            SprintInput = false;
        }

        #endregion

        #region Unity Lifecycle

        protected override void InitializeViewModel()
        {
            // 싱글톤 설정
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[PlayerMovementViewModel] 중복 인스턴스 감지. 제거합니다.");
                Destroy(gameObject);
                return;
            }

            base.InitializeViewModel();

            // 초기 상태 설정
            ResetAllInputs();
            CanMove = true;
            AnalogMovement = true;

            if (EnableDebugLogs)
            {
                Debug.Log("[PlayerMovementViewModel] 싱글톤 초기화 완료");
            }
        }

        protected override void CleanupViewModel()
        {
            base.CleanupViewModel();

            // 이벤트 정리
            MoveInputChanged = null;
            LookInputChanged = null;
            JumpInputChanged = null;
            SprintInputChanged = null;
            MovingStateChanged = null;

            // 싱글톤 정리
            if (_instance == this)
            {
                _instance = null;
            }

            if (EnableDebugLogs)
            {
                Debug.Log("[PlayerMovementViewModel] 정리 완료");
            }
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// 현재 상태를 로그로 출력 (디버깅용)
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log(
                $"[PlayerMovementViewModel] State:\n"
                    + $"  MoveInput: {MoveInput}\n"
                    + $"  LookInput: {LookInput}\n"
                    + $"  JumpInput: {JumpInput}\n"
                    + $"  SprintInput: {SprintInput}\n"
                    + $"  IsMoving: {IsMoving}\n"
                    + $"  CanMove: {CanMove}\n"
                    + $"  AnalogMovement: {AnalogMovement}"
            );
        }

        #endregion
    }
}
