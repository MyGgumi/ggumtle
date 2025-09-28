using System.Collections.Generic;
using System.Numerics;
using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Game;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.ExitOpen)]
    public class ExitOpenFactory
    {
        public class ExitPacket
        {
            public int id;
            public int leftTopX;
            public int leftTopY;
            public int leftTopZ;
            public int rightBottomX;
            public int rightBottomY;
            public int rightBottomZ;

            public ExitPacket(
                int id,
                int leftTopX,
                int leftTopY,
                int leftTopZ,
                int rightBottomX,
                int rightBottomY,
                int rightBottomZ
            )
            {
                this.id = id;
                this.leftTopX = leftTopX;
                this.leftTopY = leftTopY;
                this.leftTopZ = leftTopZ;
                this.rightBottomX = rightBottomX;
                this.rightBottomY = rightBottomY;
                this.rightBottomZ = rightBottomZ;
            }
        }

        public static ExitOpenCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var count = buffer.ReadInt();
            var exits = new List<ExitPacket>();

            // count 개수만큼 List에 직접 추가
            for (int i = 0; i < count; i++)
            {
                int id = buffer.ReadInt();
                int leftTopX = buffer.ReadInt();
                int leftTopY = buffer.ReadInt();
                int leftTopZ = buffer.ReadInt();

                int rightBottomX = buffer.ReadInt();
                int rightBottomY = buffer.ReadInt();
                int rightBottomZ = buffer.ReadInt();

                exits.Add(
                    new ExitPacket(
                        id,
                        leftTopX,
                        leftTopY,
                        leftTopZ,
                        rightBottomX,
                        rightBottomY,
                        rightBottomZ
                    )
                );
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
