using Networks.Rooms.Domains;
using UnityEngine;

namespace Features.Map.Utils
{
    /// <summary>
    /// 플레이어 패킷 데이터를 Unity 타입으로 변환하는 유틸리티
    /// 서버 플레이어 데이터를 Unity 월드 좌표 및 게임 데이터로 변환
    /// </summary>
    public static class PlayerPacketUtils
    {
        /// <summary>
        /// PlayerPacket의 Position을 Unity Vector3로 변환
        /// System.Numerics.Vector3를 UnityEngine.Vector3로 변환
        /// </summary>
        public static Vector3 ToVector3(this PlayerPacket packet)
        {
            return new Vector3(
                packet.Position.X,
                packet.Position.Y,
                packet.Position.Z
            );
        }

        /// <summary>
        /// PlayerPacket이 Mongging 타입인지 확인
        /// </summary>
        public static bool IsMongging(this PlayerPacket packet)
        {
            return packet.IsMongging;
        }

        /// <summary>
        /// PlayerPacket이 Mongdung 타입인지 확인
        /// </summary>
        public static bool IsMongdung(this PlayerPacket packet)
        {
            return !packet.IsMongging;
        }

        /// <summary>
        /// PlayerPacket이 로컬 플레이어인지 확인
        /// </summary>
        public static bool IsLocalPlayer(this PlayerPacket packet)
        {
            return packet.IsMine;
        }

        /// <summary>
        /// PlayerPacket의 타입에 따른 Addressable 키 반환
        /// </summary>
        public static string GetPrefabKey(this PlayerPacket packet)
        {
            return packet.IsMongging ? "Mongging" : "Mongdung";
        }
    }
}