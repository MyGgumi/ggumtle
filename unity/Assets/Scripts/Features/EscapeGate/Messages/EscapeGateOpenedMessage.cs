namespace Features.EscapeGate.Messages
{
    /// <summary>
    /// 서버에서 탈출구가 열렸다는 신호를 받았을 때 발행되는 브릿지 메시지
    /// EscapeGateNetworkEventHandler에서 EscapeGateService로 전달하는 용도
    /// </summary>
    public readonly struct EscapeGateOpenedMessage
    {
        public readonly int Count;
        public readonly int[] GateIds;

        public EscapeGateOpenedMessage(int count, int[] gateIds)
        {
            Count = count;
            GateIds = gateIds;
        }
    }
}