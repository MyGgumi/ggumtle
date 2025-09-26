using Features.Mongdung.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Mongdung.Services
{
    /// <summary>
    /// 몽둥이 서비스 인터페이스
    /// 몽둥이 관련 비즈니스 로직을 담당
    /// </summary>
    public interface IMongdungService
    {
        /// <summary>
        /// 몽둥이 액션 실행
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="actionType">액션 타입</param>
        /// <param name="position">실행 위치</param>
        /// <param name="direction">실행 방향</param>
        /// <param name="targetId">타겟 플레이어 ID (Attack 전용, 미적중 시 -1)</param>
        /// <returns>실행 성공 여부</returns>
        UniTask<bool> ExecuteActionAsync(long playerId, MongdungActionType actionType, Vector3 position, Vector3 direction, long targetId = -1);

        /// <summary>
        /// 몽둥이 액션 취소
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="actionType">취소할 액션 타입</param>
        /// <returns>취소 성공 여부</returns>
        UniTask<bool> CancelActionAsync(long playerId, MongdungActionType actionType);

        /// <summary>
        /// 특정 액션이 실행 가능한지 확인
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="actionType">액션 타입</param>
        /// <returns>실행 가능 여부</returns>
        bool CanExecuteAction(long playerId, MongdungActionType actionType);

        /// <summary>
        /// 몽둥이 상태 업데이트
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="newState">새로운 상태</param>
        /// <returns>업데이트 성공 여부</returns>
        UniTask<bool> UpdateStateAsync(long playerId, MongdungState newState);

        /// <summary>
        /// 플레이어의 현재 몽둥이 상태 가져오기
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <returns>현재 상태</returns>
        MongdungState GetCurrentState(long playerId);

        /// <summary>
        /// 특정 액션의 남은 쿨다운 시간 가져오기
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="actionType">액션 타입</param>
        /// <returns>남은 쿨다운 시간 (초)</returns>
        float GetRemainingCooldown(long playerId, MongdungActionType actionType);

        /// <summary>
        /// 몽둥이 플레이어 등록
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="playerName">플레이어 이름</param>
        void RegisterMongdungPlayer(long playerId, string playerName);

        /// <summary>
        /// 몽둥이 플레이어 해제
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        void UnregisterMongdungPlayer(long playerId);
    }
}