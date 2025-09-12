namespace Networks.Rooms.Domains
{
    public class SpeedPackPacket
    {
        public int Id;
        public int X;
        public int Y;
        public int Z;
        
        public SpeedPackPacket(int id, int x, int y, int z)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
        }
    }
}