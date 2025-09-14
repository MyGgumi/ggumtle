using DotNetty.Buffers;
using Network;
using Networks.Packets;
using UnityEngine;

namespace Networks.Players
{
    public class MongdungAttackSend : Sendable
    {
        public override PacketType Type => PacketType.MongdungAttack;

        public int vx;
        public int vy;
        public int vz;
        public long targetId;

        public MongdungAttackSend(Vector3 direction, long targetId)
        {
            this.vx = (int)(direction.x * 1000);
            this.vy = (int)(direction.y * 1000);
            this.vz = (int)(direction.z * 1000);
            this.targetId = targetId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(20);
            
            buffer.WriteInt(vx);
            buffer.WriteInt(vy);
            buffer.WriteInt(vz);
            buffer.WriteLong(targetId);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class MongdungAttackCommand : Command
    {
        public override PacketType Type => PacketType.MongdungAttackResponse;

        public int result;
        public int leftHp;

        public MongdungAttackCommand(int result, int leftHp)
        {
            this.result = result;
            this.leftHp = leftHp;
        }
    }
}