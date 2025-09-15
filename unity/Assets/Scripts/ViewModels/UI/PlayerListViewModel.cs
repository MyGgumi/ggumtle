using System;
using System.Collections.Generic;
using UnityEngine;
using MVVM.Core;

namespace MVVM.UI
{
    [System.Serializable]
    public class PlayerListData
    {
        public int playerId;
        public string nickname;
        public string colorTheme;
        public string status;
        public Sprite avatarSprite;
        public bool isOnline;
        public bool isHost;

        public PlayerListData()
        {
            playerId = 0;
            nickname = "";
            colorTheme = "yellow";
            status = "default";
            avatarSprite = null;
            isOnline = true;
            isHost = false;
        }

        public PlayerListData(int id, string name, string color = "yellow", string playerStatus = "default")
        {
            playerId = id;
            nickname = name;
            colorTheme = color;
            status = playerStatus;
            avatarSprite = null;
            isOnline = true;
            isHost = false;
        }
    }

    public class PlayerListViewModel : BaseViewModel
    {
        [Header("Player Data")]
        [SerializeField] private Dictionary<int, PlayerListData> _playerDataCache = new Dictionary<int, PlayerListData>();

        [Header("Sprites")]
        [SerializeField] private Sprite _iconMongingDefault;
        [SerializeField] private Sprite _iconMongingFaint;
        [SerializeField] private Sprite _iconMongingDead;
        [SerializeField] private Sprite _iconMongingEscape;

        public event Action<int, PlayerListData> PlayerUpdated; // playerId, playerData
        public event Action<int, string> PlayerStatusChanged; // playerId, status
        public event Action<int, bool> PlayerConnectionChanged; // playerId, isOnline
        public event Action<int, bool, float> PlayerHighlighted; // playerId, highlight, duration
        public event Action AllPlayersCleared;
        public event Action<Sprite, Sprite, Sprite, Sprite> SpritesUpdated;

        #region Properties

        public Dictionary<int, PlayerListData> PlayerListDataCache => new Dictionary<int, PlayerListData>(_playerDataCache);

        public Sprite IconMongingDefault
        {
            get => _iconMongingDefault;
            set
            {
                if (SetProperty(ref _iconMongingDefault, value))
                {
                    SpritesUpdated?.Invoke(_iconMongingDefault, _iconMongingFaint, _iconMongingDead, _iconMongingEscape);
                    UpdateAllPlayerSprites();
                }
            }
        }

        public Sprite IconMongingFaint
        {
            get => _iconMongingFaint;
            set
            {
                if (SetProperty(ref _iconMongingFaint, value))
                {
                    SpritesUpdated?.Invoke(_iconMongingDefault, _iconMongingFaint, _iconMongingDead, _iconMongingEscape);
                    UpdateAllPlayerSprites();
                }
            }
        }

        public Sprite IconMongingDead
        {
            get => _iconMongingDead;
            set
            {
                if (SetProperty(ref _iconMongingDead, value))
                {
                    SpritesUpdated?.Invoke(_iconMongingDefault, _iconMongingFaint, _iconMongingDead, _iconMongingEscape);
                    UpdateAllPlayerSprites();
                }
            }
        }

        public Sprite IconMongingEscape
        {
            get => _iconMongingEscape;
            set
            {
                if (SetProperty(ref _iconMongingEscape, value))
                {
                    SpritesUpdated?.Invoke(_iconMongingDefault, _iconMongingFaint, _iconMongingDead, _iconMongingEscape);
                    UpdateAllPlayerSprites();
                }
            }
        }

        public int OnlinePlayerCount => GetOnlinePlayerCount();
        public int TotalPlayerCount => _playerDataCache.Count;

        #endregion

        #region Player Management

        public void UpdatePlayer(
            int playerId,
            string nickname,
            string colorTheme,
            string status = "default",
            bool isOnline = true,
            bool isHost = false)
        {
            if (playerId < 1 || playerId > 4) return;

            var playerData = new PlayerListData
            {
                playerId = playerId,
                nickname = nickname,
                colorTheme = colorTheme,
                status = status,
                avatarSprite = GetStatusSprite(status),
                isOnline = isOnline,
                isHost = isHost,
            };

            _playerDataCache[playerId] = playerData;

            PlayerUpdated?.Invoke(playerId, playerData);
            HUDEvents.TriggerPlayerStatusChange(playerId, status, nickname);
            HUDEvents.TriggerPlayerConnectionChange(playerId, isOnline);

            OnPropertyChanged(nameof(PlayerListDataCache));
            OnPropertyChanged(nameof(OnlinePlayerCount));
            OnPropertyChanged(nameof(TotalPlayerCount));

            if (EnableDebugLogs)
            {
                Debug.Log($"[PlayerListViewModel] 플레이어 {playerId} 업데이트: {nickname} ({status})");
            }
        }

        public void SetupDefaultPlayers()
        {
            for (int i = 1; i <= 4; i++)
            {
                var playerData = new PlayerListData
                {
                    playerId = i,
                    nickname = $"플레이어{i}",
                    colorTheme = GetDefaultColorTheme(i),
                    status = "default",
                    avatarSprite = _iconMongingDefault,
                    isOnline = true,
                    isHost = i == 1
                };

                _playerDataCache[i] = playerData;
                PlayerUpdated?.Invoke(i, playerData);
            }

            OnPropertyChanged(nameof(PlayerListDataCache));
            OnPropertyChanged(nameof(OnlinePlayerCount));
            OnPropertyChanged(nameof(TotalPlayerCount));

            if (EnableDebugLogs)
            {
                Debug.Log("[PlayerListViewModel] 기본 플레이어 설정 완료");
            }
        }

        public void UpdatePlayerFromData(List<PlayerListData> players)
        {
            foreach (var player in players)
            {
                if (player.playerId >= 1 && player.playerId <= 4)
                {
                    player.avatarSprite = GetStatusSprite(player.status);
                    _playerDataCache[player.playerId] = player;
                    PlayerUpdated?.Invoke(player.playerId, player);
                }
            }

            OnPropertyChanged(nameof(PlayerListDataCache));
            OnPropertyChanged(nameof(OnlinePlayerCount));
            OnPropertyChanged(nameof(TotalPlayerCount));
        }

        public void SetPlayerStatus(int playerId, string status)
        {
            if (_playerDataCache.ContainsKey(playerId))
            {
                var playerData = _playerDataCache[playerId];
                string oldStatus = playerData.status;
                playerData.status = status;
                playerData.avatarSprite = GetStatusSprite(status);

                if (oldStatus != status)
                {
                    PlayerUpdated?.Invoke(playerId, playerData);
                    PlayerStatusChanged?.Invoke(playerId, status);
                    OnPropertyChanged(nameof(PlayerListDataCache));

                    if (EnableDebugLogs)
                    {
                        Debug.Log($"[PlayerListViewModel] 플레이어 {playerId} 상태 변경: {oldStatus} → {status}");
                    }
                }
            }
        }

        public void SetPlayerOnlineStatus(int playerId, bool isOnline)
        {
            if (_playerDataCache.ContainsKey(playerId))
            {
                var playerData = _playerDataCache[playerId];
                bool oldOnlineStatus = playerData.isOnline;
                playerData.isOnline = isOnline;

                if (oldOnlineStatus != isOnline)
                {
                    PlayerUpdated?.Invoke(playerId, playerData);
                    PlayerConnectionChanged?.Invoke(playerId, isOnline);
                    OnPropertyChanged(nameof(PlayerListDataCache));
                    OnPropertyChanged(nameof(OnlinePlayerCount));

                    if (EnableDebugLogs)
                    {
                        Debug.Log($"[PlayerListViewModel] 플레이어 {playerId} 접속 상태 변경: {isOnline}");
                    }
                }
            }
        }

        public void HighlightPlayer(int playerId, bool highlight, float duration = 1f)
        {
            if (playerId < 1 || playerId > 4) return;

            PlayerHighlighted?.Invoke(playerId, highlight, duration);

            if (EnableDebugLogs)
            {
                Debug.Log($"[PlayerListViewModel] 플레이어 {playerId} 하이라이트: {highlight} ({duration}초)");
            }
        }

        public void ClearAllPlayers()
        {
            _playerDataCache.Clear();
            AllPlayersCleared?.Invoke();

            OnPropertyChanged(nameof(PlayerListDataCache));
            OnPropertyChanged(nameof(OnlinePlayerCount));
            OnPropertyChanged(nameof(TotalPlayerCount));

            if (EnableDebugLogs)
            {
                Debug.Log("[PlayerListViewModel] 모든 플레이어 데이터 초기화");
            }
        }

        #endregion

        #region Utility Methods

        public PlayerListData GetPlayer(int playerId)
        {
            return _playerDataCache.ContainsKey(playerId) ? _playerDataCache[playerId] : null;
        }

        public List<PlayerListData> GetAllPlayers()
        {
            return new List<PlayerListData>(_playerDataCache.Values);
        }

        public List<PlayerListData> GetOnlinePlayers()
        {
            var onlinePlayers = new List<PlayerListData>();
            foreach (var player in _playerDataCache.Values)
            {
                if (player.isOnline)
                {
                    onlinePlayers.Add(player);
                }
            }
            return onlinePlayers;
        }

        public int GetOnlinePlayerCount()
        {
            int count = 0;
            foreach (var player in _playerDataCache.Values)
            {
                if (player.isOnline)
                    count++;
            }
            return count;
        }

        public Color GetPlayerColor(string colorType)
        {
            return colorType switch
            {
                "yellow" => new Color(1f, 0.78f, 0.31f, 1f),
                "mint" => new Color(0.31f, 1f, 0.78f, 1f),
                "pink" => new Color(1f, 0.59f, 0.78f, 1f),
                "blue" => new Color(0.31f, 0.78f, 1f, 1f),
                _ => Color.white,
            };
        }

        private string GetDefaultColorTheme(int playerId)
        {
            return playerId switch
            {
                1 => "yellow",
                2 => "mint",
                3 => "pink",
                4 => "blue",
                _ => "yellow"
            };
        }

        private Sprite GetStatusSprite(string status)
        {
            return status switch
            {
                "default" => _iconMongingDefault,
                "dead" => _iconMongingDead,
                "escape" => _iconMongingEscape,
                "faint" => _iconMongingFaint,
                _ => _iconMongingDefault,
            };
        }

        private void UpdateAllPlayerSprites()
        {
            foreach (var kvp in _playerDataCache)
            {
                var playerData = kvp.Value;
                playerData.avatarSprite = GetStatusSprite(playerData.status);
                PlayerUpdated?.Invoke(playerData.playerId, playerData);
            }
        }

        #endregion

        #region Sprite Management

        public void SetSprites(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon)
        {
            bool changed = false;

            if (_iconMongingDefault != defaultIcon)
            {
                _iconMongingDefault = defaultIcon;
                changed = true;
            }

            if (_iconMongingFaint != faintIcon)
            {
                _iconMongingFaint = faintIcon;
                changed = true;
            }

            if (_iconMongingDead != deadIcon)
            {
                _iconMongingDead = deadIcon;
                changed = true;
            }

            if (_iconMongingEscape != escapeIcon)
            {
                _iconMongingEscape = escapeIcon;
                changed = true;
            }

            if (changed)
            {
                OnPropertiesChanged(nameof(IconMongingDefault), nameof(IconMongingFaint),
                                  nameof(IconMongingDead), nameof(IconMongingEscape));
                SpritesUpdated?.Invoke(_iconMongingDefault, _iconMongingFaint, _iconMongingDead, _iconMongingEscape);
                UpdateAllPlayerSprites();
            }
        }

        #endregion

        #region BaseViewModel Override

        protected override void InitializeViewModel()
        {
            base.InitializeViewModel();

            _playerDataCache = new Dictionary<int, PlayerListData>();
            SetupDefaultPlayers();

            if (EnableDebugLogs)
            {
                Debug.Log("[PlayerListViewModel] 초기화 완료");
            }
        }

        protected override void CleanupViewModel()
        {
            base.CleanupViewModel();

            PlayerUpdated = null;
            PlayerStatusChanged = null;
            PlayerConnectionChanged = null;
            PlayerHighlighted = null;
            AllPlayersCleared = null;
            SpritesUpdated = null;

            if (EnableDebugLogs)
            {
                Debug.Log("[PlayerListViewModel] 정리 완료");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log($"[PlayerListViewModel] State:\n" +
                     $"  TotalPlayers: {TotalPlayerCount}\n" +
                     $"  OnlinePlayers: {OnlinePlayerCount}\n" +
                     $"  Players: {string.Join(", ", _playerDataCache.Keys)}");

            foreach (var kvp in _playerDataCache)
            {
                var player = kvp.Value;
                Debug.Log($"    Player{player.playerId}: {player.nickname} ({player.status}, {player.colorTheme}, Online: {player.isOnline})");
            }
        }

        [ContextMenu("Setup Test Players")]
        private void SetupTestPlayers()
        {
            UpdatePlayer(1, "호스트", "yellow", "default", true, true);
            UpdatePlayer(2, "플레이어2", "mint", "faint", true, false);
            UpdatePlayer(3, "플레이어3", "pink", "dead", false, false);
            UpdatePlayer(4, "플레이어4", "blue", "escape", true, false);
        }

        [ContextMenu("Highlight Player 1")]
        private void DebugHighlightPlayer1() => HighlightPlayer(1, true, 2f);

        #endregion
    }
}