using System.Collections.Generic;
using Features.PlayerList.Models;
using R3;
using UnityEngine;

namespace Features.PlayerList.Services
{
    /// <summary>
    /// 플레이어 리스트 관리 서비스 인터페이스
    /// </summary>
    public interface IPlayerListService
    {
        /// <summary>
        /// 전체 플레이어 수 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<int> TotalPlayerCount { get; }

        /// <summary>
        /// 온라인 플레이어 수 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<int> OnlinePlayerCount { get; }

        /// <summary>
        /// 모든 플레이어 데이터 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<Dictionary<int, PlayerListData>> AllPlayers { get; }

        /// <summary>
        /// 스프라이트 리소스 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<Sprite> DefaultIcon { get; }
        ReadOnlyReactiveProperty<Sprite> FaintIcon { get; }
        ReadOnlyReactiveProperty<Sprite> DeadIcon { get; }
        ReadOnlyReactiveProperty<Sprite> EscapeIcon { get; }

        /// <summary>
        /// 플레이어 업데이트
        /// </summary>
        void UpdatePlayer(int playerId, string nickname, string colorTheme, string status = "default", bool isOnline = true, bool isHost = false);

        /// <summary>
        /// 플레이어 상태 설정
        /// </summary>
        void SetPlayerStatus(int playerId, string status);

        /// <summary>
        /// 플레이어 온라인 상태 설정
        /// </summary>
        void SetPlayerOnlineStatus(int playerId, bool isOnline);

        /// <summary>
        /// 플레이어 하이라이트
        /// </summary>
        void HighlightPlayer(int playerId, bool highlight, float duration = 1f);

        /// <summary>
        /// 플레이어 역할 변경 (호스트 등)
        /// </summary>
        void SetPlayerHost(int playerId, bool isHost);

        /// <summary>
        /// 기본 플레이어 설정
        /// </summary>
        void SetupDefaultPlayers();

        /// <summary>
        /// 모든 플레이어 초기화
        /// </summary>
        void ClearAllPlayers();

        /// <summary>
        /// 스프라이트 설정
        /// </summary>
        void SetSprites(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon);

        /// <summary>
        /// 특정 플레이어 데이터 가져오기
        /// </summary>
        PlayerListData GetPlayer(int playerId);

        /// <summary>
        /// 모든 플레이어 데이터 가져오기
        /// </summary>
        List<PlayerListData> GetAllPlayersList();

        /// <summary>
        /// 온라인 플레이어만 가져오기
        /// </summary>
        List<PlayerListData> GetOnlinePlayers();

        /// <summary>
        /// 플레이어 색상 가져오기
        /// </summary>
        Color GetPlayerColor(string colorType);

        /// <summary>
        /// 플레이어 리스트 데이터에서 업데이트
        /// </summary>
        void UpdateFromPlayerList(List<PlayerListData> players);
    }
}