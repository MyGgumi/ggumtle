namespace Features.Revival.Messages
{
    /// <summary>
    /// 몽깅이 상호작용 상태 변경 메시지
    /// 기절/부활 상태에 따라 FaintedMonggingInteractable 활성화/비활성화 처리
    /// </summary>
    public class MonggingInteractableStateMessage
    {
        public long PlayerId { get; }
        public bool IsInteractable { get; }
        public string PlayerName { get; }

        public MonggingInteractableStateMessage(long playerId, bool isInteractable, string playerName)
        {
            PlayerId = playerId;
            IsInteractable = isInteractable;
            PlayerName = playerName;
        }
    }
}