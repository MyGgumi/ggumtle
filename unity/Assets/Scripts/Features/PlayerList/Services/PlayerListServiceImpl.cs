using System;
using System.Collections.Generic;
using Features.PlayerList.Messages;
using Features.PlayerList.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.PlayerList.Services
{
    /// <summary>
    /// 플레이어 리스트 관리 서비스 구현체
    /// </summary>
    public class PlayerListServiceImpl : IPlayerListService, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<int> TotalPlayerCount => _totalPlayerCount;
        public ReadOnlyReactiveProperty<int> OnlinePlayerCount => _onlinePlayerCount;
        public ReadOnlyReactiveProperty<Dictionary<int, PlayerListData>> AllPlayers => _allPlayers;
        public ReadOnlyReactiveProperty<Sprite> DefaultIcon => _defaultIcon;
        public ReadOnlyReactiveProperty<Sprite> FaintIcon => _faintIcon;
        public ReadOnlyReactiveProperty<Sprite> DeadIcon => _deadIcon;
        public ReadOnlyReactiveProperty<Sprite> EscapeIcon => _escapeIcon;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<int> _totalPlayerCount = new(0);
        private readonly ReactiveProperty<int> _onlinePlayerCount = new(0);
        private readonly ReactiveProperty<Dictionary<int, PlayerListData>> _allPlayers = new(new());
        private readonly ReactiveProperty<Sprite> _defaultIcon = new();
        private readonly ReactiveProperty<Sprite> _faintIcon = new();
        private readonly ReactiveProperty<Sprite> _deadIcon = new();
        private readonly ReactiveProperty<Sprite> _escapeIcon = new();

        private readonly PlayerListModel _playerListModel = new();
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Dependencies

        private readonly IPublisher<PlayerUpdatedMessage> _playerUpdatedPublisher;
        private readonly IPublisher<PlayerStatusChangedMessage> _statusChangedPublisher;
        private readonly IPublisher<PlayerConnectionChangedMessage> _connectionChangedPublisher;
        private readonly IPublisher<PlayerHighlightedMessage> _highlightedPublisher;
        private readonly IPublisher<AllPlayersClearedMessage> _allClearedPublisher;
        private readonly IPublisher<PlayerSpritesUpdatedMessage> _spritesUpdatedPublisher;
        private readonly IPublisher<PlayerListSyncMessage> _syncPublisher;
        private readonly IPublisher<PlayerHostChangedMessage> _roleChangedPublisher;

        #endregion

        #region Constructor

        [Inject]
        public PlayerListServiceImpl(
            IPublisher<PlayerUpdatedMessage> playerUpdatedPublisher,
            IPublisher<PlayerStatusChangedMessage> statusChangedPublisher,
            IPublisher<PlayerConnectionChangedMessage> connectionChangedPublisher,
            IPublisher<PlayerHighlightedMessage> highlightedPublisher,
            IPublisher<AllPlayersClearedMessage> allClearedPublisher,
            IPublisher<PlayerSpritesUpdatedMessage> spritesUpdatedPublisher,
            IPublisher<PlayerListSyncMessage> syncPublisher,
            IPublisher<PlayerHostChangedMessage> roleChangedPublisher
        )
        {
            _playerUpdatedPublisher = playerUpdatedPublisher;
            _statusChangedPublisher = statusChangedPublisher;
            _connectionChangedPublisher = connectionChangedPublisher;
            _highlightedPublisher = highlightedPublisher;
            _allClearedPublisher = allClearedPublisher;
            _spritesUpdatedPublisher = spritesUpdatedPublisher;
            _syncPublisher = syncPublisher;
            _roleChangedPublisher = roleChangedPublisher;

            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // 기본 플레이어 설정
            SetupDefaultPlayers();

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerListServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void UpdatePlayer(int playerId, string nickname, string colorTheme, string status = "default", bool isOnline = true, bool isHost = false)
        {
            if (playerId < 1 || playerId > 4)
                return;

            var playerData = new PlayerListData
            {
                playerId = playerId,
                nickname = nickname,
                colorTheme = colorTheme,
                status = status,
                avatarSprite = _playerListModel.GetStatusSprite(status),
                isOnline = isOnline,
                isHost = isHost,
            };

            _playerListModel.UpdatePlayer(playerData);
            UpdateObservables();

            _playerUpdatedPublisher.Publish(new PlayerUpdatedMessage(playerId, playerData));
            _statusChangedPublisher.Publish(new PlayerStatusChangedMessage(playerId, status, nickname));
            _connectionChangedPublisher.Publish(new PlayerConnectionChangedMessage(playerId, isOnline));

            if (isHost)
            {
                _roleChangedPublisher.Publish(new PlayerHostChangedMessage(playerId, isHost));
            }

            DebugLog($"플레이어 {playerId} 업데이트: {nickname} ({status})");
        }

        public void SetPlayerStatus(int playerId, string status)
        {
            var playerData = _playerListModel.GetPlayer(playerId);
            if (playerData != null)
            {
                string oldStatus = playerData.status;
                playerData.status = status;
                playerData.avatarSprite = _playerListModel.GetStatusSprite(status);

                _playerListModel.UpdatePlayer(playerData);
                UpdateObservables();

                if (oldStatus != status)
                {
                    _playerUpdatedPublisher.Publish(new PlayerUpdatedMessage(playerId, playerData));
                    _statusChangedPublisher.Publish(new PlayerStatusChangedMessage(playerId, status, playerData.nickname));

                    DebugLog($"플레이어 {playerId} 상태 변경: {oldStatus} → {status}");
                }
            }
        }

        public void SetPlayerOnlineStatus(int playerId, bool isOnline)
        {
            var playerData = _playerListModel.GetPlayer(playerId);
            if (playerData != null)
            {
                bool oldOnlineStatus = playerData.isOnline;
                playerData.isOnline = isOnline;

                _playerListModel.UpdatePlayer(playerData);
                UpdateObservables();

                if (oldOnlineStatus != isOnline)
                {
                    _playerUpdatedPublisher.Publish(new PlayerUpdatedMessage(playerId, playerData));
                    _connectionChangedPublisher.Publish(new PlayerConnectionChangedMessage(playerId, isOnline));

                    DebugLog($"플레이어 {playerId} 접속 상태 변경: {isOnline}");
                }
            }
        }

        public void HighlightPlayer(int playerId, bool highlight, float duration = 1f)
        {
            if (playerId < 1 || playerId > 4)
                return;

            _highlightedPublisher.Publish(new PlayerHighlightedMessage(playerId, highlight, duration));

            DebugLog($"플레이어 {playerId} 하이라이트: {highlight} ({duration}초)");
        }

        public void SetPlayerHost(int playerId, bool isHost)
        {
            var playerData = _playerListModel.GetPlayer(playerId);
            if (playerData != null)
            {
                bool oldHostStatus = playerData.isHost;
                playerData.isHost = isHost;

                _playerListModel.UpdatePlayer(playerData);
                UpdateObservables();

                if (oldHostStatus != isHost)
                {
                    _playerUpdatedPublisher.Publish(new PlayerUpdatedMessage(playerId, playerData));
                    _roleChangedPublisher.Publish(new PlayerHostChangedMessage(playerId, isHost));

                    DebugLog($"플레이어 {playerId} 호스트 상태 변경: {isHost}");
                }
            }
        }

        public void SetupDefaultPlayers()
        {
            _playerListModel.SetupDefaultPlayers();
            UpdateObservables();

            // 모든 기본 플레이어에 대해 업데이트 메시지 발송
            foreach (var kvp in _playerListModel.players)
            {
                _playerUpdatedPublisher.Publish(new PlayerUpdatedMessage(kvp.Key, kvp.Value));
            }

            DebugLog("기본 플레이어 설정 완료");
        }

        public void ClearAllPlayers()
        {
            _playerListModel.ClearAllPlayers();
            UpdateObservables();

            _allClearedPublisher.Publish(new AllPlayersClearedMessage(true));

            DebugLog("모든 플레이어 데이터 초기화");
        }

        public void SetSprites(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon)
        {
            _playerListModel.SetSprites(defaultIcon, faintIcon, deadIcon, escapeIcon);

            _defaultIcon.Value = defaultIcon;
            _faintIcon.Value = faintIcon;
            _deadIcon.Value = deadIcon;
            _escapeIcon.Value = escapeIcon;

            _spritesUpdatedPublisher.Publish(new PlayerSpritesUpdatedMessage(defaultIcon, faintIcon, deadIcon, escapeIcon));

            // 모든 플레이어의 스프라이트 업데이트 메시지 발송
            foreach (var kvp in _playerListModel.players)
            {
                _playerUpdatedPublisher.Publish(new PlayerUpdatedMessage(kvp.Key, kvp.Value));
            }

            DebugLog("플레이어 스프라이트 업데이트 완료");
        }

        public PlayerListData GetPlayer(int playerId)
        {
            return _playerListModel.GetPlayer(playerId);
        }

        public List<PlayerListData> GetAllPlayersList()
        {
            return _playerListModel.GetAllPlayers();
        }

        public List<PlayerListData> GetOnlinePlayers()
        {
            return _playerListModel.GetOnlinePlayers();
        }

        public Color GetPlayerColor(string colorType)
        {
            return _playerListModel.GetPlayerColor(colorType);
        }

        public void UpdateFromPlayerList(List<PlayerListData> players)
        {
            foreach (var player in players)
            {
                if (player.playerId >= 1 && player.playerId <= 4)
                {
                    _playerListModel.UpdatePlayer(player);
                    _playerUpdatedPublisher.Publish(new PlayerUpdatedMessage(player.playerId, player));
                }
            }

            UpdateObservables();
            DebugLog($"플레이어 리스트 업데이트 완료: {players.Count}명");
        }

        #endregion

        #region Private Methods

        private void UpdateObservables()
        {
            _totalPlayerCount.Value = _playerListModel.TotalPlayerCount;
            _onlinePlayerCount.Value = _playerListModel.OnlinePlayerCount;
            _allPlayers.Value = new Dictionary<int, PlayerListData>(_playerListModel.players);

            _syncPublisher.Publish(new PlayerListSyncMessage(_playerListModel.TotalPlayerCount, _playerListModel.OnlinePlayerCount));
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PlayerListServiceImpl] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _totalPlayerCount?.Dispose();
            _onlinePlayerCount?.Dispose();
            _allPlayers?.Dispose();
            _defaultIcon?.Dispose();
            _faintIcon?.Dispose();
            _deadIcon?.Dispose();
            _escapeIcon?.Dispose();

            DebugLog("Dispose 완료");
        }

        #endregion
    }
}