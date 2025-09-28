using Features.EscapeGate.Models;
using UnityEngine;

namespace Features.EscapeGate.Messages
{
    /// <summary>
    /// 탈출 게이트가 감지되었을 때 발행되는 메시지
    /// </summary>
    public readonly struct EscapeGateDetectedMessage
    {
        public readonly int GateId;
        public readonly Transform GateTransform;
        public readonly float Distance;
        public readonly EscapeGateState State;

        public EscapeGateDetectedMessage(int gateId, Transform gateTransform, float distance, EscapeGateState state)
        {
            GateId = gateId;
            GateTransform = gateTransform;
            Distance = distance;
            State = state;
        }
    }
}