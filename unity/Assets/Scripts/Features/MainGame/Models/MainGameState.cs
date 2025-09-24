using System;
using System.Collections.Generic;
using Features.GameResult.Models;

namespace Features.MainGame.Models
{
    /// <summary>
    /// 메인 게임의 진행 단계
    /// </summary>
    public enum GamePhase
    {
        /// <summary>
        /// 게임 준비 중 (초기화, 로딩)
        /// </summary>
        Preparing,

        /// <summary>
        /// 게임 진행 중
        /// </summary>
        InProgress,

        /// <summary>
        /// 게임 종료 처리 중
        /// </summary>
        Ending,

        /// <summary>
        /// 게임 완료됨
        /// </summary>
        Completed
    }

    /// <summary>
    /// 승리 조건 타입
    /// </summary>
    public enum WinConditionType
    {
        /// <summary>
        /// 시간 만료 (서바이벌)
        /// </summary>
        TimeExpired,

        /// <summary>
        /// 모든 꿈틀이 정화 완료
        /// </summary>
        AllGgumtlesPurified,

        /// <summary>
        /// 마지막 생존자
        /// </summary>
        LastPlayerStanding,

        /// <summary>
        /// 특정 목표 달성
        /// </summary>
        ObjectiveCompleted
    }


    /// <summary>
    /// 메인 게임의 상태 데이터
    /// </summary>
    public class MainGameData
    {
        /// <summary>
        /// 현재 게임 진행 단계
        /// </summary>
        public GamePhase currentPhase = GamePhase.Preparing;

        /// <summary>
        /// 게임 시작 시간
        /// </summary>
        public DateTime gameStartTime;

        /// <summary>
        /// 활성 플레이어 목록 (ID 기준)
        /// </summary>
        public List<string> activePlayers = new();

        /// <summary>
        /// 설정된 승리 조건
        /// </summary>
        public WinConditionType winCondition = WinConditionType.TimeExpired;

        /// <summary>
        /// 게임 제한 시간 (분)
        /// </summary>
        public int gameDurationMinutes = 15;

        /// <summary>
        /// 게임 결과
        /// </summary>
        public TeamResult? gameResult = null;

        /// <summary>
        /// 게임 종료 시간
        /// </summary>
        public DateTime? gameEndTime = null;

        /// <summary>
        /// 추가 게임 설정들
        /// </summary>
        public Dictionary<string, object> gameSettings = new();

        /// <summary>
        /// 게임이 진행 중인지 확인
        /// </summary>
        public bool IsGameInProgress => currentPhase == GamePhase.InProgress;

        /// <summary>
        /// 게임이 종료되었는지 확인
        /// </summary>
        public bool IsGameEnded => currentPhase == GamePhase.Ending || currentPhase == GamePhase.Completed;

        /// <summary>
        /// 게임 진행 시간 계산 (게임 시작 후 경과 시간)
        /// </summary>
        public TimeSpan GetElapsedTime()
        {
            if (gameStartTime == default)
                return TimeSpan.Zero;

            var endTime = gameEndTime ?? DateTime.Now;
            return endTime - gameStartTime;
        }

        /// <summary>
        /// 남은 게임 시간 계산
        /// </summary>
        public TimeSpan GetRemainingTime()
        {
            if (gameStartTime == default)
                return TimeSpan.FromMinutes(gameDurationMinutes);

            var elapsed = GetElapsedTime();
            var total = TimeSpan.FromMinutes(gameDurationMinutes);
            var remaining = total - elapsed;

            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// 게임 데이터 초기화
        /// </summary>
        public void Reset()
        {
            currentPhase = GamePhase.Preparing;
            gameStartTime = default;
            activePlayers.Clear();
            winCondition = WinConditionType.TimeExpired;
            gameDurationMinutes = 15;
            gameResult = null;
            gameEndTime = null;
            gameSettings.Clear();
        }
    }
}