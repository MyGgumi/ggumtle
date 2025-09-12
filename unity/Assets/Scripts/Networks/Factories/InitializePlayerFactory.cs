using System.Collections.Generic;
using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Rooms;
using Networks.Rooms.Domains;

namespace Networks.Factories
{
    [CommandFactory(PacketType.InitializePlayerResponse)]
    public class InitializePlayerFactory
    {
        public static InitializePlayerCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var playerCount = buffer.ReadInt();

            List<PlayerPacket> players = new();
            for (int i = 0; i < playerCount; i++)
            {
                var playerId = buffer.ReadInt();
                var isMine = buffer.ReadBoolean();
                var isMongging = buffer.ReadBoolean();
                
                var x = buffer.ReadInt();
                var y = buffer.ReadInt();
                var z = buffer.ReadInt();
                
                var classId = buffer.ReadInt();
                
                var moveSpeed = buffer.ReadInt();
                var maxHp = buffer.ReadInt();
                var healSpeed = buffer.ReadInt();
                var workSpeed = buffer.ReadInt();
                
                players.Add(new PlayerPacket(playerId, isMine, isMongging, x, y, z, classId, moveSpeed, maxHp, healSpeed, workSpeed));
            }
            
            return new InitializePlayerCommand(players);
        }
    }
}