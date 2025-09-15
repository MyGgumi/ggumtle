using DotNetty.Buffers;
using Network;
using Networks.Packets;
using UnityEngine;

namespace Networks.Players
{
    public class MonggingItemUseSend : Sendable
    {
        public override PacketType Type => PacketType.MonggingItemUse;

        public int vx;
        public int vy;
        public int vz;
        public int itemId;

        public MonggingItemUseSend(Vector3 direction, int itemId)
        {
            this.vx = (int)(direction.x * 1000);
            this.vy = (int)(direction.y * 1000);
            this.vz = (int)(direction.z * 1000);
            this.itemId = itemId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(16);
            
            buffer.WriteInt(vx);
            buffer.WriteInt(vy);
            buffer.WriteInt(vz);
            buffer.WriteInt(itemId);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class MonggingItemUseCommand : Command
    {
        public override PacketType Type => PacketType.MonggingItemUseResponse;

        public byte result;
        public int itemId;

        public MonggingItemUseCommand(byte result, int itemId)
        {
            this.result = result;
            this.itemId = itemId;
        }
    }

    public class MonggingFieldItemUseSend : Sendable
    {
        public override PacketType Type => PacketType.MonggingFieldItemUse;

        public int fieldItemId;

        public MonggingFieldItemUseSend(int fieldItemId)
        {
            this.fieldItemId = fieldItemId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(4);
            
            buffer.WriteInt(fieldItemId);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class MonggingFieldItemUseCommand : Command
    {
        public override PacketType Type => PacketType.MonggingFieldItemUseResponse;

        public byte result;
        public int fieldItemId;

        public MonggingFieldItemUseCommand(byte result, int fieldItemId)
        {
            this.result = result;
            this.fieldItemId = fieldItemId;
        }
    }
}
