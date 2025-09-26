using System;
using Features.Mongging.Models;
using R3;

namespace Features.Mongging.Services
{
    /// <summary>
    /// 개별 몽깅이 플레이어 관리 서비스 인터페이스
    /// </summary>
    public interface IMonggingPlayerService : IDisposable
    {
        #region Observable Properties

        /// <summary>
        /// 현재 체력
        /// </summary>
        ReadOnlyReactiveProperty<int> CurrentHp { get; }

        /// <summary>
        /// 최대 체력
        /// </summary>
        ReadOnlyReactiveProperty<int> MaxHp { get; }

        /// <summary>
        /// 체력 비율 (0.0 ~ 1.0)
        /// </summary>
        ReadOnlyReactiveProperty<float> HealthPercentage { get; }

        /// <summary>
        /// 현재 상태
        /// </summary>
        ReadOnlyReactiveProperty<MonggingPlayerState> CurrentState { get; }

        /// <summary>
        /// 기절 횟수
        /// </summary>
        ReadOnlyReactiveProperty<int> FaintCount { get; }

        /// <summary>
        /// 생존 여부
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsAlive { get; }

        /// <summary>
        /// 감전 상태
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsStunned { get; }

        /// <summary>
        /// 공포 상태
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsFrightened { get; }

        /// <summary>
        /// 감전 남은 시간
        /// </summary>
        ReadOnlyReactiveProperty<float> StunRemainingTime { get; }

        /// <summary>
        /// 공포 남은 시간
        /// </summary>
        ReadOnlyReactiveProperty<float> FrightenRemainingTime { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 플레이어 초기화
        /// </summary>
        void Initialize(long playerId, string playerName, MonggingPlayerType playerType, bool isLocal = false);

        /// <summary>
        /// 데미지 적용
        /// </summary>
        void TakeDamage(int damage);

        /// <summary>
        /// 체력 회복
        /// </summary>
        void Heal(int healAmount);

        /// <summary>
        /// 감전 상태 적용
        /// </summary>
        void ApplyStun(float duration = 2f);

        /// <summary>
        /// 공포 상태 적용
        /// </summary>
        void ApplyFrighten(float duration = 5f);

        /// <summary>
        /// 상태이상 해제
        /// </summary>
        void ClearStatusEffects();

        /// <summary>
        /// 부활
        /// </summary>
        void Revive(int reviveHp = 30);

        /// <summary>
        /// 탈출 완료
        /// </summary>
        void Escape();

        /// <summary>
        /// 서버 상태로 동기화
        /// </summary>
        void SyncFromServer(int hp, MonggingPlayerState state, int faintCount);

        /// <summary>
        /// 플레이어 데이터 가져오기
        /// </summary>
        MonggingPlayerData GetPlayerData();

        /// <summary>
        /// 상태이상 시간 업데이트 (Update에서 호출)
        /// </summary>
        void UpdateStatusEffects(float deltaTime);

        /// <summary>
        /// 초기화
        /// </summary>
        void Reset();

        #endregion
    }
}