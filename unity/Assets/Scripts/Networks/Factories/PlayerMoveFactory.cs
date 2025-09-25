using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;

namespace Networks.Factories
{
    [CommandFactory(PacketType.PlayerMoveResponse)]
    public class PlayerMoveFactory
    {
        public static PlayerMoveCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var playerId = buffer.ReadLong();

            var x = buffer.ReadInt();
            var y = buffer.ReadInt();
            var z = buffer.ReadInt();

            var vx = buffer.ReadInt();
            var vy = buffer.ReadInt();
            var vz = buffer.ReadInt();

            return new PlayerMoveCommand(playerId, x, y, z, vx, vy, vz);
        }
    }
}
