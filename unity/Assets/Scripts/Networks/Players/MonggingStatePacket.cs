using Network;
using Networks.Packets;

namespace Networks.Players
{
    public enum MonggingStateType
    {
        Normal = 1,
        Digging = 2,
        Feeding = 20,
        Knockout = 50,
        Dead = 70,
        Escape = 100,
        Stunned = 120,
    }
    
    public class MonggingStateBroadcastCommand : Command
    {
        public override PacketType Type => PacketType.MonggingStateBroadcast;

        public long playerId;
        public MonggingStateType type;

        public MonggingStateBroadcastCommand(long playerId, int type)
        {
            this.playerId = playerId;
            this.type = (MonggingStateType) type;
        }
    }
}