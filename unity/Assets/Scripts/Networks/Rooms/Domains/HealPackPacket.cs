using System.Numerics;
namespace Networks.Rooms.Domains
{
    public class HealPackPacket
    {
        public const int Unit = 1000;
        public int Id;
        public Vector3 Position;
        
        public HealPackPacket(int id, int x, int y, int z)
        {
            Id = id;
            var xFloat = (float) x / Unit;
            var yFloat = (float) y / Unit;
            var zFloat = (float) z / Unit;

            Position = new Vector3(xFloat, yFloat, zFloat);
        }
    }
}