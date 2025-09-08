using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Network;
using UnityEngine;

namespace Networks.Pipelines
{
    /// <summary>
    /// IL2CPP 호환성을 고려한 패킷 디코더
    /// </summary>
    public class PacketDecoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            // 최소 헤더 크기 확인
            if (input.ReadableBytes < PacketHeader.Size) 
            {
                return;
            }

            input.MarkReaderIndex();

            try
            {
                // 헤더 읽기
                var headerBytes = new byte[PacketHeader.Size];
                input.ReadBytes(headerBytes);

                var header = PacketHeader.From(headerBytes);

                // 데이터 길이 확인
                if (input.ReadableBytes < header.DataLength)
                {
                    input.ResetReaderIndex();
                    return;
                }
                
                // 데이터 읽기
                byte[] data = null;
                if (header.DataLength > 0)
                {
                    data = new byte[header.DataLength];
                    input.ReadBytes(data);
                }

                // 패킷 생성 및 출력
                var packet = new Packet(header, data);
                output.Add(packet);
                
                Debug.Log($"패킷 디코딩 완료 - Type: {header.Type}, DataLength: {header.DataLength}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"패킷 디코딩 오류: {e.Message}");
                input.ResetReaderIndex();
            }
        }
    }
}