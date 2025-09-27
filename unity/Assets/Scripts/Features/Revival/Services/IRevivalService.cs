using System;
using Cysharp.Threading.Tasks;
using Features.Revival.Models;
using R3;

namespace Features.Revival.Services
{
    /// <summary>
    /// 부활 서비스 인터페이스
    /// 직접 부활 및 자가제세동기 부활 로직 담당
    /// </summary>
    public interface IRevivalService : IDisposable
    {
        #region Observable Properties

        /// <summary>
        /// 현재 직접 부활 진행 상태
        /// </summary>
        ReadOnlyReactiveProperty<RevivalProgressData> CurrentDirectRevival { get; }

        /// <summary>
        /// 현재 자가제세동기 부활 진행 상태
        /// </summary>
        ReadOnlyReactiveProperty<RevivalProgressData> CurrentSelfDefibRevival { get; }

        /// <summary>
        /// 부활 상호작용 상태
        /// </summary>
        ReadOnlyReactiveProperty<RevivalInteractionData> InteractionState { get; }

        /// <summary>
        /// 직접 부활 진행 중인지
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsDirectReviving { get; }

        /// <summary>
        /// 자가제세동기 부활 진행 중인지
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsSelfDefibReviving { get; }

        #endregion

        #region Direct Revival Methods

        /// <summary>
        /// 직접 부활 시작
        /// </summary>
        /// <param name="revivingPlayerId">부활시키는 플레이어 ID</param>
        /// <param name="targetPlayerId">부활 대상 플레이어 ID</param>
        /// <returns>시작 성공 여부</returns>
        UniTask<bool> StartDirectRevivalAsync(long revivingPlayerId, long targetPlayerId);

        /// <summary>
        /// 직접 부활 취소
        /// </summary>
        /// <param name="revivingPlayerId">부활시키는 플레이어 ID</param>
        /// <returns>취소 성공 여부</returns>
        UniTask<bool> CancelDirectRevivalAsync(long revivingPlayerId);

        /// <summary>
        /// 직접 부활 가능 여부 확인
        /// </summary>
        /// <param name="revivingPlayerId">부활시키는 플레이어 ID</param>
        /// <param name="targetPlayerId">부활 대상 플레이어 ID</param>
        /// <returns>부활 가능 여부</returns>
        bool CanStartDirectRevival(long revivingPlayerId, long targetPlayerId);

        #endregion

        #region Self Defibrillator Methods

        /// <summary>
        /// 자가제세동기 부활 시작
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="reviveHp">부활 시 체력값 (-1이면 기본값 사용)</param>
        /// <returns>시작 성공 여부</returns>
        bool StartSelfDefibrillatorRevival(long playerId, int reviveHp = -1);

        /// <summary>
        /// 자가제세동기 부활 가능 여부 확인
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <returns>부활 가능 여부</returns>
        bool CanStartSelfDefibRevival(long playerId);

        #endregion

        #region Interaction Methods

        /// <summary>
        /// 부활 상호작용 상태 업데이트
        /// </summary>
        /// <param name="targetPlayerId">대상 플레이어 ID</param>
        /// <param name="isInRange">범위 내 여부</param>
        /// <param name="canRevive">부활 가능 여부</param>
        /// <param name="distance">거리</param>
        /// <param name="targetPosition">대상 위치</param>
        void UpdateInteractionState(long targetPlayerId, bool isInRange, bool canRevive, float distance, UnityEngine.Vector3 targetPosition);

        /// <summary>
        /// 상호작용 상태 초기화
        /// </summary>
        void ClearInteractionState();

        #endregion

        #region Utility Methods

        /// <summary>
        /// 현재 진행 중인 부활이 있는지 확인
        /// </summary>
        /// <returns>부활 진행 중 여부</returns>
        bool IsAnyRevivalInProgress();

        /// <summary>
        /// 모든 부활 상태 초기화
        /// </summary>
        void ResetAllRevivalStates();

        #endregion
    }
}