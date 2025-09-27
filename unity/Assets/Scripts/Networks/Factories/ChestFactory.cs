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

            var result = buffer.ReadBoolean();
            var inventoryId = buffer.ReadInt();
            var itemSize = buffer.ReadInt();

            UnityEngine.Debug.Log($"[ChestOpenFactory] Packet parsing - success: {result}, id: {inventoryId}, itemSize: {itemSize}");

            List<int> items = new();
            if (itemSize > 0 && itemSize <= 20) // 방어적 체크
            {
                for (var i = 0; i < itemSize; i++)
                {
                    if (buffer.IsReadable(4))
                    {
                        var item = buffer.ReadInt();
                        items.Add(item);
                    }
                }
            }
            return new ChestOpenCommand(result, inventoryId, itemSize, items);
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