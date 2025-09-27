using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Ggumtle;
using Networks.Packets;

namespace Networks.Factories
{
    [CommandFactory(PacketType.JellyStartResponse)]
    public class JellyStartFactory
    {
        public static JellyStartCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();

            UnityEngine.Debug.Log($"[JellyStartFactory] 서버 응답 - result={result}, JellyStartResult={(JellyStartResult)result}");

            return new JellyStartCommand(result);
        }
    }
    
    [CommandFactory(PacketType.JellyQuitResponse)]
    public class JellyQuitFactory
    {
        public static JellyQuitCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var result = buffer.ReadByte();
            var leftJellyCount = buffer.ReadInt();

            return new JellyQuitCommand(result, leftJellyCount);
        }
    }
    
    [CommandFactory(PacketType.JellyForceQuitResponse)]
    public class JellyForceQuitFactory
    {
        public static JellyForceQuitCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var ggumtleId = buffer.ReadInt();
            var leftJellyCount = buffer.ReadInt();

            return new JellyForceQuitCommand(ggumtleId, leftJellyCount);
        }
    }

    [CommandFactory(PacketType.JellyCount)]
    public class JellyCountFactory
    {
        public static JellyCountCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var jellyCount = buffer.ReadInt();

            UnityEngine.Debug.Log($"[JellyCountFactory] 서버에서 젤리 개수 수신 - JellyCount: {jellyCount}");

            return new JellyCountCommand(jellyCount);
        }
    }

    [CommandFactory(PacketType.GgumtleJellyEaten)]
    public class GgumtleJellyEatenFactory
    {
        public static GgumtleJellyEatenCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);

            var ggumtleId = buffer.ReadInt();
            var eatenCount = buffer.ReadInt();

            UnityEngine.Debug.Log($"[GgumtleJellyEatenFactory] 꿈틀이 먹은 젤리 개수 수신 - GgumtleId: {ggumtleId}, EatenCount: {eatenCount}");

            return new GgumtleJellyEatenCommand(ggumtleId, eatenCount);
        }
    }
}