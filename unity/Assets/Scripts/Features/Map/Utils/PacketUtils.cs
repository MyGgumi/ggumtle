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
        /// 서버 좌표를 Unity 월드 좌표로 변환하는 스케일
        /// 서버에서 이미 Unity 범위 좌표로 전송하도록 변경됨
        /// </summary>
        private const float COORDINATE_SCALE = 0.01f; // 1/100 스케일로 변환
        private const float Y_OFFSET = 0f; // Y축 오프셋 제거 (서버에서 직접 전송)

        /// <summary>
        /// GgumtlePacket의 XYZ를 Unity Vector3로 변환
        /// 서버 정수 좌표를 Unity 월드 좌표로 스케일 변환
        /// </summary>
        public static Vector3 ToVector3(this GgumtlePacket packet)
        {
            return new Vector3(
                packet.X * COORDINATE_SCALE,
                packet.Y * COORDINATE_SCALE + Y_OFFSET, // Y축에 기본 높이 추가
                packet.Z * COORDINATE_SCALE
            );
        }

        /// <summary>
        /// ChestPacket의 XYZ를 Unity Vector3로 변환
        /// 서버 정수 좌표를 Unity 월드 좌표로 스케일 변환
        /// </summary>
        public static Vector3 ToVector3(this ChestPacket packet)
        {
            return new Vector3(
                packet.X * COORDINATE_SCALE,
                packet.Y * COORDINATE_SCALE + Y_OFFSET,
                packet.Z * COORDINATE_SCALE
            );
        }

        /// <summary>
        /// HealPackPacket의 XYZ를 Unity Vector3로 변환
        /// 서버 정수 좌표를 Unity 월드 좌표로 스케일 변환
        /// </summary>
        public static Vector3 ToVector3(this HealPackPacket packet)
        {
            return new Vector3(
                packet.X * COORDINATE_SCALE,
                packet.Y * COORDINATE_SCALE + Y_OFFSET,
                packet.Z * COORDINATE_SCALE
            );
        }

        /// <summary>
        /// SpeedPackPacket의 XYZ를 Unity Vector3로 변환
        /// 서버 정수 좌표를 Unity 월드 좌표로 스케일 변환
        /// </summary>
        public static Vector3 ToVector3(this SpeedPackPacket packet)
        {
            return new Vector3(
                packet.X * COORDINATE_SCALE,
                packet.Y * COORDINATE_SCALE + Y_OFFSET,
                packet.Z * COORDINATE_SCALE
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