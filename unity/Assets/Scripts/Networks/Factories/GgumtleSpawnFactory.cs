using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Ggumtle;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.GgumtleSpawn)]
    public class GgumtleSpawnFactory
    {
        public static GgumtleSpawnCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var id = buffer.ReadInt();
            var x = buffer.ReadInt();
            var y = buffer.ReadInt();
            var z = buffer.ReadInt();

            return new GgumtleSpawnCommand(id, x, y, z);
        }
    }
}
