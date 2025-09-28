using Features.GameResult.Models;
using System;

namespace Features.GameResult.Services
{
    /// <summary>
    /// 게임 결과 처리 서비스 인터페이스
    /// </summary>
    public interface IGameResultService
    {
        /// <summary>
        /// 현재 게임 결과 데이터
        /// </summary>
        GameResultData CurrentResult { get; }

        /// <summary>
        /// 게임 결과 변경 이벤트
        /// </summary>
        event Action<GameResultData> OnGameResultUpdated;

        /// <summary>
        /// 게임 결과 표시 요청 이벤트
        /// </summary>
        event Action OnShowGameResult;

        /// <summary>
        /// 게임 결과 업데이트
        /// </summary>
        /// <param name="resultData">게임 결과 데이터</param>
        void UpdateGameResult(GameResultData resultData);

        /// <summary>
        /// 게임 결과 UI 표시
        /// </summary>
        void ShowGameResult();

        /// <summary>
        /// 게임 결과 UI 숨김
        /// </summary>
        void HideGameResult();

        /// <summary>
        /// 로비로 돌아가기
        /// </summary>
        void ReturnToLobby();
    }
}