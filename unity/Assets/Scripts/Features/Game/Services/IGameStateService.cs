using Features.Game.Models;
using System;

namespace Features.Game.Services
{
    /// <summary>
    /// 게임 상태 관리를 담당하는 서비스 인터페이스
    /// </summary>
    public interface IGameStateService
    {
        /// <summary>
        /// 현재 게임 상태
        /// </summary>
        GameState CurrentState { get; }

        /// <summary>
        /// 게임 상태 변경 이벤트
        /// </summary>
        event Action<GameState, GameState> OnStateChanged;

        /// <summary>
        /// 게임 상태 변경
        /// </summary>
        /// <param name="newState">새로운 상태</param>
        void SetState(GameState newState);

        /// <summary>
        /// 특정 상태인지 확인
        /// </summary>
        /// <param name="state">확인할 상태</param>
        /// <returns>현재 상태가 지정된 상태인지 여부</returns>
        bool IsState(GameState state);

        /// <summary>
        /// 상태 전환이 가능한지 확인
        /// </summary>
        /// <param name="fromState">현재 상태</param>
        /// <param name="toState">목표 상태</param>
        /// <returns>전환 가능 여부</returns>
        bool CanTransition(GameState fromState, GameState toState);
    }
}