using UnityEngine;

namespace Features.MobileControls.Models
{
    /// <summary>
    /// 모바일 컨트롤 버튼 타입
    /// </summary>
    public enum MobileButtonType
    {
        Jump,
        Interact,
        Menu,
        Inventory,
    }

    /// <summary>
    /// 개별 버튼 상태 데이터
    /// </summary>
    public class MobileButtonData
    {
        public MobileButtonType buttonType;
        public bool isVisible;
        public bool isEnabled;
        public bool isPressed;
        public float pressScale;
        public float opacity;

        public MobileButtonData(MobileButtonType type)
        {
            buttonType = type;
            isVisible = true;
            isEnabled = true;
            isPressed = false;
            pressScale = 1.0f;
            opacity = 1.0f;
        }

        public void SetPressed(bool pressed, float pressedScale = 0.9f)
        {
            isPressed = pressed;
            pressScale = pressed ? pressedScale : 1.0f;
            opacity = pressed ? 0.8f : 1.0f;
        }

        public void SetVisibility(bool visible)
        {
            isVisible = visible;
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            opacity = enabled ? 1.0f : 0.5f;
        }
    }

    /// <summary>
    /// 조이스틱 상태 데이터
    /// </summary>
    public class JoystickData
    {
        public bool isActive;
        public bool isDragging;
        public Vector2 inputValue;
        public Vector2 knobPosition;
        public float knobRadius;
        public float deadZone;
        public float opacity;

        public JoystickData()
        {
            isActive = true;
            isDragging = false;
            inputValue = Vector2.zero;
            knobPosition = Vector2.zero;
            knobRadius = 100f;
            deadZone = 0.1f;
            opacity = 0.7f;
        }

        public void SetInput(Vector2 input, Vector2 knobPos)
        {
            inputValue = input;
            knobPosition = knobPos;
            isDragging = input.magnitude > deadZone;
        }

        public void Reset()
        {
            inputValue = Vector2.zero;
            knobPosition = Vector2.zero;
            isDragging = false;
        }
    }

    /// <summary>
    /// 전체 모바일 컨트롤 상태 관리
    /// </summary>
    public class MobileControlsData
    {
        public JoystickData joystick;
        public MobileButtonData jumpButton;
        public MobileButtonData interactButton;

        // 설정 옵션
        public bool isEnabled;
        public float globalOpacity;

        public MobileControlsData()
        {
            joystick = new JoystickData();
            jumpButton = new MobileButtonData(MobileButtonType.Jump);
            interactButton = new MobileButtonData(MobileButtonType.Interact);

            isEnabled = true;
            globalOpacity = 1.0f;

            // 상호작용 버튼은 기본적으로 숨김 상태
            interactButton.SetVisibility(false);

            UnityEngine.Debug.Log($"[MobileControlsData] 생성자 완료 - isEnabled: {isEnabled}");
        }

        /// <summary>
        /// 특정 버튼 데이터 가져오기
        /// </summary>
        public MobileButtonData GetButtonData(MobileButtonType type)
        {
            return type switch
            {
                MobileButtonType.Jump => jumpButton,
                MobileButtonType.Interact => interactButton,
                _ => null,
            };
        }

        /// <summary>
        /// 모바일 환경인지 확인
        /// </summary>
        public bool ShouldShowControls()
        {
            return isEnabled;
        }
    }
}
