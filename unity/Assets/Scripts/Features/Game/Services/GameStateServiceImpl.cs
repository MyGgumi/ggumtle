using System;
using Features.Game.Models;
using UnityEngine;

namespace Features.Game.Services
{
    /// <summary>
    /// 게임 상태 관리 서비스 구현체
    /// </summary>
    public class GameStateServiceImpl : IGameStateService
    {
        private GameState _currentState = GameState.Lobby;
        private readonly bool _enableDebugLogs = true;

        public GameState CurrentState => _currentState;

        public event Action<GameState, GameState> OnStateChanged;

        public void SetState(GameState newState)
        {
            if (_currentState == newState)
            {
                if (_enableDebugLogs)
                    Debug.LogWarning($"[GameStateService] 같은 상태로 전환 시도 무시: {newState}");
                return;
            }

            if (!CanTransition(_currentState, newState))
            {
                Debug.LogError(
                    $"[GameStateService] 잘못된 상태 전환: {_currentState} → {newState}"
                );
                return;
            }

            var previousState = _currentState;
            _currentState = newState;

            if (_enableDebugLogs)
                Debug.Log($"[GameStateService] 상태 전환: {previousState} → {newState}");

            OnStateChanged?.Invoke(previousState, newState);
        }

        public bool IsState(GameState state)
        {
            return _currentState == state;
        }

        public bool CanTransition(GameState fromState, GameState toState)
        {
            // 허용되는 상태 전환 규칙 정의
            return fromState switch
            {
                GameState.Lobby => toState == GameState.Loading,
                GameState.Loading => toState == GameState.InGame || toState == GameState.Lobby,
                GameState.InGame => toState == GameState.GameOver || toState == GameState.Lobby,
                GameState.GameOver => toState == GameState.Lobby,
                _ => false,
            };
        }
    }
}
