using System;
using System.Collections.Generic;
using System.Linq;

namespace Features.Mongging.Models
{
    /// <summary>
    /// 몽깅이 팀 전체 데이터 (4명)
    /// </summary>
    [Serializable]
    public class MonggingTeamData
    {
        private readonly Dictionary<long, MonggingPlayerData> _players = new();

        /// <summary>
        /// 총 플레이어 수
        /// </summary>
        public int TotalPlayerCount => _players.Count;

        /// <summary>
        /// 생존 플레이어 수
        /// </summary>
        public int AlivePlayerCount => _players.Values.Count(p => p.IsAlive);

        /// <summary>
        /// 기절 플레이어 수
        /// </summary>
        public int FaintedPlayerCount => _players.Values.Count(p => p.IsFainted);

        /// <summary>
        /// 사망 플레이어 수
        /// </summary>
        public int DeadPlayerCount => _players.Values.Count(p => p.currentState == MonggingPlayerState.Dead);

        /// <summary>
        /// 탈출 플레이어 수
        /// </summary>
        public int EscapedPlayerCount => _players.Values.Count(p => p.currentState == MonggingPlayerState.Escaped);

        /// <summary>
        /// 모든 플레이어 데이터
        /// </summary>
        public IReadOnlyDictionary<long, MonggingPlayerData> Players => _players;

        /// <summary>
        /// 플레이어 추가/업데이트
        /// </summary>
        public void UpdatePlayer(MonggingPlayerData playerData)
        {
            if (playerData != null)
            {
                _players[playerData.playerId] = playerData;
            }
        }

        /// <summary>
        /// 플레이어 가져오기
        /// </summary>
        public MonggingPlayerData GetPlayer(long playerId)
        {
            return _players.TryGetValue(playerId, out var player) ? player : null;
        }

        /// <summary>
        /// 플레이어 제거
        /// </summary>
        public void RemovePlayer(long playerId)
        {
            _players.Remove(playerId);
        }

        /// <summary>
        /// 로컬 플레이어 가져오기
        /// </summary>
        public MonggingPlayerData GetLocalPlayer()
        {
            return _players.Values.FirstOrDefault(p => p.isLocal);
        }

        /// <summary>
        /// 생존 플레이어 목록
        /// </summary>
        public List<MonggingPlayerData> GetAlivePlayers()
        {
            return _players.Values.Where(p => p.IsAlive).ToList();
        }

        /// <summary>
        /// 기절 플레이어 목록
        /// </summary>
        public List<MonggingPlayerData> GetFaintedPlayers()
        {
            return _players.Values.Where(p => p.IsFainted).ToList();
        }

        /// <summary>
        /// 게임 종료 조건 확인
        /// </summary>
        public bool IsGameOver()
        {
            // 모든 플레이어가 사망하거나 탈출한 경우
            return _players.Values.All(p =>
                p.currentState == MonggingPlayerState.Dead ||
                p.currentState == MonggingPlayerState.Escaped);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Clear()
        {
            _players.Clear();
        }
    }
}