using Networks.Packets;
using Network;

namespace Networks.Ggumtle
{
    public enum Status : int
    {
        Buried = 1,
        Diging = 2,
        PullUp = 10,
        Fake = 11,
        Eating = 20,
        Disappear = 30,
    }

    public class GgumtleStatusCommand : Command
    {
        public override PacketType Type => PacketType.GgumtleState;

        public int ggumtleId;
        public Status state;

        public GgumtleStatusCommand(int ggumtleId, int state)
        {
            this.ggumtleId = ggumtleId;
            this.state = (Status)state;
        }
    }
}