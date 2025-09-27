using System;
using System.Collections.Generic;
using Features.PlayerList.Messages;
using Features.PlayerList.Models;
using Features.PlayerList.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.PlayerList.ViewModels
{
    /// <summary>
    /// 플레이어 리스트 ViewModel
    /// R3 + MessagePipe 기반의 반응형 ViewModel
    /// </summary>
    public class PlayerListViewModel : IDisposable
    {
        #region Observable Properties

        // 플레이어 리스트 데이터
        public readonly ReadOnlyReactiveProperty<Dictionary<int, PlayerListData>> AllPlayers;
        public readonly ReadOnlyReactiveProperty<int> TotalPlayerCount;
        public readonly ReadOnlyReactiveProperty<int> OnlinePlayerCount;

        // 스프라이트는 UI에서 직접 관리하므로 ViewModel에서 제거

        // 특정 플레이어 데이터 (Observable)
        public readonly ReadOnlyReactiveProperty<PlayerListData> Player1;
        public readonly ReadOnlyReactiveProperty<PlayerListData> Player2;
        public readonly ReadOnlyReactiveProperty<PlayerListData> Player3;
        public readonly ReadOnlyReactiveProperty<PlayerListData> Player4;

        #endregion

        #region Dependencies

        private readonly IPlayerListService _playerListService;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public PlayerListViewModel(
            IPlayerListService playerListService,
            ISubscriber<PlayerUpdatedMessage> playerUpdatedSubscriber,
            ISubscriber<PlayerStatusChangedMessage> statusChangedSubscriber,
            ISubscriber<PlayerConnectionChangedMessage> connectionChangedSubscriber,
            ISubscriber<PlayerHighlightedMessage> highlightedSubscriber,
            ISubscriber<AllPlayersClearedMessage> allClearedSubscriber,
            ISubscriber<PlayerListSyncMessage> syncSubscriber,
            ISubscriber<PlayerHostChangedMessage> roleChangedSubscriber,
            ISubscriber<PlayerListMonggingStateUpdateMessage> monggingStateSubscriber
        )
        {
            _playerListService = playerListService;

            // Service의 Observable 속성들을 직접 연결
            AllPlayers = _playerListService.AllPlayers;
            TotalPlayerCount = _playerListService.TotalPlayerCount;
            OnlinePlayerCount = _playerListService.OnlinePlayerCount;

            // 개별 플레이어 데이터 Observable 생성
            Player1 = AllPlayers
                .Select(players => players.ContainsKey(1) ? players[1] : null)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            Player2 = AllPlayers
                .Select(players => players.ContainsKey(2) ? players[2] : null)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            Player3 = AllPlayers
                .Select(players => players.ContainsKey(3) ? players[3] : null)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            Player4 = AllPlayers
                .Select(players => players.ContainsKey(4) ? players[4] : null)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            // 메시지 구독
            playerUpdatedSubscriber.Subscribe(OnPlayerUpdated).AddTo(_disposables);
            statusChangedSubscriber.Subscribe(OnPlayerStatusChanged).AddTo(_disposables);
            connectionChangedSubscriber.Subscribe(OnPlayerConnectionChanged).AddTo(_disposables);
            highlightedSubscriber.Subscribe(OnPlayerHighlighted).AddTo(_disposables);
            allClearedSubscriber.Subscribe(OnAllPlayersCleared).AddTo(_disposables);
            syncSubscriber.Subscribe(OnPlayerListSync).AddTo(_disposables);
            roleChangedSubscriber.Subscribe(OnPlayerRoleChanged).AddTo(_disposables);
            monggingStateSubscriber.Subscribe(OnMonggingPlayerStateChanged).AddTo(_disposables);

            DebugLog("초기화 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 플레이어 업데이트
        /// </summary>
        public void UpdatePlayer(int playerId, string nickname, string colorTheme, string status = "default", bool isOnline = true, bool isHost = false)
        {
            _playerListService.UpdatePlayer(playerId, nickname, colorTheme, status, isOnline, isHost);
        }

        /// <summary>
        /// 플레이어 상태 설정
        /// </summary>
        public void SetPlayerStatus(int playerId, string status)
        {
            _playerListService.SetPlayerStatus(playerId, status);
        }

        /// <summary>
        /// 플레이어 온라인 상태 설정
        /// </summary>
        public void SetPlayerOnlineStatus(int playerId, bool isOnline)
        {
            _playerListService.SetPlayerOnlineStatus(playerId, isOnline);
        }

        /// <summary>
        /// 플레이어 하이라이트
        /// </summary>
        public void HighlightPlayer(int playerId, bool highlight, float duration = 1f)
        {
            _playerListService.HighlightPlayer(playerId, highlight, duration);
        }

        /// <summary>
        /// 플레이어 호스트 설정
        /// </summary>
        public void SetPlayerHost(int playerId, bool isHost)
        {
            _playerListService.SetPlayerHost(playerId, isHost);
        }

        /// <summary>
        /// 기본 플레이어 설정
        /// </summary>
        public void SetupDefaultPlayers()
        {
            _playerListService.SetupDefaultPlayers();
        }

        /// <summary>
        /// 모든 플레이어 초기화
        /// </summary>
        public void ClearAllPlayers()
        {
            _playerListService.ClearAllPlayers();
        }

        // SetSprites 메서드 제거 - UI에서 직접 관리

        /// <summary>
        /// 특정 플레이어 데이터 가져오기
        /// </summary>
        public PlayerListData GetPlayer(int playerId)
        {
            return _playerListService.GetPlayer(playerId);
        }

        /// <summary>
        /// 모든 플레이어 데이터 가져오기
        /// </summary>
        public List<PlayerListData> GetAllPlayersList()
        {
            return _playerListService.GetAllPlayersList();
        }

        /// <summary>
        /// 온라인 플레이어만 가져오기
        /// </summary>
        public List<PlayerListData> GetOnlinePlayers()
        {
            return _playerListService.GetOnlinePlayers();
        }

        /// <summary>
        /// 플레이어 색상 가져오기
        /// </summary>
        public Color GetPlayerColor(string colorType)
        {
            return _playerListService.GetPlayerColor(colorType);
        }

        /// <summary>
        /// 플레이어 리스트 데이터에서 업데이트
        /// </summary>
        public void UpdateFromPlayerList(List<PlayerListData> players)
        {
            _playerListService.UpdateFromPlayerList(players);
        }

        #endregion

        #region Message Handlers

        private void OnPlayerUpdated(PlayerUpdatedMessage message)
        {
            DebugLog($"플레이어 업데이트: {message.playerId} - {message.playerData.nickname} ({message.playerData.status})");
        }

        private void OnPlayerStatusChanged(PlayerStatusChangedMessage message)
        {
            DebugLog($"플레이어 상태 변경: {message.playerId} - {message.status}");
        }

        private void OnPlayerConnectionChanged(PlayerConnectionChangedMessage message)
        {
            DebugLog($"플레이어 접속 상태 변경: {message.playerId} - {message.isOnline}");
        }

        private void OnPlayerHighlighted(PlayerHighlightedMessage message)
        {
            DebugLog($"플레이어 하이라이트: {message.playerId} - {message.highlight} ({message.duration}초)");
        }

        private void OnAllPlayersCleared(AllPlayersClearedMessage message)
        {
            DebugLog("모든 플레이어 초기화");
        }


        private void OnPlayerListSync(PlayerListSyncMessage message)
        {
            DebugLog($"플레이어 리스트 동기화: 전체 {message.totalPlayers}명, 온라인 {message.onlinePlayers}명");
        }

        private void OnPlayerRoleChanged(PlayerHostChangedMessage message)
        {
            DebugLog($"플레이어 역할 변경: {message.playerId} - 호스트: {message.isHost}");
        }

        private void OnMonggingPlayerStateChanged(PlayerListMonggingStateUpdateMessage message)
        {
            DebugLog($"몽깅이 상태 변경: Player{message.playerId} → {message.newStatus} ({message.playerName})");
        }

        #endregion

        #region Private Methods

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PlayerListViewModel] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            DebugLog("Dispose 완료");
        }

        #endregion
    }
}