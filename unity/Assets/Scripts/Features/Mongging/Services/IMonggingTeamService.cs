using System;
using System.Collections.Generic;
using Features.Mongging.Models;
using R3;

namespace Features.Mongging.Services
{
    /// <summary>
    /// 몽깅이 팀 전체 관리 서비스 인터페이스
    /// </summary>
    public interface IMonggingTeamService : IDisposable
    {
        #region Observable Properties

        /// <summary>
        /// 총 플레이어 수
        /// </summary>
        ReadOnlyReactiveProperty<int> TotalPlayerCount { get; }

        /// <summary>
        /// 생존 플레이어 수
        /// </summary>
        ReadOnlyReactiveProperty<int> AlivePlayerCount { get; }

        /// <summary>
        /// 기절 플레이어 수
        /// </summary>
        ReadOnlyReactiveProperty<int> FaintedPlayerCount { get; }

        /// <summary>
        /// 사망 플레이어 수
        /// </summary>
        ReadOnlyReactiveProperty<int> DeadPlayerCount { get; }

        /// <summary>
        /// 탈출 플레이어 수
        /// </summary>
        ReadOnlyReactiveProperty<int> EscapedPlayerCount { get; }

        /// <summary>
        /// 게임 종료 여부
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsGameOver { get; }

        /// <summary>
        /// 모든 플레이어 데이터
        /// </summary>
        ReadOnlyReactiveProperty<Dictionary<long, MonggingPlayerData>> AllPlayers { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 팀 초기화
        /// </summary>
        void Initialize();

        /// <summary>
        /// 플레이어 등록
        /// </summary>
        void RegisterPlayer(long playerId, string playerName, MonggingPlayerType playerType, bool isLocal = false);

        /// <summary>
        /// 플레이어 제거
        /// </summary>
        void UnregisterPlayer(long playerId);

        /// <summary>
        /// 플레이어 가져오기
        /// </summary>
        MonggingPlayerData GetPlayer(long playerId);

        /// <summary>
        /// 로컬 플레이어 가져오기
        /// </summary>
        MonggingPlayerData GetLocalPlayer();

        /// <summary>
        /// 생존 플레이어 목록
        /// </summary>
        List<MonggingPlayerData> GetAlivePlayers();

        /// <summary>
        /// 기절 플레이어 목록
        /// </summary>
        List<MonggingPlayerData> GetFaintedPlayers();

        /// <summary>
        /// 모든 플레이어 목록
        /// </summary>
        List<MonggingPlayerData> GetAllPlayers();

        /// <summary>
        /// 팀 데이터 초기화
        /// </summary>
        void Reset();

        #endregion
    }
}