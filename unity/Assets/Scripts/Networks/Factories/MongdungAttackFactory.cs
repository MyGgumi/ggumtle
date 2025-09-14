using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Players;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.MongdungAttackResponse)]
    public class MongdungAttackFactory
    {
        public static MongdungAttackCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadInt();
            var leftHp = buffer.ReadInt();

            return new MongdungAttackCommand(result, leftHp);
        }
    }
}