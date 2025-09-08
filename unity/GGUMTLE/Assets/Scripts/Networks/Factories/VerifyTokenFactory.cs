using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Sessions;

namespace Networks.Factories
{
    [CommandFactory(PacketType.VerifyTokenResponse)]
    public class VerifyTokenFactory
    {
        public VerifyTokenFactory()
        {
            
        }

        public VerifyTokenCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            var sessionId = buffer.ReadLong();
            
            return new VerifyTokenCommand(sessionId);
        }
    }
}