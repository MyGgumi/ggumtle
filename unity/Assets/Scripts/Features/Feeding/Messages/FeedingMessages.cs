namespace Features.Feeding.Messages
{
    /// <summary>
    /// 젤리 개수 업데이트 메시지 (서버에서 클라이언트로)
    /// </summary>
    public class JellyCountUpdateMessage
    {
        public int JellyCount { get; set; }

        public JellyCountUpdateMessage(int jellyCount)
        {
            JellyCount = jellyCount;
        }
    }
}