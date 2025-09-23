using Networks.Rooms.Domains;
using UnityEngine;

namespace Features.Map.Utils
{
    /// <summary>
    /// 네트워크 패킷 데이터를 Unity 타입으로 변환하는 유틸리티
    /// 서버 정수 좌표를 Unity 월드 좌표로 변환
    /// </summary>
    public static class PacketUtils
    {
        /// <summary>
        /// 패킷의 Position은 이미 Unity 좌표계로 변환되어 전송됨
        /// System.Numerics.Vector3를 UnityEngine.Vector3로 변환만 수행
        /// </summary>

        /// <summary>
        /// GgumtlePacket의 Position을 Unity Vector3로 변환
        /// System.Numerics.Vector3를 UnityEngine.Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this GgumtlePacket packet)
        {
            return new Vector3(
                packet.Position.X,
                packet.Position.Y,
                packet.Position.Z
            );
        }

        /// <summary>
        /// ChestPacket의 Position을 Unity Vector3로 변환
        /// System.Numerics.Vector3를 UnityEngine.Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this ChestPacket packet)
        {
            return new Vector3(
                packet.Position.X,
                packet.Position.Y,
                packet.Position.Z
            );
        }

        /// <summary>
        /// HealPackPacket의 Position을 Unity Vector3로 변환
        /// System.Numerics.Vector3를 UnityEngine.Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this HealPackPacket packet)
        {
            return new Vector3(
                packet.Position.X,
                packet.Position.Y,
                packet.Position.Z
            );
        }

        /// <summary>
        /// SpeedPackPacket의 Position을 Unity Vector3로 변환
        /// System.Numerics.Vector3를 UnityEngine.Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this SpeedPackPacket packet)
        {
            return new Vector3(
                packet.Position.X,
                packet.Position.Y,
                packet.Position.Z
            );
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