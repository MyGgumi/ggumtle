using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Ggumtle;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.JellyStartResponse)]
    public class JellyStartFactory
    {
        public static JellyStartCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadInt();
            
            return new JellyStartCommand(result);
        }
    }
    
    [CommandFactory(PacketType.JellyQuitResponse)]
    public class JellyQuitFactory
    {
        public static JellyQuitCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            var leftJellyCount = buffer.ReadInt();

            return new JellyQuitCommand(result, leftJellyCount);
        }
    }
    
    [CommandFactory(PacketType.JellyForceQuitResponse)]
    public class JellyForceQuitFactory
    {
        public static JellyForceQuitCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var ggumtleId = buffer.ReadInt();
            var leftJellyCount = buffer.ReadInt();

            return new JellyForceQuitCommand(ggumtleId, leftJellyCount);
        }
    }
}