namespace Features.Chest.Messages
{
    /// <summary>
    /// 상자 범위를 벗어났을 때 발행되는 메시지
    /// </summary>
    public readonly struct ChestLeftMessage
    {
        public readonly int ChestId;

        public ChestLeftMessage(int chestId)
        {
            ChestId = chestId;
        }
    }
}