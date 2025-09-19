using Features.Player.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Features.MobileControls.Testing
{
    /// <summary>
    /// 키보드 입력 디버그 컨트롤러 (테스트용)
    /// 새로운 Input System 사용 - WASD로 이동, Space로 점프
    /// </summary>
    public class KeyboardDebugController : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField]
        private bool enableKeyboardInput = true;

        [SerializeField]
        private bool showDebugLogs = false;

        private PlayerMovementService _playerMovementService;
        private Vector2 _currentMoveInput;
        private bool _wasJumping = false;

        // Input Actions
        private InputAction _moveAction;
        private InputAction _jumpAction;

        [Inject]
        public void Initialize(PlayerMovementService playerMovementService)
        {
            _playerMovementService = playerMovementService;
            UnityEngine.Debug.Log("[KeyboardDebugController] PlayerMovementService 연결 완료");
        }

        void Start()
        {
            // VContainer 주입이 안된 경우 직접 찾기 시도
            if (_playerMovementService == null)
            {
                UnityEngine.Debug.LogWarning("[KeyboardDebugController] PlayerMovementService가 주입되지 않음. 직접 찾기 시도...");

                try
                {
                    var lifetimeScope = VContainer.Unity.LifetimeScope.Find<VContainer.Unity.LifetimeScope>();
                    if (lifetimeScope != null)
                    {
                        _playerMovementService = lifetimeScope.Container.Resolve<Features.Player.Services.PlayerMovementService>();
                        UnityEngine.Debug.Log("[KeyboardDebugController] VContainer에서 PlayerMovementService 찾기 성공");
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[KeyboardDebugController] VContainer에서 PlayerMovementService 찾기 실패: {e.Message}");
                }
            }

            if (_playerMovementService == null)
            {
                UnityEngine.Debug.LogError(
                    "[KeyboardDebugController] PlayerMovementService를 찾을 수 없습니다!"
                );
                return;
            }

            // Input Actions 설정
            SetupInputActions();

            UnityEngine.Debug.Log(
                "[KeyboardDebugController] 키보드 디버그 컨트롤러 초기화 완료 (WASD 이동, Space 점프)"
            );
        }

        private void SetupInputActions()
        {
            // Move Action (WASD)
            _moveAction = new InputAction("Move", binding: "<Keyboard>/wasd");
            _moveAction
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            // Jump Action (Space)
            _jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");

            // 콜백 등록
            _moveAction.performed += OnMovePerformed;
            _moveAction.canceled += OnMoveCanceled;
            _jumpAction.performed += OnJumpPerformed;
            _jumpAction.canceled += OnJumpCanceled;

            // 활성화
            _moveAction.Enable();
            _jumpAction.Enable();
        }

        #region Input Action Callbacks

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (!enableKeyboardInput || _playerMovementService == null)
                return;

            Vector2 moveInput = context.ReadValue<Vector2>();
            _currentMoveInput = moveInput;
            _playerMovementService.SetMoveInput(moveInput);

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log($"[KeyboardDebugController] 이동 입력: {moveInput}");
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            if (!enableKeyboardInput || _playerMovementService == null)
                return;

            _currentMoveInput = Vector2.zero;
            _playerMovementService.SetMoveInput(Vector2.zero);

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("[KeyboardDebugController] 이동 입력 종료");
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!enableKeyboardInput || _playerMovementService == null)
                return;

            _playerMovementService.SetJumpInput(true);
            _wasJumping = true;

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("[KeyboardDebugController] 점프 시작");
            }
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            if (!enableKeyboardInput || _playerMovementService == null)
                return;

            _playerMovementService.SetJumpInput(false);
            _wasJumping = false;

            if (showDebugLogs)
            {
                UnityEngine.Debug.Log("[KeyboardDebugController] 점프 종료");
            }
        }

        #endregion

        void OnDisable()
        {
            // 비활성화 시 입력 초기화
            if (_playerMovementService != null)
            {
                _playerMovementService.SetMoveInput(Vector2.zero);
                _playerMovementService.SetJumpInput(false);
            }

            // Input Actions 정리
            CleanupInputActions();
        }

        void OnDestroy()
        {
            CleanupInputActions();
        }

        private void CleanupInputActions()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
                _moveAction.Disable();
                _moveAction.Dispose();
                _moveAction = null;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJumpPerformed;
                _jumpAction.canceled -= OnJumpCanceled;
                _jumpAction.Disable();
                _jumpAction.Dispose();
                _jumpAction = null;
            }
        }

        #region Public Methods

        /// <summary>
        /// 키보드 입력 활성화/비활성화
        /// </summary>
        public void SetKeyboardInputEnabled(bool enabled)
        {
            enableKeyboardInput = enabled;

            if (!enabled && _playerMovementService != null)
            {
                _playerMovementService.SetMoveInput(Vector2.zero);
                _playerMovementService.SetJumpInput(false);
            }

            UnityEngine.Debug.Log(
                $"[KeyboardDebugController] 키보드 입력 {(enabled ? "활성화" : "비활성화")}"
            );
        }

        /// <summary>
        /// 디버그 로그 표시 설정
        /// </summary>
        public void SetDebugLogsEnabled(bool enabled)
        {
            showDebugLogs = enabled;
        }

        #endregion
    }
}
