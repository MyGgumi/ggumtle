using Features.Mongging.Models;
using System.Collections.Generic;

namespace Features.Mongging.Messages
{
    /// <summary>
    /// 몽깅이 팀 상태 동기화 메시지
    /// </summary>
    public readonly struct MonggingTeamSyncMessage
    {
        public readonly int TotalPlayerCount;
        public readonly int AlivePlayerCount;
        public readonly int FaintedPlayerCount;
        public readonly int DeadPlayerCount;
        public readonly int EscapedPlayerCount;
        public readonly bool IsGameOver;

        public MonggingTeamSyncMessage(
            int totalPlayerCount,
            int alivePlayerCount,
            int faintedPlayerCount,
            int deadPlayerCount,
            int escapedPlayerCount,
            bool isGameOver)
        {
            TotalPlayerCount = totalPlayerCount;
            AlivePlayerCount = alivePlayerCount;
            FaintedPlayerCount = faintedPlayerCount;
            DeadPlayerCount = deadPlayerCount;
            EscapedPlayerCount = escapedPlayerCount;
            IsGameOver = isGameOver;
        }
    }

    /// <summary>
    /// 몽깅이 팀 플레이어 업데이트 메시지
    /// </summary>
    public readonly struct MonggingTeamPlayerUpdatedMessage
    {
        public readonly long PlayerId;
        public readonly MonggingPlayerData PlayerData;

        public MonggingTeamPlayerUpdatedMessage(long playerId, MonggingPlayerData playerData)
        {
            PlayerId = playerId;
            PlayerData = playerData;
        }
    }

    /// <summary>
    /// 서버로부터 받은 몽깅이 상태 브로드캐스트 메시지
    /// </summary>
    public readonly struct MonggingStateBroadcastMessage
    {
        public readonly long PlayerId;
        public readonly int StateType; // 서버에서 오는 상태 타입
        public readonly int CurrentHp;
        public readonly int FaintCount;

        public MonggingStateBroadcastMessage(
            long playerId,
            int stateType,
            int currentHp = 0,
            int faintCount = 0)
        {
            PlayerId = playerId;
            StateType = stateType;
            CurrentHp = currentHp;
            FaintCount = faintCount;
        }
    }

    /// <summary>
    /// 몽깅이 팀 초기화 메시지
    /// </summary>
    public readonly struct MonggingTeamInitializedMessage
    {
        public readonly List<MonggingPlayerData> Players;

        public MonggingTeamInitializedMessage(List<MonggingPlayerData> players)
        {
            Players = players;
        }
    }

    /// <summary>
    /// 몽깅이 팀 게임 종료 메시지
    /// </summary>
    public readonly struct MonggingTeamGameOverMessage
    {
        public readonly bool AllDead;
        public readonly bool AllEscaped;
        public readonly List<MonggingPlayerData> FinalStates;

        public MonggingTeamGameOverMessage(
            bool allDead,
            bool allEscaped,
            List<MonggingPlayerData> finalStates)
        {
            AllDead = allDead;
            AllEscaped = allEscaped;
            FinalStates = finalStates;
        }
    }
}