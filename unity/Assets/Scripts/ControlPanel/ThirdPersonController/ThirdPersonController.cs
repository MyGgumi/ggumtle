using MVVM.Movement;
using UnityEngine;

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
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
        public bool useFreeLookCamera = true; // FreeLook 카메라 사용 여부

        [Header("상호작용 설정")]
        public bool canMove = true;

        // 내부 변수들
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // 애니메이션 해시
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private PlayerMovementViewModel _movementViewModel;
        private GameObject _mainCamera;

        private bool _hasAnimator;

        private void Awake()
        {
            // 메인 카메라 참조 (플레이어 회전에 필요)
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();

            // PlayerMovementViewModel 싱글톤 사용
            _movementViewModel = MVVM.Movement.PlayerMovementViewModel.Instance;

            if (_movementViewModel != null)
            {
                Debug.Log(
                    $"[ThirdPersonController] PlayerMovementViewModel 싱글톤 인스턴스 연결 완료!"
                );

                // 싱글톤이 플레이어 GameObject에 있지 않으면 옮기기
                if (
                    _movementViewModel.gameObject != gameObject
                    && !_movementViewModel.transform.IsChildOf(transform)
                )
                {
                    Debug.Log(
                        $"[ThirdPersonController] PlayerMovementViewModel이 다른 GameObject에 있음: {_movementViewModel.gameObject.name}"
                    );
                }
            }
            else
            {
                Debug.LogError(
                    "[ThirdPersonController] PlayerMovementViewModel 싱글톤 인스턴스를 가져올 수 없습니다!"
                );
            }

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
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

            Grounded = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void Move()
        {
            if (!canMove)
                return;

            // 디버그: ViewModel 상태 확인 (로그 제거됨)
            if (_movementViewModel != null)
            {
                // 로그 제거됨
            }
            else
            {
                Debug.LogWarning(
                    "[ThirdPersonController] Move() - PlayerMovementViewModel이 null입니다!"
                );
            }

            // 스프린트 여부에 따른 목표 속도 설정
            float targetSpeed =
                (_movementViewModel != null && _movementViewModel.SprintInput)
                    ? SprintSpeed
                    : MoveSpeed;

            if (_movementViewModel == null || _movementViewModel.MoveInput == Vector2.zero)
                targetSpeed = 0.0f;

            // 현재 수평 속도
            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x,
                0.0f,
                _controller.velocity.z
            ).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude =
                (_movementViewModel != null && _movementViewModel.AnalogMovement)
                    ? _movementViewModel.MoveInput.magnitude
                    : 1f;

            // 속도 가속/감속
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

            // 입력 방향 정규화
            Vector2 moveInput =
                _movementViewModel != null ? _movementViewModel.MoveInput : Vector2.zero;
            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            // 이동 시 플레이어 회전
            if (moveInput != Vector2.zero)
            {
                _targetRotation =
                    Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                    + _mainCamera.transform.eulerAngles.y;

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

            // 플레이어 이동
            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime)
                    + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
            );

            // 애니메이터 업데이트
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

                // 점프 디버그
                bool jumpInput = _movementViewModel != null && _movementViewModel.JumpInput;
                // 로그 제거됨

                // 점프
                if (jumpInput && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 로그 제거됨

                    // 점프 타임아웃 리셋 - 다음 점프까지 대기
                    _jumpTimeoutDelta = JumpTimeout;

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

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

                // PlayerMovementViewModel의 점프 입력은 ActionButtonController에서 관리
            }

            // 중력 적용
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            // 땅 체크 영역 표시
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
