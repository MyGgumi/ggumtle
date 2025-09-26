using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;

namespace Networks.Factories
{
    [CommandFactory(PacketType.MongdungAttackResponse)]
    public class MongdungAttackFactory
    {
        public static MongdungAttackCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadInt();
            var targetId = buffer.ReadLong();
            var leftHp = buffer.ReadInt();

            return new MongdungAttackCommand(result, targetId, leftHp);
        }
    }

    [CommandFactory(PacketType.MongdungSkillResponse)]
    public class MongdungSkillFactory
    {
        public static MongdungSkillCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var skillType = buffer.ReadInt();
            var result = buffer.ReadByte();

            return new MongdungSkillCommand(skillType, result);
        }
    }
}
