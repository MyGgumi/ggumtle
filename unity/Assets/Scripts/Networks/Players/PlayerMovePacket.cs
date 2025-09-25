using DotNetty.Buffers;
using Network;
using Networks.Packets;
using UnityEngine;

namespace Networks.Players
{
    public class PlayerMoveSend : Sendable
    {
        public const int Unit = 100;

        public override PacketType Type => PacketType.PlayerMove;

        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }

        public PlayerMoveSend(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(24); // 24 bytes for position + direction

            // Position data
            buffer.WriteInt((int)(Position.x * Unit));
            buffer.WriteInt((int)(Position.y * Unit));
            buffer.WriteInt((int)(Position.z * Unit));

            // Direction data
            buffer.WriteInt((int)(Direction.x * Unit));
            buffer.WriteInt((int)(Direction.y * Unit));
            buffer.WriteInt((int)(Direction.z * Unit));

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class PlayerMoveCommand : Command
    {
        public const int Unit = 100;

        public override PacketType Type => PacketType.PlayerMoveResponse;

        public long PlayerId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }

        public PlayerMoveCommand(long playerId, int x, int y, int z, int vx, int vy, int vz)
        {
            this.PlayerId = playerId;

            var xFloat = (float)x / Unit;
            var yFloat = (float)y / Unit;
            var zFloat = (float)z / Unit;
            var vxFloat = (float)vx / Unit;
            var vyFloat = (float)vy / Unit;
            var vzFloat = (float)vz / Unit;

            Position = new Vector3(xFloat, yFloat, zFloat);
            Direction = new Vector3(vxFloat, vyFloat, vzFloat);
        }
    }
}
