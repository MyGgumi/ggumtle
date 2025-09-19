using Cysharp.Threading.Tasks;
using Features.Room.Models;
using Networks.Rooms.Domains;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Map.Services
{
    /// <summary>
    /// 맵 오브젝트 스폰을 담당하는 서비스 인터페이스
    /// </summary>
    public interface IMapSpawnService
    {
        /// <summary>
        /// 오브젝트 스폰 완료 이벤트
        /// </summary>
        event Action<string, GameObject> OnObjectSpawned;

        /// <summary>
        /// 오브젝트 제거 완료 이벤트
        /// </summary>
        event Action<string, GameObject> OnObjectRemoved;

        /// <summary>
        /// 모든 초기 오브젝트 스폰 완료 이벤트
        /// </summary>
        event Action OnAllObjectsSpawned;

        /// <summary>
        /// 방 데이터로부터 스폰 데이터 준비
        /// </summary>
        /// <param name="roomData">방 데이터</param>
        void PrepareSpawnData(RoomData roomData);

        /// <summary>
        /// 모든 오브젝트 스폰 (비동기)
        /// </summary>
        /// <returns>스폰 완료까지 대기하는 Task</returns>
        UniTask SpawnAllObjectsAsync();

        /// <summary>
        /// 상자들 스폰
        /// </summary>
        /// <param name="chests">상자 데이터 목록</param>
        /// <returns>스폰 완료까지 대기하는 Task</returns>
        UniTask SpawnChestsAsync(List<ChestPacket> chests);

        /// <summary>
        /// 꿈틀이들 스폰
        /// </summary>
        /// <param name="ggumtles">꿈틀이 데이터 목록</param>
        /// <returns>스폰 완료까지 대기하는 Task</returns>
        UniTask SpawnGgumtlesAsync(List<GgumtlePacket> ggumtles);

        /// <summary>
        /// 아이템들 스폰 (힐팩, 스피드팩)
        /// </summary>
        /// <param name="healPacks">힐팩 데이터 목록</param>
        /// <param name="speedPacks">스피드팩 데이터 목록</param>
        /// <returns>스폰 완료까지 대기하는 Task</returns>
        UniTask SpawnItemsAsync(List<HealPackPacket> healPacks, List<SpeedPackPacket> speedPacks);

        /// <summary>
        /// 단일 꿈틀이 스폰 (런타임 스폰용)
        /// </summary>
        /// <param name="id">꿈틀이 ID</param>
        /// <param name="position">스폰 위치</param>
        /// <returns>생성된 GameObject</returns>
        UniTask<GameObject> SpawnGgumtleAsync(int id, Vector3 position);

        /// <summary>
        /// 꿈틀이 제거 (성불용)
        /// </summary>
        /// <param name="id">꿈틀이 ID</param>
        /// <returns>제거 성공 여부</returns>
        bool RemoveGgumtle(int id);

        /// <summary>
        /// 모든 스폰된 오브젝트 정리
        /// </summary>
        void ClearAllObjects();

        /// <summary>
        /// 특정 타입의 오브젝트 개수 가져오기
        /// </summary>
        /// <param name="objectType">오브젝트 타입</param>
        /// <returns>스폰된 오브젝트 개수</returns>
        int GetSpawnedObjectCount(string objectType);
    }
}