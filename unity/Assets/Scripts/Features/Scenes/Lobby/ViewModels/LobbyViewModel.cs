using System;
using Features.Scenes.Lobby.Messages;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Scenes.Lobby.ViewModels
{
    /// <summary>
    /// 로비 UI 상태를 관리하는 ViewModel
    /// </summary>
    public class LobbyViewModel : IDisposable
    {
        private readonly System.Collections.Generic.List<System.IDisposable> _messageDisposables =
            new();
        private readonly bool _enableDebugLogs = true;

        // UI 상태
        private readonly ReactiveProperty<bool> _isConnecting = new(false);
        private readonly ReactiveProperty<bool> _isAuthenticated = new(false);
        private readonly ReactiveProperty<string> _statusMessage = new("로그인 준비");

        public ReadOnlyReactiveProperty<bool> IsConnecting => _isConnecting;
        public ReadOnlyReactiveProperty<bool> IsAuthenticated => _isAuthenticated;
        public ReadOnlyReactiveProperty<string> StatusMessage => _statusMessage;

        // VContainer 의존성 주입
        private readonly ISubscriber<LobbyUIStateMessage> _lobbyUIStateSubscriber;

        [Inject]
        public LobbyViewModel(ISubscriber<LobbyUIStateMessage> lobbyUIStateSubscriber)
        {
            _lobbyUIStateSubscriber = lobbyUIStateSubscriber;

            // LobbyUIStateMessage 구독
            SubscribeToUIStateMessage();

            if (_enableDebugLogs)
            {
                Debug.Log("[LobbyViewModel] 초기화 완료");
            }
        }


        /// <summary>
        /// LobbyUIStateMessage 구독 (UI 상태 업데이트 전용)
        /// </summary>
        private void SubscribeToUIStateMessage()
        {
            var subscription = _lobbyUIStateSubscriber.Subscribe(message =>
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[LobbyViewModel] UI 상태 메시지 수신: {message.StatusMessage}");
                }

                // UI 상태 업데이트
                _isConnecting.Value = message.IsConnecting;
                _isAuthenticated.Value = message.IsAuthenticated;
                _statusMessage.Value = message.StatusMessage;
            });
            _messageDisposables.Add(subscription);
        }

        public void Dispose()
        {
            foreach (var disposable in _messageDisposables)
            {
                disposable.Dispose();
            }
            _messageDisposables.Clear();

            if (_enableDebugLogs)
            {
                Debug.Log("[LobbyViewModel] 해제됨");
            }
        }
    }
}
