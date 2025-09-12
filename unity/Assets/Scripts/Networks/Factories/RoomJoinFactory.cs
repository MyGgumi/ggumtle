using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Rooms;

namespace Networks.Factories
{
    [CommandFactory(PacketType.RoomJoinResponse)]
    public class RoomJoinFactory
    {
        public static RoomJoinCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            var result = buffer.ReadInt(); // 4byte int: 0=실패, 1=성공
            
            return new RoomJoinCommand(result);
        }
    }
}
