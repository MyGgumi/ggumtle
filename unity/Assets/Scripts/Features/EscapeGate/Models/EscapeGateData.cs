using UnityEngine;

namespace Features.EscapeGate.Models
{
    /// <summary>
    /// 탈출 게이트 데이터 모델
    /// </summary>
    public class EscapeGateData
    {
        public int GateId { get; set; }
        public string GateName { get; set; }
        public Vector3 Position { get; set; }
        public EscapeGateState State { get; set; }
        public bool IsActive => State == EscapeGateState.Active;
        public bool CanInteract => State == EscapeGateState.Active;

        public EscapeGateData()
        {
            State = EscapeGateState.Inactive;
        }

        public EscapeGateData(int gateId, string gateName, Vector3 position)
        {
            GateId = gateId;
            GateName = gateName;
            Position = position;
            State = EscapeGateState.Inactive;
        }

        public void Activate()
        {
            if (State == EscapeGateState.Inactive)
            {
                State = EscapeGateState.Active;
            }
        }

        public void SetInUse()
        {
            if (State == EscapeGateState.Active)
            {
                State = EscapeGateState.InUse;
            }
        }

        public void Disable()
        {
            State = EscapeGateState.Disabled;
        }

        public void Reset()
        {
            State = EscapeGateState.Inactive;
        }
    }
}