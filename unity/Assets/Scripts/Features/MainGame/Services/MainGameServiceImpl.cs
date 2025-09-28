using System;
using Features.GameInfo.Messages;
using Features.GameInfo.Services;
using Features.GameResult.Models;
using Features.MainGame.Messages;
using Features.MainGame.Models;
using Features.Notification.Services;
using Features.PlayerHealth.Services;
using Features.PlayerList.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.MainGame.Services
{
    /// <summary>
    /// 메인 게임의 전체적인 진행 상황을 관리하는 서비스 구현체
    /// VContainer EntryPoint로 동작하여 자동으로 게임을 초기화
    /// </summary>
    public class MainGameServiceImpl : IMainGameService, IStartable, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<GamePhase> CurrentPhase => _currentPhase;
        public ReadOnlyReactiveProperty<bool> IsGameInProgress => _isGameInProgress;
        public ReadOnlyReactiveProperty<int> ActivePlayerCount => _activePlayerCount;
        public ReadOnlyReactiveProperty<WinConditionType> WinCondition => _winCondition;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<GamePhase> _currentPhase = new(GamePhase.Preparing);
        private readonly ReactiveProperty<bool> _isGameInProgress = new(false);
        private readonly ReactiveProperty<int> _activePlayerCount = new(0);
        private readonly ReactiveProperty<WinConditionType> _winCondition = new(
            WinConditionType.TimeExpired
        );

        private readonly MainGameData _gameData = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = false;

        private bool _isInitialized = false;
        private bool _isPaused = false;

        #endregion

        #region Dependencies

        private readonly IGameInfoService _gameInfoService;
        private readonly IPlayerListService _playerListService;
        private readonly IPlayerHealthService _playerHealthService;
        private readonly INotificationService _notificationService;

        // MessagePipe Subscribers
        private readonly ISubscriber<GameTimeExpiredMessage> _timeExpiredSubscriber;

        // MessagePipe Publishers
        private readonly IPublisher<GameInitializedMessage> _gameInitializedPublisher;
        private readonly IPublisher<GameStartedMessage> _gameStartedPublisher;
        private readonly IPublisher<GamePhaseChangedMessage> _gamePhaseChangedPublisher;
        private readonly IPublisher<WinConditionMetMessage> _winConditionMetPublisher;
        private readonly IPublisher<GameEndedMessage> _gameEndedPublisher;
        private readonly IPublisher<PlayerEliminatedMessage> _playerEliminatedPublisher;
        private readonly IPublisher<GameStateSyncMessage> _gameStateSyncPublisher;
        private readonly IPublisher<GamePausedMessage> _gamePausedPublisher;

        #endregion

        #region Constructor

        [Inject]
        public MainGameServiceImpl(
            IGameInfoService gameInfoService,
            IPlayerListService playerListService,
            IPlayerHealthService playerHealthService,
            INotificationService notificationService,
            ISubscriber<GameTimeExpiredMessage> timeExpiredSubscriber,
            IPublisher<GameInitializedMessage> gameInitializedPublisher,
            IPublisher<GameStartedMessage> gameStartedPublisher,
            IPublisher<GamePhaseChangedMessage> gamePhaseChangedPublisher,
            IPublisher<WinConditionMetMessage> winConditionMetPublisher,
            IPublisher<GameEndedMessage> gameEndedPublisher,
            IPublisher<PlayerEliminatedMessage> playerEliminatedPublisher,
            IPublisher<GameStateSyncMessage> gameStateSyncPublisher,
            IPublisher<GamePausedMessage> gamePausedPublisher
        )
        {
            _gameInfoService = gameInfoService;
            _playerListService = playerListService;
            _playerHealthService = playerHealthService;
            _notificationService = notificationService;
            _timeExpiredSubscriber = timeExpiredSubscriber;

            _gameInitializedPublisher = gameInitializedPublisher;
            _gameStartedPublisher = gameStartedPublisher;
            _gamePhaseChangedPublisher = gamePhaseChangedPublisher;
            _winConditionMetPublisher = winConditionMetPublisher;
            _gameEndedPublisher = gameEndedPublisher;
            _playerEliminatedPublisher = playerEliminatedPublisher;
            _gameStateSyncPublisher = gameStateSyncPublisher;
            _gamePausedPublisher = gamePausedPublisher;

            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Observable 속성들 연결
            _currentPhase
                .Subscribe(phase =>
                {
                    _gameData.currentPhase = phase;
                    _isGameInProgress.Value = _gameData.IsGameInProgress;
                })
                .AddTo(_disposables);

            _winCondition
                .Subscribe(condition => _gameData.winCondition = condition)
                .AddTo(_disposables);

            // 메시지 구독
            _timeExpiredSubscriber.Subscribe(OnTimeExpiredMessage).AddTo(_disposables);

            DebugLog("MainGameService 초기화 완료");
        }

        #endregion

        #region VContainer EntryPoint

        public void Start()
        {
            DebugLog("MainGameService EntryPoint 시작");

            // 게임 초기화
            InitializeGame();

            // 1프레임 대기 후 씬 초기화 완료 처리 (다른 EntryPoint들이 완료될 때까지)
            DelayedSceneComplete().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid DelayedSceneComplete()
        {
            // 1프레임 대기하여 다른 모든 EntryPoint들이 완료되도록 함
            await Cysharp.Threading.Tasks.UniTask.DelayFrame(1);

            DebugLog("씬 초기화 완료 처리 시작");
            OnSceneInitializationComplete();
        }

        #endregion

        #region Public Methods

        public void InitializeGame()
        {
            if (_isInitialized)
            {
                DebugLog("게임이 이미 초기화되었습니다.");
                return;
            }

            DebugLog("게임 초기화 시작");

            // 게임 데이터 초기화
            _gameData.Reset();
            _gameData.currentPhase = GamePhase.Preparing;

            // Observable 값들 초기화
            _currentPhase.Value = GamePhase.Preparing;
            _activePlayerCount.Value = 1; // 기본적으로 로컬 플레이어 1명
            _winCondition.Value = WinConditionType.TimeExpired;

            // 게임 시간 미리 설정 (UI가 0:00으로 나타나지 않도록)
            var gameTime = TimeSpan.FromMinutes(_gameData.gameDurationMinutes);
            DebugLog(
                $"게임 시간 설정: {gameTime.TotalMinutes}분 = {gameTime.Minutes:D2}:{gameTime.Seconds:D2}"
            );
            _gameInfoService.SetCurrentTime(gameTime);

            _isInitialized = true;

            // 초기화 완료 메시지 발송
            _gameInitializedPublisher.Publish(
                new GameInitializedMessage(_gameData, "MainGameService")
            );

            DebugLog("게임 초기화 완료");
        }

        public void StartGame()
        {
            ActualStartGame();
        }

        public void ActualStartGame()
        {
            if (!_isInitialized)
            {
                DebugLog("게임이 초기화되지 않았습니다. 먼저 InitializeGame()을 호출하세요.");
                return;
            }

            if (_gameData.IsGameInProgress)
            {
                DebugLog("게임이 이미 진행 중입니다.");
                return;
            }

            DebugLog("실제 게임 시작");

            // 게임 시작 알림 표시
            _notificationService.ShowGameStartNotification();

            // 게임 시작 시간 설정
            _gameData.gameStartTime = DateTime.Now;

            // 단계 변경
            var previousPhase = _currentPhase.Value;
            _currentPhase.Value = GamePhase.InProgress;

            // 타이머 시작 (시간은 이미 InitializeGame에서 설정됨)
            _gameInfoService.StartTimer();

            // 상태 메시지 변경
            _gameInfoService.SetStatusMessage("• 꿈 속을 탈출하세요.");

            // 메시지 발송
            _gamePhaseChangedPublisher.Publish(
                new GamePhaseChangedMessage(previousPhase, GamePhase.InProgress, "게임 시작")
            );
            _gameStartedPublisher.Publish(
                new GameStartedMessage(
                    _gameData.gameStartTime,
                    _activePlayerCount.Value,
                    _gameData.winCondition
                )
            );

            DebugLog(
                $"게임 시작 완료 - 제한시간: {_gameData.gameDurationMinutes}분, 승리조건: {_gameData.winCondition}"
            );
        }

        public void SetWaitingForPlayersState()
        {
            DebugLog("다른 플레이어들을 기다리는 상태로 변경");

            // 단계 변경
            var previousPhase = _currentPhase.Value;
            _currentPhase.Value = GamePhase.WaitingForPlayers;

            // 상태 메시지 설정
            _gameInfoService.SetStatusMessage("다른 플레이어를 기다리는 중...");

            // 메시지 발송
            _gamePhaseChangedPublisher.Publish(
                new GamePhaseChangedMessage(
                    previousPhase,
                    GamePhase.WaitingForPlayers,
                    "플레이어 대기 중"
                )
            );

            DebugLog("플레이어 대기 상태 설정 완료");
        }

        public void EndGame(TeamResult result, string reason = "")
        {
            if (!_gameData.IsGameInProgress)
            {
                DebugLog("게임이 진행 중이 아닙니다.");
                return;
            }

            DebugLog($"게임 종료: {result}, 이유: {reason}");

            // 게임 종료 알림 표시
            _notificationService.ShowGameEndNotification();

            // 게임 종료 시간 설정
            _gameData.gameEndTime = DateTime.Now;
            _gameData.gameResult = result;

            // 단계 변경
            var previousPhase = _currentPhase.Value;
            _currentPhase.Value = GamePhase.Ending;

            // 타이머 정지
            _gameInfoService.StopTimer();

            // 메시지 발송
            _gamePhaseChangedPublisher.Publish(
                new GamePhaseChangedMessage(previousPhase, GamePhase.Ending, reason)
            );
            _gameEndedPublisher.Publish(
                new GameEndedMessage(
                    result,
                    _gameData.winCondition,
                    "", // winnerId - 추후 구현
                    _gameData.GetElapsedTime(),
                    reason
                )
            );

            // 완료 상태로 전환
            _currentPhase.Value = GamePhase.Completed;

            DebugLog($"게임 종료 완료 - 소요시간: {_gameData.GetElapsedTime():mm\\:ss}");
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                _gameInfoService.StopTimer();
            }
            else
            {
                _gameInfoService.StartTimer();
            }

            _gamePausedPublisher.Publish(
                new GamePausedMessage(_isPaused, _isPaused ? "게임 일시정지" : "게임 재개")
            );

            DebugLog($"게임 {(_isPaused ? "일시정지" : "재개")}");
        }

        public void SetWinCondition(WinConditionType conditionType)
        {
            _winCondition.Value = conditionType;
            DebugLog($"승리 조건 변경: {conditionType}");
        }

        public void EliminatePlayer(string playerId, string reason = "")
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            if (_gameData.activePlayers.Contains(playerId))
            {
                _gameData.activePlayers.Remove(playerId);
                _activePlayerCount.Value = _gameData.activePlayers.Count;

                _playerEliminatedPublisher.Publish(
                    new PlayerEliminatedMessage(playerId, reason, _gameData.activePlayers.Count)
                );

                DebugLog(
                    $"플레이어 탈락: {playerId}, 이유: {reason}, 남은 플레이어: {_gameData.activePlayers.Count}"
                );

                // 승리 조건 체크
                CheckWinConditions();
            }
        }

        public MainGameData GetCurrentGameData()
        {
            return _gameData;
        }

        public void RestartGame()
        {
            DebugLog("게임 재시작");

            // 현재 게임 강제 종료
            if (_gameData.IsGameInProgress)
            {
                EndGame(TeamResult.MongdungWin, "게임 재시작");
            }

            // 초기화 후 다시 시작
            _isInitialized = false;
            InitializeGame();
        }

        #endregion

        #region Event Handlers

        public void OnSceneInitializationComplete()
        {
            DebugLog("씬 초기화 완료 - 바로 게임 시작");

            // 씬 초기화가 완료되면 바로 게임 시작
            if (_isInitialized)
            {
                ActualStartGame();
            }
            else
            {
                DebugLog("게임이 초기화되지 않았습니다. 먼저 InitializeGame()이 필요합니다.");
            }
        }

        public void OnTimeExpired()
        {
            DebugLog("시간 만료 - 게임 종료");

            if (_gameData.winCondition == WinConditionType.TimeExpired)
            {
                _winConditionMetPublisher.Publish(
                    new WinConditionMetMessage(WinConditionType.TimeExpired, "", "제한시간 만료")
                );
                EndGame(TeamResult.MonggingWin, "제한시간 생존 성공");
            }
        }

        public void OnPlayerDied(string playerId)
        {
            DebugLog($"플레이어 사망: {playerId}");
            EliminatePlayer(playerId, "사망");
        }

        public void OnGgumtlePurified(string ggumtleId)
        {
            DebugLog($"꿈틀이 정화 완료: {ggumtleId}");

            // 모든 꿈틀이 정화되었는지 확인 (추후 구현)
            if (_gameData.winCondition == WinConditionType.AllGgumtlesPurified)
            {
                // TODO: 실제 꿈틀이 정화 상태 체크 로직 필요
                CheckWinConditions();
            }
        }

        #endregion

        #region Message Handlers

        private void OnTimeExpiredMessage(GameTimeExpiredMessage message)
        {
            DebugLog($"시간 만료 메시지 수신: {message.expiredReason}");
            OnTimeExpired();
        }

        #endregion

        #region Private Methods

        private void CheckWinConditions()
        {
            if (!_gameData.IsGameInProgress)
                return;

            switch (_gameData.winCondition)
            {
                case WinConditionType.LastPlayerStanding:
                    if (_activePlayerCount.Value <= 1)
                    {
                        var winner =
                            _gameData.activePlayers.Count > 0 ? _gameData.activePlayers[0] : "";
                        _winConditionMetPublisher.Publish(
                            new WinConditionMetMessage(WinConditionType.LastPlayerStanding, winner)
                        );
                        EndGame(TeamResult.MonggingWin, "마지막 생존자");
                    }
                    break;

                case WinConditionType.AllGgumtlesPurified:
                    // TODO: 실제 꿈틀이 정화 상태 체크 로직 구현
                    break;

                case WinConditionType.TimeExpired:
                    // 시간 만료는 OnTimeExpired에서 처리
                    break;
            }
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[MainGameService] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _gameInfoService?.StopTimer();
            _disposables?.Dispose();
            _currentPhase?.Dispose();
            _isGameInProgress?.Dispose();
            _activePlayerCount?.Dispose();
            _winCondition?.Dispose();

            DebugLog("MainGameService Dispose 완료");
        }

        #endregion
    }
}
