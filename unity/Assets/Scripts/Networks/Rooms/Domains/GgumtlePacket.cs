namespace Networks.Rooms.Domains
{
    public class GgumtlePacket
    {
        public int Id;
        public int X;
        public int Y;
        public int Z;
        
        public GgumtlePacket(int id, int x, int y, int z)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
        }
    }
}