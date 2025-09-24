using Cysharp.Threading.Tasks;
using Features.Room.Models;
using Networks.Rooms.Domains;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Map.Services
{
    /// <summary>
    /// 플레이어 스폰을 담당하는 서비스 인터페이스
    /// Addressable을 통해 플레이어 프리팹을 동적 생성하고 VContainer 의존성 주입
    /// </summary>
    public interface IPlayerSpawnService
    {
        /// <summary>
        /// 플레이어 스폰 완료 이벤트
        /// </summary>
        event Action<string, GameObject> OnPlayerSpawned;

        /// <summary>
        /// 플레이어 제거 완료 이벤트
        /// </summary>
        event Action<string, GameObject> OnPlayerRemoved;

        /// <summary>
        /// 모든 플레이어 스폰 완료 이벤트
        /// </summary>
        event Action OnAllPlayersSpawned;

        /// <summary>
        /// 방 데이터로부터 플레이어 스폰 데이터 준비
        /// </summary>
        /// <param name="roomData">방 데이터</param>
        void PrepareSpawnData(RoomData roomData);

        /// <summary>
        /// 모든 플레이어 스폰 (비동기)
        /// </summary>
        /// <returns>스폰 완료까지 대기하는 Task</returns>
        UniTask SpawnAllPlayersAsync();

        /// <summary>
        /// 특정 플레이어 스폰
        /// </summary>
        /// <param name="players">플레이어 데이터 목록</param>
        UniTask SpawnPlayersAsync(List<PlayerPacket> players);

        /// <summary>
        /// 플레이어 제거
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <returns>제거 성공 여부</returns>
        bool RemovePlayer(long playerId);

        /// <summary>
        /// 모든 플레이어 정리
        /// </summary>
        void ClearAllPlayers();

        /// <summary>
        /// 로컬 플레이어 GameObject 가져오기
        /// </summary>
        /// <returns>로컬 플레이어 GameObject (없으면 null)</returns>
        GameObject GetLocalPlayer();

        /// <summary>
        /// 특정 ID의 플레이어 GameObject 가져오기
        /// </summary>
        /// <param name="playerId">플레이어 ID</param>
        /// <returns>플레이어 GameObject (없으면 null)</returns>
        GameObject GetPlayer(long playerId);

        /// <summary>
        /// 스폰된 플레이어 개수 가져오기
        /// </summary>
        /// <param name="playerType">플레이어 타입 (Mongging, Mongdung)</param>
        /// <returns>스폰된 플레이어 개수</returns>
        int GetSpawnedPlayerCount(string playerType);
    }
}