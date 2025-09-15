using System.Numerics;
using Network;
using Networks.Packets;

namespace Networks.Ggumtle
{
    public class GgumtleSpawnCommand : Command
    {
        public override PacketType Type => PacketType.GgumtleSpawn;

        public int id;
        public Vector3 position;

        public GgumtleSpawnCommand(int id, int x, int y, int z)
        {
            this.id = id;
            
            // 1000으로 나눠서 Vector3로 변환
            var xFloat = (float) x / 1000;
            var yFloat = (float) y / 1000;
            var zFloat = (float) z / 1000;
            
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
