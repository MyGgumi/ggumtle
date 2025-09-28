using System;
using System.Collections.Generic;
using System.Linq;
using Features.Mongging.Messages;
using Features.Mongging.Models;
using Features.Mongdung.Messages;
using Features.PlayerHealth.Services;
using Features.PlayerList.Messages;
using Features.Revival.Messages;
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

        // PlayerHealth 연동
        private readonly IPlayerHealthService _playerHealthService;

        // PlayerList 연동
        private readonly IPublisher<PlayerListMonggingStateUpdateMessage> _playerListStatePublisher;

        // 몽둥이 액션 구독
        private readonly ISubscriber<MongdungAttackActionMessage> _attackActionSubscriber;
        private readonly ISubscriber<MongdungSkillActionMessage> _skillActionSubscriber;

        // 네트워크 브로드캐스트 구독
        private readonly ISubscriber<MonggingStateBroadcastMessage> _stateBroadcastSubscriber;
        private readonly ISubscriber<MonggingPlayerRevivedMessage> _playerRevivedSubscriber;
        private readonly ISubscriber<MonggingPlayerServerStateMessage> _serverStateSubscriber;

        // Revival 시스템 구독
        private readonly ISubscriber<RevivalCompletedMessage> _revivalCompletedSubscriber;

        // EscapeGate 시스템 구독
        private readonly ISubscriber<MonggingPlayerEscapedMessage> _playerEscapedSubscriber;

        // 상호작용 상태 발행
        private readonly IPublisher<Features.Revival.Messages.MonggingInteractableStateMessage> _interactableStatePublisher;

        // 체력바 시스템 메시지 발행
        private readonly IPublisher<Features.PlayerHealth.Messages.HealReceivedMessage> _healReceivedPublisher;

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
            IPlayerHealthService playerHealthService,
            IPublisher<PlayerListMonggingStateUpdateMessage> playerListStatePublisher,
            ISubscriber<MongdungAttackActionMessage> attackActionSubscriber,
            ISubscriber<MongdungSkillActionMessage> skillActionSubscriber,
            ISubscriber<MonggingStateBroadcastMessage> stateBroadcastSubscriber,
            ISubscriber<MonggingPlayerRevivedMessage> playerRevivedSubscriber,
            ISubscriber<MonggingPlayerServerStateMessage> serverStateSubscriber,
            ISubscriber<RevivalCompletedMessage> revivalCompletedSubscriber,
            ISubscriber<MonggingPlayerEscapedMessage> playerEscapedSubscriber,
            IPublisher<Features.Revival.Messages.MonggingInteractableStateMessage> interactableStatePublisher,
            IPublisher<Features.PlayerHealth.Messages.HealReceivedMessage> healReceivedPublisher)
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
            _playerHealthService = playerHealthService;
            _playerListStatePublisher = playerListStatePublisher;
            _attackActionSubscriber = attackActionSubscriber;
            _skillActionSubscriber = skillActionSubscriber;
            _stateBroadcastSubscriber = stateBroadcastSubscriber;
            _playerRevivedSubscriber = playerRevivedSubscriber;
            _serverStateSubscriber = serverStateSubscriber;
            _revivalCompletedSubscriber = revivalCompletedSubscriber;
            _playerEscapedSubscriber = playerEscapedSubscriber;
            _interactableStatePublisher = interactableStatePublisher;
            _healReceivedPublisher = healReceivedPublisher;

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

            // Revival 완료 구독
            _revivalCompletedSubscriber
                .Subscribe(OnRevivalCompleted)
                .AddTo(_disposables);

            // EscapeGate 탈출 구독
            _playerEscapedSubscriber
                .Subscribe(OnPlayerEscapedFromGate)
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
                    // 피격 처리 (Local/Remote 모두)
                    var targetData = targetPlayerService.GetPlayerData();

                    // 공격 결과에 따른 처리
                    switch (message.Result)
                    {
                        case MongdungAttackResult.HitSuccess:
                            if (targetData.isLocal)
                            {
                                // Local 플레이어: 서버에서 받은 leftHP로 PlayerHealth 동기화
                                if (message.LeftHp >= 0 && _playerHealthService != null)
                                {
                                    _playerHealthService.SetHealth(message.LeftHp);

                                    if (_enableDebugLogs)
                                    {
                                        Debug.Log($"[MonggingTeamServiceImpl] PlayerHealth 동기화: TargetId={message.TargetId}, LeftHP={message.LeftHp}");
                                    }
                                }

                                // Mongging 시스템의 데미지 처리 (애니메이션용)
                                targetPlayerService.TakeDamage(40); // 기본 공격 데미지
                                UpdateObservables();

                                if (_enableDebugLogs)
                                {
                                    Debug.Log($"[MonggingTeamServiceImpl] 로컬 몽깅이 피격 처리: TargetId={message.TargetId}, Damage=40, ServerLeftHP={message.LeftHp}");
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
                            break;

                        case MongdungAttackResult.NotAlive:
                            // 타겟이 기절/사망 상태여서 공격 실패
                            if (_enableDebugLogs)
                            {
                                Debug.Log($"[MonggingTeamServiceImpl] 공격 실패 - 타겟이 기절/사망 상태: TargetId={message.TargetId}");
                            }
                            break;

                        case MongdungAttackResult.HitFail:
                        case MongdungAttackResult.PlayerNotFound:
                        case MongdungAttackResult.NotMongdung:
                        case MongdungAttackResult.TargetNotFound:
                            // 기타 실패 케이스들
                            if (_enableDebugLogs)
                            {
                                Debug.Log($"[MonggingTeamServiceImpl] 공격 실패 - {message.Result}: TargetId={message.TargetId}");
                            }
                            break;
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

        /// <summary>
        /// PlayerList에게 플레이어 상태 변경 알림
        /// </summary>
        private void NotifyPlayerListStateChange(long playerId, MonggingPlayerState newState)
        {
            try
            {
                // MonggingPlayerState를 PlayerList 상태 문자열로 변환
                string statusString = newState switch
                {
                    MonggingPlayerState.Normal => "default",
                    MonggingPlayerState.Fainted => "faint",
                    MonggingPlayerState.Dead => "dead",
                    MonggingPlayerState.Escaped => "escape",
                    MonggingPlayerState.Stunned => "default", // 스턴은 기본 아이콘 유지
                    MonggingPlayerState.Frightened => "default", // 공포도 기본 아이콘 유지
                    _ => "default"
                };

                // 플레이어 이름 가져오기
                string playerName = "Unknown";
                if (_playerServices.TryGetValue(playerId, out var playerService))
                {
                    var playerData = playerService.GetPlayerData();
                    playerName = playerData.playerName;
                }

                // PlayerList에게 상태 변경 알림
                _playerListStatePublisher.Publish(new PlayerListMonggingStateUpdateMessage(
                    playerId,
                    statusString,
                    playerName
                ));

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingTeamServiceImpl] PlayerList 상태 알림: Player{playerId} → {statusString} ({playerName})");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] PlayerList 상태 알림 실패: {e.Message}");
            }
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

                    // 서버 상태를 즉시 PlayerList에 알림 (상태 변경 전에 먼저 전파)
                    NotifyPlayerListStateChange(message.PlayerId, message.NewState);

                    // 서버 상태에 따라 로컬 상태 업데이트
                    switch (message.NewState)
                    {
                        case MonggingPlayerState.Normal:
                            // 정상 상태로 변경 - Revival 시스템에서 이미 처리되지 않은 경우만 처리
                            if (playerData.currentState == MonggingPlayerState.Fainted)
                            {
                                // 기절에서 정상으로 변경된 경우 (다른 플레이어가 직접 부활시켜줌)
                                // Revival 시스템에서 처리되지 않은 경우이므로 기본 체력으로 설정
                                var localPlayer = GetLocalPlayer();
                                if (localPlayer != null && localPlayer.playerId == message.PlayerId)
                                {
                                    // 체력바 시스템에 회복 메시지 전송
                                    _healReceivedPublisher.Publish(new Features.PlayerHealth.Messages.HealReceivedMessage(
                                        50, // healAmount
                                        0,  // previousHp
                                        50, // currentHp
                                        Features.PlayerHealth.Models.PlayerState.Normal // newState
                                    ));

                                    if (_enableDebugLogs)
                                    {
                                        Debug.Log($"[MonggingTeamServiceImpl] 체력바 시스템에 직접 부활 회복 메시지 전송: PlayerId={message.PlayerId}, HP=0→50");
                                    }
                                }
                                else if (_enableDebugLogs)
                                {
                                    Debug.Log($"[MonggingTeamServiceImpl] 체력바 시스템 부활 처리 건너뜀: LocalPlayer={localPlayer?.playerId}, TargetPlayer={message.PlayerId}");
                                }

                                // 몽깅이 시스템도 부활 체력으로 동기화
                                playerService.SyncFromServer(50, message.NewState, playerData.faintCount);

                                if (_enableDebugLogs)
                                {
                                    Debug.Log($"[MonggingTeamServiceImpl] 서버 상태 변경으로 직접 부활 처리: PlayerId={message.PlayerId}, HP=50, State=Normal");
                                }
                            }
                            else
                            {
                                // 이미 정상 상태이거나 다른 상태에서 정상으로 변경된 경우
                                playerService.SyncFromServer(playerData.currentHp, message.NewState, playerData.faintCount);
                            }

                            // 상호작용 불가능 상태로 변경 (정상 상태이므로)
                            PublishInteractableState(message.PlayerId, false, playerData.playerName);
                            break;

                        case MonggingPlayerState.Fainted:
                            // 기절 상태로 변경 - 서버 상태 그대로 동기화
                            playerService.SyncFromServer(0, message.NewState, playerData.faintCount);
                            // 상호작용 가능 상태로 변경
                            PublishInteractableState(message.PlayerId, true, playerData.playerName);
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

                        case MonggingPlayerState.Frightened:
                            // 공포 상태로 변경 - 서버 상태 그대로 동기화
                            playerService.SyncFromServer(playerData.currentHp, message.NewState, playerData.faintCount);
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

        /// <summary>
        /// Revival 시스템에서 부활 완료 메시지 처리
        /// </summary>
        private void OnRevivalCompleted(RevivalCompletedMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingTeamServiceImpl] Revival 부활 완료 수신: RevivedId={message.revivedPlayerId}, FromSelfDefib={message.isFromSelfDefib}");
                }

                if (_playerServices.TryGetValue(message.revivedPlayerId, out var playerService))
                {
                    // 체력바 시스템 업데이트 먼저 (로컬 플레이어만)
                    var localPlayer = GetLocalPlayer();
                    if (_playerHealthService != null && localPlayer != null && localPlayer.playerId == message.revivedPlayerId)
                    {
                        _playerHealthService.RevivePlayer(message.reviveHp);
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MonggingTeamServiceImpl] 로컬 플레이어 체력바 부활 처리 완료: PlayerId={message.revivedPlayerId}, HP={message.reviveHp}");
                        }
                    }
                    else if (_enableDebugLogs && localPlayer != null && localPlayer.playerId != message.revivedPlayerId)
                    {
                        Debug.Log($"[MonggingTeamServiceImpl] 원격 플레이어 부활 처리 완료: PlayerId={message.revivedPlayerId}, HP={message.reviveHp} (체력바 업데이트 없음)");
                    }

                    // 몽깅이 부활 처리
                    playerService.Revive(message.reviveHp);
                    UpdateObservables();

                    // HP 동기화 확인 로그
                    if (_enableDebugLogs)
                    {
                        var playerData = playerService.GetPlayerData();
                        Debug.Log($"[MonggingTeamServiceImpl] 몽깅이 부활 후 HP 확인: PlayerId={message.revivedPlayerId}, 설정HP={message.reviveHp}, 실제HP={playerData.currentHp}, 상태={playerData.currentState}");
                    }

                    // PlayerList에 상태 변경 알림
                    NotifyPlayerListStateChange(message.revivedPlayerId, MonggingPlayerState.Normal);

                    // 애니메이션 상태 변경 (기절 → 정상)
                    try
                    {
                        var animationMessage = new MonggingPlayerAnimationMessage(
                            message.revivedPlayerId,
                            "Revive", // 부활 애니메이션 트리거
                            1.0f // 애니메이션 지속시간
                        );
                        _animationPublisher.Publish(animationMessage);

                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MonggingTeamServiceImpl] 부활 애니메이션 메시지 발행: PlayerId={message.revivedPlayerId}");
                        }
                    }
                    catch (Exception animEx)
                    {
                        Debug.LogError($"[MonggingTeamServiceImpl] 애니메이션 메시지 발행 실패: {animEx.Message}");
                    }

                    // 상호작용 불가 상태로 변경
                    if (_playerServices.TryGetValue(message.revivedPlayerId, out var revivedPlayerService))
                    {
                        var revivedPlayerData = revivedPlayerService.GetPlayerData();
                        PublishInteractableState(message.revivedPlayerId, false, revivedPlayerData.playerName);
                    }

                    if (_enableDebugLogs)
                    {
                        string revivalType = message.isFromSelfDefib ? "자가제세동기" : "직접 부활";
                        Debug.Log($"[MonggingTeamServiceImpl] {revivalType} 부활 처리 완료: PlayerId={message.revivedPlayerId}, HP={message.reviveHp}");
                    }
                }
                else
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[MonggingTeamServiceImpl] 부활할 플레이어 서비스를 찾을 수 없음: PlayerId={message.revivedPlayerId}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] Revival 부활 완료 처리 실패: {e.Message}");
                Debug.LogError($"[MonggingTeamServiceImpl] Stack trace: {e.StackTrace}");
            }
        }

        /// <summary>
        /// EscapeGate에서 탈출한 플레이어 처리
        /// </summary>
        private void OnPlayerEscapedFromGate(MonggingPlayerEscapedMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingTeamServiceImpl] EscapeGate 탈출 메시지 수신: PlayerId={message.PlayerId}, Position={message.EscapePosition}");
                }

                if (_playerServices.TryGetValue(message.PlayerId, out var playerService))
                {
                    // 플레이어 상태를 탈출로 변경 (메시지 재발행 방지를 위해 SyncFromServer 사용)
                    var currentPlayerData = playerService.GetPlayerData();
                    playerService.SyncFromServer(currentPlayerData.currentHp, MonggingPlayerState.Escaped, currentPlayerData.faintCount);
                    UpdateObservables();

                    // PlayerList에 탈출 상태 알림
                    NotifyPlayerListStateChange(message.PlayerId, MonggingPlayerState.Escaped);

                    // 상호작용 불가 상태로 변경 (탈출했으므로)
                    PublishInteractableState(message.PlayerId, false, currentPlayerData.playerName);

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[MonggingTeamServiceImpl] 플레이어 탈출 처리 완료: PlayerId={message.PlayerId}, 탈출 위치={message.EscapePosition}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[MonggingTeamServiceImpl] 탈출할 플레이어 서비스를 찾을 수 없음: PlayerId={message.PlayerId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] EscapeGate 탈출 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 상호작용 상태 변경 메시지 발행
        /// </summary>
        private void PublishInteractableState(long playerId, bool isInteractable, string playerName)
        {
            try
            {
                var message = new Features.Revival.Messages.MonggingInteractableStateMessage(
                    playerId,
                    isInteractable,
                    playerName ?? "Unknown"
                );

                _interactableStatePublisher.Publish(message);

                if (_enableDebugLogs)
                {
                    string action = isInteractable ? "활성화" : "비활성화";
                    Debug.Log($"[MonggingTeamServiceImpl] 상호작용 상태 {action} 메시지 발행: PlayerId={playerId}, Name={playerName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingTeamServiceImpl] 상호작용 상태 메시지 발행 실패: {e.Message}");
            }
        }

        #endregion
    }
}