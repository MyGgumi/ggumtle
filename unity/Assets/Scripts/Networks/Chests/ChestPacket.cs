using System.Collections.Generic;
using DotNetty.Buffers;
using Network;
using Networks.Packets;

namespace Networks.chests
{
    public enum ChestOpenResult
    {
        Fail = 0,
        Success = 1
    }

    public enum ChestCloseResult
    {
        Fail = 0,
        Success = 1,
        NotWatching = 2
    }

    public class ChestOpenSend : Sendable
    {
        public override PacketType Type => PacketType.ChestOpen;
        
        public int ChestId { get; set; }
        
        public ChestOpenSend(int chestId)
        {
            ChestId = chestId;
        }
        
        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(4);
            
            buffer.WriteInt(ChestId);
            
            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);
            
            return bytes;
        }
    }

    public class ChestOpenCommand : Command
    {
        public override PacketType Type => PacketType.ChestOpenResponse;
        
        public ChestOpenResult Result { get; set; }
        public int ChestId { get; set; }
        public int ItemSize { get; set; }
        public List<int> Items { get; set; }
        
        public ChestOpenCommand(int result, int chestId, int itemSize, List<int> items)
        {
            Result = (ChestOpenResult)result;
            ChestId = chestId;
            ItemSize = itemSize;
            Items = items;
        }
        
        public bool Success => Result == ChestOpenResult.Success;
    }

    public class ChestCloseSend : Sendable
    {
        public override PacketType Type => PacketType.ChestClose;

        public int ChestId { get; set; }

        public ChestCloseSend(int chestId)
        {
            ChestId = chestId;
        }
        
        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(4);
            
            buffer.WriteInt(ChestId);
            
            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);
            
            return bytes;
        }
    }

    public class ChestCloseCommand : Command
    {
        public override PacketType Type => PacketType.ChestCloseResponse;

        public ChestCloseResult Result { get; set; }

        public ChestCloseCommand(int result)
        {
            Result = (ChestCloseResult)result;
        }
        
        public bool Success => Result == ChestCloseResult.Success;
    }
}