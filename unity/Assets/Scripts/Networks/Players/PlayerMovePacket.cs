using System.Numerics;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Players
{
    public class PlayerMoveSend: Sendable
    {
        public const int Unit = 1000;
        
        public override PacketType Type => PacketType.PlayerMove;
        
        public Vector3 Position { get; set; }
        
        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(12);

            buffer.WriteInt((int) (Position.X * Unit));
            buffer.WriteInt((int) (Position.Y * Unit));
            buffer.WriteInt((int) (Position.Z * Unit));
            
            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);
            
            return bytes;
        }
    }

    public class PlayerMoveCommand : Command
    {
        public const int Unit = 1000;
        
        public override PacketType Type => PacketType.PlayerMoveResponse;
        
        public long PlayerId { get; set; }
        public Vector3 Position { get; set; }
        
        public PlayerMoveCommand(long playerId, int x, int y, int z)
        {
            this.PlayerId = playerId;
            
            var xFloat = (float) x / Unit;
            var yFloat = (float) y / Unit;
            var zFloat = (float) z / Unit;
            
            Position = new Vector3(xFloat, yFloat, zFloat);
        }
    }
}