using System.Collections.Generic;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.Game
{
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

        public byte result;

        public ExitAttemptCommand(byte result)
        {
            this.result = result;
        }
    }
}
