using System.Collections.Generic;
using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Game;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.ExitOpen)]
    public class ExitOpenFactory
    {
        public static ExitOpenCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var count = buffer.ReadInt();
            var exits = new List<int>();
            
            // count 개수만큼 List에 직접 추가
            for (int i = 0; i < count; i++)
            {
                exits.Add(buffer.ReadInt());
            }

            return new ExitOpenCommand(count, exits);
        }
    }

    [CommandFactory(PacketType.ExitAttemptResponse)]
    public class ExitAttemptFactory
    {
        public static ExitAttemptCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();

            return new ExitAttemptCommand(result);
        }
    }
}
