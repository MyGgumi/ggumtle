using Network;
using Networks.Packets;

namespace Networks.Players
{
    public class MonggingStateBroadcastCommand : Command
    {
        public override PacketType Type => PacketType.MonggingStateBroadcast;

        public long playerId;
        public int type;

        public MonggingStateBroadcastCommand(long playerId, int type)
        {
            this.playerId = playerId;
            this.type = type;
        }
    }
}
