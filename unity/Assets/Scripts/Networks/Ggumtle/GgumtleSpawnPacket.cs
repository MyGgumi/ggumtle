using System.Numerics;
using Network;
using Networks.Packets;

namespace Networks.Ggumtle
{
    public class GgumtleSpawnCommand : Command
    {
        public const int Unit = 100;
        public override PacketType Type => PacketType.GgumtleSpawn;

        public int id;
        public Vector3 position;

        public GgumtleSpawnCommand(int id, int x, int y, int z)
        {
            this.id = id;
            
            // 100으로 나눠서 Vector3로 변환
            var xFloat = (float) x / Unit;
            var yFloat = (float) y / Unit;
            var zFloat = (float) z / Unit;
            
            position = new Vector3(xFloat, yFloat, zFloat);
        }
    }

    public class GgumtleNirvanaCommand : Command
    {
        public override PacketType Type => PacketType.GgumtleNirvana;

        public int id;

        public GgumtleNirvanaCommand(int id)
        {
            this.id = id;
        }
    }
}
