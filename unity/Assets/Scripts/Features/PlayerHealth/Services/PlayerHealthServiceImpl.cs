using System;
using Features.PlayerHealth.Messages;
using Features.PlayerHealth.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.PlayerHealth.Services
{
    public class PlayerHealthServiceImpl : IPlayerHealthService, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<int> CurrentHp => _currentHp;
        public ReadOnlyReactiveProperty<int> MaxHp => _maxHp;
        public ReadOnlyReactiveProperty<float> HealthPercentage => _healthPercentage;
        public ReadOnlyReactiveProperty<PlayerState> CurrentState => _currentState;
        public ReadOnlyReactiveProperty<bool> IsFainted => _isFainted;
        public ReadOnlyReactiveProperty<float> FaintTimeRemaining => _faintTimeRemaining;
        public ReadOnlyReactiveProperty<bool> IsAlive => _isAlive;
        public ReadOnlyReactiveProperty<bool> IsDead => _isDead;
        public ReadOnlyReactiveProperty<bool> IsLowHealth => _isLowHealth;
        public ReadOnlyReactiveProperty<bool> IsCriticalHealth => _isCriticalHealth;
        public ReadOnlyReactiveProperty<HealthBarData> Bar1 => _bar1;
        public ReadOnlyReactiveProperty<HealthBarData> Bar2 => _bar2;
        public ReadOnlyReactiveProperty<HealthBarData> Bar3 => _bar3;
        public ReadOnlyReactiveProperty<float> FaintReviveTime => _faintReviveTime;
        public ReadOnlyReactiveProperty<bool> EnableFaintSystem => _enableFaintSystem;
        public ReadOnlyReactiveProperty<Color> NormalBarColor => _normalBarColor;
        public ReadOnlyReactiveProperty<Color> CriticalBarColor => _criticalBarColor;
        public ReadOnlyReactiveProperty<Color> ThirdBarColor => _thirdBarColor;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<int> _currentHp = new(100);
        private readonly ReactiveProperty<int> _maxHp = new(100);
        private readonly ReactiveProperty<float> _healthPercentage = new(1.0f);
        private readonly ReactiveProperty<PlayerState> _currentState = new(PlayerState.Normal);
        private readonly ReactiveProperty<bool> _isFainted = new(false);
        private readonly ReactiveProperty<float> _faintTimeRemaining = new(0f);
        private readonly ReactiveProperty<bool> _isAlive = new(true);
        private readonly ReactiveProperty<bool> _isDead = new(false);
        private readonly ReactiveProperty<bool> _isLowHealth = new(false);
        private readonly ReactiveProperty<bool> _isCriticalHealth = new(false);
        private readonly ReactiveProperty<HealthBarData> _bar1 = new(new HealthBarData(HealthBarType.Bar1, 0, 40));
        private readonly ReactiveProperty<HealthBarData> _bar2 = new(new HealthBarData(HealthBarType.Bar2, 40, 80));
        private readonly ReactiveProperty<HealthBarData> _bar3 = new(new HealthBarData(HealthBarType.Bar3, 80, 100));
        private readonly ReactiveProperty<float> _faintReviveTime = new(5f);
        private readonly ReactiveProperty<bool> _enableFaintSystem = new(true);
        private readonly ReactiveProperty<Color> _normalBarColor = new(new Color(255f / 255f, 255f / 255f, 255f / 255f, 0.4f));
        private readonly ReactiveProperty<Color> _criticalBarColor = new(new Color(154f / 255f, 8f / 255f, 11f / 255f, 1.0f));
        private readonly ReactiveProperty<Color> _thirdBarColor = new(new Color(235f / 255f, 255f / 255f, 123f / 255f, 0.4f));

        private readonly PlayerHealthData _healthData = new();
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Dependencies

        private readonly IPublisher<HealthChangedMessage> _healthChangedPublisher;
        private readonly IPublisher<DamageReceivedMessage> _damageReceivedPublisher;
        private readonly IPublisher<HealReceivedMessage> _healReceivedPublisher;
        private readonly IPublisher<PlayerFaintedMessage> _playerFaintedPublisher;
        private readonly IPublisher<PlayerRevivedMessage> _playerRevivedPublisher;
        private readonly IPublisher<PlayerDiedMessage> _playerDiedPublisher;
        private readonly IPublisher<FaintTimeUpdatedMessage> _faintTimeUpdatedPublisher;
        private readonly IPublisher<HealthBarUpdatedMessage> _healthBarUpdatedPublisher;
        private readonly IPublisher<HealthStateChangedMessage> _healthStateChangedPublisher;
        private readonly IPublisher<HealthSettingsChangedMessage> _healthSettingsChangedPublisher;

        #endregion

        #region Constructor

        [Inject]
        public PlayerHealthServiceImpl(
            IPublisher<HealthChangedMessage> healthChangedPublisher,
            IPublisher<DamageReceivedMessage> damageReceivedPublisher,
            IPublisher<HealReceivedMessage> healReceivedPublisher,
            IPublisher<PlayerFaintedMessage> playerFaintedPublisher,
            IPublisher<PlayerRevivedMessage> playerRevivedPublisher,
            IPublisher<PlayerDiedMessage> playerDiedPublisher,
            IPublisher<FaintTimeUpdatedMessage> faintTimeUpdatedPublisher,
            IPublisher<HealthBarUpdatedMessage> healthBarUpdatedPublisher,
            IPublisher<HealthStateChangedMessage> healthStateChangedPublisher,
            IPublisher<HealthSettingsChangedMessage> healthSettingsChangedPublisher
        )
        {
            _healthChangedPublisher = healthChangedPublisher;
            _damageReceivedPublisher = damageReceivedPublisher;
            _healReceivedPublisher = healReceivedPublisher;
            _playerFaintedPublisher = playerFaintedPublisher;
            _playerRevivedPublisher = playerRevivedPublisher;
            _playerDiedPublisher = playerDiedPublisher;
            _faintTimeUpdatedPublisher = faintTimeUpdatedPublisher;
            _healthBarUpdatedPublisher = healthBarUpdatedPublisher;
            _healthStateChangedPublisher = healthStateChangedPublisher;
            _healthSettingsChangedPublisher = healthSettingsChangedPublisher;

            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            UpdateObservables();

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerHealthServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void SetHealth(int newHp, int newMaxHp = -1)
        {
            int previousHp = _healthData.currentHp;
            PlayerState previousState = _healthData.CurrentState;

            _healthData.SetHealth(newHp, newMaxHp);
            UpdateObservables();

            _healthChangedPublisher.Publish(new HealthChangedMessage(
                _healthData.currentHp,
                _healthData.maxHp,
                _healthData.HealthPercentage,
                _healthData.CurrentState,
                newHp < previousHp
            ));

            if (_healthData.CurrentState != previousState)
            {
                _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                    previousState,
                    _healthData.CurrentState,
                    _healthData.currentHp,
                    _healthData.HealthPercentage
                ));
            }

            DebugLog($"체력 설정: {previousHp} -> {_healthData.currentHp}/{_healthData.maxHp}");
        }

        public void DamageHealth(int damage)
        {
            int previousHp = _healthData.currentHp;
            PlayerState previousState = _healthData.CurrentState;

            _healthData.DamageHealth(damage);
            UpdateObservables();

            _damageReceivedPublisher.Publish(new DamageReceivedMessage(
                damage,
                previousHp,
                _healthData.currentHp,
                _healthData.CurrentState
            ));

            _healthChangedPublisher.Publish(new HealthChangedMessage(
                _healthData.currentHp,
                _healthData.maxHp,
                _healthData.HealthPercentage,
                _healthData.CurrentState,
                true
            ));

            if (_healthData.CurrentState != previousState)
            {
                _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                    previousState,
                    _healthData.CurrentState,
                    _healthData.currentHp,
                    _healthData.HealthPercentage
                ));
            }

            if (_healthData.ShouldBeFainted())
            {
                SetFaintState(true);
            }

            DebugLog($"데미지 받음: {damage} (체력: {previousHp} -> {_healthData.currentHp})");
        }

        public void HealHealth(int healAmount)
        {
            int previousHp = _healthData.currentHp;
            PlayerState previousState = _healthData.CurrentState;

            _healthData.HealHealth(healAmount);
            UpdateObservables();

            _healReceivedPublisher.Publish(new HealReceivedMessage(
                healAmount,
                previousHp,
                _healthData.currentHp,
                _healthData.CurrentState
            ));

            _healthChangedPublisher.Publish(new HealthChangedMessage(
                _healthData.currentHp,
                _healthData.maxHp,
                _healthData.HealthPercentage,
                _healthData.CurrentState,
                false
            ));

            if (_healthData.CurrentState != previousState)
            {
                _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                    previousState,
                    _healthData.CurrentState,
                    _healthData.currentHp,
                    _healthData.HealthPercentage
                ));
            }

            DebugLog($"체력 회복: {healAmount} (체력: {previousHp} -> {_healthData.currentHp})");
        }

        public void SetFaintState(bool fainted)
        {
            if (_healthData.isFainted == fainted)
                return;

            PlayerState previousState = _healthData.CurrentState;

            _healthData.SetFaintState(fainted);
            UpdateObservables();

            if (fainted)
            {
                _playerFaintedPublisher.Publish(new PlayerFaintedMessage(
                    _healthData.faintReviveTime,
                    previousState
                ));
            }
            else
            {
                _playerRevivedPublisher.Publish(new PlayerRevivedMessage(
                    _healthData.currentHp,
                    _healthData.CurrentState
                ));
            }

            _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                previousState,
                _healthData.CurrentState,
                _healthData.currentHp,
                _healthData.HealthPercentage
            ));

            DebugLog($"기절 상태 설정: {fainted} (남은 시간: {_healthData.faintTimeRemaining}초)");
        }

        public void UpdateFaintTime(float deltaTime)
        {
            if (!_healthData.isFainted)
                return;

            float previousTime = _healthData.faintTimeRemaining;
            _healthData.UpdateFaintTime(deltaTime);
            UpdateObservables();

            bool shouldDie = _healthData.ShouldDie();

            _faintTimeUpdatedPublisher.Publish(new FaintTimeUpdatedMessage(
                _healthData.faintTimeRemaining,
                _healthData.faintReviveTime,
                shouldDie
            ));

            if (shouldDie)
            {
                PlayerState previousState = _healthData.CurrentState;
                _healthData.SetFaintState(false);

                _playerDiedPublisher.Publish(new PlayerDiedMessage(previousState, true));

                _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                    previousState,
                    _healthData.CurrentState,
                    _healthData.currentHp,
                    _healthData.HealthPercentage
                ));

                DebugLog("기절 시간 만료로 사망");
            }
        }

        public void RevivePlayer(int reviveHp = 30)
        {
            if (!_healthData.isFainted)
                return;

            PlayerState previousState = _healthData.CurrentState;

            _healthData.RevivePlayer(reviveHp);
            UpdateObservables();

            _playerRevivedPublisher.Publish(new PlayerRevivedMessage(
                reviveHp,
                _healthData.CurrentState
            ));

            _healthChangedPublisher.Publish(new HealthChangedMessage(
                _healthData.currentHp,
                _healthData.maxHp,
                _healthData.HealthPercentage,
                _healthData.CurrentState,
                false
            ));

            _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                previousState,
                _healthData.CurrentState,
                _healthData.currentHp,
                _healthData.HealthPercentage
            ));

            DebugLog($"플레이어 부활: {reviveHp} HP로 복구");
        }

        public void SetBarColors(Color normal, Color critical, Color third)
        {
            _healthData.SetBarColors(normal, critical, third);
            UpdateObservables();

            DebugLog("체력바 색상 변경");
        }

        public void SetHealthSettings(int maxHp, float faintReviveTime, bool enableFaintSystem)
        {
            _healthData.maxHp = maxHp;
            _healthData.faintReviveTime = faintReviveTime;
            _healthData.enableFaintSystem = enableFaintSystem;

            UpdateObservables();

            _healthSettingsChangedPublisher.Publish(new HealthSettingsChangedMessage(
                maxHp,
                faintReviveTime,
                enableFaintSystem
            ));

            DebugLog($"체력 설정 변경: MaxHP={maxHp}, FaintTime={faintReviveTime}s, EnableFaint={enableFaintSystem}");
        }

        public void Reset()
        {
            PlayerState previousState = _healthData.CurrentState;

            _healthData.Reset();
            UpdateObservables();

            _healthChangedPublisher.Publish(new HealthChangedMessage(
                _healthData.currentHp,
                _healthData.maxHp,
                _healthData.HealthPercentage,
                _healthData.CurrentState,
                false
            ));

            if (_healthData.CurrentState != previousState)
            {
                _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                    previousState,
                    _healthData.CurrentState,
                    _healthData.currentHp,
                    _healthData.HealthPercentage
                ));
            }

            DebugLog("체력 시스템 초기화");
        }

        public bool ShouldBeFainted()
        {
            return _healthData.ShouldBeFainted();
        }

        public bool ShouldDie()
        {
            return _healthData.ShouldDie();
        }

        public PlayerHealthData GetHealthData()
        {
            return _healthData;
        }

        public void LoadHealthData(PlayerHealthData data)
        {
            if (data == null)
                return;

            PlayerState previousState = _healthData.CurrentState;

            _healthData.currentHp = data.currentHp;
            _healthData.maxHp = data.maxHp;
            _healthData.isFainted = data.isFainted;
            _healthData.faintTimeRemaining = data.faintTimeRemaining;
            _healthData.faintReviveTime = data.faintReviveTime;
            _healthData.enableFaintSystem = data.enableFaintSystem;
            _healthData.normalBarColor = data.normalBarColor;
            _healthData.criticalBarColor = data.criticalBarColor;
            _healthData.thirdBarColor = data.thirdBarColor;

            _healthData.UpdateHealthBars();
            UpdateObservables();

            _healthChangedPublisher.Publish(new HealthChangedMessage(
                _healthData.currentHp,
                _healthData.maxHp,
                _healthData.HealthPercentage,
                _healthData.CurrentState,
                false
            ));

            if (_healthData.CurrentState != previousState)
            {
                _healthStateChangedPublisher.Publish(new HealthStateChangedMessage(
                    previousState,
                    _healthData.CurrentState,
                    _healthData.currentHp,
                    _healthData.HealthPercentage
                ));
            }

            DebugLog("체력 데이터 로드 완료");
        }

        #endregion

        #region Private Methods

        private void UpdateObservables()
        {
            _currentHp.Value = _healthData.currentHp;
            _maxHp.Value = _healthData.maxHp;
            _healthPercentage.Value = _healthData.HealthPercentage;
            _currentState.Value = _healthData.CurrentState;
            _isFainted.Value = _healthData.isFainted;
            _faintTimeRemaining.Value = _healthData.faintTimeRemaining;
            _isAlive.Value = _healthData.IsAlive;
            _isDead.Value = _healthData.IsDead;
            _isLowHealth.Value = _healthData.IsLowHealth;
            _isCriticalHealth.Value = _healthData.IsCriticalHealth;
            _bar1.Value = _healthData.bar1;
            _bar2.Value = _healthData.bar2;
            _bar3.Value = _healthData.bar3;
            _faintReviveTime.Value = _healthData.faintReviveTime;
            _enableFaintSystem.Value = _healthData.enableFaintSystem;
            _normalBarColor.Value = _healthData.normalBarColor;
            _criticalBarColor.Value = _healthData.criticalBarColor;
            _thirdBarColor.Value = _healthData.thirdBarColor;

            _healthBarUpdatedPublisher.Publish(new HealthBarUpdatedMessage(
                _healthData.bar1,
                _healthData.bar2,
                _healthData.bar3
            ));
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PlayerHealthServiceImpl] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _currentHp?.Dispose();
            _maxHp?.Dispose();
            _healthPercentage?.Dispose();
            _currentState?.Dispose();
            _isFainted?.Dispose();
            _faintTimeRemaining?.Dispose();
            _isAlive?.Dispose();
            _isDead?.Dispose();
            _isLowHealth?.Dispose();
            _isCriticalHealth?.Dispose();
            _bar1?.Dispose();
            _bar2?.Dispose();
            _bar3?.Dispose();
            _faintReviveTime?.Dispose();
            _enableFaintSystem?.Dispose();
            _normalBarColor?.Dispose();
            _criticalBarColor?.Dispose();
            _thirdBarColor?.Dispose();

            DebugLog("Dispose 완료");
        }

        #endregion
    }
}