using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Players;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.MonggingStateBroadcast)]
    public class MonggingStateFactory
    {
        public static MonggingStateBroadcastCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var playerId = buffer.ReadLong();
            var type = buffer.ReadInt();

            return new MonggingStateBroadcastCommand(playerId, type);
        }
    }
}
