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

    [CommandFactory(PacketType.DiggingDoneResponse)]
    public class DiggingDoneFactory
    {
        public static DiggingDoneCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            var id = buffer.ReadInt();
            var isRealGgumtle = buffer.ReadBoolean();
            
            return new DiggingDoneCommand(id, isRealGgumtle);
        }
    }
}