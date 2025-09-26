using System;
using System.Collections.Generic;
using Features.Mongging.Models;
using Features.Mongging.Services;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Mongging.ViewModels
{
    /// <summary>
    /// 몽깅이 팀 전체 ViewModel
    /// </summary>
    public class MonggingTeamViewModel : IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<int> TotalPlayerCount => _teamService.TotalPlayerCount;
        public ReadOnlyReactiveProperty<int> AlivePlayerCount => _teamService.AlivePlayerCount;
        public ReadOnlyReactiveProperty<int> FaintedPlayerCount => _teamService.FaintedPlayerCount;
        public ReadOnlyReactiveProperty<int> DeadPlayerCount => _teamService.DeadPlayerCount;
        public ReadOnlyReactiveProperty<int> EscapedPlayerCount => _teamService.EscapedPlayerCount;
        public ReadOnlyReactiveProperty<bool> IsGameOver => _teamService.IsGameOver;
        public ReadOnlyReactiveProperty<Dictionary<long, MonggingPlayerData>> AllPlayers => _teamService.AllPlayers;

        #endregion

        #region Private Fields

        private readonly IMonggingTeamService _teamService;
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public MonggingTeamViewModel(IMonggingTeamService teamService)
        {
            _teamService = teamService;

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamViewModel] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 팀 초기화
        /// </summary>
        public void Initialize()
        {
            _teamService.Initialize();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamViewModel] 팀 초기화 완료");
            }
        }

        /// <summary>
        /// 플레이어 등록
        /// </summary>
        public void RegisterPlayer(long playerId, string playerName, MonggingPlayerType playerType, bool isLocal = false)
        {
            _teamService.RegisterPlayer(playerId, playerName, playerType, isLocal);

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingTeamViewModel] 플레이어 등록: ID={playerId}, Name={playerName}, Type={playerType}, Local={isLocal}");
            }
        }

        /// <summary>
        /// 플레이어 제거
        /// </summary>
        public void UnregisterPlayer(long playerId)
        {
            _teamService.UnregisterPlayer(playerId);

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingTeamViewModel] 플레이어 해제: ID={playerId}");
            }
        }

        /// <summary>
        /// 플레이어 가져오기
        /// </summary>
        public MonggingPlayerData GetPlayer(long playerId)
        {
            return _teamService.GetPlayer(playerId);
        }

        /// <summary>
        /// 로컬 플레이어 가져오기
        /// </summary>
        public MonggingPlayerData GetLocalPlayer()
        {
            return _teamService.GetLocalPlayer();
        }

        /// <summary>
        /// 생존 플레이어 목록
        /// </summary>
        public List<MonggingPlayerData> GetAlivePlayers()
        {
            return _teamService.GetAlivePlayers();
        }

        /// <summary>
        /// 기절 플레이어 목록
        /// </summary>
        public List<MonggingPlayerData> GetFaintedPlayers()
        {
            return _teamService.GetFaintedPlayers();
        }

        /// <summary>
        /// 모든 플레이어 목록
        /// </summary>
        public List<MonggingPlayerData> GetAllPlayers()
        {
            return _teamService.GetAllPlayers();
        }

        /// <summary>
        /// 팀 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _teamService.Reset();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamViewModel] 팀 데이터 초기화");
            }
        }

        /// <summary>
        /// 팀 상태 요약 가져오기
        /// </summary>
        public string GetTeamStatusSummary()
        {
            return $"총 {TotalPlayerCount.CurrentValue}명 | " +
                   $"생존 {AlivePlayerCount.CurrentValue}명 | " +
                   $"기절 {FaintedPlayerCount.CurrentValue}명 | " +
                   $"사망 {DeadPlayerCount.CurrentValue}명 | " +
                   $"탈출 {EscapedPlayerCount.CurrentValue}명";
        }

        /// <summary>
        /// 게임 진행 가능 여부 확인
        /// </summary>
        public bool CanContinueGame()
        {
            // 생존자나 기절자가 있으면 게임 계속 진행 가능
            return AlivePlayerCount.CurrentValue > 0 || FaintedPlayerCount.CurrentValue > 0;
        }

        /// <summary>
        /// 승리 조건 확인
        /// </summary>
        public bool IsVictory()
        {
            // 모든 플레이어가 탈출했거나, 일부가 탈출하고 나머지는 사망한 경우
            return EscapedPlayerCount.CurrentValue > 0 &&
                   (EscapedPlayerCount.CurrentValue + DeadPlayerCount.CurrentValue) == TotalPlayerCount.CurrentValue;
        }

        /// <summary>
        /// 패배 조건 확인
        /// </summary>
        public bool IsDefeat()
        {
            // 모든 플레이어가 사망한 경우
            return DeadPlayerCount.CurrentValue == TotalPlayerCount.CurrentValue && TotalPlayerCount.CurrentValue > 0;
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _teamService?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingTeamViewModel] Dispose 완료");
            }
        }

        #endregion
    }
}