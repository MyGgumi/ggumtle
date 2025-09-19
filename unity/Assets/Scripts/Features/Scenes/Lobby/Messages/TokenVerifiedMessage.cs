namespace Features.Scenes.Lobby.Messages
{
    /// <summary>
    /// 토큰 검증 완료 시 발행되는 메시지
    /// </summary>
    public class TokenVerifiedMessage
    {
        public long SessionId { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public TokenVerifiedMessage(long sessionId, bool isSuccess, string errorMessage = "")
        {
            SessionId = sessionId;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static TokenVerifiedMessage Success(long sessionId) =>
            new(sessionId, true);

        public static TokenVerifiedMessage Failure(string errorMessage) =>
            new(0, false, errorMessage);
    }
}