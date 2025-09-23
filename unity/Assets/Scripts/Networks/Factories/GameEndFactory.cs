using System.Collections.Generic;
using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Game;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.GameEnd)]
    public class GameEndFactory
    {
        public static GameEndCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            var escapedMonggingCount = buffer.ReadInt();

            var playerSize = buffer.ReadInt();
            var playerResults = new List<PlayerResult>();
            
            // playerSize 개수만큼 (long id, int status, int coin) 쌍을 읽어서 List에 추가
            for (int i = 0; i < playerSize; i++)
            {
                var id = buffer.ReadLong();
                var status = buffer.ReadInt();
                var coin = buffer.ReadInt();

                playerResults.Add(new PlayerResult(id, status, coin));
            }

            return new GameEndCommand(result, escapedMonggingCount, playerResults);
        }
    }
}
