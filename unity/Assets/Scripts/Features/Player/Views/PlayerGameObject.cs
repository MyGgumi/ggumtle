using Cinemachine;
using Features.Player.Services;
using UnityEngine;
using VContainer;

namespace Features.Player.Views
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerGameObject : MonoBehaviour
    {
        [Header("플레이어 이동")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("점프")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("땅 체크")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("모바일 카메라 설정")]
        public bool useFreeLookCamera = true;
        public float CameraSensitivity = 0.5f;
        public float CameraInputSmoothing = 0.1f;
        public bool InvertY = false;

        [Header("상호작용 설정")]
        public bool canMove = true;

        [Header("Debug Settings")]
        public bool enableDebugLogs = false;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private PlayerMovementService _playerMovementService;
        private GameObject _mainCamera;
        private CinemachineFreeLook _freeLookCamera;

        // 카메라 회전 관련
        private Vector2 _currentCameraInput;

        private bool _hasAnimator;

        private void Awake()
        {
            // 카메라 찾기 - 여러 방법 시도
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                if (_mainCamera == null)
                {
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        _mainCamera = camera.gameObject;
                        Debug.Log($"[PlayerGameObject] Camera.main으로 카메라 찾음: {_mainCamera.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerGameObject] Awake()에서 MainCamera를 찾을 수 없음 - Start()에서 재시도");
                    }
                }
            }

            // CinemachineFreeLook 카메라 찾기
            if (_freeLookCamera == null)
            {
                _freeLookCamera = FindFirstObjectByType<CinemachineFreeLook>();
                if (_freeLookCamera != null)
                {
                    if (enableDebugLogs)
                        Debug.Log(
                            $"[PlayerGameObject] CinemachineFreeLook 발견: {_freeLookCamera.name}"
                        );
                    SetupFreeLookCamera();
                }
                else
                {
                    Debug.LogWarning("[PlayerGameObject] CinemachineFreeLook을 찾을 수 없습니다!");
                }
            }
        }

        [Inject]
        public void Initialize(PlayerMovementService playerMovementService)
        {
            _playerMovementService = playerMovementService;

            // 점프 이벤트 직접 구독
            _playerMovementService.JumpInputChanged += OnJumpInputChanged;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerGameObject] PlayerMovementService 연결 및 이벤트 구독 완료: {_playerMovementService != null}"
                );
            }
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();

            // 카메라 재시도 (Awake에서 못 찾았을 경우)
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                if (_mainCamera == null)
                {
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        _mainCamera = camera.gameObject;
                        Debug.Log($"[PlayerGameObject] Start()에서 Camera.main으로 카메라 찾음: {_mainCamera.name}");
                    }
                    else
                    {
                        // 모든 카메라 검색
                        var allCameras = FindObjectsOfType<Camera>();
                        if (allCameras.Length > 0)
                        {
                            _mainCamera = allCameras[0].gameObject;
                            Debug.LogWarning($"[PlayerGameObject] 첫 번째 Camera 컴포넌트 사용: {_mainCamera.name}");
                        }
                        else
                        {
                            Debug.LogError("[PlayerGameObject] 씬에 카메라가 없습니다!");
                        }
                    }
                }
            }

            // VContainer 의존성 주입 확인 및 대체 방법 시도
            if (_playerMovementService == null)
            {
                Debug.LogWarning("[PlayerGameObject] PlayerMovementService가 주입되지 않음. MainLifetimeScope에서 직접 찾기 시도...");

                try
                {
                    var mainLifetimeScope = FindFirstObjectByType<DI.MainLifetimeScope>();
                    if (mainLifetimeScope != null && mainLifetimeScope.Container != null)
                    {
                        var playerMovementService = mainLifetimeScope.Container.Resolve<Features.Player.Services.PlayerMovementService>();
                        Initialize(playerMovementService);
                        Debug.Log("[PlayerGameObject] MainLifetimeScope에서 PlayerMovementService 찾기 성공");
                    }
                    else
                    {
                        Debug.LogError("[PlayerGameObject] MainLifetimeScope를 찾을 수 없습니다!");
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PlayerGameObject] PlayerMovementService 수동 해결 실패: {e.Message}");
                    return;
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log("[PlayerGameObject] VContainer 의존성 주입 완료. 플레이어 초기화 시작.");
            }

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            // GameObject가 파괴되었는지 체크
            if (this == null || gameObject == null)
                return;

            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
            CameraLook();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z
            );

            bool wasGrounded = Grounded;

            // GroundLayers가 설정되지 않은 경우 기본 레이어로 체크
            if (GroundLayers.value == 0)
            {
                Debug.LogWarning("[PlayerGameObject] GroundLayers가 설정되지 않음. 기본 레이어로 체크합니다.");
                Grounded = Physics.CheckSphere(
                    spherePosition,
                    GroundedRadius,
                    ~0, // 모든 레이어
                    QueryTriggerInteraction.Ignore
                );
            }
            else
            {
                Grounded = Physics.CheckSphere(
                    spherePosition,
                    GroundedRadius,
                    GroundLayers,
                    QueryTriggerInteraction.Ignore
                );
            }

            if (wasGrounded != Grounded && enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerGameObject] Grounded 상태 변화: {wasGrounded} → {Grounded}, 위치: {transform.position}, 체크 위치: {spherePosition}, 레이어: {GroundLayers.value}"
                );
            }

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void Move()
        {
            // GameObject와 Transform이 파괴되었는지 체크
            if (this == null || gameObject == null || transform == null)
                return;

            if (!canMove)
            {
                if (enableDebugLogs)
                    Debug.Log("[PlayerGameObject] canMove가 false여서 이동하지 않습니다.");
                return;
            }

            if (_playerMovementService == null)
            {
                Debug.LogError(
                    "[PlayerGameObject] Move() - PlayerMovementService가 null입니다! 플레이어 이동이 작동하지 않습니다."
                );
                return;
            }

            // 필수 컴포넌트들이 null인지 체크 - FreeLook 카메라 우선 사용
            Transform cameraTransform = null;
            if (_freeLookCamera != null)
            {
                cameraTransform = _freeLookCamera.transform;
            }
            else if (_mainCamera != null)
            {
                cameraTransform = _mainCamera.transform;
            }

            if (_controller == null || cameraTransform == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[PlayerGameObject] 필수 컴포넌트가 null입니다 - Controller: {_controller != null}, Camera: {cameraTransform != null}");
                return;
            }

            float targetSpeed = _playerMovementService.SprintInput ? SprintSpeed : MoveSpeed;

            if (_playerMovementService.MoveInput == Vector2.zero)
                targetSpeed = 0.0f;
            else if (enableDebugLogs)
                Debug.Log($"[PlayerGameObject] 이동 입력 감지: {_playerMovementService.MoveInput}, targetSpeed: {targetSpeed}");

            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x,
                0.0f,
                _controller.velocity.z
            ).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _playerMovementService.AnalogMovement
                ? _playerMovementService.MoveInput.magnitude
                : 1f;

            if (
                currentHorizontalSpeed < targetSpeed - speedOffset
                || currentHorizontalSpeed > targetSpeed + speedOffset
            )
            {
                _speed = Mathf.Lerp(
                    currentHorizontalSpeed,
                    targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate
                );

                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetSpeed,
                Time.deltaTime * SpeedChangeRate
            );

            if (_animationBlend < 0.01f)
                _animationBlend = 0f;

            Vector2 moveInput = _playerMovementService.MoveInput;
            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            if (moveInput != Vector2.zero)
            {
                _targetRotation =
                    Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                    + cameraTransform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    RotationSmoothTime
                );

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection =
                Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime)
                    + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
            );

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            canMove = enabled;
        }

        /// <summary>
        /// 플레이어 이동 허용
        /// </summary>
        public void AllowMovement(string reason = "")
        {
            if (_playerMovementService != null)
            {
                _playerMovementService.CanMove = true;
                if (enableDebugLogs)
                {
                    Debug.Log($"[PlayerGameObject] 이동 허용 - {reason}");
                }
            }
        }

        /// <summary>
        /// 플레이어 이동 제한
        /// </summary>
        public void BlockMovement(string reason = "")
        {
            if (_playerMovementService != null)
            {
                _playerMovementService.CanMove = false;
                if (enableDebugLogs)
                {
                    Debug.Log($"[PlayerGameObject] 이동 제한 - {reason}");
                }
            }
        }

        private void OnJumpInputChanged(bool jumpInput)
        {
            // 점프 입력이 True일 때만 처리 (False는 무시)
            if (jumpInput && Grounded && _jumpTimeoutDelta <= 0.0f)
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[PlayerGameObject] 점프 실행 조건 만족! Grounded: {Grounded}, JumpTimeout: {_jumpTimeoutDelta}"
                    );
                }
                PerformJump();
            }
        }

        private void PerformJump()
        {
            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            _jumpTimeoutDelta = JumpTimeout;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerGameObject] 점프 실행! 수직속도: {_verticalVelocity}");
            }

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, true);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // 점프 처리는 이제 OnJumpInputChanged 이벤트에서 처리

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void SetupFreeLookCamera()
        {
            if (_freeLookCamera == null) return;

            // FreeLook 기본 입력 비활성화
            _freeLookCamera.m_XAxis.m_InputAxisName = "";
            _freeLookCamera.m_YAxis.m_InputAxisName = "";

            // Y축 범위 설정 (위아래 시야각)
            _freeLookCamera.m_YAxis.m_MinValue = 0.2f;
            _freeLookCamera.m_YAxis.m_MaxValue = 0.7f;
            _freeLookCamera.m_YAxis.Value = 0.3f;

            // 모바일 최적화 설정
            _freeLookCamera.m_XAxis.m_MaxSpeed = 300f;
            _freeLookCamera.m_XAxis.m_AccelTime = 0.01f;
            _freeLookCamera.m_XAxis.m_DecelTime = 0.01f;

            _freeLookCamera.m_YAxis.m_MaxSpeed = 3f;
            _freeLookCamera.m_YAxis.m_AccelTime = 0.01f;
            _freeLookCamera.m_YAxis.m_DecelTime = 0.01f;

            if (enableDebugLogs)
                Debug.Log("[PlayerGameObject] FreeLook 카메라 설정 완료");
        }

        private void CameraLook()
        {
            if (_playerMovementService == null)
            {
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                    Debug.LogWarning("[PlayerGameObject] _playerMovementService is null!");
                return;
            }

            if (_freeLookCamera == null)
            {
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                    Debug.LogWarning("[PlayerGameObject] _freeLookCamera is null!");
                return;
            }

            Vector2 lookInput = _playerMovementService.LookInput; // 이미 델타값

            // 터치가 있을 때
            if (lookInput.magnitude > 0.01f)
            {
                // 감도 적용 (모바일 FPS용으로 높인 값)
                Vector2 cameraInput = lookInput * CameraSensitivity * 0.02f;
                if (InvertY)
                    cameraInput.y = -cameraInput.y;

                // 부드러운 입력을 위한 약간의 스무딩
                _currentCameraInput = Vector2.Lerp(_currentCameraInput, cameraInput, 0.8f);

                // Cinemachine Axis에 직접 값 설정
                _freeLookCamera.m_XAxis.m_InputAxisValue = _currentCameraInput.x;
                _freeLookCamera.m_YAxis.m_InputAxisValue = -_currentCameraInput.y; // Y축 반전

                if (enableDebugLogs && Time.frameCount % 20 == 0)
                {
                    Debug.Log(
                        $"[PlayerGameObject] 카메라 입력: {lookInput}, 적용값: X({_currentCameraInput.x}), Y({-_currentCameraInput.y})"
                    );
                }
            }
            else
            {
                // 터치가 끝났을 때
                _currentCameraInput = Vector2.Lerp(_currentCameraInput, Vector2.zero, 0.2f);

                // 매우 작은 값이면 완전히 0으로
                if (_currentCameraInput.magnitude < 0.001f)
                {
                    _currentCameraInput = Vector2.zero;
                }

                _freeLookCamera.m_XAxis.m_InputAxisValue = _currentCameraInput.x;
                _freeLookCamera.m_YAxis.m_InputAxisValue = -_currentCameraInput.y;
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_playerMovementService != null)
            {
                _playerMovementService.JumpInputChanged -= OnJumpInputChanged;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(
                new Vector3(
                    transform.position.x,
                    transform.position.y - GroundedOffset,
                    transform.position.z
                ),
                GroundedRadius
            );
        }
    }
}
