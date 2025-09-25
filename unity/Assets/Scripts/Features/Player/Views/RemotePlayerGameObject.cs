using System.Collections.Generic;
using Features.Player.NetworkSources;
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
        private const float SERVER_SPEED_TO_UNITY_SPEED = 2.5f / 1000000000f; // 서버 100 = Unity 2.5

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
        private Vector3 _networkDirection = Vector3.zero; // 이동 방향 (rotation에서 direction으로 변경)
        private bool _networkIsMoving;
        private bool _networkIsJumping;
        private float _networkSpeed;

        // 보간 상태 추적
        private bool _isInterpolating = false;
        private float _lastNetworkUpdateTime;
        private Vector3 _lastValidDirection = Vector3.zero; // 더미값 필터링을 위한 마지막 유효 방향

        // 간단한 보간 시스템용 데이터
        private PlayerNetworkSettings _networkSettings = new PlayerNetworkSettings();

        // 간단한 보간용 변수들
        private Vector3 _targetPosition;
        private Vector3 _previousPosition;
        private bool _useInterpolation = true;

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();

            AssignAnimationIDs();

            // 보간 시스템 초기화
            InitializeInterpolationSystem();

            if (enableDebugLogs)
            {
                Debug.Log($"[RemotePlayerGameObject] 원격 플레이어 초기화 완료: ID={PlayerId}");
            }
        }

        /// <summary>
        /// 간단한 보간 시스템 초기화
        /// </summary>
        private void InitializeInterpolationSystem()
        {
            _networkSettings.ValidateSettings();

            // InitializeFromPacket에서 이미 위치가 설정되었다면 그대로 사용
            if (_targetPosition == Vector3.zero)
            {
                _targetPosition = transform.position;
                _previousPosition = transform.position;
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[RemotePlayerGameObject] 간단한 보간 시스템 초기화: SendRate={_networkSettings.networkSendRate}Hz, 위치={_targetPosition}"
                );
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

            // 초기 위치 설정 (서버에서 온 정확한 위치 사용)
            Vector3 initialPosition = new Vector3(
                packet.Position.X,
                packet.Position.Y,
                packet.Position.Z
            );
            transform.position = initialPosition;
            _targetPosition = initialPosition;
            _networkPosition = initialPosition;
            _previousPosition = initialPosition;

            // GameObject 이름 설정
            gameObject.name =
                $"RemotePlayer_{packet.Id}_{(packet.IsMongging ? "Mongging" : "Mongdung")}";

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[RemotePlayerGameObject] 플레이어 패킷으로 초기화: ID={PlayerId}, 타입={GetPlayerTypeString()}, 서버속도={packet.MoveSpeed} → Unity속도={MoveSpeed}, 초기위치={initialPosition}"
                );
            }
        }

        /// <summary>
        /// 네트워크에서 받은 위치/방향 데이터 설정 (간단한 보간 적용)
        /// </summary>
        public void UpdateNetworkTransform(
            Vector3 position,
            Vector3 direction,
            bool isMoving,
            float speed
        )
        {
            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[REMOTE_PLAYER] ID={PlayerId}: 📡 네트워크 데이터 수신"
                        + $"\n새 위치: {position}"
                        + $"\n이전 위치: {_networkPosition}"
                        + $"\n실제 위치: {transform.position}"
                        + $"\n방향: {direction}, 움직임: {isMoving}, 속도: {speed:F2}"
                );
            }

            // 중복 명령 필터링 강화 (임계값 증가)
            float positionDifference = Vector3.Distance(_networkPosition, position);
            if (positionDifference < 0.1f)
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[REMOTE_PLAYER] ID={PlayerId}: 🚫 중복 명령 무시 (강화) - 거리={positionDifference:F3}m"
                    );
                }
                return;
            }

            // 특정 문제 위치 패턴 감지 및 필터링
            Vector3 problemPos1 = new Vector3(45.00f, 5.00f, 0.00f);
            Vector3 problemPos2 = new Vector3(49.16f, 3.70f, -1.77f);

            if (Vector3.Distance(position, problemPos1) < 0.1f || Vector3.Distance(position, problemPos2) < 0.1f)
            {
                Debug.LogWarning(
                    $"[REMOTE_PLAYER] ID={PlayerId}: ⚠️ 문제 위치 패턴 감지됨 - 무시"
                        + $"\n수신 위치: {position}"
                        + $"\n문제 패턴1: {problemPos1}"
                        + $"\n문제 패턴2: {problemPos2}"
                );
                return;
            }

            // 현재 실제 위치에서 새 목표까지의 거리 계산
            float distanceFromCurrent = Vector3.Distance(transform.position, position);

            // 너무 가까우면 보간 생략하고 즉시 설정
            if (distanceFromCurrent < 0.1f)
            {
                transform.position = position;
                _targetPosition = position;
                _networkPosition = position;
                _isInterpolating = false;

                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[REMOTE_PLAYER] ID={PlayerId}: ⚡ 즉시 설정 - 거리={distanceFromCurrent:F3}m"
                    );
                }
                return;
            }

            // 보간 중단 처리 (새로운 명령이 들어오면 현재 위치에서 다시 시작)
            if (_isInterpolating)
            {
                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[REMOTE_PLAYER] ID={PlayerId}: 🔄 보간 중단 후 재시작 - 현재위치={transform.position}"
                    );
                }
            }

            // 더미 방향값 필터링 (서버에서 보내는 의미없는 값 제거)
            Vector3 filteredDirection = direction;
            if (
                Mathf.Approximately(direction.x, -0.01f)
                && Mathf.Approximately(direction.y, -0.01f)
                && Mathf.Approximately(direction.z, -0.01f)
            )
            {
                // 더미값 (-0.01, -0.01, -0.01)이면 이전 유효한 방향 사용
                filteredDirection = _lastValidDirection;

                if (enableDebugLogs && Time.frameCount % 300 == 0) // 5초마다
                {
                    Debug.Log(
                        $"[REMOTE_PLAYER] ID={PlayerId}: 🚫 더미 방향값 필터링됨 - 이전 유효값 사용: {_lastValidDirection}"
                    );
                }
            }
            else if (direction != Vector3.zero)
            {
                // 유효한 방향값이면 저장
                _lastValidDirection = direction;
            }

            // 네트워크 데이터 업데이트 (이전 네트워크 위치를 저장)
            _previousPosition = _networkPosition; // ✅ 수정: 이전 네트워크 위치를 저장
            _networkPosition = position;
            _networkDirection = filteredDirection; // 필터링된 방향값 사용
            _networkIsMoving = isMoving;
            _networkSpeed = speed;
            _lastNetworkUpdateTime = Time.time;

            // 텔레포트 거리 체크 (distanceFromCurrent는 이미 계산됨)
            if (distanceFromCurrent > _networkSettings.teleportThreshold)
            {
                // 즉시 이동 (텔레포트)
                transform.position = position;
                _targetPosition = position;
                _useInterpolation = false;
                _isInterpolating = false;

                Debug.LogWarning(
                    $"[REMOTE_PLAYER] ID={PlayerId}: 🚀 텔레포트 발생! 거리={distanceFromCurrent:F3}m (임계값={_networkSettings.teleportThreshold}m)"
                        + $"\n이전 네트워크: {_previousPosition}"
                        + $"\n새 네트워크: {position}"
                        + $"\n현재 실제: {transform.position}"
                        + $"\n텔레포트 후: {position}"
                );
            }
            else
            {
                // 보간 시작
                _targetPosition = position;
                _useInterpolation = true;
                _isInterpolating = true;

                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[REMOTE_PLAYER] ID={PlayerId}: 🎬 보간 시작"
                            + $"\n이전 네트워크: {_previousPosition}"
                            + $"\n새 네트워크: {position}"
                            + $"\n현재 실제: {transform.position}"
                            + $"\n네트워크 거리: {Vector3.Distance(_previousPosition, position):F3}m"
                            + $"\n실제 거리: {distanceFromCurrent:F3}m"
                    );
                }
            }

            // 애니메이션 상태 업데이트
            UpdateAnimationState(isMoving);

            // 이동 패턴 분석 (매 3초마다)
            if (enableDebugLogs && Time.frameCount % 180 == 0)
            {
                Vector3 networkMovement = position - _previousPosition;
                float networkDistance = networkMovement.magnitude;
                Debug.Log(
                    $"[REMOTE_PLAYER] ID={PlayerId}: 📊 이동 패턴 분석"
                        + $"\n네트워크 이동: {_previousPosition} → {position} (거리: {networkDistance:F3})"
                        + $"\n현재 실제: {transform.position}"
                        + $"\n실제→목표: {distanceFromCurrent:F3}m"
                        + $"\n보간상태: {_isInterpolating}"
                        + $"\n이동상태: {isMoving}"
                        + $"\n속도: {speed:F2}"
                );
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
                Debug.Log(
                    $"[RemotePlayerGameObject] 애니메이션 상태 업데이트: IsMoving={isMoving}"
                );
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
                Debug.Log(
                    $"[RemotePlayerGameObject] GroundCheck - Position: {transform.position}, SpherePos: {spherePosition}, GroundLayers: {GroundLayers.value}"
                );
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
                    Debug.Log(
                        $"[RemotePlayerGameObject] Using all layers (~0), Grounded: {Grounded}"
                    );
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
                    Debug.Log(
                        $"[RemotePlayerGameObject] Using GroundLayers: {GroundLayers.value}, Grounded: {Grounded}"
                    );
                }
            }

            if (wasGrounded != Grounded && enableDebugLogs)
            {
                Debug.Log(
                    $"[RemotePlayerGameObject] Grounded 상태 변화: {wasGrounded} → {Grounded}"
                );
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

            if (_useInterpolation)
            {
                ProcessInterpolatedMovement();
            }

            // 애니메이터 업데이트
            UpdateAnimatorParameters();
        }

        /// <summary>
        /// 간단한 보간된 움직임 처리
        /// </summary>
        private void ProcessInterpolatedMovement()
        {
            // 네트워크 위치 기준으로 보간 계산
            float distanceToTarget = Vector3.Distance(_previousPosition, _targetPosition);

            // 보간 진행률 계산 (0~1)
            float lerpSpeed = _networkSettings.interpolationSpeed;
            float lerpProgress = Time.deltaTime * lerpSpeed;

            // 이전 네트워크 위치에서 새 네트워크 위치로 보간
            Vector3 newPosition = Vector3.Lerp(_previousPosition, _targetPosition, lerpProgress);

            // CharacterController를 통한 이동
            Vector3 currentPosition = transform.position;
            Vector3 moveVector = newPosition - currentPosition;
            if (moveVector.magnitude > 0.001f)
            {
                _controller.Move(moveVector);

                // 프레임별 이동 분석 (매 3초마다)
                if (enableDebugLogs && Time.frameCount % 180 == 0)
                {
                    Debug.Log(
                        $"[REMOTE_PLAYER] ID={PlayerId}: 🎭 보간 처리중"
                            + $"\n이전 네트워크: {_previousPosition}"
                            + $"\n목표 네트워크: {_targetPosition}"
                            + $"\n현재 실제: {currentPosition}"
                            + $"\n새 위치: {newPosition}"
                            + $"\n이동벡터: {moveVector} (크기: {moveVector.magnitude:F4})"
                            + $"\n목표거리: {distanceToTarget:F4}m"
                            + $"\n보간진행: {lerpProgress:F3}"
                    );
                }
            }

            // 네트워크 방향 기반 회전 처리
            if (_networkIsMoving && _networkDirection != Vector3.zero)
            {
                // 네트워크에서 받은 방향 정보 사용 (Y축 무시)
                Vector3 horizontalDirection = new Vector3(
                    _networkDirection.x,
                    0,
                    _networkDirection.z
                ).normalized;
                if (horizontalDirection.magnitude > 0.1f)
                {
                    float previousRotation = transform.eulerAngles.y;
                    _targetRotation =
                        Mathf.Atan2(horizontalDirection.x, horizontalDirection.z) * Mathf.Rad2Deg;

                    float rotation = Mathf.SmoothDampAngle(
                        previousRotation,
                        _targetRotation,
                        ref _rotationVelocity,
                        RotationSmoothTime
                    );

                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                    // 회전 디버그 로그 (5초마다)
                    if (enableDebugLogs && Time.frameCount % 300 == 0)
                    {
                        float rotationDelta = Mathf.DeltaAngle(previousRotation, rotation);
                        Debug.Log(
                            $"[REMOTE_PLAYER] ID={PlayerId}: 🔄 회전 처리"
                                + $"\n네트워크방향: {_networkDirection}"
                                + $"\n수평방향: {horizontalDirection}"
                                + $"\n이전회전: {previousRotation:F1}°"
                                + $"\n목표회전: {_targetRotation:F1}°"
                                + $"\n현재회전: {rotation:F1}°"
                                + $"\n변화량: {rotationDelta:F1}°"
                        );
                    }
                }
            }

            // 보간 완료 체크 및 강제 스냅 (네트워크 위치 기준)
            float currentToTargetDistance = Vector3.Distance(transform.position, _targetPosition);
            if (currentToTargetDistance < 0.05f || distanceToTarget < 0.01f)
            {
                // 목표에 거의 도달 - 정확히 목표로 스냅
                transform.position = _targetPosition;
                _isInterpolating = false;

                if (enableDebugLogs)
                {
                    Debug.Log(
                        $"[REMOTE_PLAYER] ID={PlayerId}: 🎯 보간 완료 및 스냅 - 네트워크거리: {distanceToTarget:F6}m, 실제거리: {currentToTargetDistance:F6}m"
                    );
                }
            }
            else if (distanceToTarget > 3.0f)
            {
                // 네트워크 거리가 너무 멀어짐 - 새로운 데이터가 필요함을 의미
                Debug.LogWarning(
                    $"[REMOTE_PLAYER] ID={PlayerId}: ⚠️ 네트워크 거리 과다! 거리={distanceToTarget:F3}m"
                        + $"\n이전 네트워크: {_previousPosition}"
                        + $"\n목표 네트워크: {_targetPosition}"
                        + $"\n현재 실제: {transform.position}"
                        + $"\n보간 상태: {_isInterpolating}"
                );
            }
        }

        /// <summary>
        /// 애니메이터 파라미터 업데이트 (간단화)
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            if (!_hasAnimator)
                return;

            // 간단한 이동 상태 기반 애니메이션
            float targetAnimationSpeed = _networkIsMoving ? _networkSpeed : 0f;

            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetAnimationSpeed,
                Time.deltaTime * SpeedChangeRate
            );

            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, _networkIsMoving ? 1f : 0f);
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
            if (!IsMongging)
                return "Mongdung";

            return ClassId switch
            {
                0 => "Mongging_Tanker", // 서버에서 0부터 시작
                1 => "Mongging_Healer",
                2 => "Mongging_Worker",
                _ => "Mongging_Unknown",
            };
        }

        /// <summary>
        /// 네트워크 설정 업데이트
        /// </summary>
        public void UpdateNetworkSettings(PlayerNetworkSettings settings)
        {
            if (settings == null)
            {
                Debug.LogWarning(
                    "[RemotePlayerGameObject] 네트워크 설정이 null입니다. 기본 설정을 사용합니다."
                );
                return;
            }

            _networkSettings = settings;
            _networkSettings.ValidateSettings();

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[RemotePlayerGameObject] 네트워크 설정 업데이트: SendRate={_networkSettings.networkSendRate}Hz, 보간속도={_networkSettings.interpolationSpeed}"
                );
            }
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
