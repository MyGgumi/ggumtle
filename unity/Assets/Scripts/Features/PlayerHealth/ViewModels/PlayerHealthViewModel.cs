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
            IPlayerHealthService playerHealthService,
            ISubscriber<HealthChangedMessage> healthChangedSubscriber,
            ISubscriber<DamageReceivedMessage> damageReceivedSubscriber,
            ISubscriber<HealReceivedMessage> healReceivedSubscriber,
            ISubscriber<PlayerFaintedMessage> playerFaintedSubscriber,
            ISubscriber<PlayerRevivedMessage> playerRevivedSubscriber,
            ISubscriber<PlayerDiedMessage> playerDiedSubscriber,
            ISubscriber<FaintTimeUpdatedMessage> faintTimeUpdatedSubscriber,
            ISubscriber<HealthBarUpdatedMessage> healthBarUpdatedSubscriber,
            ISubscriber<HealthStateChangedMessage> healthStateChangedSubscriber,
            ISubscriber<HealthSettingsChangedMessage> healthSettingsChangedSubscriber
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

            healthChangedSubscriber.Subscribe(OnHealthChanged).AddTo(_disposables);
            damageReceivedSubscriber.Subscribe(OnDamageReceived).AddTo(_disposables);
            healReceivedSubscriber.Subscribe(OnHealReceived).AddTo(_disposables);
            playerFaintedSubscriber.Subscribe(OnPlayerFainted).AddTo(_disposables);
            playerRevivedSubscriber.Subscribe(OnPlayerRevived).AddTo(_disposables);
            playerDiedSubscriber.Subscribe(OnPlayerDied).AddTo(_disposables);
            faintTimeUpdatedSubscriber.Subscribe(OnFaintTimeUpdated).AddTo(_disposables);
            healthBarUpdatedSubscriber.Subscribe(OnHealthBarUpdated).AddTo(_disposables);
            healthStateChangedSubscriber.Subscribe(OnHealthStateChanged).AddTo(_disposables);
            healthSettingsChangedSubscriber.Subscribe(OnHealthSettingsChanged).AddTo(_disposables);

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

        #region Message Handlers

        private void OnHealthChanged(HealthChangedMessage message)
        {
            string changeType = message.isDamage ? "감소" : "증가";
            DebugLog($"체력 변화: {message.currentHp}/{message.maxHp} ({message.healthPercentage:P1}) - {changeType}, 상태: {message.state}");
        }

        private void OnDamageReceived(DamageReceivedMessage message)
        {
            DebugLog($"피해: {message.damage} (체력: {message.currentHp} -> {message.remainingHp}, 상태: {message.newState})");
        }

        private void OnHealReceived(HealReceivedMessage message)
        {
            DebugLog($"치료: +{message.healAmount} (체력: {message.previousHp} -> {message.currentHp}, 상태: {message.newState})");
        }

        private void OnPlayerFainted(PlayerFaintedMessage message)
        {
            DebugLog($"플레이어 기절 - 이전 상태: {message.previousState}, 기절 시간: {message.faintDuration}초");
        }

        private void OnPlayerRevived(PlayerRevivedMessage message)
        {
            DebugLog($"플레이어 부활 - 체력: {message.reviveHp}, 새 상태: {message.newState}");
        }

        private void OnPlayerDied(PlayerDiedMessage message)
        {
            string deathCause = message.wasFromFaint ? "기절 시간 만료" : "체력 소진";
            DebugLog($"플레이어 사망 - 이전 상태: {message.previousState}, 사망 원인: {deathCause}");
        }

        private void OnFaintTimeUpdated(FaintTimeUpdatedMessage message)
        {
            if (message.shouldDie)
            {
                DebugLog($"기절 시간 만료 - 사망 처리됨");
            }
        }

        private void OnHealthBarUpdated(HealthBarUpdatedMessage message)
        {
            DebugLog($"체력바 업데이트 - Bar1: {message.bar1.currentPercent:F1}%, Bar2: {message.bar2.currentPercent:F1}%, Bar3: {message.bar3.currentPercent:F1}%");
        }

        private void OnHealthStateChanged(HealthStateChangedMessage message)
        {
            DebugLog($"상태 변화: {message.previousState} -> {message.currentState} (체력: {message.currentHp}, {message.healthPercentage:P1})");
        }

        private void OnHealthSettingsChanged(HealthSettingsChangedMessage message)
        {
            DebugLog($"설정 변경 - MaxHP: {message.maxHp}, 기절시간: {message.faintReviveTime}초, 기절시스템: {message.enableFaintSystem}");
        }

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