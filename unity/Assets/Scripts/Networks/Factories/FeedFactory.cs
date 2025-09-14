using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Ggumtle;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.FeedStartResponse)]
    public class FeedStartFactory
    {
        public static FeedStartCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadInt();
            
            return new FeedStartCommand(result);
        }
    }
    
    [CommandFactory(PacketType.FeedQuitResponse)]
    public class FeedQuitFactory
    {
        public static FeedQuitCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            var leftFeedCount = buffer.ReadInt();

            return new FeedQuitCommand(result, leftFeedCount);
        }
    }
    
    [CommandFactory(PacketType.FeedForceQuitResponse)]
    public class FeedForceQuitFactory
    {
        public static FeedForceQuitCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var ggumtleId = buffer.ReadInt();
            var leftFeedCount = buffer.ReadInt();

            return new FeedForceQuitCommand(ggumtleId, leftFeedCount);
        }
    }
}