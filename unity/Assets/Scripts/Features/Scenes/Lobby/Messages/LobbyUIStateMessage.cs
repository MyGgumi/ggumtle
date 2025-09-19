namespace Features.Scenes.Lobby.Messages
{
    /// <summary>
    /// 로비 UI 상태 업데이트 메시지 (ViewModel 전용)
    /// </summary>
    public class LobbyUIStateMessage
    {
        public bool IsConnecting { get; }
        public bool IsAuthenticated { get; }
        public string StatusMessage { get; }

        public LobbyUIStateMessage(bool isConnecting, bool isAuthenticated, string statusMessage)
        {
            IsConnecting = isConnecting;
            IsAuthenticated = isAuthenticated;
            StatusMessage = statusMessage;
        }

        public static LobbyUIStateMessage Connecting(string message) =>
            new(true, false, message);

        public static LobbyUIStateMessage Authenticated(string message) =>
            new(false, true, message);

        public static LobbyUIStateMessage Ready(string message) =>
            new(false, true, message);

        public static LobbyUIStateMessage Error(string message) =>
            new(false, false, message);
    }
}