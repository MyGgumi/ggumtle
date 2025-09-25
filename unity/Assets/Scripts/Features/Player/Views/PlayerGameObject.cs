using Cinemachine;
using Features.Player.NetworkSources;
using Features.Player.Services;
using UnityEngine;
using VContainer;

namespace Features.Player.Views
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerGameObject : MonoBehaviour
    {
        // 서버 속도 값을 Unity 속도로 변환하는 상수
        private const float SERVER_SPEED_TO_UNITY_SPEED = 2.5f / 1000000000f; // 서버 100 = Unity 2.5

        [Header("플레이어 이동")]
        public float MoveSpeed = 2.5f;
        public float SprintSpeed = 5.335f;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("점프")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.30f;
        public float FallTimeout = 0.15f;

        [Header("땅 체크")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("모바일 카메라 설정")]
        public bool useFreeLookCamera = true;
        public float CameraSensitivity = 0.05f;
        public float CameraInputSmoothing = 0.1f;
        public bool InvertY = true;

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

        [Inject]
        private IPlayerNetworkSource _playerNetworkSource;

        [Header("카메라 설정")]
        [SerializeField]
        private CinemachineFreeLook _freeLookCamera;

        // 카메라 회전 관련
        private Vector2 _currentCameraInput;

        private bool _hasAnimator;
        private bool _isLocalPlayer;

        private void Awake()
        {
            // Local 플레이어인지 확인 - 오브젝트 이름으로 판단
            _isLocalPlayer = gameObject.name.Contains("Local");

            Debug.Log(
                $"[PlayerGameObject] Awake 호출됨 - GameObject: {gameObject.name}, IsLocal: {_isLocalPlayer}"
            );

            if (_isLocalPlayer)
            {
                // Local 플레이어만 카메라 설정
                if (_freeLookCamera == null)
                {
                    _freeLookCamera = FindFirstObjectByType<CinemachineFreeLook>();
                    if (_freeLookCamera == null)
                    {
                        Debug.LogError(
                            "[PlayerGameObject] FreeLook 카메라가 설정되지 않았고 씬에서도 찾을 수 없습니다!"
                        );
                        return;
                    }
                }

                if (enableDebugLogs)
                    Debug.Log(
                        $"[PlayerGameObject] Local 플레이어 - FreeLook 카메라 연결됨: {_freeLookCamera.name}"
                    );

                SetupFreeLookCamera();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log(
                        $"[PlayerGameObject] Remote 플레이어 - 카메라 설정 건너뜀: {gameObject.name}"
                    );
            }
        }

        [Inject]
        public void Initialize(PlayerMovementService playerMovementService)
        {
            Debug.Log(
                $"[PlayerGameObject] Initialize 호출됨 - GameObject: {gameObject.name}, MovementService: {playerMovementService != null}"
            );

            _playerMovementService = playerMovementService;

            // 점프 이벤트 직접 구독
            _playerMovementService.JumpInputChanged += OnJumpInputChanged;

            // 네트워크 전송 시작
            if (_playerNetworkSource != null)
            {
                _playerNetworkSource.StartNetworkTransmission();
                Debug.Log(
                    $"[PlayerGameObject] 네트워크 전송 시작 - NetworkSource: {_playerNetworkSource.GetType().Name}"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[PlayerGameObject] PlayerNetworkSource가 null - GameObject: {gameObject.name}"
                );
            }

            Debug.Log(
                $"[PlayerGameObject] PlayerMovementService 연결 및 이벤트 구독 완료: {_playerMovementService != null}"
            );
        }

        private void Start()
        {
            Debug.Log($"[PlayerGameObject] Start 호출됨 - GameObject: {gameObject.name}");

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();

            // VContainer 의존성 주입 확인 및 대체 방법 시도
            if (_playerMovementService == null)
            {
                Debug.LogWarning(
                    $"[PlayerGameObject] PlayerMovementService가 주입되지 않음 - GameObject: {gameObject.name}. MainLifetimeScope에서 직접 찾기 시도..."
                );

                try
                {
                    var mainLifetimeScope = FindFirstObjectByType<DI.MainLifetimeScope>();
                    if (mainLifetimeScope != null && mainLifetimeScope.Container != null)
                    {
                        var playerMovementService =
                            mainLifetimeScope.Container.Resolve<Features.Player.Services.PlayerMovementService>();

                        // PlayerNetworkSource도 함께 주입받기 시도
                        try
                        {
                            _playerNetworkSource =
                                mainLifetimeScope.Container.Resolve<IPlayerNetworkSource>();
                            Debug.Log(
                                $"[PlayerGameObject] PlayerNetworkSource 수동 주입 성공 - GameObject: {gameObject.name}"
                            );
                        }
                        catch (VContainer.VContainerException)
                        {
                            Debug.LogWarning(
                                $"[PlayerGameObject] PlayerNetworkSource가 DI 컨테이너에 등록되지 않음 - GameObject: {gameObject.name}"
                            );
                        }

                        Initialize(playerMovementService);
                        Debug.Log(
                            $"[PlayerGameObject] MainLifetimeScope에서 PlayerMovementService 찾기 성공 - GameObject: {gameObject.name}"
                        );
                    }
                    else
                    {
                        Debug.LogError(
                            $"[PlayerGameObject] MainLifetimeScope를 찾을 수 없습니다! - GameObject: {gameObject.name}"
                        );
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[PlayerGameObject] PlayerMovementService 수동 해결 실패 - GameObject: {gameObject.name}: {e.Message}"
                    );
                    return;
                }
            }

            Debug.Log(
                $"[PlayerGameObject] VContainer 의존성 주입 완료. 플레이어 초기화 시작 - GameObject: {gameObject.name}"
            );

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

            // 위치 변화 추적 (5초마다)
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log(
                    $"[PlayerGameObject] 현재 위치: {transform.position}, GameObject: {gameObject.name}, Grounded: {Grounded}"
                );
            }

            JumpAndGravity();
            GroundedCheck();
            Move();
            CameraLook();
        }

        /// <summary>
        /// 네트워크 전송을 위한 FixedUpdate (일정한 주기 보장)
        /// </summary>
        private void FixedUpdate()
        {
            // FixedUpdate 호출 디버그 (5초마다)
            if (Time.fixedTime % 5f < Time.fixedDeltaTime)
            {
                Debug.Log(
                    $"[PlayerGameObject] FixedUpdate 호출됨 - GameObject: {gameObject.name}, NetworkSource: {_playerNetworkSource != null}"
                );
            }

            // 네트워크로 이동 데이터 전송 (일정한 주기)
            SendMovementDataToNetworkFixed();
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
                Debug.LogWarning(
                    "[PlayerGameObject] GroundLayers가 설정되지 않음. 기본 레이어로 체크합니다."
                );
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

            if (_controller == null || _freeLookCamera == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning(
                        $"[PlayerGameObject] 필수 컴포넌트가 null입니다 - Controller: {_controller != null}, FreeLookCamera: {_freeLookCamera != null}"
                    );
                return;
            }

            float targetSpeed = _playerMovementService.SprintInput ? SprintSpeed : MoveSpeed;

            if (_playerMovementService.MoveInput == Vector2.zero)
                targetSpeed = 0.0f;
            else if (enableDebugLogs)
                Debug.Log(
                    $"[PlayerGameObject] 이동 입력 감지: {_playerMovementService.MoveInput}, targetSpeed: {targetSpeed}"
                );

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

            Vector3 targetDirection = Vector3.zero;

            if (moveInput != Vector2.zero)
            {
                // 카메라 기준 상대적 이동 방향 계산
                // FreeLook 카메라의 현재 회전값을 기준으로 함
                float cameraYRotation = _freeLookCamera.m_XAxis.Value;

                // 카메라가 보고 있는 방향을 기준으로 forward와 right 벡터 계산
                Vector3 cameraForward = Quaternion.Euler(0, cameraYRotation, 0) * Vector3.forward;
                Vector3 cameraRight = Quaternion.Euler(0, cameraYRotation, 0) * Vector3.right;

                // 입력에 따른 이동 방향 계산 (카메라 기준 상대적)
                targetDirection = (
                    cameraForward * moveInput.y + cameraRight * moveInput.x
                ).normalized;

                // 캐릭터가 이동하는 방향으로 회전
                if (targetDirection != Vector3.zero)
                {
                    _targetRotation =
                        Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

                    float rotation = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y,
                        _targetRotation,
                        ref _rotationVelocity,
                        RotationSmoothTime
                    );

                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }

                if (enableDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log(
                        $"[PlayerGameObject] 카메라 Y회전: {cameraYRotation:F1}°, 이동방향: {targetDirection}, 입력: {moveInput}"
                    );
                }
            }

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

        /// <summary>
        /// FixedUpdate에서 호출되는 네트워크 전송 (일정한 주기 보장)
        /// </summary>
        private async void SendMovementDataToNetworkFixed()
        {
            // 메서드 호출 확인 (5초마다)
            if (Time.fixedTime % 5f < Time.fixedDeltaTime)
            {
                Debug.Log(
                    $"[PlayerGameObject] SendMovementDataToNetworkFixed 호출됨 - NetworkSource: {_playerNetworkSource != null}"
                );
            }

            if (_playerNetworkSource == null)
            {
                if (Time.frameCount % 300 == 0) // 5초마다
                {
                    Debug.LogWarning(
                        $"[PlayerGameObject] PlayerNetworkSource가 null입니다! GameObject: {gameObject.name}"
                    );
                }
                return;
            }

            try
            {
                // 현재 상태 정보 수집
                Vector3 position = transform.position;
                Vector2 moveInput = _playerMovementService?.MoveInput ?? Vector2.zero;
                bool isMoving = moveInput != Vector2.zero && _speed > 0.1f;
                float currentSpeed = _speed;

                // 이동 방향 계산 (Move() 메서드와 동일한 로직)
                Vector3 direction = Vector3.zero;
                if (moveInput != Vector2.zero && _freeLookCamera != null)
                {
                    // 카메라 기준 상대적 이동 방향 계산
                    float cameraYRotation = _freeLookCamera.m_XAxis.Value;
                    Vector3 cameraForward =
                        Quaternion.Euler(0, cameraYRotation, 0) * Vector3.forward;
                    Vector3 cameraRight = Quaternion.Euler(0, cameraYRotation, 0) * Vector3.right;

                    // 입력에 따른 이동 방향 계산 (카메라 기준 상대적)
                    direction = (
                        cameraForward * moveInput.y + cameraRight * moveInput.x
                    ).normalized;
                }
                else if (isMoving)
                {
                    // 카메라 정보가 없을 때는 transform의 forward 사용
                    direction = transform.forward;
                }

                // 5초마다 상세 디버깅
                if (Time.fixedTime % 5f < Time.fixedDeltaTime)
                {
                    Debug.Log(
                        $"[PlayerGameObject] 전송 데이터 - Pos: {position}, Direction: {direction}, MoveInput: {moveInput}, Speed: {_speed:F2}, IsMoving: {isMoving}"
                    );
                }

                // FixedUpdate 주기에 맞춰 네트워크 전송
                var success = await _playerNetworkSource.SendPlayerMoveAsync(
                    position,
                    direction,
                    isMoving,
                    currentSpeed
                );

                // 전송 성공 시 로그 (3초마다)
                if (success && enableDebugLogs && Time.frameCount % 150 == 0)
                {
                    Debug.Log(
                        $"[PlayerGameObject] 네트워크 전송 성공: Pos={position}, Dir={direction}, Moving={isMoving}, Speed={currentSpeed:F2}"
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerGameObject] 네트워크 이동 데이터 전송 예외: {ex.Message}");
            }
        }

        /// <summary>
        /// 이동 데이터를 네트워크로 전송 (레거시 - 필요시 사용)
        /// </summary>
        private async void SendMovementDataToNetwork()
        {
            if (_playerNetworkSource == null)
                return;

            try
            {
                // 현재 상태 정보 수집
                Vector3 position = transform.position;
                Vector3 rotation = transform.eulerAngles;
                bool isMoving = _playerMovementService?.MoveInput != Vector2.zero && _speed > 0.1f;
                float currentSpeed = _speed;

                // 네트워크로 전송 (비동기)
                await _playerNetworkSource.SendPlayerMoveAsync(
                    position,
                    rotation,
                    isMoving,
                    currentSpeed
                );
            }
            catch (System.Exception ex)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning(
                        $"[PlayerGameObject] 네트워크 이동 데이터 전송 실패: {ex.Message}"
                    );
                }
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

        /// <summary>
        /// 서버 속도를 Unity 속도로 변환하여 설정
        /// </summary>
        public void SetServerSpeed(int serverSpeed)
        {
            // Unity 속도로 변환
            MoveSpeed = serverSpeed * SERVER_SPEED_TO_UNITY_SPEED;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerGameObject] 서버 속도 설정: 서버={serverSpeed} → Unity={MoveSpeed}"
                );
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
            if (_freeLookCamera == null)
                return;

            // **핵심 수정: Follow와 LookAt 타겟 설정**
            _freeLookCamera.Follow = transform;
            _freeLookCamera.LookAt = transform;

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

            Debug.Log(
                $"[PlayerGameObject] FreeLook 카메라 설정 완료 - Follow: {_freeLookCamera.Follow?.name}, LookAt: {_freeLookCamera.LookAt?.name}"
            );
        }

        private void CameraLook()
        {
            if (_playerMovementService == null || _freeLookCamera == null)
                return;

            Vector2 lookInput = _playerMovementService.LookInput; // 이미 델타값

            // 터치가 있을 때
            if (lookInput.magnitude > 0.01f)
            {
                // 감도 적용
                Vector2 cameraInput = lookInput * CameraSensitivity;
                if (InvertY)
                    cameraInput.y = -cameraInput.y;

                // Cinemachine Axis에 직접 값 설정 (보간 제거)
                _freeLookCamera.m_XAxis.m_InputAxisValue = cameraInput.x;
                _freeLookCamera.m_YAxis.m_InputAxisValue = -cameraInput.y;

                if (enableDebugLogs && Time.frameCount % 20 == 0)
                {
                    Debug.Log(
                        $"[PlayerGameObject] 카메라 입력: {lookInput}, 적용값: X({cameraInput.x}), Y({-cameraInput.y}), 현재 X축값: {_freeLookCamera.m_XAxis.Value:F1}"
                    );
                }
            }
            else
            {
                // 터치가 끝났을 때 즉시 0으로 (보간 제거)
                _freeLookCamera.m_XAxis.m_InputAxisValue = 0f;
                _freeLookCamera.m_YAxis.m_InputAxisValue = 0f;
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_playerMovementService != null)
            {
                _playerMovementService.JumpInputChanged -= OnJumpInputChanged;
            }

            // 네트워크 전송 중지
            if (_playerNetworkSource != null)
            {
                _playerNetworkSource.StopNetworkTransmission();
                if (enableDebugLogs)
                {
                    Debug.Log("[PlayerGameObject] 네트워크 전송 중지");
                }
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
