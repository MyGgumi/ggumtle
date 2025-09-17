using System;
using Network;
using Networks.Packets;

namespace Networks.Scenes
{
    public enum SceneChangeResult
    {
        Fail = 0,
        Success = 1
    }

    /// <summary>
    /// 씬 전환 완료 알림 패킷 (빈 내용)
    /// </summary>
    public class SceneChangeSend : Sendable
    {
        public SceneChangeSend()
        {
        }

        public override PacketType Type => PacketType.SceneChange;

        public override byte[] ToBytes()
        {
            // 빈 내용만 보냄
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// 씬 전환 응답 패킷 (int result만)
    /// </summary>
    public class SceneChangeCommand : Command
    {
        public SceneChangeResult Result { get; set; }
        public bool Success => Result == SceneChangeResult.Success;

        public SceneChangeCommand(int result)
        {
            Result = (SceneChangeResult)result;
        }

        public override PacketType Type => PacketType.SceneChangeResponse;
    }
}
