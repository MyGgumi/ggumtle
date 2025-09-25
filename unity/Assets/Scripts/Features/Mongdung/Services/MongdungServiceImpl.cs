using System;
using System.Collections.Generic;
using Features.Mongdung.Models;
using Features.Mongdung.NetworkSources;
using Features.Mongdung.Messages;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Features.Mongdung.Services
{
    /// <summary>
    /// 몽둥이 서비스 구현체
    /// 몽둥이 관련 비즈니스 로직과 상태 관리를 담당
    /// </summary>
    public class MongdungServiceImpl : IMongdungService
    {
        private readonly IMongdungNetworkSource _networkSource;
        private readonly IPublisher<MongdungActionStartedMessage> _actionStartedPublisher;
        private readonly IPublisher<MongdungActionCompletedMessage> _actionCompletedPublisher;
        private readonly IPublisher<MongdungStateChangedMessage> _stateChangedPublisher;
        private readonly IPublisher<MongdungMovementBlockedMessage> _movementBlockedPublisher;

        private readonly bool _enableDebugLogs = true;

        // 플레이어별 몽둥이 데이터 관리
        private readonly Dictionary<long, MongdungData> _playerDataMap = new();
        private readonly Dictionary<long, Dictionary<MongdungActionType, MongdungActionState>> _actionStatesMap = new();

        [Inject]
        public MongdungServiceImpl(
            IMongdungNetworkSource networkSource,
            IPublisher<MongdungActionStartedMessage> actionStartedPublisher,
            IPublisher<MongdungActionCompletedMessage> actionCompletedPublisher,
            IPublisher<MongdungStateChangedMessage> stateChangedPublisher,
            IPublisher<MongdungMovementBlockedMessage> movementBlockedPublisher)
        {
            _networkSource = networkSource ?? throw new ArgumentNullException(nameof(networkSource));
            _actionStartedPublisher = actionStartedPublisher;
            _actionCompletedPublisher = actionCompletedPublisher;
            _stateChangedPublisher = stateChangedPublisher;
            _movementBlockedPublisher = movementBlockedPublisher;

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungServiceImpl] 초기화 완료");
            }
        }

        public async UniTask<bool> ExecuteActionAsync(long playerId, MongdungActionType actionType, Vector3 position, Vector3 direction)
        {
            if (!CanExecuteAction(playerId, actionType))
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[MongdungServiceImpl] 액션 실행 불가: PlayerId={playerId}, ActionType={actionType}");
                }
                return false;
            }

            try
            {
                // 액션 데이터 가져오기
                var playerData = GetOrCreatePlayerData(playerId);
                var actionData = playerData.GetActionData(actionType);

                // 상태를 실행 중으로 변경
                var previousState = playerData.currentState;
                playerData.currentState = MongdungState.ExecutingAction;

                // 액션 상태 업데이트
                var actionStates = GetOrCreateActionStates(playerId);
                var actionState = actionStates[actionType];
                actionState.IsExecuting = true;
                actionState.RemainingExecutionTime = actionData.executionDuration;

                // 상태 변경 메시지 발행
                _stateChangedPublisher.Publish(new MongdungStateChangedMessage(playerId, previousState, MongdungState.ExecutingAction));

                // 이동 제한 메시지 발행
                if (actionData.blockMovement)
                {
                    _movementBlockedPublisher.Publish(new MongdungMovementBlockedMessage(playerId, true, $"Executing {actionType}"));
                }

                // 액션 시작 메시지 발행
                _actionStartedPublisher.Publish(new MongdungActionStartedMessage(playerId, actionType, position, actionData.executionDuration));

                // 서버에 액션 전송
                bool networkSuccess = await _networkSource.SendMongdungActionAsync(actionType, position, direction);

                if (networkSuccess)
                {
                    // 실행 시간만큼 대기
                    await UniTask.Delay(TimeSpan.FromSeconds(actionData.executionDuration));

                    // 액션 완료 처리
                    await CompleteActionAsync(playerId, actionType, true, position);
                }
                else
                {
                    // 네트워크 실패 시 즉시 취소
                    await CompleteActionAsync(playerId, actionType, false, position);
                }

                return networkSuccess;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] 액션 실행 실패: PlayerId={playerId}, ActionType={actionType}, Error={e.Message}");
                await CompleteActionAsync(playerId, actionType, false, position);
                return false;
            }
        }

        public async UniTask<bool> CancelActionAsync(long playerId, MongdungActionType actionType)
        {
            try
            {
                var actionStates = GetOrCreateActionStates(playerId);
                if (!actionStates[actionType].IsExecuting)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[MongdungServiceImpl] 실행 중이 아닌 액션 취소 시도: PlayerId={playerId}, ActionType={actionType}");
                    }
                    return false;
                }

                // 서버에 취소 요청
                bool networkSuccess = await _networkSource.CancelMongdungActionAsync(actionType);

                // 액션 완료 처리 (취소)
                await CompleteActionAsync(playerId, actionType, false, Vector3.zero);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungServiceImpl] 액션 취소 완료: PlayerId={playerId}, ActionType={actionType}");
                }

                return networkSuccess;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] 액션 취소 실패: PlayerId={playerId}, ActionType={actionType}, Error={e.Message}");
                return false;
            }
        }

        private async UniTask CompleteActionAsync(long playerId, MongdungActionType actionType, bool success, Vector3 position)
        {
            var playerData = GetOrCreatePlayerData(playerId);
            var actionData = playerData.GetActionData(actionType);
            var actionStates = GetOrCreateActionStates(playerId);
            var actionState = actionStates[actionType];

            // 액션 상태 업데이트
            actionState.IsExecuting = false;

            if (success)
            {
                // 성공 시 쿨다운 시작
                actionState.IsOnCooldown = true;
                actionState.RemainingCooldownTime = actionData.cooldownDuration;

                // 쿨다운 타이머 시작
                _ = StartCooldownTimer(playerId, actionType, actionData.cooldownDuration);
            }

            // 상태를 Idle로 복귀
            var previousState = playerData.currentState;
            playerData.currentState = MongdungState.Idle;

            // 상태 변경 메시지 발행
            _stateChangedPublisher.Publish(new MongdungStateChangedMessage(playerId, previousState, MongdungState.Idle));

            // 이동 제한 해제 메시지 발행
            if (actionData.blockMovement)
            {
                _movementBlockedPublisher.Publish(new MongdungMovementBlockedMessage(playerId, false, "Action completed"));
            }

            // 액션 완료 메시지 발행
            _actionCompletedPublisher.Publish(new MongdungActionCompletedMessage(playerId, actionType, success, position));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungServiceImpl] 액션 완료: PlayerId={playerId}, ActionType={actionType}, Success={success}");
            }
        }

        private async UniTask StartCooldownTimer(long playerId, MongdungActionType actionType, float cooldownDuration)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(cooldownDuration));

            var actionStates = GetOrCreateActionStates(playerId);
            if (actionStates.ContainsKey(actionType))
            {
                actionStates[actionType].IsOnCooldown = false;
                actionStates[actionType].RemainingCooldownTime = 0f;

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungServiceImpl] 쿨다운 완료: PlayerId={playerId}, ActionType={actionType}");
                }
            }
        }

        public bool CanExecuteAction(long playerId, MongdungActionType actionType)
        {
            var playerData = GetOrCreatePlayerData(playerId);
            var actionStates = GetOrCreateActionStates(playerId);

            // 게임 종료 상태면 실행 불가
            if (playerData.currentState == MongdungState.GameEnd)
                return false;

            // 이미 다른 액션 실행 중이면 실행 불가
            if (playerData.currentState == MongdungState.ExecutingAction)
                return false;

            // 해당 액션이 쿨다운 중이거나 실행 중이면 실행 불가
            var actionState = actionStates[actionType];
            return actionState.CanExecute;
        }

        public async UniTask<bool> UpdateStateAsync(long playerId, MongdungState newState)
        {
            var playerData = GetOrCreatePlayerData(playerId);
            var previousState = playerData.currentState;

            if (previousState != newState)
            {
                playerData.currentState = newState;
                _stateChangedPublisher.Publish(new MongdungStateChangedMessage(playerId, previousState, newState));

                // 서버에 상태 업데이트
                bool networkSuccess = await _networkSource.UpdateMongdungStateAsync(newState);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungServiceImpl] 상태 업데이트: PlayerId={playerId}, {previousState} → {newState}");
                }

                return networkSuccess;
            }

            return true;
        }

        public MongdungState GetCurrentState(long playerId)
        {
            var playerData = GetOrCreatePlayerData(playerId);
            return playerData.currentState;
        }

        public float GetRemainingCooldown(long playerId, MongdungActionType actionType)
        {
            var actionStates = GetOrCreateActionStates(playerId);
            return actionStates[actionType].RemainingCooldownTime;
        }

        public void RegisterMongdungPlayer(long playerId, string playerName)
        {
            if (!_playerDataMap.ContainsKey(playerId))
            {
                var playerData = new MongdungData
                {
                    playerId = playerId,
                    playerName = playerName,
                    currentState = MongdungState.Idle
                };

                _playerDataMap[playerId] = playerData;
                InitializeActionStates(playerId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungServiceImpl] 몽둥이 플레이어 등록: PlayerId={playerId}, Name={playerName}");
                }
            }
        }

        public void UnregisterMongdungPlayer(long playerId)
        {
            _playerDataMap.Remove(playerId);
            _actionStatesMap.Remove(playerId);

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungServiceImpl] 몽둥이 플레이어 해제: PlayerId={playerId}");
            }
        }

        private MongdungData GetOrCreatePlayerData(long playerId)
        {
            if (!_playerDataMap.ContainsKey(playerId))
            {
                RegisterMongdungPlayer(playerId, $"Player_{playerId}");
            }

            return _playerDataMap[playerId];
        }

        private Dictionary<MongdungActionType, MongdungActionState> GetOrCreateActionStates(long playerId)
        {
            if (!_actionStatesMap.ContainsKey(playerId))
            {
                InitializeActionStates(playerId);
            }

            return _actionStatesMap[playerId];
        }

        private void InitializeActionStates(long playerId)
        {
            var actionStates = new Dictionary<MongdungActionType, MongdungActionState>
            {
                { MongdungActionType.Attack, new MongdungActionState(MongdungActionType.Attack) },
                { MongdungActionType.TrapSetting, new MongdungActionState(MongdungActionType.TrapSetting) },
                { MongdungActionType.Frighten, new MongdungActionState(MongdungActionType.Frighten) }
            };

            _actionStatesMap[playerId] = actionStates;
        }
    }
}