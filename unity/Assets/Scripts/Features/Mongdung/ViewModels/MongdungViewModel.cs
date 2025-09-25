using System;
using System.Collections.Generic;
using Features.Mongdung.Models;
using Features.Mongdung.Services;
using Features.Mongdung.Messages;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Mongdung.ViewModels
{
    /// <summary>
    /// 몽둥이 ViewModel
    /// 몽둥이 상태와 액션을 R3 Reactive Properties로 관리
    /// UI와 View 컴포넌트가 구독하여 반응형으로 업데이트
    /// </summary>
    public class MongdungViewModel : IDisposable
    {
        private readonly IMongdungService _mongdungService;
        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        // Reactive Properties
        private readonly ReactiveProperty<MongdungState> _currentState = new(MongdungState.Idle);
        private readonly ReactiveProperty<bool> _isMovementBlocked = new(false);
        private readonly ReactiveProperty<string> _blockReason = new("");

        // 액션별 상태 관리
        private readonly Dictionary<MongdungActionType, ReactiveProperty<bool>> _actionExecutingStates = new();
        private readonly Dictionary<MongdungActionType, ReactiveProperty<bool>> _actionCooldownStates = new();
        private readonly Dictionary<MongdungActionType, ReactiveProperty<float>> _actionRemainingTimes = new();

        // Read-only Properties
        public ReadOnlyReactiveProperty<MongdungState> CurrentState => _currentState.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<bool> IsMovementBlocked => _isMovementBlocked.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<string> BlockReason => _blockReason.ToReadOnlyReactiveProperty();

        // 플레이어 정보
        public long PlayerId { get; private set; }
        public string PlayerName { get; private set; }

        [Inject]
        public MongdungViewModel(
            IMongdungService mongdungService,
            ISubscriber<MongdungStateChangedMessage> stateChangedSubscriber,
            ISubscriber<MongdungActionStartedMessage> actionStartedSubscriber,
            ISubscriber<MongdungActionCompletedMessage> actionCompletedSubscriber,
            ISubscriber<MongdungMovementBlockedMessage> movementBlockedSubscriber)
        {
            _mongdungService = mongdungService ?? throw new ArgumentNullException(nameof(mongdungService));

            InitializeActionStates();
            SubscribeToMessages(stateChangedSubscriber, actionStartedSubscriber, actionCompletedSubscriber, movementBlockedSubscriber);

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungViewModel] 초기화 완료");
            }
        }

        /// <summary>
        /// 플레이어 정보로 ViewModel 초기화
        /// </summary>
        public void Initialize(long playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;

            // 서비스에 플레이어 등록
            _mongdungService.RegisterMongdungPlayer(playerId, playerName);

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungViewModel] 플레이어 초기화: PlayerId={playerId}, Name={playerName}");
            }
        }

        private void InitializeActionStates()
        {
            var actionTypes = new[] { MongdungActionType.Attack, MongdungActionType.TrapSetting, MongdungActionType.Frighten };

            foreach (var actionType in actionTypes)
            {
                _actionExecutingStates[actionType] = new ReactiveProperty<bool>(false);
                _actionCooldownStates[actionType] = new ReactiveProperty<bool>(false);
                _actionRemainingTimes[actionType] = new ReactiveProperty<float>(0f);
            }
        }

        private void SubscribeToMessages(
            ISubscriber<MongdungStateChangedMessage> stateChangedSubscriber,
            ISubscriber<MongdungActionStartedMessage> actionStartedSubscriber,
            ISubscriber<MongdungActionCompletedMessage> actionCompletedSubscriber,
            ISubscriber<MongdungMovementBlockedMessage> movementBlockedSubscriber)
        {
            // 상태 변경 구독
            stateChangedSubscriber
                .Subscribe(OnStateChanged)
                .AddTo(_disposables);

            // 액션 시작 구독
            actionStartedSubscriber
                .Subscribe(OnActionStarted)
                .AddTo(_disposables);

            // 액션 완료 구독
            actionCompletedSubscriber
                .Subscribe(OnActionCompleted)
                .AddTo(_disposables);

            // 이동 제한 구독
            movementBlockedSubscriber
                .Subscribe(OnMovementBlocked)
                .AddTo(_disposables);
        }

        private void OnStateChanged(MongdungStateChangedMessage message)
        {
            if (message.PlayerId != PlayerId) return;

            _currentState.Value = message.NewState;

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungViewModel] 상태 변경: PlayerId={PlayerId}, {message.PreviousState} → {message.NewState}");
            }
        }

        private void OnActionStarted(MongdungActionStartedMessage message)
        {
            if (message.PlayerId != PlayerId) return;

            _actionExecutingStates[message.ActionType].Value = true;
            _actionRemainingTimes[message.ActionType].Value = message.ExecutionDuration;

            // 실행 시간 카운트다운 시작
            StartActionTimer(message.ActionType, message.ExecutionDuration).AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungViewModel] 액션 시작: PlayerId={PlayerId}, ActionType={message.ActionType}");
            }
        }

        private void OnActionCompleted(MongdungActionCompletedMessage message)
        {
            if (message.PlayerId != PlayerId) return;

            _actionExecutingStates[message.ActionType].Value = false;
            _actionRemainingTimes[message.ActionType].Value = 0f;

            if (message.Success)
            {
                // 성공 시 쿨다운 시작
                _actionCooldownStates[message.ActionType].Value = true;

                // 쿨다운 시간 가져오기
                float cooldownDuration = GetCooldownDuration(message.ActionType);
                _actionRemainingTimes[message.ActionType].Value = cooldownDuration;

                // 쿨다운 카운트다운 시작
                StartCooldownTimer(message.ActionType, cooldownDuration).AddTo(_disposables);
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungViewModel] 액션 완료: PlayerId={PlayerId}, ActionType={message.ActionType}, Success={message.Success}");
            }
        }

        private void OnMovementBlocked(MongdungMovementBlockedMessage message)
        {
            if (message.PlayerId != PlayerId) return;

            _isMovementBlocked.Value = message.IsBlocked;
            _blockReason.Value = message.Reason;

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungViewModel] 이동 제한 변경: PlayerId={PlayerId}, Blocked={message.IsBlocked}, Reason={message.Reason}");
            }
        }

        private IDisposable StartActionTimer(MongdungActionType actionType, float duration)
        {
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f))
                .TakeWhile(_ => _actionRemainingTimes[actionType].Value > 0)
                .Subscribe(_ =>
                {
                    var remaining = _actionRemainingTimes[actionType].Value - 0.1f;
                    _actionRemainingTimes[actionType].Value = Mathf.Max(0f, remaining);
                });
        }

        private IDisposable StartCooldownTimer(MongdungActionType actionType, float duration)
        {
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f))
                .TakeWhile(_ => _actionRemainingTimes[actionType].Value > 0)
                .Subscribe(_ =>
                {
                    var remaining = _actionRemainingTimes[actionType].Value - 0.1f;
                    _actionRemainingTimes[actionType].Value = Mathf.Max(0f, remaining);

                    if (remaining <= 0f)
                    {
                        _actionCooldownStates[actionType].Value = false;
                    }
                });
        }

        private float GetCooldownDuration(MongdungActionType actionType)
        {
            // 기본값들 (실제로는 MongdungData에서 가져와야 함)
            return actionType switch
            {
                MongdungActionType.Attack => 8.0f,
                MongdungActionType.TrapSetting => 12.0f,
                MongdungActionType.Frighten => 6.0f,
                _ => 5.0f
            };
        }

        /// <summary>
        /// 특정 액션이 실행 중인지 확인
        /// </summary>
        public ReadOnlyReactiveProperty<bool> GetActionExecuting(MongdungActionType actionType)
        {
            return _actionExecutingStates[actionType].ToReadOnlyReactiveProperty();
        }

        /// <summary>
        /// 특정 액션이 쿨다운 중인지 확인
        /// </summary>
        public ReadOnlyReactiveProperty<bool> GetActionCooldown(MongdungActionType actionType)
        {
            return _actionCooldownStates[actionType].ToReadOnlyReactiveProperty();
        }

        /// <summary>
        /// 특정 액션의 남은 시간 (실행 시간 또는 쿨다운 시간)
        /// </summary>
        public ReadOnlyReactiveProperty<float> GetActionRemainingTime(MongdungActionType actionType)
        {
            return _actionRemainingTimes[actionType].ToReadOnlyReactiveProperty();
        }

        /// <summary>
        /// 특정 액션이 실행 가능한지 확인
        /// </summary>
        public bool CanExecuteAction(MongdungActionType actionType)
        {
            return _mongdungService.CanExecuteAction(PlayerId, actionType);
        }

        public void Dispose()
        {
            // 서비스에서 플레이어 해제
            if (PlayerId > 0)
            {
                _mongdungService.UnregisterMongdungPlayer(PlayerId);
            }

            // Reactive Properties 정리
            _disposables?.Dispose();
            _currentState?.Dispose();
            _isMovementBlocked?.Dispose();
            _blockReason?.Dispose();

            foreach (var kvp in _actionExecutingStates)
                kvp.Value?.Dispose();
            foreach (var kvp in _actionCooldownStates)
                kvp.Value?.Dispose();
            foreach (var kvp in _actionRemainingTimes)
                kvp.Value?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungViewModel] 리소스 정리 완료: PlayerId={PlayerId}");
            }
        }
    }
}