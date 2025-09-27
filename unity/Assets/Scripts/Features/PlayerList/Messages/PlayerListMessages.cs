using Features.PlayerList.Models;
using UnityEngine;

namespace Features.PlayerList.Messages
{
    /// <summary>
    /// 플레이어 업데이트 메시지
    /// </summary>
    public readonly struct PlayerUpdatedMessage
    {
        public readonly int playerId;
        public readonly PlayerListData playerData;

        public PlayerUpdatedMessage(int playerId, PlayerListData playerData)
        {
            this.playerId = playerId;
            this.playerData = playerData;
        }
    }

    /// <summary>
    /// 플레이어 상태 변경 메시지
    /// </summary>
    public readonly struct PlayerStatusChangedMessage
    {
        public readonly int playerId;
        public readonly string status;
        public readonly string nickname;

        public PlayerStatusChangedMessage(int playerId, string status, string nickname)
        {
            this.playerId = playerId;
            this.status = status;
            this.nickname = nickname;
        }
    }

    /// <summary>
    /// 플레이어 접속 상태 변경 메시지
    /// </summary>
    public readonly struct PlayerConnectionChangedMessage
    {
        public readonly int playerId;
        public readonly bool isOnline;

        public PlayerConnectionChangedMessage(int playerId, bool isOnline)
        {
            this.playerId = playerId;
            this.isOnline = isOnline;
        }
    }

    /// <summary>
    /// 플레이어 하이라이트 메시지
    /// </summary>
    public readonly struct PlayerHighlightedMessage
    {
        public readonly int playerId;
        public readonly bool highlight;
        public readonly float duration;

        public PlayerHighlightedMessage(int playerId, bool highlight, float duration)
        {
            this.playerId = playerId;
            this.highlight = highlight;
            this.duration = duration;
        }
    }

    /// <summary>
    /// 플레이어 호스트 변경 메시지
    /// </summary>
    public readonly struct PlayerHostChangedMessage
    {
        public readonly int playerId;
        public readonly bool isHost;

        public PlayerHostChangedMessage(int playerId, bool isHost)
        {
            this.playerId = playerId;
            this.isHost = isHost;
        }
    }

    /// <summary>
    /// 플레이어 스프라이트 업데이트 메시지
    /// </summary>
    public readonly struct PlayerSpritesUpdatedMessage
    {
        public readonly Sprite defaultIcon;
        public readonly Sprite faintIcon;
        public readonly Sprite deadIcon;
        public readonly Sprite escapeIcon;

        public PlayerSpritesUpdatedMessage(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon)
        {
            this.defaultIcon = defaultIcon;
            this.faintIcon = faintIcon;
            this.deadIcon = deadIcon;
            this.escapeIcon = escapeIcon;
        }
    }

    /// <summary>
    /// 플레이어 목록 동기화 메시지
    /// </summary>
    public readonly struct PlayerListSyncMessage
    {
        public readonly int totalPlayers;
        public readonly int onlinePlayers;

        public PlayerListSyncMessage(int totalPlayers, int onlinePlayers)
        {
            this.totalPlayers = totalPlayers;
            this.onlinePlayers = onlinePlayers;
        }
    }

    /// <summary>
    /// 모든 플레이어 초기화 메시지
    /// </summary>
    public readonly struct AllPlayersClearedMessage
    {
        public readonly bool cleared;

        public AllPlayersClearedMessage(bool cleared)
        {
            this.cleared = cleared;
        }
    }

    /// <summary>
    /// 몽깅이 플레이어 상태 변경 메시지 (MonggingTeam → PlayerList)
    /// </summary>
    public readonly struct PlayerListMonggingStateUpdateMessage
    {
        public readonly long playerId;
        public readonly string newStatus; // "default", "faint", "dead", "escape"
        public readonly string playerName;

        public PlayerListMonggingStateUpdateMessage(long playerId, string newStatus, string playerName)
        {
            this.playerId = playerId;
            this.newStatus = newStatus;
            this.playerName = playerName;
        }
    }
}