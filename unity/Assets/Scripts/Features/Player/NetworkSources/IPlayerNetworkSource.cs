using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Player.NetworkSources
{
    /// <summary>
    /// 플레이어 네트워크 통신을 담당하는 인터페이스
    /// </summary>
    public interface IPlayerNetworkSource
    {
        /// <summary>
        /// 플레이어 이동 정보를 서버로 전송
        /// </summary>
        /// <param name="position">플레이어 위치</param>
        /// <param name="direction">플레이어 이동 방향</param>
        /// <param name="isMoving">이동 중 여부</param>
        /// <param name="speed">이동 속도</param>
        /// <returns>전송 성공 여부</returns>
        UniTask<bool> SendPlayerMoveAsync(Vector3 position, Vector3 direction, bool isMoving, float speed);

        /// <summary>
        /// 플레이어 점프 정보를 서버로 전송
        /// </summary>
        /// <param name="isJumping">점프 중 여부</param>
        /// <returns>전송 성공 여부</returns>
        UniTask<bool> SendPlayerJumpAsync(bool isJumping);

        /// <summary>
        /// 네트워크 전송을 시작
        /// </summary>
        void StartNetworkTransmission();

        /// <summary>
        /// 네트워크 전송을 중지
        /// </summary>
        void StopNetworkTransmission();

        /// <summary>
        /// 네트워크 설정을 업데이트
        /// </summary>
        /// <param name="settings">새로운 설정</param>
        void UpdateSettings(PlayerNetworkSettings settings);
    }
}