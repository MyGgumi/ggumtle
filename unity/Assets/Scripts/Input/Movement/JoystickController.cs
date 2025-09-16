using System;
using UnityEngine;
using UnityEngine.UIElements;
using InputSystem.Core;

namespace InputSystem.Movement
{
    /// <summary>
    /// 조이스틱 입력만 처리하는 컨트롤러
    /// </summary>
    public class JoystickController : MonoBehaviour, IInputLayer
    {
        [Header("Joystick Settings")]
        [SerializeField] private float joystickRange = 140f;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private float magnitudeMultiplier = 1f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugVisualization = false;

        // IInputLayer 구현
        public string LayerName => "JoystickMovement";
        public int Priority { get; set; } = 10;
        public bool IsEnabled { get; private set; }

        // 이벤트
        public event Action<Vector2> OnMove;
        public event Action OnMoveEnd;

        // UI References
        private VisualElement _root;
        private VisualElement _joystickArea;
        private VisualElement _joystickKnob;

        // Joystick state
        private bool _isJoystickActive = false;
        private Vector2 _joystickCenter;
        private Vector2 _currentInput;
        private int _joystickPointerId = -1;

        // CSS에서 동적으로 값을 읽어오는 프로퍼티들
        private float JoystickSize => _joystickArea?.resolvedStyle.width ?? 400f;
        private float KnobSize => _joystickKnob?.resolvedStyle.width ?? 110f;
        private float KnobRadius => KnobSize / 2f;
        private float MaxJoystickRadius => (JoystickSize / 2f) - KnobRadius;

        // InputCoordinator 참조
        private InputCoordinator _coordinator;

        public Vector2 CurrentInput => _currentInput;

        public void Initialize(VisualElement root)
        {
            _root = root;
            _coordinator = GetComponentInParent<InputCoordinator>();

            CacheUIElements();
            SetupInputEvents();
            InitializeJoystickKnob();

            UnityEngine.Debug.Log($"[{LayerName}] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _joystickArea = _root.Q<VisualElement>("joystickArea");
            _joystickKnob = _root.Q<VisualElement>("joystickKnob");

            UnityEngine.Debug.Log($"[{LayerName}] UI 요소 캐싱: " +
                $"조이스틱={(_joystickArea != null ? "OK" : "NULL")}, " +
                $"손잡이={(_joystickKnob != null ? "OK" : "NULL")}");
        }

        private void SetupInputEvents()
        {
            if (_joystickArea != null)
            {
                _joystickArea.RegisterCallback<PointerDownEvent>(OnJoystickPointerDown);
                _joystickArea.RegisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
                _joystickArea.RegisterCallback<PointerUpEvent>(OnJoystickPointerUp);
                _joystickArea.RegisterCallback<PointerLeaveEvent>(OnJoystickPointerLeave);

                if (enableDebugVisualization)
                {
                    _joystickArea.AddToClassList("debug-joystick");
                }
            }
        }

        private void InitializeJoystickKnob()
        {
            if (_joystickKnob != null && _joystickArea != null)
            {
                float joystickSize = _joystickArea.resolvedStyle.width;
                float knobSize = _joystickKnob.resolvedStyle.width;

                if (float.IsNaN(joystickSize) || joystickSize <= 0)
                {
                    joystickSize = 400f;
                    UnityEngine.Debug.LogWarning($"[{LayerName}] 조이스틱 크기를 CSS에서 읽을 수 없음 - 기본값 사용: 400px");
                }

                if (float.IsNaN(knobSize) || knobSize <= 0)
                {
                    knobSize = 110f;
                    UnityEngine.Debug.LogWarning($"[{LayerName}] 손잡이 크기를 CSS에서 읽을 수 없음 - 기본값 사용: 110px");
                }

                float maxRadius = (joystickSize / 2f) - (knobSize / 2f);
                joystickRange = maxRadius;

                UnityEngine.Debug.Log($"[{LayerName}] 조이스틱 초기화 - Size: {joystickSize}, KnobSize: {knobSize}, MaxRadius: {maxRadius}");
            }
        }

        #region 조이스틱 처리

        private void OnJoystickPointerDown(PointerDownEvent evt)
        {
            if (!IsEnabled) return;

            // 멀티터치: 이미 활성화된 경우 무시
            if (_isJoystickActive) return;

            // InputCoordinator에 포인터 획득 요청
            if (_coordinator != null && !_coordinator.TryAcquirePointer(evt.pointerId, this))
            {
                return; // 다른 레이어가 사용 중
            }

            _isJoystickActive = true;
            _joystickPointerId = evt.pointerId;
            _joystickCenter = (Vector2)evt.localPosition;
            _joystickArea.CapturePointer(evt.pointerId);

            UnityEngine.Debug.Log($"[{LayerName}] 조이스틱 시작: {_joystickCenter}, PointerID: {evt.pointerId}");
        }

        private void OnJoystickPointerMove(PointerMoveEvent evt)
        {
            if (!IsEnabled || !_isJoystickActive || evt.pointerId != _joystickPointerId)
                return;

            Vector2 currentPosition = (Vector2)evt.localPosition;
            Vector2 joystickCenter = new Vector2(JoystickSize / 2, JoystickSize / 2);
            Vector2 offset = currentPosition - joystickCenter;
            Vector2 clampedOffset = Vector2.ClampMagnitude(offset, MaxJoystickRadius);

            // 핸들 위치 업데이트
            if (_joystickKnob != null)
            {
                _joystickKnob.style.translate = new Translate(
                    new Length(clampedOffset.x, LengthUnit.Pixel),
                    new Length(clampedOffset.y, LengthUnit.Pixel)
                );
            }

            // 입력값 계산
            Vector2 normalizedInput = Vector2.zero;
            if (MaxJoystickRadius > 0)
            {
                normalizedInput = clampedOffset / MaxJoystickRadius;
                normalizedInput.y = -normalizedInput.y; // Y축 반전
            }

            // 데드존 적용
            float magnitude = normalizedInput.magnitude;
            if (magnitude > deadZone)
            {
                float adjustedMagnitude = (magnitude - deadZone) / (1f - deadZone);
                _currentInput = normalizedInput.normalized * adjustedMagnitude * magnitudeMultiplier;
            }
            else
            {
                _currentInput = Vector2.zero;
            }

            // 이벤트 발생
            OnMove?.Invoke(_currentInput);

            if (Time.frameCount % 30 == 0) // 30프레임마다 로그
            {
                UnityEngine.Debug.Log($"[{LayerName}] Input: {_currentInput}");
            }
        }

        private void OnJoystickPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == _joystickPointerId)
            {
                ResetJoystick();
            }
        }

        private void OnJoystickPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.pointerId == _joystickPointerId)
            {
                ResetJoystick();
            }
        }

        private void ResetJoystick()
        {
            _isJoystickActive = false;
            _currentInput = Vector2.zero;

            // 손잡이를 중앙으로 리셋
            if (_joystickKnob != null)
            {
                _joystickKnob.style.translate = new Translate(
                    new Length(0, LengthUnit.Pixel),
                    new Length(0, LengthUnit.Pixel)
                );
            }

            // InputCoordinator에 포인터 해제
            _coordinator?.ReleasePointer(_joystickPointerId, this);
            _joystickPointerId = -1;

            // 이벤트 발생
            OnMoveEnd?.Invoke();
            OnMove?.Invoke(Vector2.zero);

            UnityEngine.Debug.Log($"[{LayerName}] 조이스틱 리셋");
        }

        #endregion

        #region IInputLayer 구현

        public void Enable()
        {
            IsEnabled = true;
            if (_joystickArea != null)
            {
                _joystickArea.style.display = DisplayStyle.Flex;
            }
        }

        public void Disable()
        {
            IsEnabled = false;
            ResetInput();
            if (_joystickArea != null)
            {
                _joystickArea.style.display = DisplayStyle.None;
            }
        }

        public void ResetInput()
        {
            if (_isJoystickActive)
            {
                ResetJoystick();
            }
        }

        public void Cleanup()
        {
            if (_joystickArea != null)
            {
                _joystickArea.UnregisterCallback<PointerDownEvent>(OnJoystickPointerDown);
                _joystickArea.UnregisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
                _joystickArea.UnregisterCallback<PointerUpEvent>(OnJoystickPointerUp);
                _joystickArea.UnregisterCallback<PointerLeaveEvent>(OnJoystickPointerLeave);
            }

            ResetInput();
        }

        #endregion

        #region Public Methods

        public void SetJoystickRange(float range)
        {
            joystickRange = range;
        }

        public void SetDeadZone(float zone)
        {
            deadZone = Mathf.Clamp01(zone);
        }

        public void SetMagnitudeMultiplier(float multiplier)
        {
            magnitudeMultiplier = Mathf.Max(0, multiplier);
        }

        #endregion
    }
}