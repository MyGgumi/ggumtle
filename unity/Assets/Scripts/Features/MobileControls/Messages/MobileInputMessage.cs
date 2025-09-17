using UnityEngine;

namespace Features.MobileControls.Messages
{
    public enum MobileInputType
    {
        Move,
        Look,
        Jump,
        Sprint
    }

    public class MobileInputMessage
    {
        public MobileInputType InputType { get; }
        public Vector2 Vector2Value { get; }
        public bool BoolValue { get; }

        private MobileInputMessage(MobileInputType inputType, Vector2 vector2Value = default, bool boolValue = false)
        {
            InputType = inputType;
            Vector2Value = vector2Value;
            BoolValue = boolValue;
        }

        public static MobileInputMessage Move(Vector2 input) => new(MobileInputType.Move, input);
        public static MobileInputMessage Look(Vector2 input) => new(MobileInputType.Look, input);
        public static MobileInputMessage Jump(bool input) => new(MobileInputType.Jump, boolValue: input);
        public static MobileInputMessage Sprint(bool input) => new(MobileInputType.Sprint, boolValue: input);
    }
}