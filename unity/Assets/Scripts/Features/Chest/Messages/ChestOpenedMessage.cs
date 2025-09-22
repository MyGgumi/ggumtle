namespace Features.Chest.Messages
{
    /// <summary>
    /// 상자가 열렸을 때 발행되는 메시지
    /// </summary>
    public readonly struct ChestOpenedMessage
    {
        public readonly string ChestId;
        public readonly string ChestName;

        public ChestOpenedMessage(string chestId, string chestName)
        {
            ChestId = chestId;
            ChestName = chestName;
        }
    }
}