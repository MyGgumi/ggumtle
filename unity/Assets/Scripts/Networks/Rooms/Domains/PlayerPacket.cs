namespace Networks.Rooms.Domains
{
    public class PlayerPacket
    {
        public int Id;
        public bool IsMine;
        public bool IsMongging;
        public int X;
        public int Y;
        public int Z;
        public int ClassId;
        public int MoveSpeed;
        public int MaxHp;
        public int HealSpeed;
        public int WorkSpeed;
        
        public PlayerPacket(int id, bool isMine, bool isMongging, int x, int y, int z, int classId, int moveSpeed, int maxHp, int healSpeed, int workSpeed)
        {
            Id = id;
            IsMine = isMine;
            IsMongging = isMongging;
            X = x;
            Y = y;
            Z = z;
            ClassId = classId;
            MoveSpeed = moveSpeed;
        }
    }
}