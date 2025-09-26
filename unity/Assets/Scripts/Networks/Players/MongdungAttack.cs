using DotNetty.Buffers;
using Network;
using Networks.Packets;
using UnityEngine;

namespace Networks.Players
{
    public enum MongdungAttackResult
    {
        HitFail = 0, // 타격 실패
        HitSuccess = 1, // 타격 성공
        PlayerNotFound = 2, // 플레이어 조회 실패
        NotMongdung = 3, // 요청자가 몽둥이가 아님
        TargetNotFound = 4, // 타겟을 찾을 수 없음
    }

    public enum MongdungSkillResult : byte
    {
        Success = 1, // 성공
        PlayerNotFound = 2, // 몽둥이가 아니거나 찾지 못함
        SkillNotFound = 3, // ID에 대응하는 스킬을 찾지 못함
        CooldownLimit = 10, // 쿨타임 제한
        CountLimit = 11, // 개수 제한
    }

    public class MongdungAttackSend : Sendable
    {
        public const int Unit = 100;
        public override PacketType Type => PacketType.MongdungAttack;

        public int vx;
        public int vy;
        public int vz;
        public long targetId;

        public MongdungAttackSend(Vector3 direction, long targetId)
        {
            this.vx = (int)(direction.x * Unit);
            this.vy = (int)(direction.y * Unit);
            this.vz = (int)(direction.z * Unit);
            this.targetId = targetId;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(20);

            buffer.WriteInt(vx);
            buffer.WriteInt(vy);
            buffer.WriteInt(vz);
            buffer.WriteLong(targetId);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class MongdungAttackCommand : Command
    {
        public override PacketType Type => PacketType.MongdungAttackResponse;

        public MongdungAttackResult Result { get; set; }
        public long targetId;
        public int leftHp;

        public MongdungAttackCommand(int result, long targetId, int leftHp)
        {
            this.Result = (MongdungAttackResult)result;
            this.targetId = targetId;
            this.leftHp = leftHp;
        }

        public bool Success => Result == MongdungAttackResult.HitSuccess;
    }

    public class MongdungSkillSend : Sendable
    {
        public override PacketType Type => PacketType.MongdungSkill;

        public int skillType;

        public MongdungSkillSend(int skillType)
        {
            this.skillType = skillType;
        }

        public override byte[] ToBytes()
        {
            var buffer = Unpooled.Buffer(4);

            buffer.WriteInt(skillType);

            var bytes = new byte[buffer.ReadableBytes];
            buffer.ReadBytes(bytes);

            return bytes;
        }
    }

    public class MongdungSkillCommand : Command
    {
        public override PacketType Type => PacketType.MongdungSkillResponse;

        public int skillType;
        public MongdungSkillResult Result { get; set; }

        public MongdungSkillCommand(int skillType, byte result)
        {
            this.skillType = skillType;
            this.Result = (MongdungSkillResult)result;
        }

        public bool Success => Result == MongdungSkillResult.Success;
    }
}
