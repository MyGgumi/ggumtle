using System;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Ggumtle
{
    public class FeedStartSend : Sendable
    {
        public override PacketType Type => PacketType.FeedStart;
        
        public int GgumtleId { get; set; }
        
        public FeedStartSend(int ggumtleId)
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

    public class FeedStartCommand : Command
    {
        public override PacketType Type => PacketType.FeedStartResponse;
        
        public int Result { get; set; }
        
        public FeedStartCommand(int result)
        {
            Result = result;
        }
    }
    
    public class FeedQuitSend : Sendable
    {
        public override PacketType Type => PacketType.FeedQuit;

        public FeedQuitSend()
        {
        }

        public override byte[] ToBytes() => Array.Empty<byte>();
    }
    
    public class FeedQuitCommand : Command
    {
        public override PacketType Type => PacketType.FeedQuitResponse;
        
        public byte Result { get; set; }
        public int LeftFeedCount { get; set; }

        public FeedQuitCommand(byte result, int leftFeedCount)
        {
            Result = result;
            LeftFeedCount = leftFeedCount;
        }
    }
    
    public class FeedForceQuitCommand : Command
    {
        public override PacketType Type => PacketType.FeedForceQuitResponse;
        
        public int GgumtleId { get; set; }
        public int LeftFeedCount { get; set; }

        public FeedForceQuitCommand(int ggumtleId, int leftFeedCount)
        {
            GgumtleId = ggumtleId;
            LeftFeedCount = leftFeedCount;
        }
    }
}