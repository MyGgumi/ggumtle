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

    [Header("Camera")]
    public CinemachineFreeLook freeLookCamera;

    [SerializeField]
    private float cameraSensitivity = 0.5f; // 기존 1.5f에서 0.5f로 감소

    [Header("Debug")]
    [SerializeField]
    private bool enableDebugVisualization = false;

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
    private bool _isTouching = false;
    private Vector2 _touchStartPosition;
    private Vector2 _currentTouchPosition;
    private int _cameraPointerId = -1;

    // Camera input smoothing (UICanvasControllerInput 방식)
    private Vector2 _currentCameraInput;
    private Vector2 _cameraInputVelocity;

    private void Awake()
    {
        // Cinemachine 카메라 자동 검색
        if (freeLookCamera == null)
        {
            freeLookCamera = FindFirstObjectByType<CinemachineFreeLook>();
            if (freeLookCamera != null)
            {
                Debug.Log(
                    $"[MobileInputManager] CinemachineFreeLook 자동 검색 성공: {freeLookCamera.name}"
                );
            }
            else
            {
                Debug.LogWarning("[MobileInputManager] CinemachineFreeLook을 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.Log(
                $"[MobileInputManager] CinemachineFreeLook 이미 할당됨: {freeLookCamera.name}"
            );
        }

        SetupFreeLookCamera();
    }

    private void Update()
    {
        UpdateCameraInput();
    }

    /// <summary>
    /// UICanvasControllerInput 방식의 카메라 입력 처리
    /// </summary>
    private void UpdateCameraInput()
    {
        if (freeLookCamera == null)
        {
            Debug.LogWarning("[MobileInputManager] freeLookCamera가 null입니다!");
            return;
        }

        // 터치 중일 때만 카메라 회전 적용
        if (_isTouching)
        {
            // 터치 시작 위치와 현재 위치의 차이 계산 (전체 누적 델타)
            Vector2 touchDelta = _currentTouchPosition - _touchStartPosition;

            // 감도 적용 (UICanvasControllerInput과 동일한 방식)
            Vector2 targetInput = touchDelta * cameraSensitivity * 0.005f;
            if (invertY)
                targetInput.y = -targetInput.y;

            // 부드러운 입력 적용
            _currentCameraInput = Vector2.SmoothDamp(
                _currentCameraInput,
                targetInput,
                ref _cameraInputVelocity,
                inputSmoothing
            );

            // Cinemachine에 연속 입력 적용
            freeLookCamera.m_XAxis.m_InputAxisValue = _currentCameraInput.x;
            freeLookCamera.m_YAxis.m_InputAxisValue = -_currentCameraInput.y;

            // 디버그 (30프레임마다)
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log(
                    $"[MobileInputManager] 카메라 입력 - touchDelta: {touchDelta}, targetInput: {targetInput}, currentInput: {_currentCameraInput}, XAxis: {freeLookCamera.m_XAxis.m_InputAxisValue}, YAxis: {freeLookCamera.m_YAxis.m_InputAxisValue}"
                );
            }
        }
        else
        {
            // 터치하지 않을 때는 점진적으로 멈춤
            _currentCameraInput = Vector2.SmoothDamp(
                _currentCameraInput,
                Vector2.zero,
                ref _cameraInputVelocity,
                inputSmoothing
            );

            freeLookCamera.m_XAxis.m_InputAxisValue = _currentCameraInput.x;
            freeLookCamera.m_YAxis.m_InputAxisValue = -_currentCameraInput.y;
        }
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

            // 디버그 시각화
            if (enableDebugVisualization)
            {
                _joystickArea.AddToClassList("debug-joystick");
            }
        }

        // 카메라 터치존 이벤트
        if (_cameraТouchZone != null)
        {
            _cameraТouchZone.RegisterCallback<PointerDownEvent>(OnCameraPointerDown);
            _cameraТouchZone.RegisterCallback<PointerMoveEvent>(OnCameraPointerMove);
            _cameraТouchZone.RegisterCallback<PointerUpEvent>(OnCameraPointerUp);

            // 디버그 시각화
            if (enableDebugVisualization)
            {
                _cameraТouchZone.AddToClassList("debug-touch-zone");
            }
        }

        Debug.Log("[MobileInputManager] 입력 이벤트 설정 완료");
    }

    private void InitializeJoystickKnob()
    {
        // CSS에서 이미 중앙 정렬되어 있으므로 범위만 설정
        if (_joystickKnob != null && _joystickArea != null)
        {
            // 레이아웃이 완료될 때까지 대기하거나 기본값 사용
            float joystickSize = _joystickArea.resolvedStyle.width;
            float knobSize = _joystickKnob.resolvedStyle.width;

            if (float.IsNaN(joystickSize) || joystickSize <= 0)
            {
                joystickSize = 400f; // CSS 기본값
                Debug.LogWarning(
                    "[MobileInputManager] 조이스틱 크기를 CSS에서 읽을 수 없음 - 기본값 사용: 400px"
                );
            }

            if (float.IsNaN(knobSize) || knobSize <= 0)
            {
                knobSize = 110f; // CSS 기본값
                Debug.LogWarning(
                    "[MobileInputManager] 손잡이 크기를 CSS에서 읽을 수 없음 - 기본값 사용: 110px"
                );
            }

            float maxRadius = (joystickSize / 2f) - (knobSize / 2f);
            joystickRange = maxRadius;

            Debug.Log(
                $"[MobileInputManager] 조이스틱 초기화 완료 - Size: {joystickSize}, KnobSize: {knobSize}, MaxRadius: {maxRadius}"
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

        Debug.Log(
            $"[MobileInputManager] 카메라 터치 시작: {_touchStartPosition}, PointerID: {evt.pointerId}"
        );
    }

    private void OnCameraPointerMove(PointerMoveEvent evt)
    {
        // 해당 포인터만 처리
        if (!_isTouching || evt.pointerId != _cameraPointerId)
        {
            Debug.Log(
                $"[MobileInputManager] 카메라 드래그 무시 - isTouching: {_isTouching}, pointerId: {evt.pointerId}, cameraPointerId: {_cameraPointerId}"
            );
            return;
        }

        // 현재 터치 위치만 업데이트 (실제 카메라 처리는 UpdateCameraInput에서)
        _currentTouchPosition = (Vector2)evt.localPosition;

        Debug.Log(
            $"[MobileInputManager] 카메라 드래그: {_currentTouchPosition}, 델타: {_currentTouchPosition - _touchStartPosition}"
        );
    }

    private void OnCameraPointerUp(PointerUpEvent evt)
    {
        // 해당 포인터만 처리
        if (evt.pointerId == _cameraPointerId)
        {
            _isTouching = false;
            _cameraPointerId = -1;

            // 카메라 입력값은 UpdateCameraInput에서 점진적으로 0이 됨
            Debug.Log("[MobileInputManager] 카메라 터치 종료");
        }
    }

    #endregion

    #region 카메라 설정

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

        // 모바일 최적화 설정 (속도를 더 크게)
        freeLookCamera.m_XAxis.m_MaxSpeed = 300f; // 120f -> 300f로 증가
        freeLookCamera.m_XAxis.m_AccelTime = 0.01f; // 0.1f -> 0.01f로 감소 (더 빠른 반응)
        freeLookCamera.m_XAxis.m_DecelTime = 0.01f; // 0.1f -> 0.01f로 감소

        freeLookCamera.m_YAxis.m_MaxSpeed = 3f; // 1f -> 3f로 증가
        freeLookCamera.m_YAxis.m_AccelTime = 0.01f; // 0.1f -> 0.01f로 감소
        freeLookCamera.m_YAxis.m_DecelTime = 0.01f; // 0.1f -> 0.01f로 감소

        Debug.Log(
            $"[MobileInputManager] FreeLook 카메라 설정 완료 - XSpeed: {freeLookCamera.m_XAxis.m_MaxSpeed}, YSpeed: {freeLookCamera.m_YAxis.m_MaxSpeed}"
        );
        Debug.Log(
            $"[MobileInputManager] 초기 X축 값: {freeLookCamera.m_XAxis.Value}, Y축 값: {freeLookCamera.m_YAxis.Value}"
        );
        Debug.Log(
            $"[MobileInputManager] 카메라 활성화 상태: {freeLookCamera.enabled}, 게임오브젝트 활성화: {freeLookCamera.gameObject.activeInHierarchy}"
        );
    }

    #endregion

    #region 입력 전달

    private void SendMoveInput(Vector2 moveInput)
    {
        if (starterAssetsInputs != null)
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
