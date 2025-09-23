using System.Numerics;
namespace Networks.Rooms.Domains
{
    public class GgumtlePacket
    {
        public const int Unit = 100;
        public int Id;
        public Vector3 Position;
        
        public GgumtlePacket(int id, int x, int y, int z)
        {
            Id = id;
            var xFloat = (float) x / Unit;
            var yFloat = (float) y / Unit;
            var zFloat = (float) z / Unit;
            Position = new Vector3(xFloat, yFloat, zFloat);
        }
    }
}