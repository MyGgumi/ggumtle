using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Players
{
    public enum MonggingRevivalStartResult : byte
    {
        Fail = 0,              // 실패
        Success = 1,           // 성공
        PlayerNotFound = 2,    // 요청자나 타겟 플레이어를 찾을 수 없거나 몽깅이가 아님
        NotUnconscious = 10    // 타겟 플레이어가 기절 상태가 아님
    }

    public enum MonggingRevivalStopResult : byte
    {
        Fail = 0,              // 실패
        Success = 1            // 성공
    }

    public class MonggingRevivalStartSend : Sendable
    {
        public override PacketType Type => PacketType.MonggingRevivalStart;

        public long targetMonggingId;

        public MonggingRevivalStartSend(long targetMonggingId)
        {
            this.targetMonggingId = targetMonggingId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(8);
            
            buffer.WriteLong(targetMonggingId);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class MonggingRevivalStartCommand : Command
    {
        public override PacketType Type => PacketType.MonggingRevivalStartResponse;

        public MonggingRevivalStartResult Result { get; set; }

        public MonggingRevivalStartCommand(byte result)
        {
            this.Result = (MonggingRevivalStartResult)result;
        }
        
        public bool Success => Result == MonggingRevivalStartResult.Success;
    }

    public class MonggingRevivalCompleteCommand : Command
    {
        public override PacketType Type => PacketType.MonggingRevivalComplete;

        public long revivedMonggingId;

        public MonggingRevivalCompleteCommand(long revivedMonggingId)
        {
            this.revivedMonggingId = revivedMonggingId;
        }
    }

    public class MonggingRevivalStopSend : Sendable
    {
        public override PacketType Type => PacketType.MonggingRevivalStop;

        public MonggingRevivalStopSend()
        {
            // 요청은 패킷 바디가 없고 헤더만 보내면 됨
        }

        public override byte[] ToBytes()
        {
            // 빈 바이트 배열 반환 (헤더만 전송)
            return new byte[0];
        }
    }

    public class MonggingRevivalStopCommand : Command
    {
        public override PacketType Type => PacketType.MonggingRevivalStopResponse;

        public MonggingRevivalStopResult Result { get; set; }

        public MonggingRevivalStopCommand(byte result)
        {
            this.Result = (MonggingRevivalStopResult)result;
        }

        public bool Success => Result == MonggingRevivalStopResult.Success;
    }

    public enum UseDefibrillatorResult : byte
    {
        Fail = 0,              // 실패
        Success = 1,           // 성공
        PlayerNotFound = 2,    // 플레이어를 찾지 못하거나 몽깅이가 아님
        NotUnconscious = 3,    // 기절 상태가 아님
        NoDefibrillator = 4    // 제세동기 없음
    }

    public class UseDefibrillatorSend : Sendable
    {
        public override PacketType Type => PacketType.UseDefibrillator;

        public UseDefibrillatorSend()
        {
            // 요청은 패킷 바디가 없고 헤더만 보내면 됨
        }

        public override byte[] ToBytes()
        {
            // 빈 바이트 배열 반환 (헤더만 전송)
            return new byte[0];
        }
    }

    public class UseDefibrillatorCommand : Command
    {
        public override PacketType Type => PacketType.UseDefibrillatorResponse;

        public UseDefibrillatorResult Result { get; set; }
        public int Hp { get; set; }

        public UseDefibrillatorCommand(byte result, int hp)
        {
            this.Result = (UseDefibrillatorResult)result;
            this.Hp = hp;
        }

        public bool Success => Result == UseDefibrillatorResult.Success;
    }
}
