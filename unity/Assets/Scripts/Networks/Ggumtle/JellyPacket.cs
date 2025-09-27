using System;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Ggumtle
{
    public enum JellyStartResult : int
    {
        Fail = 0,                           // 실패
        Start = 1,                          // 시작
        GgumtleNotFound = 2,                // ID에 대응하는 꿈틀이가 없음
        PlayerNotFoundOrNotMongging = 3,    // 플레이어가 없거나 몽깅이가 아님
        AlreadyPurified = 10,               // 꿈틀이가 이미 정화됨
        AlreadyPurifiedGgumtle = 11,        // 이미 정화된 꿈틀이
        NoJelly = 12,                       // 요청한 몽깅이에게 꿈젤리가 없음
        TooFar = 13                         // 요청한 몽깅이가 꿈틀이 근처에 없음
    }

    public enum JellyQuitResult : byte
    {
        Fail = 0,                           // 실패
        Success = 1,                        // 성공
        NotFeeding = 2                      // 먹이고 있지 않음
    }
    public class JellyStartSend : Sendable
    {
        public override PacketType Type => PacketType.JellyStart;
        
        public int GgumtleId { get; set; }
        
        public JellyStartSend(int ggumtleId)
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

    public class JellyStartCommand : Command
    {
        public override PacketType Type => PacketType.JellyStartResponse;
        
        public JellyStartResult Result { get; set; }
        
        public JellyStartCommand(int result)
        {
            Result = (JellyStartResult)result;
        }
        
        public bool Success => Result == JellyStartResult.Start;
    }
    
    public class JellyQuitSend : Sendable
    {
        public override PacketType Type => PacketType.JellyQuit;

        public JellyQuitSend()
        {
        }

        public override byte[] ToBytes() => Array.Empty<byte>();
    }
    
    public class JellyQuitCommand : Command
    {
        public override PacketType Type => PacketType.JellyQuitResponse;
        
        public JellyQuitResult Result { get; set; }
        public int LeftJellyCount { get; set; }

        public JellyQuitCommand(byte result, int leftJellyCount)
        {
            Result = (JellyQuitResult)result;
            LeftJellyCount = leftJellyCount;
        }
        
        public bool Success => Result == JellyQuitResult.Success;
    }
    
    public class JellyForceQuitCommand : Command
    {
        public override PacketType Type => PacketType.JellyForceQuitResponse;

        public int GgumtleId { get; set; }
        public int LeftJellyCount { get; set; }

        public JellyForceQuitCommand(int ggumtleId, int leftJellyCount)
        {
            GgumtleId = ggumtleId;
            LeftJellyCount = leftJellyCount;
        }
    }

    public class JellyCountCommand : Command
    {
        public override PacketType Type => PacketType.JellyCount;

        public int JellyCount { get; set; }

        public JellyCountCommand(int jellyCount)
        {
            JellyCount = jellyCount;
        }
    }

    /// <summary>
    /// 꿈틀이별 먹은 젤리 개수 업데이트 (서버 → 클라이언트)
    /// </summary>
    public class GgumtleJellyEatenCommand : Command
    {
        public override PacketType Type => PacketType.GgumtleJellyEaten;
        public int GgumtleId { get; set; }
        public int EatenCount { get; set; }

        public GgumtleJellyEatenCommand(int ggumtleId, int eatenCount)
        {
            GgumtleId = ggumtleId;
            EatenCount = eatenCount;
        }
    }
}