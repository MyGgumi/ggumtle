namespace Networks.Rooms.Domains
{
    public class ChestPacket
    {
        public int Id;
        public int X;
        public int Y;
        public int Z;

        public ChestPacket(int id, int x, int y, int z)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
        }
    }
}