using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cinemachine;
using InputSystem.Core;

namespace InputSystem.Movement
{
    /// <summary>
    /// 카메라 회전 입력만 처리하는 컨트롤러
    /// </summary>
    public class CameraRotationController : MonoBehaviour, IInputLayer
    {
        [Header("Camera")]
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        [SerializeField] private float cameraSensitivity = 0.5f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private float inputSmoothing = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugVisualization = false;

        // IInputLayer 구현
        public string LayerName => "CameraRotation";
        public int Priority { get; set; } = 5;
        public bool IsEnabled { get; private set; }

        // 이벤트
        public event Action<Vector2> OnCameraRotate;
        public event Action OnRotateStart;
        public event Action OnRotateEnd;

        // UI References
        private VisualElement _root;
        private VisualElement _cameraТouchZone;

        // Camera touch state
        private bool _isTouching = false;
        private Vector2 _touchStartPosition;
        private Vector2 _currentTouchPosition;
        private int _cameraPointerId = -1;

        // Camera input smoothing
        private Vector2 _currentCameraInput;
        private Vector2 _cameraInputVelocity;

        // InputCoordinator 참조
        private InputCoordinator _coordinator;

        public Vector2 CurrentRotation => _currentCameraInput;
        public bool IsRotating => _isTouching;

        private void Awake()
        {
            // Cinemachine 카메라 자동 검색
            if (freeLookCamera == null)
            {
                freeLookCamera = FindFirstObjectByType<CinemachineFreeLook>();
                if (freeLookCamera != null)
                {
                    UnityEngine.Debug.Log($"[{LayerName}] CinemachineFreeLook 자동 검색 성공: {freeLookCamera.name}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[{LayerName}] CinemachineFreeLook을 찾을 수 없습니다!");
                }
            }

            SetupFreeLookCamera();
        }

        private void Update()
        {
            if (IsEnabled)
            {
                UpdateCameraInput();
            }
        }

        public void Initialize(VisualElement root)
        {
            _root = root;
            _coordinator = GetComponentInParent<InputCoordinator>();

            CacheUIElements();
            SetupInputEvents();

            UnityEngine.Debug.Log($"[{LayerName}] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _cameraТouchZone = _root.Q<VisualElement>("cameraТouchZone");

            UnityEngine.Debug.Log($"[{LayerName}] UI 요소 캐싱: " +
                $"터치존={(_cameraТouchZone != null ? "OK" : "NULL")}");
        }

        private void SetupInputEvents()
        {
            if (_cameraТouchZone != null)
            {
                _cameraТouchZone.RegisterCallback<PointerDownEvent>(OnCameraPointerDown);
                _cameraТouchZone.RegisterCallback<PointerMoveEvent>(OnCameraPointerMove);
                _cameraТouchZone.RegisterCallback<PointerUpEvent>(OnCameraPointerUp);

                if (enableDebugVisualization)
                {
                    _cameraТouchZone.AddToClassList("debug-touch-zone");
                }
            }
        }

        private void UpdateCameraInput()
        {
            if (freeLookCamera == null)
            {
                return;
            }

            // 터치 중일 때만 카메라 회전 적용
            if (_isTouching)
            {
                // 터치 시작 위치와 현재 위치의 차이 계산
                Vector2 touchDelta = _currentTouchPosition - _touchStartPosition;

                // 감도 적용
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

                // Cinemachine에 입력 적용
                freeLookCamera.m_XAxis.m_InputAxisValue = _currentCameraInput.x;
                freeLookCamera.m_YAxis.m_InputAxisValue = -_currentCameraInput.y;

                // 이벤트 발생
                OnCameraRotate?.Invoke(_currentCameraInput);

                if (Time.frameCount % 30 == 0)
                {
                    UnityEngine.Debug.Log($"[{LayerName}] 카메라 입력: {_currentCameraInput}");
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

        #region 카메라 터치 처리

        private void OnCameraPointerDown(PointerDownEvent evt)
        {
            if (!IsEnabled) return;

            // 멀티터치: 이미 활성화된 경우 무시
            if (_isTouching) return;

            // InputCoordinator에 포인터 획득 요청
            if (_coordinator != null && !_coordinator.TryAcquirePointer(evt.pointerId, this))
            {
                return; // 다른 레이어가 사용 중
            }

            _isTouching = true;
            _cameraPointerId = evt.pointerId;
            _touchStartPosition = (Vector2)evt.localPosition;
            _currentTouchPosition = (Vector2)evt.localPosition;
            _cameraТouchZone.CapturePointer(evt.pointerId);

            OnRotateStart?.Invoke();

            UnityEngine.Debug.Log($"[{LayerName}] 카메라 터치 시작: {_touchStartPosition}, PointerID: {evt.pointerId}");
        }

        private void OnCameraPointerMove(PointerMoveEvent evt)
        {
            if (!IsEnabled || !_isTouching || evt.pointerId != _cameraPointerId)
            {
                return;
            }

            // 현재 터치 위치만 업데이트 (실제 카메라 처리는 UpdateCameraInput에서)
            _currentTouchPosition = (Vector2)evt.localPosition;

            if (Time.frameCount % 30 == 0)
            {
                UnityEngine.Debug.Log($"[{LayerName}] 카메라 드래그: {_currentTouchPosition}, 델타: {_currentTouchPosition - _touchStartPosition}");
            }
        }

        private void OnCameraPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId == _cameraPointerId)
            {
                ResetCameraTouch();
            }
        }

        private void ResetCameraTouch()
        {
            _isTouching = false;

            // InputCoordinator에 포인터 해제
            _coordinator?.ReleasePointer(_cameraPointerId, this);
            _cameraPointerId = -1;

            OnRotateEnd?.Invoke();

            UnityEngine.Debug.Log($"[{LayerName}] 카메라 터치 종료");
        }

        #endregion

        #region 카메라 설정

        private void SetupFreeLookCamera()
        {
            if (freeLookCamera == null) return;

            // FreeLook 기본 입력 비활성화
            freeLookCamera.m_XAxis.m_InputAxisName = "";
            freeLookCamera.m_YAxis.m_InputAxisName = "";

            // Y축 범위 설정
            freeLookCamera.m_YAxis.m_MinValue = 0.2f;
            freeLookCamera.m_YAxis.m_MaxValue = 0.7f;
            freeLookCamera.m_YAxis.Value = 0.3f;

            // 모바일 최적화 설정
            freeLookCamera.m_XAxis.m_MaxSpeed = 300f;
            freeLookCamera.m_XAxis.m_AccelTime = 0.01f;
            freeLookCamera.m_XAxis.m_DecelTime = 0.01f;

            freeLookCamera.m_YAxis.m_MaxSpeed = 3f;
            freeLookCamera.m_YAxis.m_AccelTime = 0.01f;
            freeLookCamera.m_YAxis.m_DecelTime = 0.01f;

            UnityEngine.Debug.Log($"[{LayerName}] FreeLook 카메라 설정 완료");
        }

        #endregion

        #region IInputLayer 구현

        public void Enable()
        {
            IsEnabled = true;
            if (_cameraТouchZone != null)
            {
                _cameraТouchZone.style.display = DisplayStyle.Flex;
            }
        }

        public void Disable()
        {
            IsEnabled = false;
            ResetInput();
            if (_cameraТouchZone != null)
            {
                _cameraТouchZone.style.display = DisplayStyle.None;
            }
        }

        public void ResetInput()
        {
            if (_isTouching)
            {
                ResetCameraTouch();
            }
            _currentCameraInput = Vector2.zero;
            _cameraInputVelocity = Vector2.zero;
        }

        public void Cleanup()
        {
            if (_cameraТouchZone != null)
            {
                _cameraТouchZone.UnregisterCallback<PointerDownEvent>(OnCameraPointerDown);
                _cameraТouchZone.UnregisterCallback<PointerMoveEvent>(OnCameraPointerMove);
                _cameraТouchZone.UnregisterCallback<PointerUpEvent>(OnCameraPointerUp);
            }

            ResetInput();
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

        public void SetInputSmoothing(float smoothing)
        {
            inputSmoothing = Mathf.Clamp01(smoothing);
        }

        public void SetFreeLookCamera(CinemachineFreeLook camera)
        {
            freeLookCamera = camera;
            SetupFreeLookCamera();
        }

        #endregion
    }
}