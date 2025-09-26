using System;
using UnityEngine;

namespace Features.Mongging.Services
{
    /// <summary>
    /// 몽깅이 액션 서비스 인터페이스 (구조만 - 추후 구현)
    /// 아이템 사용 및 부활 액션 관리
    /// </summary>
    public interface IMonggingActionService : IDisposable
    {
        #region Item Actions (TODO - Inventory 연동 대기)

        /// <summary>
        /// 테이저건 사용
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="targetId">대상 ID</param>
        /// <param name="usePosition">사용 위치</param>
        bool UseTaserGun(long userId, long targetId, Vector3 usePosition);

        /// <summary>
        /// 섬광탄 사용
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="usePosition">사용 위치</param>
        bool UseFlashBang(long userId, Vector3 usePosition);

        /// <summary>
        /// 자가제세동기 사용
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        bool UseSelfDefib(long userId);

        /// <summary>
        /// 아이템 사용 가능 여부 확인
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="itemType">아이템 타입</param>
        bool CanUseItem(long userId, string itemType);

        #endregion

        #region Revival Actions (TODO - 추후 구현)

        /// <summary>
        /// 부활 시작
        /// </summary>
        /// <param name="revivingPlayerId">부활을 시도하는 플레이어 ID</param>
        /// <param name="targetPlayerId">부활 대상 플레이어 ID</param>
        bool StartRevival(long revivingPlayerId, long targetPlayerId);

        /// <summary>
        /// 부활 취소
        /// </summary>
        /// <param name="revivingPlayerId">부활을 시도하는 플레이어 ID</param>
        bool CancelRevival(long revivingPlayerId);

        /// <summary>
        /// 부활 진행률 업데이트
        /// </summary>
        /// <param name="revivingPlayerId">부활을 시도하는 플레이어 ID</param>
        /// <param name="progress">진행률 (0.0 ~ 1.0)</param>
        void UpdateRevivalProgress(long revivingPlayerId, float progress);

        /// <summary>
        /// 부활 가능 여부 확인
        /// </summary>
        /// <param name="revivingPlayerId">부활을 시도하는 플레이어 ID</param>
        /// <param name="targetPlayerId">부활 대상 플레이어 ID</param>
        bool CanRevive(long revivingPlayerId, long targetPlayerId);

        /// <summary>
        /// 부활 범위 내 확인
        /// </summary>
        /// <param name="revivingPlayerId">부활을 시도하는 플레이어 ID</param>
        /// <param name="targetPlayerId">부활 대상 플레이어 ID</param>
        bool IsInRevivalRange(long revivingPlayerId, long targetPlayerId);

        #endregion

        #region Interaction System (TODO - 추후 구현)

        /// <summary>
        /// 상호작용 시작
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="interactionType">상호작용 타입</param>
        /// <param name="targetId">대상 ID</param>
        bool StartInteraction(long playerId, string interactionType, long targetId);

        /// <summary>
        /// 상호작용 종료
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        bool EndInteraction(long playerId);

        /// <summary>
        /// 현재 상호작용 중인지 확인
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        bool IsInteracting(long playerId);

        #endregion
    }
}