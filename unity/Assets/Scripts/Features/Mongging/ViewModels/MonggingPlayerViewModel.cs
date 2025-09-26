using System;
using Features.Mongging.Models;
using Features.Mongging.Services;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Mongging.ViewModels
{
    /// <summary>
    /// 개별 몽깅이 플레이어 ViewModel
    /// </summary>
    public class MonggingPlayerViewModel : IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<int> CurrentHp => _playerService.CurrentHp;
        public ReadOnlyReactiveProperty<int> MaxHp => _playerService.MaxHp;
        public ReadOnlyReactiveProperty<float> HealthPercentage => _playerService.HealthPercentage;
        public ReadOnlyReactiveProperty<MonggingPlayerState> CurrentState => _playerService.CurrentState;
        public ReadOnlyReactiveProperty<int> FaintCount => _playerService.FaintCount;
        public ReadOnlyReactiveProperty<bool> IsAlive => _playerService.IsAlive;
        public ReadOnlyReactiveProperty<bool> IsStunned => _playerService.IsStunned;
        public ReadOnlyReactiveProperty<bool> IsFrightened => _playerService.IsFrightened;
        public ReadOnlyReactiveProperty<float> StunRemainingTime => _playerService.StunRemainingTime;
        public ReadOnlyReactiveProperty<float> FrightenRemainingTime => _playerService.FrightenRemainingTime;

        #endregion

        #region Private Fields

        private IMonggingPlayerService _playerService;
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        public MonggingPlayerViewModel()
        {
            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingPlayerViewModel] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 플레이어 서비스 설정
        /// </summary>
        public void SetPlayerService(IMonggingPlayerService playerService)
        {
            _playerService = playerService;

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerViewModel] 플레이어 서비스 설정 완료");
            }
        }

        /// <summary>
        /// 플레이어 초기화
        /// </summary>
        public void Initialize(long playerId, string playerName, MonggingPlayerType playerType, bool isLocal = false)
        {
            _playerService.Initialize(playerId, playerName, playerType, isLocal);

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerViewModel] 플레이어 초기화: ID={playerId}, Name={playerName}, Type={playerType}, Local={isLocal}");
            }
        }

        /// <summary>
        /// 데미지 적용
        /// </summary>
        public void TakeDamage(int damage)
        {
            _playerService.TakeDamage(damage);
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(int healAmount)
        {
            _playerService.Heal(healAmount);
        }

        /// <summary>
        /// 감전 상태 적용
        /// </summary>
        public void ApplyStun(float duration = 2f)
        {
            _playerService.ApplyStun(duration);
        }

        /// <summary>
        /// 공포 상태 적용
        /// </summary>
        public void ApplyFrighten(float duration = 5f)
        {
            _playerService.ApplyFrighten(duration);
        }

        /// <summary>
        /// 상태이상 해제
        /// </summary>
        public void ClearStatusEffects()
        {
            _playerService.ClearStatusEffects();
        }

        /// <summary>
        /// 부활
        /// </summary>
        public void Revive(int reviveHp = 30)
        {
            _playerService.Revive(reviveHp);
        }

        /// <summary>
        /// 탈출 완료
        /// </summary>
        public void Escape()
        {
            _playerService.Escape();
        }

        /// <summary>
        /// 서버 상태로 동기화
        /// </summary>
        public void SyncFromServer(int hp, MonggingPlayerState state, int faintCount)
        {
            _playerService.SyncFromServer(hp, state, faintCount);
        }

        /// <summary>
        /// 플레이어 데이터 가져오기
        /// </summary>
        public MonggingPlayerData GetPlayerData()
        {
            return _playerService.GetPlayerData();
        }

        /// <summary>
        /// 상태이상 시간 업데이트
        /// </summary>
        public void UpdateStatusEffects(float deltaTime)
        {
            _playerService.UpdateStatusEffects(deltaTime);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Reset()
        {
            _playerService.Reset();
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _playerService?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingPlayerViewModel] Dispose 완료");
            }
        }

        #endregion
    }
}