using Cysharp.Threading.Tasks;
using Features.Room.Models;
using Networks.Rooms.Domains;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Map.Services
{
    /// <summary>
    /// 맵 오브젝트 설정을 담당하는 서비스 인터페이스
    /// Preplaced 방식으로 미리 배치된 오브젝트들을 활성화/비활성화하여 관리
    /// </summary>
    public interface IMapSpawnService
    {
        /// <summary>
        /// 오브젝트 활성화 완료 이벤트
        /// </summary>
        event Action<string, GameObject> OnObjectSpawned;

        /// <summary>
        /// 오브젝트 비활성화 완료 이벤트
        /// </summary>
        event Action<string, GameObject> OnObjectRemoved;

        /// <summary>
        /// 모든 초기 오브젝트 설정 완료 이벤트
        /// </summary>
        event Action OnAllObjectsSpawned;

        /// <summary>
        /// 방 데이터로부터 오브젝트 설정 데이터 준비
        /// </summary>
        /// <param name="roomData">방 데이터</param>
        void PrepareSpawnData(RoomData roomData);

        /// <summary>
        /// 모든 preplaced 오브젝트 설정 (비동기)
        /// </summary>
        /// <returns>설정 완료까지 대기하는 Task</returns>
        UniTask SpawnAllObjectsAsync();

        /// <summary>
        /// 미리 배치된 상자들 설정
        /// </summary>
        /// <param name="chests">상자 데이터 목록</param>
        void SetupPreplacedChests(List<ChestPacket> chests);

        /// <summary>
        /// 미리 배치된 꿈틀이들 설정
        /// </summary>
        /// <param name="ggumtles">꿈틀이 데이터 목록</param>
        void SetupPreplacedGgumtles(List<GgumtlePacket> ggumtles);

        /// <summary>
        /// 미리 배치된 힐팩들 설정
        /// </summary>
        /// <param name="healPacks">힐팩 데이터 목록</param>
        void SetupPreplacedHealPacks(List<HealPackPacket> healPacks);

        /// <summary>
        /// 미리 배치된 스피드팩들 설정
        /// </summary>
        /// <param name="speedPacks">스피드팩 데이터 목록</param>
        void SetupPreplacedSpeedPacks(List<SpeedPackPacket> speedPacks);

        /// <summary>
        /// 미리 배치된 아이템들 설정 (힐팩, 스피드팩)
        /// </summary>
        /// <param name="healPacks">힐팩 데이터 목록</param>
        /// <param name="speedPacks">스피드팩 데이터 목록</param>
        void SpawnItems(List<HealPackPacket> healPacks, List<SpeedPackPacket> speedPacks);

        // SpawnGgumtleAsync는 preplaced 방식으로 대체됨 - SetupPreplacedGgumtles 사용

        /// <summary>
        /// 단일 꿈틀이 동적 스폰 (함정 발동용)
        /// </summary>
        /// <param name="ggumtleId">꿈틀이 ID</param>
        /// <param name="position">스폰 위치</param>
        /// <returns>스폰 성공 여부</returns>
        UniTask<bool> SpawnSingleGgumtleAsync(int ggumtleId, Vector3 position);

        /// <summary>
        /// 꿈틀이 제거 (성불용) - preplaced 오브젝트 비활성화
        /// </summary>
        /// <param name="id">꿈틀이 ID</param>
        /// <returns>제거 성공 여부</returns>
        bool RemoveGgumtle(int id);

        /// <summary>
        /// 모든 preplaced 오브젝트 비활성화
        /// </summary>
        void ClearAllObjects();

        /// <summary>
        /// 특정 타입의 활성화된 오브젝트 개수 가져오기
        /// </summary>
        /// <param name="objectType">오브젝트 타입 (Ggumtle, Chest, HealPack, SpeedPack)</param>
        /// <returns>활성화된 오브젝트 개수</returns>
        int GetSpawnedObjectCount(string objectType);
    }
}