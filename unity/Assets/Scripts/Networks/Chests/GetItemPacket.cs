using System.Collections.Generic;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Chests
{
    public class GetItemSend : Sendable
    {
        public override PacketType Type => PacketType.GetItem;

        public int ChestId { get; set; }
        public int Index { get; set; }

        public GetItemSend(int chestId, int index)
        {
            ChestId = chestId;
            Index = index;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(8);

            buffer.WriteInt(ChestId);
            buffer.WriteInt(Index);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class GetItemCommand : Command
    {
        public override PacketType Type => PacketType.GetItemResponse;

        public int Result { get; set; }
        public long PlayerId { get; set; }
        public int ChestId { get; set; }
        public int ItemId { get; set; }
        public List<int> Items { get; set; }

        public GetItemCommand(int result, long playerId, int chestId, int itemId, List<int> items)
        {
            Result = result;
            PlayerId = playerId;
            ChestId = chestId;
            ItemId = itemId;
            Items = items;
        }
    }

    public class PutItemSend : Sendable
    {
        public override PacketType Type => PacketType.PutItem;

        public int ItemId { get; set; }

        public PutItemSend(int itemId)
        {
            ItemId = itemId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(4);

            buffer.WriteInt(ItemId);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class PutItemCommand : Command
    {
        public override PacketType Type => PacketType.PutItemResponse;

        public int Result { get; set; }
        public List<int> Items { get; set; }

        public PutItemCommand(int result, List<int> items)
        {
            Result = result;
            Items = items;
        }
    }
}