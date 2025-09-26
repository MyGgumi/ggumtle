using System;
using System.Collections.Generic;
using Features.Mongdung.Models;
using Features.Mongdung.NetworkSources;
using Features.Mongdung.Messages;
using Cysharp.Threading.Tasks;
using MessagePipe;
using R3;
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
        private readonly IPublisher<MongdungAttackActionMessage> _attackActionPublisher;
        private readonly IPublisher<MongdungSkillActionMessage> _skillActionPublisher;
        private readonly ISubscriber<MongdungActionNetworkResponseMessage> _networkResponseSubscriber;
        private readonly ISubscriber<MongdungActionBroadcastMessage> _actionBroadcastSubscriber;
        private readonly ISubscriber<MongdungActionRequestMessage> _actionRequestSubscriber;
        private readonly ISubscriber<MongdungAttackResponseMessage> _attackResponseSubscriber;
        private readonly ISubscriber<MongdungSkillResponseMessage> _skillResponseSubscriber;

        private readonly bool _enableDebugLogs = true;

        // 플레이어별 몽둥이 데이터 관리
        private readonly Dictionary<long, MongdungData> _playerDataMap = new();
        private readonly Dictionary<long, Dictionary<MongdungActionType, MongdungActionState>> _actionStatesMap = new();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public MongdungServiceImpl(
            IMongdungNetworkSource networkSource,
            IPublisher<MongdungActionStartedMessage> actionStartedPublisher,
            IPublisher<MongdungActionCompletedMessage> actionCompletedPublisher,
            IPublisher<MongdungStateChangedMessage> stateChangedPublisher,
            IPublisher<MongdungMovementBlockedMessage> movementBlockedPublisher,
            IPublisher<MongdungAttackActionMessage> attackActionPublisher,
            IPublisher<MongdungSkillActionMessage> skillActionPublisher,
            ISubscriber<MongdungActionNetworkResponseMessage> networkResponseSubscriber,
            ISubscriber<MongdungActionBroadcastMessage> actionBroadcastSubscriber,
            ISubscriber<MongdungActionRequestMessage> actionRequestSubscriber,
            ISubscriber<MongdungAttackResponseMessage> attackResponseSubscriber,
            ISubscriber<MongdungSkillResponseMessage> skillResponseSubscriber)
        {
            _networkSource = networkSource ?? throw new ArgumentNullException(nameof(networkSource));
            _actionStartedPublisher = actionStartedPublisher;
            _actionCompletedPublisher = actionCompletedPublisher;
            _stateChangedPublisher = stateChangedPublisher;
            _movementBlockedPublisher = movementBlockedPublisher;
            _attackActionPublisher = attackActionPublisher;
            _skillActionPublisher = skillActionPublisher;
            _networkResponseSubscriber = networkResponseSubscriber;
            _actionBroadcastSubscriber = actionBroadcastSubscriber;
            _actionRequestSubscriber = actionRequestSubscriber;
            _attackResponseSubscriber = attackResponseSubscriber;
            _skillResponseSubscriber = skillResponseSubscriber;

            // 네트워크 응답 구독
            _networkResponseSubscriber
                .Subscribe(OnNetworkResponseReceived)
                .AddTo(_disposables);

            // 액션 브로드캐스트 구독 (원격 플레이어 애니메이션용)
            _actionBroadcastSubscriber
                .Subscribe(OnActionBroadcastReceived)
                .AddTo(_disposables);

            // 액션 요청 구독 (로컬 입력 처리용)
            _actionRequestSubscriber
                .Subscribe(OnActionRequestReceived)
                .AddTo(_disposables);

            // 공격 응답 구독
            _attackResponseSubscriber
                .Subscribe(OnAttackResponseReceived)
                .AddTo(_disposables);

            // 스킬 응답 구독
            _skillResponseSubscriber
                .Subscribe(OnSkillResponseReceived)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungServiceImpl] 초기화 완료");
            }
        }

        public async UniTask<bool> ExecuteActionAsync(long playerId, MongdungActionType actionType, Vector3 position, Vector3 direction, long targetId = -1)
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
                bool networkSuccess = await _networkSource.SendMongdungActionAsync(actionType, position, direction, targetId);

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

        /// <summary>
        /// 액션 요청 메시지 처리 (로컬 입력 처리용)
        /// </summary>
        private async void OnActionRequestReceived(MongdungActionRequestMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MongdungServiceImpl] 액션 요청 수신: PlayerId={message.PlayerId}, ActionType={actionName}, Source={message.RequestSource}");
                }

                // AttackHitDetector를 위해 targetId 계산 (Attack 액션의 경우)
                long targetId = message.TargetId;
                if (message.ActionType == MongdungActionType.Attack)
                {
                    // MongdungGameObject의 AttackHitDetector를 통해 실제 targetId 계산
                    targetId = GetAttackTargetId(message.PlayerId, message.Direction);
                }

                // ExecuteActionAsync 호출
                bool success = await ExecuteActionAsync(
                    message.PlayerId,
                    message.ActionType,
                    message.Position,
                    message.Direction,
                    targetId
                );

                if (_enableDebugLogs)
                {
                    string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MongdungServiceImpl] 액션 요청 처리 결과: {actionName} = {success}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] 액션 요청 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 액션 브로드캐스트 메시지 처리 (원격 플레이어 애니메이션용)
        /// </summary>
        private void OnActionBroadcastReceived(MongdungActionBroadcastMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MongdungServiceImpl] 액션 브로드캐스트 수신: PlayerId={message.PlayerId}, ActionType={actionName}, Status={message.StatusCode}");
                }

                // StatusCode 0 = 액션 시작
                if (message.StatusCode == 0)
                {
                    // 액션 시작 메시지를 모든 등록된 몽둥이 플레이어에게 발행
                    foreach (var playerId in _playerDataMap.Keys)
                    {
                        var playerData = GetOrCreatePlayerData(playerId);
                        var actionData = playerData.GetActionData(message.ActionType);

                        _actionStartedPublisher.Publish(new MongdungActionStartedMessage(
                            playerId,
                            message.ActionType,
                            message.Position,
                            actionData.executionDuration
                        ));
                    }

                    if (_enableDebugLogs)
                    {
                        string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                        Debug.Log($"[MongdungServiceImpl] 액션 시작 메시지를 모든 몽둥이에게 발행: {actionName}");
                    }
                }
                // StatusCode 1 = 액션 완료 성공
                else if (message.StatusCode == 1)
                {
                    // 액션 완료 성공 메시지를 모든 등록된 몽둥이 플레이어에게 발행
                    foreach (var playerId in _playerDataMap.Keys)
                    {
                        _actionCompletedPublisher.Publish(new MongdungActionCompletedMessage(
                            playerId,
                            message.ActionType,
                            true,
                            message.Position
                        ));
                    }

                    if (_enableDebugLogs)
                    {
                        string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                        Debug.Log($"[MongdungServiceImpl] 액션 완료 성공 메시지를 모든 몽둥이에게 발행: {actionName}");
                    }
                }
                // StatusCode 2 = 액션 완료 실패
                else if (message.StatusCode == 2)
                {
                    // 액션 완료 실패 메시지를 모든 등록된 몽둥이 플레이어에게 발행
                    foreach (var playerId in _playerDataMap.Keys)
                    {
                        _actionCompletedPublisher.Publish(new MongdungActionCompletedMessage(
                            playerId,
                            message.ActionType,
                            false,
                            message.Position
                        ));
                    }

                    if (_enableDebugLogs)
                    {
                        string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                        Debug.Log($"[MongdungServiceImpl] 액션 완료 실패 메시지를 모든 몽둥이에게 발행: {actionName}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] 액션 브로드캐스트 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 네트워크 응답 메시지 처리
        /// </summary>
        private void OnNetworkResponseReceived(MongdungActionNetworkResponseMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MongdungServiceImpl] 네트워크 응답 수신: PlayerId={message.PlayerId}, ActionType={actionName}, Success={message.Success}");
                }

                // 액션 완료 처리
                var playerData = GetOrCreatePlayerData(message.PlayerId);
                var actionStates = GetOrCreateActionStates(message.PlayerId);
                var actionState = actionStates[message.ActionType];
                var actionData = playerData.GetActionData(message.ActionType);

                // 액션 실행 상태 정리
                actionState.IsExecuting = false;

                // 성공한 경우 쿨다운 시작
                if (message.Success)
                {
                    actionState.IsOnCooldown = true;
                    actionState.RemainingCooldownTime = actionData.cooldownDuration;

                    // 쿨다운 타이머 시작
                    _ = StartCooldownTimer(message.PlayerId, message.ActionType, actionData.cooldownDuration);
                }

                // 상태를 Idle로 복귀
                var previousState = playerData.currentState;
                playerData.currentState = MongdungState.Idle;

                // 이동 제한 해제
                if (actionData.blockMovement)
                {
                    _movementBlockedPublisher.Publish(new MongdungMovementBlockedMessage(message.PlayerId, false, "Network response completed"));
                }

                // 상태 변경 메시지 발행
                _stateChangedPublisher.Publish(new MongdungStateChangedMessage(message.PlayerId, previousState, MongdungState.Idle));

                // 액션 완료 메시지 발행 (ViewModel과 View에서 처리)
                _actionCompletedPublisher.Publish(new MongdungActionCompletedMessage(
                    message.PlayerId,
                    message.ActionType,
                    message.Success,
                    Vector3.zero
                ));

                if (_enableDebugLogs)
                {
                    string actionName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MongdungServiceImpl] 네트워크 응답 처리 완료: {actionName}, Success={message.Success}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] 네트워크 응답 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 몽둥이 공격 응답 처리 - 바로 AttackAction 메시지 발행
        /// </summary>
        private void OnAttackResponseReceived(MongdungAttackResponseMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungServiceImpl] 몽둥이 공격 응답 수신: Result={message.Result}, TargetId={message.TargetId}, LeftHp={message.LeftHp}, Success={message.Success}");
                }

                // 모든 몽둥이/몽깅이에게 Attack 애니메이션 실행 메시지 발행
                var attackActionMessage = new MongdungAttackActionMessage(
                    message.Result,
                    message.TargetId,
                    message.LeftHp
                );
                _attackActionPublisher.Publish(attackActionMessage);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungServiceImpl] Attack 애니메이션 메시지를 모든 몽둥이/몽깅이에게 발행: Success={message.Success}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] 공격 응답 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 몽둥이 스킬 응답 처리 - 바로 SkillAction 메시지 발행
        /// </summary>
        private void OnSkillResponseReceived(MongdungSkillResponseMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    string skillName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MongdungServiceImpl] 몽둥이 스킬 응답 수신: SkillType={message.SkillType}, ActionType={skillName}, Result={message.Result}, Success={message.Success}");
                }

                // 모든 몽둥이/몽깅이에게 Skill 애니메이션 실행 메시지 발행
                var skillActionMessage = new MongdungSkillActionMessage(
                    message.SkillType,
                    message.ActionType,
                    message.Result
                );
                _skillActionPublisher.Publish(skillActionMessage);

                if (_enableDebugLogs)
                {
                    string skillName = message.ActionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MongdungServiceImpl] {skillName} 애니메이션 메시지를 모든 몽둥이/몽깅이에게 발행: Success={message.Success}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] 스킬 응답 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 공격 대상 ID 계산 (AttackHitDetector 활용)
        /// </summary>
        private long GetAttackTargetId(long playerId, Vector3 direction)
        {
            try
            {
                // Scene에서 해당 PlayerId를 가진 MongdungGameObject 찾기
                var mongdungGameObjects = UnityEngine.Object.FindObjectsOfType<Features.Mongdung.Views.MongdungGameObject>();

                foreach (var mongdungObj in mongdungGameObjects)
                {
                    if (mongdungObj.PlayerId == playerId)
                    {
                        var attackDetector = mongdungObj.GetComponent<Features.Mongdung.Components.AttackHitDetector>();
                        if (attackDetector != null)
                        {
                            long targetId = attackDetector.DetectHitTarget(direction);

                            if (_enableDebugLogs)
                            {
                                Debug.Log($"[MongdungServiceImpl] AttackHitDetector 결과: PlayerId={playerId}, TargetId={targetId}");
                            }

                            return targetId;
                        }
                    }
                }

                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[MongdungServiceImpl] PlayerId={playerId}에 해당하는 MongdungGameObject 또는 AttackHitDetector를 찾을 수 없음");
                }

                return -1;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungServiceImpl] AttackHitDetector 호출 실패: {e.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}