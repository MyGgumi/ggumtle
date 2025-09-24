using System;
using Features.MainGame.Models;
using Features.GameResult.Models;

namespace Features.MainGame.Messages
{
    /// <summary>
    /// 게임 초기화 완료 메시지
    /// </summary>
    public readonly struct GameInitializedMessage
    {
        public readonly MainGameData gameData;
        public readonly string initializationInfo;

        public GameInitializedMessage(MainGameData gameData, string initializationInfo = "")
        {
            this.gameData = gameData;
            this.initializationInfo = initializationInfo;
        }
    }

    /// <summary>
    /// 게임 시작 메시지
    /// </summary>
    public readonly struct GameStartedMessage
    {
        public readonly DateTime startTime;
        public readonly int playerCount;
        public readonly WinConditionType winCondition;

        public GameStartedMessage(DateTime startTime, int playerCount, WinConditionType winCondition)
        {
            this.startTime = startTime;
            this.playerCount = playerCount;
            this.winCondition = winCondition;
        }
    }

    /// <summary>
    /// 게임 단계 변경 메시지
    /// </summary>
    public readonly struct GamePhaseChangedMessage
    {
        public readonly GamePhase previousPhase;
        public readonly GamePhase newPhase;
        public readonly string reason;

        public GamePhaseChangedMessage(GamePhase previousPhase, GamePhase newPhase, string reason = "")
        {
            this.previousPhase = previousPhase;
            this.newPhase = newPhase;
            this.reason = reason;
        }
    }

    /// <summary>
    /// 승리 조건 달성 메시지
    /// </summary>
    public readonly struct WinConditionMetMessage
    {
        public readonly WinConditionType conditionType;
        public readonly string playerId;
        public readonly string details;

        public WinConditionMetMessage(WinConditionType conditionType, string playerId = "", string details = "")
        {
            this.conditionType = conditionType;
            this.playerId = playerId;
            this.details = details;
        }
    }

    /// <summary>
    /// 게임 종료 메시지
    /// </summary>
    public readonly struct GameEndedMessage
    {
        public readonly TeamResult result;
        public readonly WinConditionType winCondition;
        public readonly string winnerId;
        public readonly TimeSpan gameDuration;
        public readonly string endReason;

        public GameEndedMessage(TeamResult result, WinConditionType winCondition, string winnerId = "",
                               TimeSpan gameDuration = default, string endReason = "")
        {
            this.result = result;
            this.winCondition = winCondition;
            this.winnerId = winnerId;
            this.gameDuration = gameDuration;
            this.endReason = endReason;
        }
    }

    /// <summary>
    /// 플레이어 탈락 메시지
    /// </summary>
    public readonly struct PlayerEliminatedMessage
    {
        public readonly string playerId;
        public readonly string eliminationReason;
        public readonly int remainingPlayerCount;

        public PlayerEliminatedMessage(string playerId, string eliminationReason, int remainingPlayerCount)
        {
            this.playerId = playerId;
            this.eliminationReason = eliminationReason;
            this.remainingPlayerCount = remainingPlayerCount;
        }
    }

    /// <summary>
    /// 게임 상태 동기화 메시지 (디버그/관리용)
    /// </summary>
    public readonly struct GameStateSyncMessage
    {
        public readonly MainGameData gameData;
        public readonly string source;

        public GameStateSyncMessage(MainGameData gameData, string source = "")
        {
            this.gameData = gameData;
            this.source = source;
        }
    }

    /// <summary>
    /// 게임 일시정지/재개 메시지
    /// </summary>
    public readonly struct GamePausedMessage
    {
        public readonly bool isPaused;
        public readonly string reason;

        public GamePausedMessage(bool isPaused, string reason = "")
        {
            this.isPaused = isPaused;
            this.reason = reason;
        }
    }
}