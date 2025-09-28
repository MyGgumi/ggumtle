namespace Features.EscapeGate.Messages
{
    /// <summary>
    /// 탈출 시도가 성공했을 때 발행되는 메시지
    /// </summary>
    public readonly struct EscapeAttemptSuccessMessage
    {
        public readonly int GateId;
        public readonly long PlayerId;
        public readonly string PlayerName;

        public EscapeAttemptSuccessMessage(int gateId, long playerId, string playerName)
        {
            GateId = gateId;
            PlayerId = playerId;
            PlayerName = playerName;
        }
    }
}