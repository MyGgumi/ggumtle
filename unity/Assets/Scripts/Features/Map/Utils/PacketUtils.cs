using Networks.Rooms.Domains;
using UnityEngine;

namespace Features.Map.Utils
{
    /// <summary>
    /// 네트워크 패킷 데이터를 Unity 타입으로 변환하는 유틸리티
    /// </summary>
    public static class PacketUtils
    {
        /// <summary>
        /// GgumtlePacket의 XYZ를 Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this GgumtlePacket packet)
        {
            return new Vector3(packet.X, packet.Y, packet.Z);
        }

        /// <summary>
        /// ChestPacket의 XYZ를 Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this ChestPacket packet)
        {
            return new Vector3(packet.X, packet.Y, packet.Z);
        }

        /// <summary>
        /// HealPackPacket의 XYZ를 Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this HealPackPacket packet)
        {
            return new Vector3(packet.X, packet.Y, packet.Z);
        }

        /// <summary>
        /// SpeedPackPacket의 XYZ를 Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this SpeedPackPacket packet)
        {
            return new Vector3(packet.X, packet.Y, packet.Z);
        }

        /// <summary>
        /// Vector3를 XYZ 정수로 변환 (필요시 사용)
        /// </summary>
        public static (int x, int y, int z) ToXYZ(this Vector3 vector)
        {
            return (Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
        }
    }
}