using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Ggumtle;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.DiggingStartResponse)]
    public class DiggingStartFactory
    {
        public static DiggingStartCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            
            return new DiggingStartCommand(result);
        }
    }

    [CommandFactory(PacketType.DiggingQuitResponse)]
    public class DiggingQuitFactory
    {
        public static DiggingQuitCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();

            return new DiggingQuitCommand(result);
        }
    }
}