using System;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Digging
{
    public class DiggingStartSend : Sendable
    {
        public override PacketType Type => PacketType.DiggingStart;
        
        public int GgumtleId { get; set; }
        
        public DiggingStartSend(int ggumtleId)
        {
            GgumtleId = ggumtleId;
        }
        
        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(4);
            
            buffer.WriteInt(GgumtleId);
            
            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);
            
            return bytes;
        }
    }

    public class DiggingStartCommand : Command
    {
        public override PacketType Type => PacketType.DiggingStartResponse;

        public byte Result { get; set; }

        public DiggingStartCommand(byte result)
        {
            Result = result;
        }
    }

    public class DiggingQuitSend : Sendable
    {
        public override PacketType Type => PacketType.DiggingQuit;
        public override byte[] ToBytes() => Array.Empty<byte>();
    }

    public class DiggingQuitCommand : Command
    {
        public override PacketType Type => PacketType.DiggingQuitResponse;
        
        public byte Result { get; set; }
        
        public DiggingQuitCommand(byte result)
        {
            Result = result;
        }
    }
}