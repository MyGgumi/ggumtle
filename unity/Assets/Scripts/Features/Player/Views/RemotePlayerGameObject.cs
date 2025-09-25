using Networks.Rooms.Domains;
using UnityEngine;
using VContainer;

namespace Features.Player.Views
{
    /// <summary>
    /// 원격 플레이어 GameObject 컨트롤러
    /// 네트워크를 통해 받은 데이터를 기반으로 애니메이션과 이동 처리
    /// 입력 처리나 카메라 설정은 제외
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class RemotePlayerGameObject : MonoBehaviour
    {
        // 서버 속도 값을 Unity 속도로 변환하는 상수
        private const float SERVER_SPEED_TO_UNITY_SPEED = 2.5f / 100f; // 서버 100 = Unity 2.5
        [Header("플레이어 이동")]
        public float MoveSpeed = 2.5f;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("점프")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;

        [Header("땅 체크")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Debug Settings")]
        public bool enableDebugLogs = true;

        // 플레이어 정보
        [Header("Player Info")]
        public long PlayerId;
        public bool IsMongging;
        public long ClassId;

        // 애니메이션 관련
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private bool _hasAnimator;

        // 네트워크 동기화용 데이터
        private Vector3 _networkPosition = Vector3.zero;
        private Vector3 _networkRotation = Vector3.zero;
        private bool _networkIsMoving;
        private bool _networkIsJumping;
        private float _networkSpeed;

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();

            AssignAnimationIDs();

            if (enableDebugLogs)
            {
                Debug.Log($"[RemotePlayerGameObject] 원격 플레이어 초기화 완료: ID={PlayerId}");
            }
        }

        private void Update()
        {
            // GameObject가 파괴되었는지 체크
            if (this == null || gameObject == null)
                return;

            _hasAnimator = TryGetComponent(out _animator);

            GroundedCheck();
            ProcessNetworkMovement();
            ApplyGravity();
        }

        /// <summary>
        /// PlayerPacket 데이터로 플레이어 정보 초기화
        /// </summary>
        public void InitializeFromPacket(PlayerPacket packet)
        {
            PlayerId = packet.Id;
            IsMongging = packet.IsMongging;
            ClassId = packet.ClassId;

            // 속도 설정 (서버 100 → Unity 2.5)
            MoveSpeed = packet.MoveSpeed * SERVER_SPEED_TO_UNITY_SPEED;

            // GameObject 이름 설정
            gameObject.name = $"RemotePlayer_{packet.Id}_{(packet.IsMongging ? "Mongging" : "Mongdung")}";

            if (enableDebugLogs)
            {
                Debug.Log($"[RemotePlayerGameObject] 플레이어 패킷으로 초기화: ID={PlayerId}, 타입={GetPlayerTypeString()}, 서버속도={packet.MoveSpeed} → Unity속도={MoveSpeed}");
            }
        }


        /// <summary>
        /// 네트워크에서 받은 위치/회전 데이터 설정
        /// </summary>
        public void UpdateNetworkTransform(Vector3 position, Vector3 rotation, bool isMoving, float speed)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[RemotePlayerGameObject] UpdateNetworkTransform 호출됨 - ID={PlayerId}, Position: {position}, Moving: {isMoving}");
            }

            _networkPosition = position;
            _networkRotation = rotation;
            _networkIsMoving = isMoving;
            _networkSpeed = speed;

            // 애니메이션 상태 업데이트
            UpdateAnimationState(isMoving);

            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[RemotePlayerGameObject] 네트워크 위치 업데이트: {position}, 이동중: {isMoving}");
            }
        }

        /// <summary>
        /// 네트워크 데이터를 기반으로 애니메이션 상태 업데이트
        /// </summary>
        private void UpdateAnimationState(bool isMoving)
        {
            // AnimationEventHandler에 애니메이션 상태 전달
            var mongdungAnimationHandler = GetComponent<MongdungAnimationEventHandler>();
            if (mongdungAnimationHandler != null)
            {
                mongdungAnimationHandler.SetMovingState(isMoving);
            }

            var monggingAnimationHandler = GetComponent<MonggingAnimationEventHandler>();
            if (monggingAnimationHandler != null)
            {
                monggingAnimationHandler.SetMovingState(isMoving);
            }

            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[RemotePlayerGameObject] 애니메이션 상태 업데이트: IsMoving={isMoving}");
            }
        }

        /// <summary>
        /// 네트워크에서 받은 점프 상태 설정
        /// </summary>
        public void UpdateNetworkJump(bool isJumping)
        {
            _networkIsJumping = isJumping;

            if (isJumping && Grounded && _hasAnimator)
            {
                _animator.SetBool(_animIDJump, true);
                if (enableDebugLogs)
                {
                    Debug.Log($"[RemotePlayerGameObject] 네트워크 점프 실행: ID={PlayerId}");
                }
            }
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

            if (enableDebugLogs && Time.frameCount % 120 == 0) // 2초마다 로그
            {
                Debug.Log($"[RemotePlayerGameObject] GroundCheck - Position: {transform.position}, SpherePos: {spherePosition}, GroundLayers: {GroundLayers.value}");
            }

            if (GroundLayers.value == 0)
            {
                Grounded = Physics.CheckSphere(
                    spherePosition,
                    GroundedRadius,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                if (enableDebugLogs && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[RemotePlayerGameObject] Using all layers (~0), Grounded: {Grounded}");
                }
            }
            else
            {
                Grounded = Physics.CheckSphere(
                    spherePosition,
                    GroundedRadius,
                    GroundLayers,
                    QueryTriggerInteraction.Ignore
                );

                if (enableDebugLogs && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[RemotePlayerGameObject] Using GroundLayers: {GroundLayers.value}, Grounded: {Grounded}");
                }
            }

            if (wasGrounded != Grounded && enableDebugLogs)
            {
                Debug.Log($"[RemotePlayerGameObject] Grounded 상태 변화: {wasGrounded} → {Grounded}");
            }

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void ProcessNetworkMovement()
        {
            if (_controller == null)
                return;

            // 네트워크 위치로 부드럽게 이동
            Vector3 targetPosition = _networkPosition;
            Vector3 currentPosition = transform.position;

            // 거리 차이가 클 때는 즉시 이동 (텔레포트)
            float distance = Vector3.Distance(currentPosition, targetPosition);
            if (distance > 5.0f)
            {
                transform.position = targetPosition;
                if (enableDebugLogs)
                {
                    Debug.Log($"[RemotePlayerGameObject] 텔레포트: {currentPosition} → {targetPosition}");
                }
                return;
            }

            // 부드러운 이동
            if (_networkIsMoving && distance > 0.1f)
            {
                Vector3 moveDirection = (targetPosition - currentPosition).normalized;
                float currentSpeed = _networkSpeed > 0 ? _networkSpeed : MoveSpeed;

                _controller.Move(moveDirection * (currentSpeed * Time.deltaTime));

                // 회전 처리
                if (moveDirection != Vector3.zero)
                {
                    _targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

                    float rotation = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y,
                        _targetRotation,
                        ref _rotationVelocity,
                        RotationSmoothTime
                    );

                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }

                // 애니메이션 속도 업데이트
                float targetSpeed = currentSpeed;
                _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            }
            else
            {
                // 정지 상태
                _animationBlend = Mathf.Lerp(_animationBlend, 0f, Time.deltaTime * SpeedChangeRate);
            }

            // 애니메이터 업데이트
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, _networkIsMoving ? 1f : 0f);
            }
        }

        private void ApplyGravity()
        {
            if (Grounded)
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // 네트워크 점프 처리
                if (_networkIsJumping)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }
            }
            else
            {
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, _verticalVelocity < -0.1f);
                }
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            // 수직 이동 적용
            _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        /// <summary>
        /// 플레이어 타입 문자열 반환
        /// </summary>
        public string GetPlayerTypeString()
        {
            if (!IsMongging) return "Mongdung";

            return ClassId switch
            {
                0 => "Mongging_Tanker",   // 서버에서 0부터 시작
                1 => "Mongging_Healer",
                2 => "Mongging_Worker",
                _ => "Mongging_Unknown"
            };
        }

        /// <summary>
        /// 애니메이션 트리거 설정 (외부에서 호출)
        /// </summary>
        public void TriggerAnimation(string triggerName)
        {
            if (_hasAnimator && !string.IsNullOrEmpty(triggerName))
            {
                _animator.SetTrigger(triggerName);
                if (enableDebugLogs)
                {
                    Debug.Log($"[RemotePlayerGameObject] 애니메이션 트리거: {triggerName}");
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