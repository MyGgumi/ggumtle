using Features.MobileControls.Models;
using UnityEngine;

namespace Features.MobileControls.Messages
{
    /// <summary>
    /// 조이스틱 입력 메시지
    /// </summary>
    public class JoystickInputMessage
    {
        public Vector2 InputValue { get; }
        public bool IsActive { get; }

        public JoystickInputMessage(Vector2 inputValue, bool isActive)
        {
            InputValue = inputValue;
            IsActive = isActive;
        }
    }

    /// <summary>
    /// 조이스틱 드래그 종료 메시지
    /// </summary>
    public class JoystickEndMessage
    {
        public static readonly JoystickEndMessage Instance = new();
        private JoystickEndMessage() { }
    }

    /// <summary>
    /// 모바일 버튼 눌림 메시지
    /// </summary>
    public class MobileButtonPressedMessage
    {
        public MobileButtonType ButtonType { get; }

        public MobileButtonPressedMessage(MobileButtonType buttonType)
        {
            ButtonType = buttonType;
        }
    }

    /// <summary>
    /// 모바일 버튼 해제 메시지
    /// </summary>
    public class MobileButtonReleasedMessage
    {
        public MobileButtonType ButtonType { get; }

        public MobileButtonReleasedMessage(MobileButtonType buttonType)
        {
            ButtonType = buttonType;
        }
    }

    /// <summary>
    /// 상호작용 버튼 홀드 시작 메시지
    /// </summary>
    public class InteractHoldStartMessage
    {
        public static readonly InteractHoldStartMessage Instance = new();
        private InteractHoldStartMessage() { }
    }

    /// <summary>
    /// 상호작용 버튼 홀드 종료 메시지
    /// </summary>
    public class InteractHoldEndMessage
    {
        public static readonly InteractHoldEndMessage Instance = new();
        private InteractHoldEndMessage() { }
    }

    /// <summary>
    /// 상호작용 버튼 가시성 요청 메시지 (GgumtleViewModel에서 발행)
    /// </summary>
    public class InteractButtonVisibilityMessage
    {
        public bool IsVisible { get; }
        public string Reason { get; }

        public InteractButtonVisibilityMessage(bool isVisible, string reason = "")
        {
            IsVisible = isVisible;
            Reason = reason;
        }
    }

    /// <summary>
    /// 카메라 터치 입력 메시지
    /// </summary>
    public class CameraTouchMessage
    {
        public Vector2 DeltaPosition { get; }
        public bool IsActive { get; }

        public CameraTouchMessage(Vector2 deltaPosition, bool isActive)
        {
            DeltaPosition = deltaPosition;
            IsActive = isActive;
        }
    }

    /// <summary>
    /// 모바일 컨트롤 설정 변경 메시지
    /// </summary>
    public class MobileControlSettingsMessage
    {
        public bool IsEnabled { get; }
        public bool ShowOnDesktop { get; }
        public float GlobalOpacity { get; }

        public MobileControlSettingsMessage(bool isEnabled, bool showOnDesktop, float globalOpacity)
        {
            IsEnabled = isEnabled;
            ShowOnDesktop = showOnDesktop;
            GlobalOpacity = globalOpacity;
        }
    }

    /// <summary>
    /// 버튼 상태 변경 메시지 (내부 사용)
    /// </summary>
    public class MobileButtonStateMessage
    {
        public MobileButtonType ButtonType { get; }
        public bool IsVisible { get; }
        public bool IsEnabled { get; }

        public MobileButtonStateMessage(MobileButtonType buttonType, bool isVisible, bool isEnabled)
        {
            ButtonType = buttonType;
            IsVisible = isVisible;
            IsEnabled = isEnabled;
        }
    }
}