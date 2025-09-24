using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Ggumtle;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.GgumtleState)]
    public class GgumtleStatusFactory
    {
        public static GgumtleStatusCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var ggumtleId = buffer.ReadInt();
            var state = buffer.ReadInt();

            return new GgumtleStatusCommand(ggumtleId, state);
        }
    }
}