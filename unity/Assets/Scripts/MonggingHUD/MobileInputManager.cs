using Cinemachine;
using StarterAssets;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 기반 모바일 입력 관리자 - 조이스틱 및 카메라 터치존 처리
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    [Header("Input System Integration")]
    public StarterAssetsInputs starterAssetsInputs;
    public StarterAssets.UICanvasControllerInput canvasControllerInput;

    [Header("Camera")]
    public CinemachineFreeLook freeLookCamera;

    [SerializeField]
    private float cameraSensitivity = 0.5f; // 기존 1.5f에서 0.5f로 감소

    [SerializeField]
    private bool invertY = false;

    [SerializeField]
    private float inputSmoothing = 0.1f;

    [Header("Joystick Settings")]
    [SerializeField]
    private float joystickRange = 140f; // 기본값, 런타임에 CSS 기준으로 자동 조정됨

    [SerializeField]
    private float deadZone = 0.1f;

    [SerializeField]
    private float magnitudeMultiplier = 1f;

    // CSS에서 동적으로 값을 읽어오는 프로퍼티들
    private float JoystickSize => _joystickArea?.resolvedStyle.width ?? 400f;
    private float KnobSize => _joystickKnob?.resolvedStyle.width ?? 110f;
    private float KnobRadius => KnobSize / 2f;

    // 조이스틱 배경 안에서 핸들이 움직일 수 있는 최대 반지름
    private float MaxJoystickRadius => (JoystickSize / 2f) - KnobRadius;

    // UI References
    private VisualElement _root;
    private VisualElement _joystickArea;
    private VisualElement _joystickKnob;
    private VisualElement _cameraТouchZone;

    // Joystick state
    private bool _isJoystickActive = false;
    private Vector2 _joystickCenter;
    private Vector2 _currentJoystickInput;
    private int _joystickPointerId = -1;

    // Camera touch state
    private Vector2 _currentTouchInput;
    private Vector2 _inputVelocity;
    private bool _isTouching = false;
    private Vector2 _touchStartPosition;
    private Vector2 _currentTouchPosition;
    private int _cameraPointerId = -1;

    private void Awake()
    {
        SetupFreeLookCamera();
    }

    private void Update()
    {
        // 카메라 처리는 UICanvasControllerInput에 완전히 위임
    }

    /// <summary>
    /// UniversalHUDController에서 호출하는 초기화 메서드
    /// </summary>
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        SetupInputEvents();
        InitializeJoystickKnob();

        Debug.Log("[MobileInputManager] 초기화 완료");
    }

    private void CacheUIElements()
    {
        _joystickArea = _root.Q<VisualElement>("joystickArea");
        _joystickKnob = _root.Q<VisualElement>("joystickKnob");
        _cameraТouchZone = _root.Q<VisualElement>("cameraТouchZone");

        Debug.Log(
            $"[MobileInputManager] UI 요소 캐싱: "
                + $"조이스틱={(_joystickArea != null ? "OK" : "NULL")}, "
                + $"손잡이={(_joystickKnob != null ? "OK" : "NULL")}, "
                + $"터치존={(_cameraТouchZone != null ? "OK" : "NULL")}"
        );
    }

    private void SetupInputEvents()
    {
        // 조이스틱 이벤트
        if (_joystickArea != null)
        {
            _joystickArea.RegisterCallback<PointerDownEvent>(OnJoystickPointerDown);
            _joystickArea.RegisterCallback<PointerMoveEvent>(OnJoystickPointerMove);
            _joystickArea.RegisterCallback<PointerUpEvent>(OnJoystickPointerUp);
            _joystickArea.RegisterCallback<PointerLeaveEvent>(OnJoystickPointerLeave);
        }

        // 카메라 터치존 이벤트
        if (_cameraТouchZone != null)
        {
            _cameraТouchZone.RegisterCallback<PointerDownEvent>(OnCameraPointerDown);
            _cameraТouchZone.RegisterCallback<PointerMoveEvent>(OnCameraPointerMove);
            _cameraТouchZone.RegisterCallback<PointerUpEvent>(OnCameraPointerUp);
        }

        Debug.Log("[MobileInputManager] 입력 이벤트 설정 완료");
    }

    private void InitializeJoystickKnob()
    {
        // CSS에서 이미 중앙 정렬되어 있으므로 범위만 설정
        if (_joystickKnob != null && _joystickArea != null)
        {
            // CSS 값 기준으로 joystickRange 자동 조정
            joystickRange = MaxJoystickRadius;
            Debug.Log(
                $"[MobileInputManager] 조이스틱 초기화 완료 - Size: {JoystickSize}, MaxRadius: {MaxJoystickRadius}"
            );
        }
    }

    #region 조이스틱 처리

    private void OnJoystickPointerDown(PointerDownEvent evt)
    {
        // 멀티터치: 이미 활성화된 경우 무시
        if (_isJoystickActive)
            return;

        _isJoystickActive = true;
        _joystickPointerId = evt.pointerId;
        _joystickCenter = (Vector2)evt.localPosition;
        _joystickArea.CapturePointer(evt.pointerId);

        Debug.Log(
            $"[MobileInputManager] 조이스틱 시작: {_joystickCenter}, PointerID: {evt.pointerId}"
        );
    }

    private void OnJoystickPointerMove(PointerMoveEvent evt)
    {
        // 해당 포인터만 처리
        if (!_isJoystickActive || evt.pointerId != _joystickPointerId)
            return;

        // 현재 터치 위치
        Vector2 currentPosition = (Vector2)evt.localPosition;

        // 조이스틱 중심점 계산 (CSS와 동일한 기준)
        Vector2 joystickCenter = new Vector2(JoystickSize / 2, JoystickSize / 2);

        // 중심점에서 현재 위치까지의 오프셋
        Vector2 offset = currentPosition - joystickCenter;

        // 최대 반지름 내로 제한 (핸들이 배경 밖으로 나가지 않도록)
        Vector2 clampedOffset = Vector2.ClampMagnitude(offset, MaxJoystickRadius);

        // 핸들 위치 설정 (CSS 중앙 위치 유지하고 transform으로 오프셋)
        if (_joystickKnob != null)
        {
            _joystickKnob.style.translate = new Translate(
                new Length(clampedOffset.x, LengthUnit.Pixel),
                new Length(clampedOffset.y, LengthUnit.Pixel)
            );
        }

        // 입력값 계산 (-1 ~ 1 범위로 정규화)
        Vector2 normalizedInput = Vector2.zero;
        if (MaxJoystickRadius > 0)
        {
            normalizedInput = clampedOffset / MaxJoystickRadius;
            normalizedInput.y = -normalizedInput.y; // Y축 반전 (게임 입력용)
        }

        // 데드존 적용
        float magnitude = normalizedInput.magnitude;
        if (magnitude > deadZone)
        {
            float adjustedMagnitude = (magnitude - deadZone) / (1f - deadZone);
            _currentJoystickInput =
                normalizedInput.normalized * adjustedMagnitude * magnitudeMultiplier;
        }
        else
        {
            _currentJoystickInput = Vector2.zero;
        }

        // 이동 입력 전달
        SendMoveInput(_currentJoystickInput);

        // 디버깅 (처음 몇 번만)
        if (Time.frameCount % 30 == 0) // 30프레임마다 로그
        {
            Debug.Log(
                $"[Joystick] Touch: {currentPosition}, Offset: {offset}, Clamped: {clampedOffset}, Input: {_currentJoystickInput}"
            );
        }
    }

    private void OnJoystickPointerUp(PointerUpEvent evt)
    {
        // 해당 포인터만 처리
        if (evt.pointerId == _joystickPointerId)
        {
            ResetJoystick();
        }
    }

    private void OnJoystickPointerLeave(PointerLeaveEvent evt)
    {
        // 해당 포인터만 처리
        if (evt.pointerId == _joystickPointerId)
        {
            ResetJoystick();
        }
    }

    private void ResetJoystick()
    {
        _isJoystickActive = false;
        _joystickPointerId = -1;
        _currentJoystickInput = Vector2.zero;

        // 손잡이를 중앙으로 리셋 (transform을 0으로 돌려 CSS 초기 위치로)
        if (_joystickKnob != null)
        {
            _joystickKnob.style.translate = new Translate(
                new Length(0, LengthUnit.Pixel),
                new Length(0, LengthUnit.Pixel)
            );
        }

        // 이동 입력 리셋
        SendMoveInput(Vector2.zero);

        Debug.Log("[MobileInputManager] 조이스틱 리셋");
    }

    #endregion

    #region 카메라 터치 처리

    private void OnCameraPointerDown(PointerDownEvent evt)
    {
        // 멀티터치: 이미 활성화된 경우 무시
        if (_isTouching)
            return;

        _isTouching = true;
        _cameraPointerId = evt.pointerId;
        _touchStartPosition = (Vector2)evt.localPosition;
        _currentTouchPosition = (Vector2)evt.localPosition;
        _cameraТouchZone.CapturePointer(evt.pointerId);

        // UICanvasControllerInput에 터치 시작 알림
        if (canvasControllerInput != null)
        {
            canvasControllerInput.VirtualLookInputStart(_touchStartPosition);
        }

        Debug.Log(
            $"[MobileInputManager] 카메라 터치 시작: {_touchStartPosition}, PointerID: {evt.pointerId}"
        );
    }

    private void OnCameraPointerMove(PointerMoveEvent evt)
    {
        // 해당 포인터만 처리
        if (!_isTouching || evt.pointerId != _cameraPointerId)
            return;

        _currentTouchPosition = (Vector2)evt.localPosition;

        // UICanvasControllerInput에 터치 델타 전달 (화면 크기 기준 정규화)
        if (canvasControllerInput != null)
        {
            Vector2 touchDelta = _currentTouchPosition - _touchStartPosition;
            // 터치존 크기 기준으로 정규화 (화면 절반 크기)
            Vector2 normalizedDelta = new Vector2(
                touchDelta.x / (Screen.width * 0.5f), // 카메라 터치존이 화면 오른쪽 절반
                touchDelta.y / (Screen.height * 0.8f) // 상단 20%는 제외
            );
            // 감도 적용 (훨씬 작은 스케일)
            Vector2 adjustedDelta = normalizedDelta * cameraSensitivity * 20f; // 100f에서 20f로 감소
            canvasControllerInput.VirtualLookInput(adjustedDelta);
        }
    }

    private void OnCameraPointerUp(PointerUpEvent evt)
    {
        // 해당 포인터만 처리
        if (evt.pointerId == _cameraPointerId)
        {
            _isTouching = false;
            _cameraPointerId = -1;

            // UICanvasControllerInput에 터치 종료 알림
            if (canvasControllerInput != null)
            {
                canvasControllerInput.VirtualLookInputEnd();
            }

            Debug.Log("[MobileInputManager] 카메라 터치 종료");
        }
    }

    #endregion

    #region 카메라 업데이트

    private void SetupFreeLookCamera()
    {
        if (freeLookCamera == null)
            return;

        // FreeLook 기본 입력 비활성화
        freeLookCamera.m_XAxis.m_InputAxisName = "";
        freeLookCamera.m_YAxis.m_InputAxisName = "";

        // Y축 범위 설정
        freeLookCamera.m_YAxis.m_MinValue = 0.2f;
        freeLookCamera.m_YAxis.m_MaxValue = 0.7f;
        freeLookCamera.m_YAxis.Value = 0.3f;

        // 모바일 최적화 설정 (속도 감소)
        freeLookCamera.m_XAxis.m_MaxSpeed = 120f; // 기존 300f에서 120f로 감소
        freeLookCamera.m_XAxis.m_AccelTime = 0.1f;
        freeLookCamera.m_XAxis.m_DecelTime = 0.1f;

        freeLookCamera.m_YAxis.m_MaxSpeed = 1f; // 기존 2f에서 1f로 감소
        freeLookCamera.m_YAxis.m_AccelTime = 0.1f;
        freeLookCamera.m_YAxis.m_DecelTime = 0.1f;

        Debug.Log("[MobileInputManager] FreeLook 카메라 설정 완료");
    }

    private void UpdateCameraInput()
    {
        if (freeLookCamera == null)
            return;

        // 터치 중일 때만 카메라 회전 적용
        if (_isTouching)
        {
            // VirtualTouchZone 방식: 누적 오프셋 계산
            Vector2 touchDelta = _currentTouchPosition - _touchStartPosition;

            // 터치존 크기에 맞게 정규화 (화면 크기 기준)
            Vector2 normalizedDelta = new Vector2(
                touchDelta.x / Screen.width,
                touchDelta.y / Screen.height
            );

            // 감도 적용
            Vector2 targetInput = normalizedDelta * cameraSensitivity;

            if (invertY)
                targetInput.y = -targetInput.y;

            // 부드러운 입력 적용
            _currentTouchInput = Vector2.SmoothDamp(
                _currentTouchInput,
                targetInput,
                ref _inputVelocity,
                inputSmoothing
            );

            // Cinemachine에 입력 적용
            freeLookCamera.m_XAxis.m_InputAxisValue = _currentTouchInput.x;
            freeLookCamera.m_YAxis.m_InputAxisValue = -_currentTouchInput.y;
        }
        else
        {
            // 터치하지 않을 때는 점진적으로 멈춤
            _currentTouchInput = Vector2.SmoothDamp(
                _currentTouchInput,
                Vector2.zero,
                ref _inputVelocity,
                inputSmoothing
            );

            freeLookCamera.m_XAxis.m_InputAxisValue = _currentTouchInput.x;
            freeLookCamera.m_YAxis.m_InputAxisValue = -_currentTouchInput.y;
        }
    }

    #endregion

    #region 입력 전달

    private void SendMoveInput(Vector2 moveInput)
    {
        if (canvasControllerInput != null)
        {
            canvasControllerInput.VirtualMoveInput(moveInput);
        }
        else if (starterAssetsInputs != null)
        {
            starterAssetsInputs.MoveInput(moveInput);
            starterAssetsInputs.LookInput(Vector2.zero);
        }
    }

    #endregion

    #region Public Methods

    public void SetCameraSensitivity(float sensitivity)
    {
        cameraSensitivity = sensitivity;
    }

    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }

    public void SetJoystickRange(float range)
    {
        joystickRange = range;
    }

    #endregion
}
