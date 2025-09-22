using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.PlayerList.Models
{
    /// <summary>
    /// 플레이어 정보 데이터 모델 (통합)
    /// </summary>
    [Serializable]
    public class PlayerListData
    {
        [Header("Player Info")]
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

        public PlayerListData(
            int id,
            string name,
            string color = "yellow",
            string playerStatus = "default",
            bool online = true,
            bool host = false
        )
        {
            playerId = id;
            nickname = name;
            colorTheme = color;
            status = playerStatus;
            avatarSprite = null;
            isOnline = online;
            isHost = host;
        }

        /// <summary>
        /// 플레이어 데이터 복사
        /// </summary>
        public PlayerListData Clone()
        {
            return new PlayerListData
            {
                playerId = this.playerId,
                nickname = this.nickname,
                colorTheme = this.colorTheme,
                status = this.status,
                avatarSprite = this.avatarSprite,
                isOnline = this.isOnline,
                isHost = this.isHost
            };
        }
    }

    /// <summary>
    /// 플레이어 리스트 전체 데이터 관리
    /// </summary>
    [Serializable]
    public class PlayerListModel
    {
        [Header("Player Data")]
        public Dictionary<int, PlayerListData> players = new();

        [Header("Sprite Resources")]
        public Sprite iconMongingDefault;
        public Sprite iconMongingFaint;
        public Sprite iconMongingDead;
        public Sprite iconMongingEscape;

        /// <summary>
        /// 플레이어 개수 (최대 4명)
        /// </summary>
        public int TotalPlayerCount => players.Count;

        /// <summary>
        /// 온라인 플레이어 개수
        /// </summary>
        public int OnlinePlayerCount
        {
            get
            {
                int count = 0;
                foreach (var player in players.Values)
                {
                    if (player.isOnline) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 플레이어 추가/업데이트
        /// </summary>
        public void UpdatePlayer(PlayerListData playerData)
        {
            if (playerData.playerId >= 1 && playerData.playerId <= 4)
            {
                playerData.avatarSprite = GetStatusSprite(playerData.status);
                players[playerData.playerId] = playerData;
            }
        }

        /// <summary>
        /// 플레이어 가져오기
        /// </summary>
        public PlayerListData GetPlayer(int playerId)
        {
            return players.ContainsKey(playerId) ? players[playerId] : null;
        }

        /// <summary>
        /// 모든 플레이어 가져오기
        /// </summary>
        public List<PlayerListData> GetAllPlayers()
        {
            return new List<PlayerListData>(players.Values);
        }

        /// <summary>
        /// 온라인 플레이어만 가져오기
        /// </summary>
        public List<PlayerListData> GetOnlinePlayers()
        {
            var onlinePlayers = new List<PlayerListData>();
            foreach (var player in players.Values)
            {
                if (player.isOnline)
                {
                    onlinePlayers.Add(player);
                }
            }
            return onlinePlayers;
        }

        /// <summary>
        /// 기본 플레이어 설정
        /// </summary>
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
                    avatarSprite = iconMongingDefault,
                    isOnline = true,
                    isHost = i == 1,
                };

                players[i] = playerData;
            }
        }

        /// <summary>
        /// 모든 플레이어 제거
        /// </summary>
        public void ClearAllPlayers()
        {
            players.Clear();
        }

        /// <summary>
        /// 플레이어 색상 가져오기
        /// </summary>
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

        /// <summary>
        /// 상태에 따른 스프라이트 가져오기
        /// </summary>
        public Sprite GetStatusSprite(string status)
        {
            return status switch
            {
                "default" => iconMongingDefault,
                "dead" => iconMongingDead,
                "escape" => iconMongingEscape,
                "faint" => iconMongingFaint,
                _ => iconMongingDefault,
            };
        }

        /// <summary>
        /// 플레이어 ID에 따른 기본 색상 테마
        /// </summary>
        private string GetDefaultColorTheme(int playerId)
        {
            return playerId switch
            {
                1 => "yellow",
                2 => "mint",
                3 => "pink",
                4 => "blue",
                _ => "yellow",
            };
        }

        /// <summary>
        /// 스프라이트 설정
        /// </summary>
        public void SetSprites(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon)
        {
            iconMongingDefault = defaultIcon;
            iconMongingFaint = faintIcon;
            iconMongingDead = deadIcon;
            iconMongingEscape = escapeIcon;

            // 모든 플레이어의 스프라이트 업데이트
            foreach (var kvp in players)
            {
                var playerData = kvp.Value;
                playerData.avatarSprite = GetStatusSprite(playerData.status);
            }
        }
    }

    /// <summary>
    /// 플레이어 역할 (Legacy MonggingHUDData.cs에서 이동)
    /// </summary>
    public enum PlayerRole
    {
        Mongging, // 몽깅이 (일반 플레이어)
        Mongdung, // 몽둥이 (술래)
    }
}