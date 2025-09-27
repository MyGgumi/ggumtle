using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;

namespace Networks.Factories
{
    [CommandFactory(PacketType.MonggingRevivalStartResponse)]
    public class MonggingRevivalStartFactory
    {
        public static MonggingRevivalStartCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();

            return new MonggingRevivalStartCommand(result);
        }
    }

    [CommandFactory(PacketType.MonggingRevivalComplete)]
    public class MonggingRevivalCompleteFactory
    {
        public static MonggingRevivalCompleteCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var revivedMonggingId = buffer.ReadLong();

            return new MonggingRevivalCompleteCommand(revivedMonggingId);
        }
    }

    [CommandFactory(PacketType.MonggingRevivalStopResponse)]
    public class MonggingRevivalStopFactory
    {
        public static MonggingRevivalStopCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();

            return new MonggingRevivalStopCommand(result);
        }
    }

    [CommandFactory(PacketType.UseDefibrillatorResponse)]
    public class UseDefibrillatorFactory
    {
        public static UseDefibrillatorCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            var hp = buffer.ReadInt();

            return new UseDefibrillatorCommand(result, hp);
        }
    }
}
