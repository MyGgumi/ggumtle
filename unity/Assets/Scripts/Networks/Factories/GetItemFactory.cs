using DotNetty.Buffers;
using System.Collections.Generic;
using Networks.Attributes;
using Networks.Chests;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.GetItemResponse)]
    public class GetItemFactory
    {
        public static GetItemCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadInt();
            var itemSize = buffer.ReadInt();

            var items = new List<int>();
            for (var i = 0; i < itemSize; i++)
            {
                items.Add(buffer.ReadInt());
            }

            return new GetItemCommand(result, items);
        }
    }

    [CommandFactory(PacketType.PutItemResponse)]
    public class PutItemFactory
    {
        public static PutItemCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadInt();
            var itemSize = buffer.ReadInt();

            var items = new List<int>();
            for (var i = 0; i < itemSize; i++)
            {
                items.Add(buffer.ReadInt());
            }

            return new PutItemCommand(result, items);
        }
    }
}