using System.Text;
using Network;
using Networks.Packets;

namespace Networks.Sessions
{
    public class VerifyTokenSend : Sendable
    {
        public override PacketType Type => PacketType.VerifyToken;

        public string AccessToken { get; set; }
        
        public VerifyTokenSend(string accessToken)
        {
            AccessToken = accessToken;
        }

        public override byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(AccessToken);
        }
    }

    public class VerifyTokenCommand : Command
    {
        public override PacketType Type => PacketType.VerifyTokenResponse;
        
        public long SessionId { get; private set; }
        
        public VerifyTokenCommand(long sessionId)
        {
            SessionId = sessionId;
        }

    }
}