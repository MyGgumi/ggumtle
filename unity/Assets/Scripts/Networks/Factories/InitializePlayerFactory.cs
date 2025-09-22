using System.Collections.Generic;
using System.Text;
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

            int playerCount = buffer.ReadInt();

            List<PlayerPacket> players = new();
            for (int i = 0; i < playerCount; i++)
            {
                var playerId = buffer.ReadLong();

                var isMine = buffer.ReadBoolean();
                bool isMongging = buffer.ReadBoolean();

                var classId = buffer.ReadLong();

                int x = buffer.ReadInt();
                int y = buffer.ReadInt();
                int z = buffer.ReadInt();

                int maxHp     = buffer.ReadInt();
                int moveSpeed = buffer.ReadInt();
                int healSpeed = buffer.ReadInt();
                int workSpeed = buffer.ReadInt();

                int nickNameLength = buffer.ReadInt();
                byte[] nickNameBytes = new byte[nickNameLength];
                buffer.ReadBytes(nickNameBytes);
                string nickName = Encoding.UTF8.GetString(nickNameBytes);

                players.Add(new PlayerPacket(
                    id: playerId,
                    isMine: isMine,
                    isMongging: isMongging,
                    x: x, y: y, z: z,
                    classId: classId,
                    moveSpeed: moveSpeed,
                    maxHp: maxHp,
                    healSpeed: healSpeed,
                    workSpeed: workSpeed,
                    nickName: nickName
                ));
            }

            return new InitializePlayerCommand(players);
        }
    }

}