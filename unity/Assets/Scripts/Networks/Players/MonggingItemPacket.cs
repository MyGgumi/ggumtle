using DotNetty.Buffers;
using Network;
using Networks.Packets;
using UnityEngine;

namespace Networks.Players
{
    public enum MonggingItemUseResult : byte
    {
        Fail = 0,              // 실패
        Success = 1,           // 성공
        ItemNotFound = 2,      // ID에 대응하는 아이템 없음
        NotMongging = 3,       // 요청한 사용자가 몽깅이가 아님
        NoMongdung = 4,        // 게임에 몽둥이가 없음
        NotAttackItem = 10,    // 공격형 아이템이 아님
        ItemNotOwned = 11,     // 요청한 몽깅이에게 해당 아이템이 없음
        Miss = 12              // MISS
    }

    public enum MonggingFieldItemUseResult : byte
    {
        Success = 1,           // 성공
        FieldItemNotFound = 2, // ID에 대응하는 필드템 없음
        NotMongging = 10,      // 몽깅이가 아님
        AlreadyUsed = 11,      // 이미 쓴 아이템
        TooFar = 12            // 너 이 근처에 없는데?
    }

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

        public MonggingItemUseResult Result { get; set; }
        public int itemId;

        public MonggingItemUseCommand(byte result, int itemId)
        {
            this.Result = (MonggingItemUseResult)result;
            this.itemId = itemId;
        }
        
        public bool Success => Result == MonggingItemUseResult.Success;
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

        public MonggingFieldItemUseResult Result { get; set; }
        public int fieldItemId;

        public MonggingFieldItemUseCommand(byte result, int fieldItemId)
        {
            this.Result = (MonggingFieldItemUseResult)result;
            this.fieldItemId = fieldItemId;
        }
        
        public bool Success => Result == MonggingFieldItemUseResult.Success;
    }
}
