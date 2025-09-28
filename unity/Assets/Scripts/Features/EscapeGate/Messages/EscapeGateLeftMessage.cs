namespace Features.EscapeGate.Messages
{
    /// <summary>
    /// 탈출 게이트를 벗어났을 때 발행되는 메시지
    /// </summary>
    public readonly struct EscapeGateLeftMessage
    {
        public readonly int GateId;

        public EscapeGateLeftMessage(int gateId)
        {
            GateId = gateId;
        }
    }
}