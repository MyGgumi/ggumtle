using Cinemachine;
using UnityEngine;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {
        [Header("Output")]
        public StarterAssetsInputs starterAssetsInputs;

        [Header("Camera")]
        public CinemachineFreeLook freeLookCamera;

        [SerializeField]
        private float cameraSensitivity = 1.5f;

        [SerializeField]
        private bool invertY = false;

        [SerializeField]
        private float inputSmoothing = 0.1f;

        // 연속 입력을 위한 변수들
        private Vector2 currentTouchInput;
        private Vector2 inputVelocity;
        private bool isTouching = false;
        private Vector2 touchStartPosition;
        private Vector2 currentTouchPosition;

        void Start()
        {
            SetupFreeLookCamera();
        }

        void Update()
        {
            UpdateCameraInput();
        }

        private void SetupFreeLookCamera()
        {
            if (freeLookCamera == null)
                return;

            // FreeLook 기본 입력 비활성화
            freeLookCamera.m_XAxis.m_InputAxisName = "";
            freeLookCamera.m_YAxis.m_InputAxisName = "";

            // 모바일 최적화 설정
            freeLookCamera.m_XAxis.m_MaxSpeed = 300f;
            freeLookCamera.m_XAxis.m_AccelTime = 0.1f;
            freeLookCamera.m_XAxis.m_DecelTime = 0.1f;

            freeLookCamera.m_YAxis.m_MaxSpeed = 2f;
            freeLookCamera.m_YAxis.m_AccelTime = 0.1f;
            freeLookCamera.m_YAxis.m_DecelTime = 0.1f;
        }

        private void UpdateCameraInput()
        {
            if (freeLookCamera == null)
                return;

            // 터치 중일 때만 카메라 회전 적용
            if (isTouching)
            {
                // 터치 시작 위치와 현재 위치의 차이 계산
                Vector2 touchDelta = currentTouchPosition - touchStartPosition;

                // 감도 적용
                Vector2 targetInput = touchDelta * cameraSensitivity * 0.01f;
                if (invertY)
                    targetInput.y = -targetInput.y;

                // 부드러운 입력 적용
                currentTouchInput = Vector2.SmoothDamp(
                    currentTouchInput,
                    targetInput,
                    ref inputVelocity,
                    inputSmoothing
                );

                // Cinemachine에 연속 입력 적용
                freeLookCamera.m_XAxis.m_InputAxisValue = currentTouchInput.x;
                freeLookCamera.m_YAxis.m_InputAxisValue = -currentTouchInput.y;
            }
            else
            {
                // 터치하지 않을 때는 점진적으로 멈춤
                currentTouchInput = Vector2.SmoothDamp(
                    currentTouchInput,
                    Vector2.zero,
                    ref inputVelocity,
                    inputSmoothing
                );

                freeLookCamera.m_XAxis.m_InputAxisValue = currentTouchInput.x;
                freeLookCamera.m_YAxis.m_InputAxisValue = -currentTouchInput.y;
            }
        }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            starterAssetsInputs.MoveInput(virtualMoveDirection);
            starterAssetsInputs.LookInput(Vector2.zero);
        }

        // UIVirtualTouchZone의 OnDrag에서 호출 (연속 입력)
        public void VirtualLookInput(Vector2 touchDelta)
        {
            if (!isTouching)
                return;

            // touchDelta는 이미 UIVirtualTouchZone에서 계산된 값
            // 단순히 현재 터치 위치로 업데이트
            currentTouchPosition = touchStartPosition + touchDelta;
        }

        // 터치 시작 - UIVirtualTouchZone의 OnPointerDown에서 호출
        public void VirtualLookInputStart(Vector2 touchPosition)
        {
            isTouching = true;
            touchStartPosition = touchPosition;
            currentTouchPosition = touchPosition;
        }

        // 터치 종료 - UIVirtualTouchZone의 OnPointerUp에서 호출
        public void VirtualLookInputEnd()
        {
            isTouching = false;
            // currentTouchInput은 Update에서 점진적으로 0이 됨
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            starterAssetsInputs.JumpInput(virtualJumpState);
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            starterAssetsInputs.SprintInput(virtualSprintState);
        }
    }
}
