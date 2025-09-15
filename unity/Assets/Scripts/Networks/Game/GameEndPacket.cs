using System.Collections.Generic;
using Network;
using Networks.Packets;

namespace Networks.Game
{
    public class PlayerResult
    {
        public long id;
        public int status;

        public PlayerResult(long id, int status)
        {
            this.id = id;
            this.status = status;
        }
    }

    public class GameEndCommand : Command
    {
        public override PacketType Type => PacketType.GameEnd;

        public byte result;
        public int playerSize;
        public List<PlayerResult> playerResults;

        public GameEndCommand(byte result, int playerSize, List<PlayerResult> playerResults)
        {
            this.result = result;
            this.playerSize = playerSize;
            this.playerResults = playerResults;
        }
    }
}
