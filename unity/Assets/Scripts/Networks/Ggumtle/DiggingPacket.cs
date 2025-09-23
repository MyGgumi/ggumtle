using System;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Ggumtle
{
    public enum DiggingStartResult : byte
    {
        Fail = 0,                           // 실패
        Success = 1,                        // 성공
        GgumtleNotFound = 2,                // ID에 대응하는 꿈틀이가 없음
        PlayerNotFoundOrNotMongging = 3,    // 플레이어가 없거나 몽깅이가 아님
        AlreadyDug = 10,                    // 이미 판 꿈틀이
        TooFar = 11                         // 인근에 없음
    }

    public enum DiggingQuitResult : byte
    {
        Fail = 0,                           // 실패
        Success = 1,                        // 성공
        NotDigging = 2                      // 파고있지 않음
    }

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

        public DiggingStartResult Result { get; set; }

        public DiggingStartCommand(byte result)
        {
            Result = (DiggingStartResult)result;
        }
        
        public bool Success => Result == DiggingStartResult.Success;
    }

    public class DiggingQuitSend : Sendable
    {
        public override PacketType Type => PacketType.DiggingQuit;
        public override byte[] ToBytes() => Array.Empty<byte>();
    }

    public class DiggingQuitCommand : Command
    {
        public override PacketType Type => PacketType.DiggingQuitResponse;
        
        public DiggingQuitResult Result { get; set; }
        
        public DiggingQuitCommand(byte result)
        {
            Result = (DiggingQuitResult)result;
        }
        
        public bool Success => Result == DiggingQuitResult.Success;
    }

    public class DiggingDoneCommand : Command
    {
        public override PacketType Type => PacketType.DiggingDoneResponse;

        public int Id { get; set; }
        public bool IsRealGgumtle { get; set; }
        
        public DiggingDoneCommand(int id, bool isRealGgumtle)
        {
            Id = id;
            IsRealGgumtle = isRealGgumtle;
        }
    }
}