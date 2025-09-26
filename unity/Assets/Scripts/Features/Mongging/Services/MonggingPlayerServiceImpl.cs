using System;
using Features.Mongging.Messages;
using Features.Mongging.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Mongging.Services
{
    /// <summary>
    /// 개별 몽깅이 플레이어 관리 서비스 구현체
    /// </summary>
    public class MonggingPlayerServiceImpl : IMonggingPlayerService, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<int> CurrentHp => _currentHp;
        public ReadOnlyReactiveProperty<int> MaxHp => _maxHp;
        public ReadOnlyReactiveProperty<float> HealthPercentage => _healthPercentage;
        public ReadOnlyReactiveProperty<MonggingPlayerState> CurrentState => _currentState;
        public ReadOnlyReactiveProperty<int> FaintCount => _faintCount;
        public ReadOnlyReactiveProperty<bool> IsAlive => _isAlive;
        public ReadOnlyReactiveProperty<bool> IsStunned => _isStunned;
        public ReadOnlyReactiveProperty<bool> IsFrightened => _isFrightened;
        public ReadOnlyReactiveProperty<float> StunRemainingTime => _stunRemainingTime;
        public ReadOnlyReactiveProperty<float> FrightenRemainingTime => _frightenRemainingTime;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<int> _currentHp = new(100);
        private readonly ReactiveProperty<int> _maxHp = new(100);
        private readonly ReactiveProperty<float> _healthPercentage = new(1.0f);
        private readonly ReactiveProperty<MonggingPlayerState> _currentState = new(MonggingPlayerState.Normal);
        private readonly ReactiveProperty<int> _faintCount = new(0);
        private readonly ReactiveProperty<bool> _isAlive = new(true);
        private readonly ReactiveProperty<bool> _isStunned = new(false);
        private readonly ReactiveProperty<bool> _isFrightened = new(false);
        private readonly ReactiveProperty<float> _stunRemainingTime = new(0f);
        private readonly ReactiveProperty<float> _frightenRemainingTime = new(0f);

        private readonly MonggingPlayerData _playerData = new();
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Dependencies

        private readonly IPublisher<MonggingPlayerStateChangedMessage> _stateChangedPublisher;
        private readonly IPublisher<MonggingPlayerHitMessage> _hitPublisher;
        private readonly IPublisher<MonggingPlayerHealMessage> _healPublisher;
        private readonly IPublisher<MonggingPlayerStatusEffectMessage> _statusEffectPublisher;
        private readonly IPublisher<MonggingPlayerRevivedMessage> _revivedPublisher;
        private readonly IPublisher<MonggingPlayerEscapedMessage> _escapedPublisher;
        private readonly IPublisher<MonggingPlayerAnimationMessage> _animationPublisher;

        #endregion

        #region Constructor

        [Inject]
        public MonggingPlayerServiceImpl(
            IPublisher<MonggingPlayerStateChangedMessage> stateChangedPublisher,
            IPublisher<MonggingPlayerHitMessage> hitPublisher,
            IPublisher<MonggingPlayerHealMessage> healPublisher,
            IPublisher<MonggingPlayerStatusEffectMessage> statusEffectPublisher,
            IPublisher<MonggingPlayerRevivedMessage> revivedPublisher,
            IPublisher<MonggingPlayerEscapedMessage> escapedPublisher,
            IPublisher<MonggingPlayerAnimationMessage> animationPublisher)
        {
            _stateChangedPublisher = stateChangedPublisher;
            _hitPublisher = hitPublisher;
            _healPublisher = healPublisher;
            _statusEffectPublisher = statusEffectPublisher;
            _revivedPublisher = revivedPublisher;
            _escapedPublisher = escapedPublisher;
            _animationPublisher = animationPublisher;

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingPlayerServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void Initialize(long playerId, string playerName, MonggingPlayerType playerType, bool isLocal = false)
        {
            _playerData.playerId = playerId;
            _playerData.playerName = playerName;
            _playerData.playerType = playerType;
            _playerData.isLocal = isLocal;

            Reset();
            UpdateObservables();

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 초기화: ID={playerId}, Name={playerName}, Type={playerType}, Local={isLocal}");
            }
        }

        public void TakeDamage(int damage)
        {
            if (!_playerData.IsAlive || _playerData.currentState == MonggingPlayerState.Fainted)
                return;

            int previousHp = _playerData.currentHp;
            MonggingPlayerState previousState = _playerData.currentState;

            _playerData.TakeDamage(damage);
            UpdateObservables();

            // 피격 메시지 발행
            _hitPublisher.Publish(new MonggingPlayerHitMessage(
                _playerData.playerId,
                damage,
                previousHp,
                _playerData.currentHp,
                _playerData.currentState,
                Vector3.zero // TODO: 실제 피격 위치
            ));

            // 상태 변경 메시지 발행
            if (previousState != _playerData.currentState)
            {
                _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                    _playerData.playerId,
                    previousState,
                    _playerData.currentState,
                    _playerData.currentHp,
                    _playerData.faintCount
                ));

                // 애니메이션 트리거
                if (_playerData.currentState == MonggingPlayerState.Fainted)
                {
                    _animationPublisher.Publish(new MonggingPlayerAnimationMessage(
                        _playerData.playerId,
                        "Faint"
                    ));
                }
            }

            // 피격 애니메이션
            _animationPublisher.Publish(new MonggingPlayerAnimationMessage(
                _playerData.playerId,
                "Hit",
                0.5f
            ));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 피격: ID={_playerData.playerId}, Damage={damage}, HP={previousHp}→{_playerData.currentHp}, State={previousState}→{_playerData.currentState}");
            }
        }

        public void Heal(int healAmount)
        {
            if (!_playerData.IsAlive || _playerData.currentState == MonggingPlayerState.Fainted)
                return;

            int previousHp = _playerData.currentHp;
            _playerData.Heal(healAmount);
            UpdateObservables();

            _healPublisher.Publish(new MonggingPlayerHealMessage(
                _playerData.playerId,
                healAmount,
                previousHp,
                _playerData.currentHp
            ));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 회복: ID={_playerData.playerId}, Heal={healAmount}, HP={previousHp}→{_playerData.currentHp}");
            }
        }

        public void ApplyStun(float duration = 2f)
        {
            if (!_playerData.IsAlive || _playerData.currentState != MonggingPlayerState.Normal)
                return;

            MonggingPlayerState previousState = _playerData.currentState;
            _playerData.ApplyStun(duration);
            UpdateObservables();

            _statusEffectPublisher.Publish(new MonggingPlayerStatusEffectMessage(
                _playerData.playerId,
                MonggingPlayerState.Stunned,
                duration,
                true
            ));

            _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                _playerData.playerId,
                previousState,
                _playerData.currentState,
                _playerData.currentHp,
                _playerData.faintCount
            ));

            _animationPublisher.Publish(new MonggingPlayerAnimationMessage(
                _playerData.playerId,
                "Stun",
                duration
            ));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 감전: ID={_playerData.playerId}, Duration={duration}s");
            }
        }

        public void ApplyFrighten(float duration = 5f)
        {
            if (!_playerData.IsAlive || _playerData.currentState != MonggingPlayerState.Normal)
                return;

            MonggingPlayerState previousState = _playerData.currentState;
            _playerData.ApplyFrighten(duration);
            UpdateObservables();

            _statusEffectPublisher.Publish(new MonggingPlayerStatusEffectMessage(
                _playerData.playerId,
                MonggingPlayerState.Frightened,
                duration,
                true
            ));

            _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                _playerData.playerId,
                previousState,
                _playerData.currentState,
                _playerData.currentHp,
                _playerData.faintCount
            ));

            _animationPublisher.Publish(new MonggingPlayerAnimationMessage(
                _playerData.playerId,
                "Frighten",
                duration
            ));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 공포: ID={_playerData.playerId}, Duration={duration}s");
            }
        }

        public void ClearStatusEffects()
        {
            bool hadStatusEffect = _playerData.isStunned || _playerData.isFrightened;

            if (hadStatusEffect)
            {
                MonggingPlayerState previousState = _playerData.currentState;

                _playerData.isStunned = false;
                _playerData.stunRemainingTime = 0f;
                _playerData.isFrightened = false;
                _playerData.frightenRemainingTime = 0f;

                if (_playerData.currentState == MonggingPlayerState.Stunned ||
                    _playerData.currentState == MonggingPlayerState.Frightened)
                {
                    _playerData.currentState = MonggingPlayerState.Normal;
                }

                UpdateObservables();

                _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                    _playerData.playerId,
                    previousState,
                    _playerData.currentState,
                    _playerData.currentHp,
                    _playerData.faintCount
                ));

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 상태이상 해제: ID={_playerData.playerId}");
                }
            }
        }

        public void Revive(int reviveHp = 30)
        {
            if (!_playerData.CanBeRevived)
                return;

            MonggingPlayerState previousState = _playerData.currentState;
            _playerData.Revive(reviveHp);
            UpdateObservables();

            _revivedPublisher.Publish(new MonggingPlayerRevivedMessage(
                _playerData.playerId,
                -1, // 부활시킨 플레이어 ID는 별도로 처리
                reviveHp
            ));

            _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                _playerData.playerId,
                previousState,
                _playerData.currentState,
                _playerData.currentHp,
                _playerData.faintCount
            ));

            _animationPublisher.Publish(new MonggingPlayerAnimationMessage(
                _playerData.playerId,
                "Revive"
            ));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 부활: ID={_playerData.playerId}, HP={reviveHp}");
            }
        }

        public void Escape()
        {
            if (!_playerData.IsAlive)
                return;

            MonggingPlayerState previousState = _playerData.currentState;
            _playerData.Escape();
            UpdateObservables();

            _escapedPublisher.Publish(new MonggingPlayerEscapedMessage(
                _playerData.playerId,
                Vector3.zero // TODO: 실제 탈출 위치
            ));

            _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                _playerData.playerId,
                previousState,
                _playerData.currentState,
                _playerData.currentHp,
                _playerData.faintCount
            ));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 탈출: ID={_playerData.playerId}");
            }
        }

        public void SyncFromServer(int hp, MonggingPlayerState state, int faintCount)
        {
            MonggingPlayerState previousState = _playerData.currentState;

            _playerData.UpdateFromServer(hp, state, faintCount);
            UpdateObservables();

            if (previousState != state)
            {
                _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                    _playerData.playerId,
                    previousState,
                    state,
                    hp,
                    faintCount
                ));

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingPlayerServiceImpl] 서버 동기화: ID={_playerData.playerId}, HP={hp}, State={previousState}→{state}, FaintCount={faintCount}");
                }
            }
        }

        public MonggingPlayerData GetPlayerData()
        {
            return _playerData;
        }

        public void UpdateStatusEffects(float deltaTime)
        {
            bool hadStatusEffect = _playerData.isStunned || _playerData.isFrightened;
            _playerData.UpdateStatusEffects(deltaTime);

            if (hadStatusEffect && !_playerData.isStunned && !_playerData.isFrightened)
            {
                UpdateObservables();

                _stateChangedPublisher.Publish(new MonggingPlayerStateChangedMessage(
                    _playerData.playerId,
                    MonggingPlayerState.Stunned, // 이전 상태는 추정
                    _playerData.currentState,
                    _playerData.currentHp,
                    _playerData.faintCount
                ));
            }
            else if (hadStatusEffect)
            {
                UpdateObservables();
            }
        }

        public void Reset()
        {
            _playerData.Reset();
            UpdateObservables();

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] 플레이어 초기화: ID={_playerData.playerId}");
            }
        }

        #endregion

        #region Private Methods

        private void UpdateObservables()
        {
            _currentHp.Value = _playerData.currentHp;
            _maxHp.Value = _playerData.maxHp;
            _healthPercentage.Value = _playerData.HealthPercentage;
            _currentState.Value = _playerData.currentState;
            _faintCount.Value = _playerData.faintCount;
            _isAlive.Value = _playerData.IsAlive;
            _isStunned.Value = _playerData.isStunned;
            _isFrightened.Value = _playerData.isFrightened;
            _stunRemainingTime.Value = _playerData.stunRemainingTime;
            _frightenRemainingTime.Value = _playerData.frightenRemainingTime;
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
            _faintCount?.Dispose();
            _isAlive?.Dispose();
            _isStunned?.Dispose();
            _isFrightened?.Dispose();
            _stunRemainingTime?.Dispose();
            _frightenRemainingTime?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingPlayerServiceImpl] Dispose 완료: ID={_playerData.playerId}");
            }
        }

        #endregion
    }
}