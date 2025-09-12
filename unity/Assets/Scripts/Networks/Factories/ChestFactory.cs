using System.Collections.Generic;
using DotNetty.Buffers;
using Networks.Attributes;
using Networks.chests;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.ChestOpenResponse)]
    public class ChestOpenFactory
    {
        public static ChestOpenCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            var success = buffer.ReadInt();
            var inventoryId = buffer.ReadInt();
            var itemSize = buffer.ReadInt();

            List<int> items = new();
            for (var i = 0; i < itemSize; i++)
            {
                var item = buffer.ReadInt();
                items.Add(item);
            }
            return new ChestOpenCommand(success, inventoryId, itemSize, items);
        }
    }
    
    [CommandFactory(PacketType.ChestCloseResponse)]
    public class ChestCloseFactory
    {
        public static ChestCloseCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            var result = buffer.ReadInt();
            return new ChestCloseCommand(result);
        }
    }
}