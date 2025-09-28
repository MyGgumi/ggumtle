using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Players;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.MonggingItemUseResponse)]
    public class MonggingItemFactory
    {
        public static MonggingItemUseCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            var itemId = buffer.ReadInt();
                        
            var x = buffer.ReadInt();
            var y = buffer.ReadInt();
            var z = buffer.ReadInt();
            
            return new MonggingItemUseCommand(result, itemId, x, y, z);
        }
    }

    [CommandFactory(PacketType.MonggingFieldItemUseResponse)]
    public class MonggingFieldItemFactory
    {
        public static MonggingFieldItemUseCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            var fieldItemId = buffer.ReadInt();

            return new MonggingFieldItemUseCommand(result, fieldItemId);
        }
    }
}