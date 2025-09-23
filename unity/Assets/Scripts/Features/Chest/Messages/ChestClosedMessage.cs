namespace Features.Chest.Messages
{
    /// <summary>
    /// 상자가 닫혔을 때 발행되는 메시지
    /// </summary>
    public readonly struct ChestClosedMessage
    {
        public readonly int ChestId;

        public ChestClosedMessage(int chestId)
        {
            ChestId = chestId;
        }
    }
}