using System.Numerics;

namespace Networks.Rooms.Domains
{
    public class PlayerPacket
    {
        public const int Unit = 1000;
        public long Id;
        public bool IsMine;
        public bool IsMongging;
        public long ClassId;
        public Vector3 Position;
        public int MoveSpeed;
        public int MaxHp;
        public int HealSpeed;
        public int WorkSpeed;
        public string NickName;
        
        public PlayerPacket(long id, bool isMine, bool isMongging, int x, int y, int z, long classId, int moveSpeed, int maxHp, int healSpeed, int workSpeed, string nickName)
        {
            Id = id;
            IsMine = isMine;
            IsMongging = isMongging;
            ClassId = classId;
            
            var xFloat = (float) x / Unit;
            var yFloat = (float) y / Unit;
            var zFloat = (float) z / Unit;

            Position = new Vector3(xFloat, yFloat, zFloat);
            MoveSpeed = moveSpeed;
            MaxHp = maxHp;
            HealSpeed = healSpeed;
            WorkSpeed = workSpeed;
            NickName = nickName;
        }
    }
}