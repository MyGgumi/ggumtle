using System;
using DotNetty.Buffers;
using Network;
using Networks.Packets;
using UnityEngine;

namespace Networks.Rooms
{
    public enum RoomJoinResult
    {
        Fail = 0,
        Success = 1
    }

    public class RoomJoinSend : Sendable
    {
        public override PacketType Type => PacketType.RoomJoin;

        public long RoomId { get; }

        public RoomJoinSend(long roomId)
        {
            RoomId = roomId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(8);

            try
            {
                buffer.WriteLong(RoomId);

                var bytes = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(bytes);

                return bytes;
            }
            finally
            {
                buffer.Release();
            }
        }
    }

    public class RoomJoinCommand : Command
    {
        public RoomJoinResult Result { get; set; }

        public RoomJoinCommand(int result)
        {
            Result = (RoomJoinResult)result;
        }

        public override PacketType Type => PacketType.RoomJoinResponse;

        public bool Success => Result == RoomJoinResult.Success;
    }

    public class GameStartCommand : Command
    {
        public override PacketType Type => PacketType.GameStart;
        
        public GameStartCommand()
        {
        }
    }
}