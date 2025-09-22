using System.Collections.Generic;
using Network;
using Networks.Packets;

namespace Networks.Game
{
    public class PlayerResult
    {
        public long id;
        public int status;
        public int coin;

        public PlayerResult(long id, int status, int coin)
        {
            this.id = id;
            this.status = status;
            this.coin = coin;
        }
    }

    public class GameEndCommand : Command
    {
        public override PacketType Type => PacketType.GameEnd;

        public byte result;
        public int playerSize;
        public int escapedMonggingCount;
        public List<PlayerResult> playerResults;

        public GameEndCommand(byte result, int playerSize, int escapedMonggingCount, List<PlayerResult> playerResults)
        {
            this.result = result;
            this.playerSize = playerSize;
            this.escapedMonggingCount = escapedMonggingCount;
            this.playerResults = playerResults;
        }
    }
}
