using System.Collections.Generic;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Game
{
    public enum ExitAttemptResult : byte
    {
        Fail = 0,                           // 실패
        Success = 1,                        // 성공
        IdNotFound = 2,                     // ID not found
        NotAtExitLocation = 10              // 탈출 위치에 없음
    }
    public class ExitOpenCommand : Command
    {
        public override PacketType Type => PacketType.ExitOpen;

        public int count;
        public List<int> exits;

        public ExitOpenCommand(int count, List<int> exits)
        {
            this.count = count;
            this.exits = exits;
        }
    }

    public class ExitAttemptSend : Sendable
    {
        public override PacketType Type => PacketType.ExitAttempt;

        public int exitId;

        public ExitAttemptSend(int exitId)
        {
            this.exitId = exitId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(4);
            
            buffer.WriteInt(exitId);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class ExitAttemptCommand : Command
    {
        public override PacketType Type => PacketType.ExitAttemptResponse;

        public ExitAttemptResult Result { get; set; }

        public ExitAttemptCommand(byte result)
        {
            this.Result = (ExitAttemptResult)result;
        }
        
        public bool Success => Result == ExitAttemptResult.Success;
    }
}
