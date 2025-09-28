using System.Collections.Generic;
using Features.GameResult.Models;

namespace Features.GameResult.Messages
{
    /// <summary>
    /// 게임 결과 메시지
    /// MainGameNetworkEventHandler에서 발행되어 GameResultService에서 처리됨
    /// </summary>
    public struct GameResultMessage
    {
        public GameResultData ResultData { get; }

        public GameResultMessage(GameResultData resultData)
        {
            ResultData = resultData;
        }
    }
}