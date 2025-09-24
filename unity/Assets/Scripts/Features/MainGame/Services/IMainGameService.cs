using System;
using Features.MainGame.Models;
using Features.GameResult.Models;
using R3;

namespace Features.MainGame.Services
{
    /// <summary>
    /// 메인 게임의 전체적인 진행 상황을 관리하는 서비스 인터페이스
    /// </summary>
    public interface IMainGameService
    {
        #region Observable Properties

        /// <summary>
        /// 현재 게임 단계 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<GamePhase> CurrentPhase { get; }

        /// <summary>
        /// 게임 진행 중 여부 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsGameInProgress { get; }

        /// <summary>
        /// 활성 플레이어 수 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<int> ActivePlayerCount { get; }

        /// <summary>
        /// 현재 승리 조건 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<WinConditionType> WinCondition { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// 게임 초기화 (MainSceneInitializer에서 호출)
        /// </summary>
        void InitializeGame();

        /// <summary>
        /// 게임 시작
        /// </summary>
        void StartGame();

        /// <summary>
        /// 게임 강제 종료
        /// </summary>
        void EndGame(TeamResult result, string reason = "");

        /// <summary>
        /// 게임 일시정지/재개
        /// </summary>
        void TogglePause();

        /// <summary>
        /// 승리 조건 변경
        /// </summary>
        void SetWinCondition(WinConditionType conditionType);

        /// <summary>
        /// 플레이어 탈락 처리
        /// </summary>
        void EliminatePlayer(string playerId, string reason = "");

        /// <summary>
        /// 현재 게임 데이터 가져오기
        /// </summary>
        MainGameData GetCurrentGameData();

        /// <summary>
        /// 게임 재시작
        /// </summary>
        void RestartGame();

        #endregion

        #region Event Handlers

        /// <summary>
        /// 씬 초기화 완료 시 호출 (MainSceneInitializer에서 호출)
        /// </summary>
        void OnSceneInitializationComplete();

        /// <summary>
        /// 시간 만료 시 호출 (GameInfoService에서 호출)
        /// </summary>
        void OnTimeExpired();

        /// <summary>
        /// 플레이어 사망 시 호출 (PlayerHealthService에서 호출)
        /// </summary>
        void OnPlayerDied(string playerId);

        /// <summary>
        /// 꿈틀이 정화 완료 시 호출 (GgumtleService에서 호출)
        /// </summary>
        void OnGgumtlePurified(string ggumtleId);

        #endregion
    }
}