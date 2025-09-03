using System;
using DotNetty.Buffers;
using Networks.Packets;

namespace Network
{
    /// <summary>
    /// IL2CPP 호환성을 고려한 패킷 헤더
    /// </summary>
    public class PacketHeader
    {
        public const int Size = 14; // const로 변경하여 IL2CPP 최적화
        
        public PacketType Type { get; private set; }
        public int DataLength { get; private set; }
        public long Timestamp { get; private set; }

        public PacketHeader(PacketType type, int dataLength, long timestamp)
        {
            Type = type;
            DataLength = dataLength;
            Timestamp = timestamp;
        }
        
        /// <summary>
        /// IL2CPP 호환성을 위한 정적 팩토리 메서드
        /// </summary>
        public static PacketHeader From(byte[] bytes)
        {
            if (bytes == null || bytes.Length < Size)
            {
                throw new ArgumentException("Invalid packet header data");
            }
            
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            try
            {
                var type = (PacketType) buffer.ReadShort();
                var dataLength = buffer.ReadInt();
                var timestamp = buffer.ReadLong();
                
                return new PacketHeader(type, dataLength, timestamp);
            }
            finally
            {
                buffer.Release();
            }
        }
    }
    
    /// <summary>
    /// IL2CPP 호환성을 고려한 패킷 클래스
    /// </summary>
    public class Packet
    {
        public PacketHeader Header { get; private set; }
        public byte[] Data { get; private set; }

        public Packet(PacketHeader header, byte[] data)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Data = data; // null 허용 (빈 패킷의 경우)
        }

        /// <summary>
        /// IL2CPP 최적화를 위한 바이트 변환
        /// </summary>
        public byte[] ToBytes()
        {
            // 패킷 전체 크기 계산
            var bufferSize = PacketHeader.Size + (Header.DataLength > 0 ? Header.DataLength : 0);
            
            var buffer = Unpooled.Buffer(bufferSize);

            try
            {
                // 헤더 변환
                buffer.WriteShort((short)Header.Type);
                buffer.WriteInt(Header.DataLength);
                buffer.WriteLong(Header.Timestamp);

                // 바디 변환
                if (Data != null && Data.Length > 0)
                {
                    buffer.WriteBytes(Data);
                }

                // byte 배열로 변환
                var bytes = new byte[buffer.ReadableBytes];
                buffer.ReadBytes(bytes);

                return bytes;
            }
            finally
            {
                buffer.Release();
            }
        }
    }

    /// <summary>
    /// IL2CPP 호환성을 고려한 전송 가능한 패킷 인터페이스
    /// </summary>
    public abstract class Sendable
    {
        public abstract PacketType Type { get; }
        public abstract byte[] ToBytes();
    }

    public abstract class Command
    {
        public abstract PacketType Type { get; }
    }
}