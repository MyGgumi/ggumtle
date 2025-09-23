using System;

namespace Features.GameResult.Models
{
    /// <summary>
    /// 게임 결과 데이터 모델
    /// </summary>
    [Serializable]
    public class GameResultModel
    {
        public TeamResult TeamResult { get; set; }
        public int EscapedCount { get; set; }
        public int CoinReward { get; set; }
        public PlayerResultModel MongdungPlayer { get; set; }
        public PlayerResultModel[] MonggingPlayers { get; set; }

        public GameResultModel()
        {
            TeamResult = TeamResult.MonggingWin;
            EscapedCount = 0;
            CoinReward = 0;
            MongdungPlayer = new PlayerResultModel();
            MonggingPlayers = new PlayerResultModel[4];
        }
    }

    /// <summary>
    /// 플레이어 결과 데이터 모델
    /// </summary>
    [Serializable]
    public class PlayerResultModel
    {
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        public bool IsMongging { get; set; }  // true면 몽깅이, false면 몽둥이
        public MonggingColor MonggingColor { get; set; }
        public PlayerStatus Status { get; set; }

        public PlayerResultModel()
        {
            PlayerId = 0;
            PlayerName = "";
            IsMongging = true;  // 기본값은 몽깅이
            MonggingColor = MonggingColor.Mint;
            Status = PlayerStatus.Escaped;
        }
    }

    /// <summary>
    /// 팀 결과 타입
    /// </summary>
    public enum TeamResult
    {
        MonggingWin = 1,    // 몽깅이 승리
        MongdungWin = 2     // 몽둥이 승리
    }

    /// <summary>
    /// 플레이어 상태
    /// </summary>
    public enum PlayerStatus
    {
        Escaped = 1,    // 탈출(생존)
        Dead = 2        // 죽음
    }
}


/// <summary>
/// 몽깅이 색상 타입
/// </summary>
public enum MonggingColor
{
    Mint = 1,
    Yellow = 2,
    Purple = 3
}