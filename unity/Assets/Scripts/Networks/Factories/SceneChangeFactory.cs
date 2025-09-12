using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Scenes;

namespace Networks.Factories
{
    [CommandFactory(PacketType.SceneChangeResponse)]
    public class SceneChangeFactory
    {
        public static SceneChangeCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            // 결과 (4byte int: 0=실패, 1=성공)
            var result = buffer.ReadInt();
            
            return new SceneChangeCommand(result);
        }
    }
}
