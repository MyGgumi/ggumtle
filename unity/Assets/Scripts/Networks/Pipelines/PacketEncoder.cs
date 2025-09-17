using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Network;
using UnityEngine;

namespace Networks.Pipelines
{
    /// <summary>
    /// IL2CPP 호환성을 고려한 패킷 인코더
    /// </summary>
    public class PacketEncoder : MessageToByteEncoder<Packet>
    {
        protected override void Encode(
            IChannelHandlerContext context,
            Packet packet,
            IByteBuffer output
        )
        {
            if (packet == null)
            {
                Debug.LogError("Cannot encode null packet");
                return;
            }

            try
            {
                var bytes = packet.ToBytes();
                output.WriteBytes(bytes);

                Debug.Log($"패킷 인코딩 완료 - Type: {packet.Header.Type}, Size: {bytes.Length}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"패킷 인코딩 오류: {e.Message}");
            }
        }
    }
}
