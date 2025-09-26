using Features.Mongdung.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Mongdung.NetworkSources
{
    /// <summary>
    /// 몽둥이 네트워크 소스 인터페이스
    /// 몽둥이 관련 서버 통신을 담당
    /// </summary>
    public interface IMongdungNetworkSource
    {
        /// <summary>
        /// Attack 액션을 서버에 전송
        /// </summary>
        /// <param name="position">액션 실행 위치</param>
        /// <param name="direction">액션 실행 방향</param>
        /// <param name="targetId">타겟 플레이어 ID (미적중 시 -1)</param>
        /// <returns>서버 응답</returns>
        UniTask<bool> SendAttackActionAsync(Vector3 position, Vector3 direction, long targetId = -1);

        /// <summary>
        /// TrapSetting 액션을 서버에 전송
        /// </summary>
        /// <param name="position">함정 설치 위치</param>
        /// <param name="direction">설치 방향</param>
        /// <returns>서버 응답</returns>
        UniTask<bool> SendTrapSettingActionAsync(Vector3 position, Vector3 direction);

        /// <summary>
        /// Frighten 액션을 서버에 전송
        /// </summary>
        /// <param name="position">위협 실행 위치</param>
        /// <param name="direction">위협 방향</param>
        /// <returns>서버 응답</returns>
        UniTask<bool> SendFrightenActionAsync(Vector3 position, Vector3 direction);

        /// <summary>
        /// 일반적인 몽둥이 액션을 서버에 전송
        /// </summary>
        /// <param name="actionType">액션 타입</param>
        /// <param name="position">실행 위치</param>
        /// <param name="direction">실행 방향</param>
        /// <param name="targetId">타겟 플레이어 ID (Attack 전용, 미적중 시 -1)</param>
        /// <returns>서버 응답</returns>
        UniTask<bool> SendMongdungActionAsync(MongdungActionType actionType, Vector3 position, Vector3 direction, long targetId = -1);

        /// <summary>
        /// 몽둥이 액션 취소 요청
        /// </summary>
        /// <param name="actionType">취소할 액션 타입</param>
        /// <returns>서버 응답</returns>
        UniTask<bool> CancelMongdungActionAsync(MongdungActionType actionType);

        /// <summary>
        /// 몽둥이 상태 업데이트를 서버에 전송
        /// </summary>
        /// <param name="state">현재 상태</param>
        /// <returns>서버 응답</returns>
        UniTask<bool> UpdateMongdungStateAsync(MongdungState state);
    }
}