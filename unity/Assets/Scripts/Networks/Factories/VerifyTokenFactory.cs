using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Sessions;

namespace Networks.Factories
{
    [CommandFactory(PacketType.VerifyTokenResponse)]
    public class VerifyTokenFactory
    {
        public static VerifyTokenCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            var success = buffer.ReadBoolean();
            var sessionId = buffer.ReadLong();
            
            return new VerifyTokenCommand(success, sessionId);
        }
    }
}