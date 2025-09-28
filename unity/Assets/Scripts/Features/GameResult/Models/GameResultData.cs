using System.Collections.Generic;
using Features.GameResult.Models;

namespace Features.GameResult.Models
{
    /// <summary>
    /// 게임 결과 타입
    /// </summary>
    public enum TeamResult
    {
        MonggingWin = 1,    // 몽깅이 승리
        MongdungWin = 2     // 몽둥이 승리
    }

    /// <summary>
    /// 플레이어 상태 타입
    /// </summary>
    public enum PlayerStatus
    {
        Escaped = 1,        // 탈출 성공
        Dead = 2           // 탈출 실패
    }

    /// <summary>
    /// 플레이어 결과 데이터
    /// </summary>
    public class PlayerResultData
    {
        public long Id { get; set; }
        public PlayerStatus Status { get; set; }
        public int Coin { get; set; }

        public PlayerResultData()
        {
            Id = 0;
            Status = PlayerStatus.Escaped;
            Coin = 0;
        }

        public PlayerResultData(long id, PlayerStatus status, int coin)
        {
            Id = id;
            Status = status;
            Coin = coin;
        }
    }

    /// <summary>
    /// 게임 결과 데이터
    /// MainGameNetworkEventHandler의 GameEndCommand를 기반으로 구성
    /// </summary>
    public class GameResultData
    {
        public TeamResult Result { get; set; }
        public int EscapedMonggingCount { get; set; }
        public int CoinReward { get; set; } // 게임 결과로 받은 코인 보상
        public List<PlayerResultData> PlayerResults { get; set; }

        public GameResultData()
        {
            Result = TeamResult.MonggingWin;
            EscapedMonggingCount = 0;
            CoinReward = 0;
            PlayerResults = new List<PlayerResultData>();
        }

        public GameResultData(TeamResult result, int escapedMonggingCount, int coinReward, List<PlayerResultData> playerResults)
        {
            Result = result;
            EscapedMonggingCount = escapedMonggingCount;
            CoinReward = coinReward;
            PlayerResults = playerResults ?? new List<PlayerResultData>();
        }
    }
}