using System;
using System.Collections.Generic;
using System.Linq;
using Features.Mongging.Messages;
using Features.Mongging.Models;
using Features.Mongdung.Messages;
using MessagePipe;
using Networks.Players;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Mongging.Services
{
    /// <summary>
    /// 몽깅이 팀 전체 관리 서비스 구현체
    /// 몽둥이 액션 구독 및 네트워크 연동 처리
    /// </summary>
    public class MonggingTeamServiceImpl : IMonggingTeamService, IStartable, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<int> TotalPlayerCount => _totalPlayerCount;
        public ReadOnlyReactiveProperty<int> AlivePlayerCount => _alivePlayerCount;
        public ReadOnlyReactiveProperty<int> FaintedPlayerCount => _faintedPlayerCount;
        public ReadOnlyReactiveProperty<int> DeadPlayerCount => _deadPlayerCount;
        public ReadOnlyReactiveProperty<int> EscapedPlayerCount => _escapedPlayerCount;
        public ReadOnlyReactiveProperty<bool> IsGameOver => _isGameOver;
        public ReadOnlyReactiveProperty<Dictionary<long, MonggingPlayerData>> AllPlayers => _allPlayers;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<int> _totalPlayerCount = new(0);
        private readonly ReactiveProperty<int> _alivePlayerCount = new(0);
        private readonly ReactiveProperty<int> _faintedPlayerCount = new(0);
        private readonly ReactiveProperty<int> _deadPlayerCount = new(0);
        private readonly ReactiveProperty<int> _escapedPlayerCount = new(0);
        private readonly ReactiveProperty<bool> _isGameOver = new(false);
        private readonly ReactiveProperty<Dictionary<long, MonggingPlayerData>> _allPlayers = new(new());

        private readonly MonggingTeamData _teamData = new();
        private readonly Dictionary<long, IMonggingPlayerService> _playerServices = new();
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Dependencies

        private readonly IPublisher<MonggingTeamSyncMessage> _teamSyncPublisher;
        private readonly IPublisher<MonggingTeamPlayerUpdatedMessage> _playerUpdatedPublisher;
        private readonly IPublisher<MonggingTeamGameOverMessage> _gameOverPublisher;
        private readonly IPublisher<MonggingPlayerHitMessage> _hitPublisher;
        private readonly IPublisher<MonggingPlayerStateChangedMessage> _stateChangedPublisher;
        private readonly IPublisher<MonggingPlayerHealMessage> _healPublisher;
        private readonly IPublisher<MonggingPlayerStatusEffectMessage> _statusEffectPublisher;
        private readonly IPublisher<MonggingPlayerRevivedMessage> _revivedPublisher;
        private readonly IPublisher<MonggingPlayerEscapedMessage> _escapedPublisher;
        private readonly IPublisher<MonggingPlayerAnimationMessage> _animationPublisher;

        // 몽둥이 액션 구독
        private readonly ISubscriber<MongdungAttackActionMessage> _attackActionSubscriber;
        private readonly ISubscriber<MongdungSkillActionMessage> _skillActionSubscriber;

        // 네트워크 브로드캐스트 구독
        private readonly ISubscriber<MonggingStateBroadcastMessage> _stateBroadcastSubscriber;
        private readonly ISubscriber<MonggingPlayerRevivedMessage> _playerRevivedSubscriber;
        private readonly ISubscriber<MonggingPlayerServerStateMessage> _serverStateSubscriber;

        #endregion

        #region Constructor

        [Inject]
        public MonggingTeamServiceImpl(
            IPublisher<MonggingTeamSyncMessage> teamSyncPublisher,
            IPublisher<MonggingTeamPlayerUpdatedMessage> playerUpdatedPublisher,
            IPublisher<MonggingTeamGameOverMessage> gameOverPublisher,
            IPublisher<MonggingPlayerHitMessage> hitPublisher,
            IPublisher<MonggingPlayerStateChangedMessage> stateChangedPublisher,
            IPublisher<MonggingPlayerHealMessage> healPublisher,
            IPublisher<MonggingPlayerStatusEffectMessage> statusEffectPublisher,
            IPublisher<MonggingPlayerRevivedMessage> revivedPublisher,
            IPublisher<MonggingPlayerEscapedMessage> escapedPublisher,
            IPublisher<MonggingPlayerAnimationMessage> animationPublisher,
            ISubscriber<MongdungAttackActionMessage> attackActionSubscriber,
            ISubscriber<MongdungSkillActionMessage> skillActionSubscriber,
            ISubscriber<MonggingStateBroadcastMessage> stateBroadcastSubscriber,
            ISubscriber<MonggingPlayerRevivedMessage> playerRevivedSubscriber,
            ISubscriber<MonggingPlayerServerStateMessage> serverStateSubscriber)
        {
            _teamSyncPublisher = teamSyncPublisher;
            _playerUpdatedPublisher = playerUpdatedPublisher;
            _gameOverPublisher = gameOverPublisher;
            _hitPublisher = hitPublisher;
            _stateChangedPublisher = stateChangedPublisher;
            _healPublisher = healPublisher;
            _statusEffectPublisher = statusEffectPublisher;
            _revivedPublisher = revivedPublisher;
            _escapedPublisher = escapedPublisher;
            _animationPublisher = animationPublisher;
            _attackActionSubscriber = attackActionSubscriber;
            _skillActionSubscriber = skillActionSubscriber;
            _stateBroadcastSubscriber = stateBroadcastSubscriber;
            _playerRevivedSubscriber = playerRevivedSubscriber;
            _serverStateSubscriber = serverStateSubscriber;

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region IStartable Implementation

        public void Start()
        {
            Initialize();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] EntryPoint로 자동 시작됨");
            }
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            SubscribeToMongdungActions();
            SubscribeToNetworkEvents();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] 팀 서비스 초기화 및 구독 완료");
            }
        }

        public void RegisterPlayer(long playerId, string playerName, MonggingPlayerType playerType, bool isLocal = false)
        {
            if (_playerServices.ContainsKey(playerId))
            {
                Debug.LogWarning($"[MonggingTeamServiceImpl] 이미 등록된 플레이어: {playerId}");
                return;
            }

            // TODO: DI Container에서 MonggingPlayerService 생성하도록 개선 필요
            // 현재는 임시로 직접 생성
            var playerService = new MonggingPlayerServiceImpl(
                _stateChangedPublisher,
                _hitPublisher,
                _healPublisher,
                _statusEffectPublisher,
                _revivedPublisher,
                _escapedPublisher,
                _animationPublisher
            );

            playerService.Initialize(playerId, playerName, playerType, isLocal);
            _playerServices[playerId] = playerService;

            var playerData = playerService.GetPlayerData();
            _teamData.UpdatePlayer(playerData);

            UpdateObservables();

            _playerUpdatedPublisher.Publish(new MonggingTeamPlayerUpdatedMessage(playerId, playerData));

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingTeamServiceImpl] 플레이어 등록: ID={playerId}, Name={playerName}, Type={playerType}, Local={isLocal}");
            }
        }

        public void UnregisterPlayer(long playerId)
        {
            if (_playerServices.TryGetValue(playerId, out var playerService))
            {
                playerService.Dispose();
                _playerServices.Remove(playerId);
            }

            _teamData.RemovePlayer(playerId);
            UpdateObservables();

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingTeamServiceImpl] 플레이어 해제: ID={playerId}");
            }
        }

        public MonggingPlayerData GetPlayer(long playerId)
        {
            return _teamData.GetPlayer(playerId);
        }

        public MonggingPlayerData GetLocalPlayer()
        {
            return _teamData.GetLocalPlayer();
        }

        public List<MonggingPlayerData> GetAlivePlayers()
        {
            return _teamData.GetAlivePlayers();
        }

        public List<MonggingPlayerData> GetFaintedPlayers()
        {
            return _teamData.GetFaintedPlayers();
        }

        public List<MonggingPlayerData> GetAllPlayers()
        {
            return _playerServices.Values.Select(service => service.GetPlayerData()).ToList();
        }

        public void Reset()
        {
            foreach (var playerService in _playerServices.Values)
            {
                playerService.Reset();
            }

            _teamData.Clear();
            UpdateObservables();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] 팀 데이터 초기화");
            }
        }

        #endregion

        #region Private Methods

        private void SubscribeToMongdungActions()
        {
            // 몽둥이 공격 액션 구독
            _attackActionSubscriber
                .Subscribe(OnMongdungAttackAction)
                .AddTo(_disposables);

            // 몽둥이 스킬 액션 구독
            _skillActionSubscriber
                .Subscribe(OnMongdungSkillAction)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] 몽둥이 액션 구독 완료");
            }
        }

        private void SubscribeToNetworkEvents()
        {
            // 서버 상태 브로드캐스트 구독
            _stateBroadcastSubscriber
                .Subscribe(OnStateBroadcastReceived)
                .AddTo(_disposables);

            // 플레이어 부활 구독
            _playerRevivedSubscriber
                .Subscribe(OnPlayerRevivedReceived)
                .AddTo(_disposables);

            // 서버 상태 구독
            _serverStateSubscriber
                .Subscribe(OnServerStateReceived)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] 네트워크 이벤트 구독 완료");
            }
        }

        /// <summary>
        /// 몽둥이 공격 액션 처리
        /// </summary>
        private void OnMongdungAttackAction(MongdungAttackActionMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingTeamServiceImpl] 몽둥이 공격 액션 수신: TargetId={message.TargetId}, Result={message.Result}, LeftHp={message.LeftHp}");
                }

                // targetId가 유효한 몽깅이인지 확인
                if (message.TargetId > 0 && _playerServices.TryGetValue(message.TargetId, out var targetPlayerService))
                {
                    // 즉시 피격 애니메이션 (빠른 피드백) - TODO: 실제 Publisher 주입 필요
                    // GlobalMessagePipe 대신 실제 Publisher 사용 예정

                    // 피격 처리 (Local/Remote 모두)
                    var targetData = targetPlayerService.GetPlayerData();
                    if (message.Result == MongdungAttackResult.HitSuccess)
                    {
                        if (targetData.isLocal)
                        {
                            // Local 플레이어: 실제 데미지 처리
                            targetPlayerService.TakeDamage(40); // 기본 공격 데미지
                            UpdateObservables();

                            if (_enableDebugLogs)
                            {
                                Debug.Log($"[MonggingTeamServiceImpl] 로컬 몽깅이 피격 처리: TargetId={message.TargetId}, Damage=40");
                            }
                        }
                        else
                        {
                            // Remote 플레이어: 시각적 피격 애니메이션만
                            var hitMessage = new MonggingPlayerHitMessage(
                                message.TargetId,
                                40, // 데미지 (시각적 목적)
                                targetData.currentHp, // 이전 HP
                                targetData.currentHp, // 현재 HP (Remote는 변경하지 않음)
                                targetData.currentState, // 현재 상태
                                Vector3.zero // 피격 위치 (추후 구현)
                            );

                            _hitPublisher.Publish(hitMessage);

                            if (_enableDebugLogs)
                            {
                                Debug.Log($"[MonggingTeamServiceImpl] 원격 몽깅이 피격 애니메이션: TargetId={message.TargetId}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] 몽둥이 공격 액션 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 몽둥이 스킬 액션 처리
        /// </summary>
        private void OnMongdungSkillAction(MongdungSkillActionMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    string skillName = message.ActionType == Features.Mongdung.Models.MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : message.ActionType.ToString();
                    Debug.Log($"[MonggingTeamServiceImpl] 몽둥이 스킬 액션 수신: SkillType={message.SkillType}, ActionType={skillName}, Result={message.Result}");
                }

                // 스킬 타입에 따른 처리
                switch (message.ActionType)
                {
                    case Features.Mongdung.Models.MongdungActionType.Frighten:
                        HandleFrightenSkill(message);
                        break;

                    case Features.Mongdung.Models.MongdungActionType.TrapSetting:
                        HandleTrapSkill(message);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] 몽둥이 스킬 액션 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 공포 스킬 처리
        /// </summary>
        private void HandleFrightenSkill(MongdungSkillActionMessage message)
        {
            if (message.Result == MongdungSkillResult.Success)
            {
                // 모든 Local 몽깅이에게 공포 적용
                foreach (var playerService in _playerServices.Values)
                {
                    var playerData = playerService.GetPlayerData();
                    if (playerData.isLocal && playerData.IsAlive && playerData.currentState == MonggingPlayerState.Normal)
                    {
                        playerService.ApplyFrighten(5f);
                    }
                }

                UpdateObservables();

                if (_enableDebugLogs)
                {
                    Debug.Log("[MonggingTeamServiceImpl] 공포 스킬 적용 완료 - 모든 로컬 몽깅이");
                }
            }
        }

        /// <summary>
        /// 함정 스킬 처리 (가짜 꿈틀이)
        /// </summary>
        private void HandleTrapSkill(MongdungSkillActionMessage message)
        {
            // TODO: 함정에 걸린 특정 몽깅이 ID가 필요
            // 현재는 서버에서 해당 정보를 제공하지 않으므로 추후 구현
            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] 함정 스킬 처리 - 추후 구현 필요");
            }
        }

        /// <summary>
        /// 서버 상태 브로드캐스트 처리
        /// </summary>
        private void OnStateBroadcastReceived(MonggingStateBroadcastMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingTeamServiceImpl] 서버 상태 브로드캐스트 수신: PlayerId={message.PlayerId}, StateType={message.StateType}");
                }

                if (_playerServices.TryGetValue(message.PlayerId, out var playerService))
                {
                    // 서버 상태 타입을 MonggingPlayerState로 변환
                    var state = ConvertServerStateType(message.StateType);
                    playerService.SyncFromServer(message.CurrentHp, state, message.FaintCount);

                    UpdateObservables();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] 서버 상태 브로드캐스트 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 부활 처리
        /// </summary>
        private void OnPlayerRevivedReceived(MonggingPlayerRevivedMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingTeamServiceImpl] 플레이어 부활 수신: RevivedId={message.RevivedPlayerId}, RevivingId={message.RevivingPlayerId}, HP={message.ReviveHp}");
                }

                if (_playerServices.TryGetValue(message.RevivedPlayerId, out var playerService))
                {
                    playerService.Revive(message.ReviveHp);
                    UpdateObservables();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] 플레이어 부활 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 서버 상태 타입을 MonggingPlayerState로 변환
        /// </summary>
        private MonggingPlayerState ConvertServerStateType(int stateType)
        {
            return stateType switch
            {
                0 => MonggingPlayerState.Normal,
                1 => MonggingPlayerState.Fainted,
                2 => MonggingPlayerState.Dead,
                3 => MonggingPlayerState.Escaped,
                4 => MonggingPlayerState.Stunned,
                5 => MonggingPlayerState.Frightened,
                _ => MonggingPlayerState.Normal
            };
        }

        private void UpdateObservables()
        {
            // 각 플레이어 서비스에서 최신 데이터 수집
            var allPlayerData = new Dictionary<long, MonggingPlayerData>();
            foreach (var kvp in _playerServices)
            {
                var playerData = kvp.Value.GetPlayerData();
                allPlayerData[kvp.Key] = playerData;
                _teamData.UpdatePlayer(playerData);
            }

            _totalPlayerCount.Value = _teamData.TotalPlayerCount;
            _alivePlayerCount.Value = _teamData.AlivePlayerCount;
            _faintedPlayerCount.Value = _teamData.FaintedPlayerCount;
            _deadPlayerCount.Value = _teamData.DeadPlayerCount;
            _escapedPlayerCount.Value = _teamData.EscapedPlayerCount;
            _isGameOver.Value = _teamData.IsGameOver();
            _allPlayers.Value = new Dictionary<long, MonggingPlayerData>(allPlayerData);

            // 팀 동기화 메시지 발행
            _teamSyncPublisher.Publish(new MonggingTeamSyncMessage(
                _teamData.TotalPlayerCount,
                _teamData.AlivePlayerCount,
                _teamData.FaintedPlayerCount,
                _teamData.DeadPlayerCount,
                _teamData.EscapedPlayerCount,
                _teamData.IsGameOver()
            ));

            // 게임 종료 체크
            if (_teamData.IsGameOver() && !_isGameOver.Value)
            {
                _gameOverPublisher.Publish(new MonggingTeamGameOverMessage(
                    _teamData.DeadPlayerCount == _teamData.TotalPlayerCount,
                    _teamData.EscapedPlayerCount == _teamData.TotalPlayerCount,
                    GetAllPlayers()
                ));
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();

            foreach (var playerService in _playerServices.Values)
            {
                playerService?.Dispose();
            }
            _playerServices.Clear();

            _totalPlayerCount?.Dispose();
            _alivePlayerCount?.Dispose();
            _faintedPlayerCount?.Dispose();
            _deadPlayerCount?.Dispose();
            _escapedPlayerCount?.Dispose();
            _isGameOver?.Dispose();
            _allPlayers?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamServiceImpl] Dispose 완료");
            }
        }

        /// <summary>
        /// 서버 상태 메시지 처리
        /// </summary>
        private void OnServerStateReceived(MonggingPlayerServerStateMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingTeamServiceImpl] 서버 상태 수신: PlayerId={message.PlayerId}, State={message.NewState}");
                    Debug.Log($"[MonggingTeamServiceImpl] 현재 등록된 플레이어 수: {_playerServices.Count}");
                    foreach (var kvp in _playerServices)
                    {
                        Debug.Log($"[MonggingTeamServiceImpl] 등록된 플레이어: {kvp.Key}");
                    }
                }

                if (_playerServices.TryGetValue(message.PlayerId, out var playerService))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[MonggingTeamServiceImpl] 플레이어 서비스 찾음: PlayerId={message.PlayerId}, Service={playerService}");
                    }

                    if (playerService == null)
                    {
                        Debug.LogError($"[MonggingTeamServiceImpl] PlayerService가 null입니다: PlayerId={message.PlayerId}");
                        return;
                    }

                    var playerData = playerService.GetPlayerData();
                    if (playerData == null)
                    {
                        Debug.LogError($"[MonggingTeamServiceImpl] PlayerData가 null입니다: PlayerId={message.PlayerId}");
                        return;
                    }

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[MonggingTeamServiceImpl] PlayerData 획득 성공: PlayerId={message.PlayerId}, PlayerData={playerData}");
                        Debug.Log($"[MonggingTeamServiceImpl] PlayerData Type: {playerData.GetType()}");
                    }

                    MonggingPlayerState previousState;
                    try
                    {
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MonggingTeamServiceImpl] PlayerData.currentState 접근 시도");
                        }
                        previousState = playerData.currentState;
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MonggingTeamServiceImpl] PreviousState 획득 성공: {previousState}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MonggingTeamServiceImpl] PlayerData.currentState 접근 실패: {ex.Message}");
                        Debug.LogError($"[MonggingTeamServiceImpl] Stack trace: {ex.StackTrace}");
                        return;
                    }

                    // 서버 상태에 따라 로컬 상태 업데이트
                    switch (message.NewState)
                    {
                        case MonggingPlayerState.Fainted:
                            // 기절 상태로 변경 (HP는 0으로 설정)
                            try
                            {
                                if (_enableDebugLogs)
                                {
                                    Debug.Log($"[MonggingTeamServiceImpl] Fainted 처리 시작 - faintCount 접근 시도");
                                }
                                int currentFaintCount = playerData.faintCount;
                                int newFaintCount = currentFaintCount + 1;
                                if (_enableDebugLogs)
                                {
                                    Debug.Log($"[MonggingTeamServiceImpl] FaintCount: {currentFaintCount} → {newFaintCount}");
                                    Debug.Log($"[MonggingTeamServiceImpl] SyncFromServer 호출 시도");
                                }
                                playerService.SyncFromServer(0, message.NewState, newFaintCount);
                                if (_enableDebugLogs)
                                {
                                    Debug.Log($"[MonggingTeamServiceImpl] SyncFromServer 호출 완료");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[MonggingTeamServiceImpl] Fainted 처리 실패: {ex.Message}");
                                Debug.LogError($"[MonggingTeamServiceImpl] Stack trace: {ex.StackTrace}");
                            }
                            break;

                        case MonggingPlayerState.Dead:
                            // 사망 상태로 변경
                            playerService.SyncFromServer(0, message.NewState, playerData.faintCount);
                            break;

                        case MonggingPlayerState.Escaped:
                            // 탈출 상태로 변경
                            playerService.Escape();
                            break;

                        case MonggingPlayerState.Stunned:
                            // 스턴 상태로 변경
                            playerService.ApplyStun(2f);
                            break;

                        default:
                            if (_enableDebugLogs)
                            {
                                Debug.LogWarning($"[MonggingTeamServiceImpl] 처리되지 않은 서버 상태: {message.NewState}");
                            }
                            break;
                    }

                    UpdateObservables();

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[MonggingTeamServiceImpl] 서버 상태 동기화 완료: PlayerId={message.PlayerId}, {previousState} → {message.NewState}");
                    }
                }
                else
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[MonggingTeamServiceImpl] 플레이어 서비스를 찾을 수 없음: PlayerId={message.PlayerId}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] 서버 상태 처리 실패: {e.Message}");
                Debug.LogError($"[MonggingTeamServiceImpl] Stack trace: {e.StackTrace}");
                Debug.LogError($"[MonggingTeamServiceImpl] Exception type: {e.GetType()}");
            }
        }

        #endregion
    }
}