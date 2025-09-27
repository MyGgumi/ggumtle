using System;
using Features.PlayerHealth.Messages;
using Features.PlayerHealth.Models;
using Features.PlayerHealth.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.PlayerHealth.ViewModels
{
    public class PlayerHealthViewModel : IDisposable
    {
        #region Observable Properties

        public readonly ReadOnlyReactiveProperty<int> CurrentHp;
        public readonly ReadOnlyReactiveProperty<int> MaxHp;
        public readonly ReadOnlyReactiveProperty<float> HealthPercentage;
        public readonly ReadOnlyReactiveProperty<PlayerState> CurrentState;
        public readonly ReadOnlyReactiveProperty<bool> IsFainted;
        public readonly ReadOnlyReactiveProperty<float> FaintTimeRemaining;
        public readonly ReadOnlyReactiveProperty<bool> IsAlive;
        public readonly ReadOnlyReactiveProperty<bool> IsDead;
        public readonly ReadOnlyReactiveProperty<bool> IsLowHealth;
        public readonly ReadOnlyReactiveProperty<bool> IsCriticalHealth;
        public readonly ReadOnlyReactiveProperty<HealthBarData> Bar1;
        public readonly ReadOnlyReactiveProperty<HealthBarData> Bar2;
        public readonly ReadOnlyReactiveProperty<HealthBarData> Bar3;
        public readonly ReadOnlyReactiveProperty<float> FaintReviveTime;
        public readonly ReadOnlyReactiveProperty<bool> EnableFaintSystem;
        public readonly ReadOnlyReactiveProperty<Color> NormalBarColor;
        public readonly ReadOnlyReactiveProperty<Color> CriticalBarColor;
        public readonly ReadOnlyReactiveProperty<Color> ThirdBarColor;

        public readonly ReadOnlyReactiveProperty<bool> ShouldShowHealthWarning;
        public readonly ReadOnlyReactiveProperty<bool> ShouldShowCriticalState;
        public readonly ReadOnlyReactiveProperty<bool> ShouldShowFaintTimer;
        public readonly ReadOnlyReactiveProperty<string> FaintTimerText;
        public readonly ReadOnlyReactiveProperty<string> HealthText;
        public readonly ReadOnlyReactiveProperty<string> StateText;

        #endregion

        #region Dependencies

        private readonly IPlayerHealthService _playerHealthService;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public PlayerHealthViewModel(
            IPlayerHealthService playerHealthService
        )
        {
            _playerHealthService = playerHealthService;

            CurrentHp = _playerHealthService.CurrentHp;
            MaxHp = _playerHealthService.MaxHp;
            HealthPercentage = _playerHealthService.HealthPercentage;
            CurrentState = _playerHealthService.CurrentState;
            IsFainted = _playerHealthService.IsFainted;
            FaintTimeRemaining = _playerHealthService.FaintTimeRemaining;
            IsAlive = _playerHealthService.IsAlive;
            IsDead = _playerHealthService.IsDead;
            IsLowHealth = _playerHealthService.IsLowHealth;
            IsCriticalHealth = _playerHealthService.IsCriticalHealth;
            Bar1 = _playerHealthService.Bar1;
            Bar2 = _playerHealthService.Bar2;
            Bar3 = _playerHealthService.Bar3;
            FaintReviveTime = _playerHealthService.FaintReviveTime;
            EnableFaintSystem = _playerHealthService.EnableFaintSystem;
            NormalBarColor = _playerHealthService.NormalBarColor;
            CriticalBarColor = _playerHealthService.CriticalBarColor;
            ThirdBarColor = _playerHealthService.ThirdBarColor;

            ShouldShowHealthWarning = HealthPercentage
                .Select(percentage => percentage <= 0.3f && percentage > 0.1f)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            ShouldShowCriticalState = IsCriticalHealth
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            ShouldShowFaintTimer = IsFainted
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            FaintTimerText = FaintTimeRemaining
                .Select(time =>
                {
                    if (time <= 0) return "0초";
                    return $"{Mathf.Ceil(time)}초";
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            HealthText = CurrentHp.CombineLatest(MaxHp, (current, max) => $"{current}/{max}")
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            StateText = CurrentState
                .Select(state => state switch
                {
                    PlayerState.Normal => "정상",
                    PlayerState.LowHealth => "주의",
                    PlayerState.Critical => "위험",
                    PlayerState.Fainted => "기절",
                    PlayerState.Dead => "사망",
                    _ => "알 수 없음"
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            // 불필요한 메시지 구독 제거 - Service의 Observable로 충분함

            DebugLog("초기화 완료");
        }

        #endregion

        #region Public Methods

        public void DamageHealth(int damage)
        {
            _playerHealthService.DamageHealth(damage);
        }

        public void HealHealth(int healAmount)
        {
            _playerHealthService.HealHealth(healAmount);
        }

        public void SetHealth(int newHp, int newMaxHp = -1)
        {
            _playerHealthService.SetHealth(newHp, newMaxHp);
        }

        public void RevivePlayer(int reviveHp = 30)
        {
            _playerHealthService.RevivePlayer(reviveHp);
        }

        public void SetFaintState(bool fainted)
        {
            _playerHealthService.SetFaintState(fainted);
        }

        public void UpdateFaintTime(float deltaTime)
        {
            _playerHealthService.UpdateFaintTime(deltaTime);
        }

        public void SetBarColors(Color normal, Color critical, Color third)
        {
            _playerHealthService.SetBarColors(normal, critical, third);
        }

        public void SetHealthSettings(int maxHp, float faintReviveTime, bool enableFaintSystem)
        {
            _playerHealthService.SetHealthSettings(maxHp, faintReviveTime, enableFaintSystem);
        }

        public void Reset()
        {
            _playerHealthService.Reset();
        }

        public bool ShouldBeFainted()
        {
            return _playerHealthService.ShouldBeFainted();
        }

        public bool ShouldDie()
        {
            return _playerHealthService.ShouldDie();
        }

        public PlayerHealthData GetHealthData()
        {
            return _playerHealthService.GetHealthData();
        }

        public void LoadHealthData(PlayerHealthData data)
        {
            _playerHealthService.LoadHealthData(data);
        }

        #endregion

        #region Message Handlers - 제거됨

        // 불필요한 메시지 핸들러들 제거 - Service의 Observable Property로 충분히 처리 가능
        // ViewModel은 단순히 Service의 데이터를 View에게 전달하는 역할만 수행

        #endregion

        #region Private Methods

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PlayerHealthViewModel] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            DebugLog("Dispose 완료");
        }

        #endregion
    }
}