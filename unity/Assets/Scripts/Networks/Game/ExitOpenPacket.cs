using System.Collections.Generic;
using System.Numerics;
using DotNetty.Buffers;
using Network;
using Networks.Factories;
using Networks.Packets;

namespace Networks.Game
{
    public class Exit
    {
        public const int Unit = 100;
        public int id;

        public Vector3 LeftTop;
        public Vector3 RightBottom;

        public Exit(ExitOpenFactory.ExitPacket exitPacket)
        {
            this.id = exitPacket.id;

            var leftTopX = exitPacket.leftTopX;
            var leftTopY = exitPacket.leftTopY;
            var leftTopZ = exitPacket.leftTopZ;

            var rightBottomX = exitPacket.rightBottomX;
            var rightBottomY = exitPacket.rightBottomY;
            var rightBottomZ = exitPacket.rightBottomZ;

            var leftTopXFloat = (float)leftTopX / Unit;
            var leftTopYFloat = (float)leftTopY / Unit;
            var leftTopZFloat = (float)leftTopZ / Unit;

            var rightBottomXFloat = (float)rightBottomX / Unit;
            var rightBottomYFloat = (float)rightBottomY / Unit;
            var rightBottomZFloat = (float)rightBottomZ / Unit;

            LeftTop = new Vector3(leftTopXFloat, leftTopYFloat, leftTopZFloat);
            RightBottom = new Vector3(rightBottomXFloat, rightBottomYFloat, rightBottomZFloat);
        }
    }

    public enum ExitAttemptResult : byte
    {
        Fail = 0, // 실패
        Success = 1, // 성공
        IdNotFound = 2, // ID not found
        NotAtExitLocation = 10, // 탈출 위치에 없음
    }

    public class ExitOpenCommand : Command
    {
        public override PacketType Type => PacketType.ExitOpen;

        public int count;
        public List<Exit> exits;

        public ExitOpenCommand(int count, List<ExitOpenFactory.ExitPacket> exitPackets)
        {
            this.count = count;
            exits = new List<Exit>();

            foreach (var exitPacket in exitPackets)
            {
                exits.Add(new Exit(exitPacket));
            }
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
