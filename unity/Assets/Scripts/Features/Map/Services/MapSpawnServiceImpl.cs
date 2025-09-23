using Cysharp.Threading.Tasks;
using Features.Room.Models;
using Features.Map.Utils;
using Features.Ggumtle.Services;
using Features.Chest.Views;
using Features.FieldItem.Views;
using Features.FieldItem.Models;
using Features.Ggumtle.Views;
using Networks.Rooms.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Map.Services
{
    /// <summary>
    /// 맵 오브젝트 스폰 서비스 구현체
    /// Addressables를 통해 프리팹을 동적 생성하고 VContainer 의존성 주입
    /// </summary>
    public class MapSpawnServiceImpl : IMapSpawnService
    {
        private readonly IAddressableLoadService _addressableLoadService;
        private readonly IGgumtleService _ggumtleService;
        private readonly bool _enableDebugLogs = false;

        // 설정할 데이터
        private RoomData _roomData;

        // 동적 생성된 오브젝트들 관리
        private readonly List<Features.Chest.Views.ChestGameObject> _spawnedChests = new();
        private readonly List<Features.Ggumtle.Views.GgumtleGameObject> _spawnedGgumtles = new();
        private readonly List<Features.FieldItem.Views.FieldItemGameObject> _spawnedHealPacks = new();
        private readonly List<Features.FieldItem.Views.FieldItemGameObject> _spawnedSpeedPacks = new();

        // Addressable 키 상수
        private const string CHEST_PREFAB_KEY = "ChestGameObject";
        private const string GGUMTLE_PREFAB_KEY = "GgumtleGameObject";
        private const string HEALPACK_PREFAB_KEY = "HealPack";
        private const string SPEEDPACK_PREFAB_KEY = "SpeedPack";

        public event Action<string, GameObject> OnObjectSpawned;
        public event Action<string, GameObject> OnObjectRemoved;
        public event Action OnAllObjectsSpawned;

        [Inject]
        public MapSpawnServiceImpl(IAddressableLoadService addressableLoadService, IGgumtleService ggumtleService)
        {
            _addressableLoadService = addressableLoadService ?? throw new ArgumentNullException(nameof(addressableLoadService));
            _ggumtleService = ggumtleService ?? throw new ArgumentNullException(nameof(ggumtleService));

            if (_enableDebugLogs)
                Debug.Log("[MapSpawnService] 초기화 완료");
        }

        public void PrepareSpawnData(RoomData roomData)
        {
            Debug.Log($"[MapSpawnService] ===== PrepareSpawnData 메서드 시작 =====");
            Debug.Log($"[MapSpawnService] roomData 매개변수: {roomData != null}");

            _roomData = roomData ?? throw new ArgumentNullException(nameof(roomData));

            Debug.Log("[MapSpawnService] 스폰 데이터 준비 완료:");
            Debug.Log($"  - 상자: {_roomData.Chests?.Count ?? 0}개");
            Debug.Log($"  - 꿈틀이: {_roomData.Ggumtles?.Count ?? 0}개");
            Debug.Log($"  - 힐팩: {_roomData.HealPacks?.Count ?? 0}개");
            Debug.Log($"  - 스피드팩: {_roomData.SpeedPacks?.Count ?? 0}개");

            if (_roomData.Chests != null && _roomData.Chests.Count > 0)
            {
                Debug.Log($"[MapSpawnService] 상자 상세 정보:");
                foreach (var chest in _roomData.Chests)
                {
                    Debug.Log($"  - 상자 ID: {chest.Id}, 위치: {chest.Position}");
                }
            }
        }

        public async UniTask SpawnAllObjectsAsync()
        {
            if (_roomData == null)
            {
                Debug.LogError("[MapSpawnService] 방 데이터가 준비되지 않음");
                return;
            }

            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[MapSpawnService] 모든 오브젝트 동적 생성 시작");

                // 기존 오브젝트들 정리
                await ClearAllObjectsAsync();

                // 부모 오브젝트들 준비
                var chestsParent = GetOrCreateParent("Chests");
                var ggumtlesParent = GetOrCreateParent("Ggumtles");
                var fieldItemsParent = GetOrCreateParent("FieldItems");

                // 동적 생성
                if (_roomData.Chests != null && _roomData.Chests.Count > 0)
                    await SpawnChestsAsync(_roomData.Chests, chestsParent);

                if (_roomData.Ggumtles != null && _roomData.Ggumtles.Count > 0)
                    await SpawnGgumtlesAsync(_roomData.Ggumtles, ggumtlesParent);

                if (_roomData.HealPacks != null && _roomData.HealPacks.Count > 0)
                    await SpawnHealPacksAsync(_roomData.HealPacks, fieldItemsParent);

                if (_roomData.SpeedPacks != null && _roomData.SpeedPacks.Count > 0)
                    await SpawnSpeedPacksAsync(_roomData.SpeedPacks, fieldItemsParent);

                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 모든 오브젝트 동적 생성 완료 - 상자:{_spawnedChests.Count}, 꿈틀이:{_spawnedGgumtles.Count}, 힐팩:{_spawnedHealPacks.Count}, 스피드팩:{_spawnedSpeedPacks.Count}");

                OnAllObjectsSpawned?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 오브젝트 동적 생성 중 오류: {e.Message}");
            }
        }

        private async UniTask SpawnChestsAsync(List<ChestPacket> chests, Transform parent)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 상자 동적 생성 시작: {chests.Count}개");

                var positions = chests.Select(c => c.ToVector3()).ToList();
                var spawnedChests = await _addressableLoadService.SpawnMultipleAsync<Features.Chest.Views.ChestGameObject>(
                    CHEST_PREFAB_KEY, positions, parent);

                // ID 설정 및 등록
                for (int i = 0; i < spawnedChests.Count && i < chests.Count; i++)
                {
                    var chestData = chests[i];
                    var chestObject = spawnedChests[i];

                    // ID 설정
                    SetChestId(chestObject, chestData.Id);

                    // 관리 리스트에 추가
                    _spawnedChests.Add(chestObject);

                    // 이벤트 발생
                    OnObjectSpawned?.Invoke("Chest", chestObject.gameObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 상자 생성 완료: ID={chestData.Id}, Position={chestData.ToVector3()}");
                }

                Debug.Log($"[MapSpawnService] 상자 동적 생성 완료: {_spawnedChests.Count}개");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 상자 동적 생성 실패: {e.Message}");
            }
        }

        private async UniTask SpawnGgumtlesAsync(List<GgumtlePacket> ggumtles, Transform parent)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 꿈틀이 동적 생성 시작: {ggumtles.Count}개");

                var positions = ggumtles.Select(g => g.ToVector3()).ToList();
                var spawnedGgumtles = await _addressableLoadService.SpawnMultipleAsync<Features.Ggumtle.Views.GgumtleGameObject>(
                    GGUMTLE_PREFAB_KEY, positions, parent);

                // ID 설정 및 서비스 등록
                for (int i = 0; i < spawnedGgumtles.Count && i < ggumtles.Count; i++)
                {
                    var ggumtleData = ggumtles[i];
                    var ggumtleObject = spawnedGgumtles[i];

                    // ID 설정
                    SetGgumtleId(ggumtleObject, ggumtleData.Id);

                    // GgumtleService에 등록
                    _ggumtleService.RegisterGgumtle(ggumtleData.Id.ToString(), ggumtleObject.gameObject.name, ggumtleData.ToVector3());

                    // 관리 리스트에 추가
                    _spawnedGgumtles.Add(ggumtleObject);

                    // 이벤트 발생
                    OnObjectSpawned?.Invoke("Ggumtle", ggumtleObject.gameObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 꿈틀이 생성 완료: ID={ggumtleData.Id}, Position={ggumtleData.ToVector3()}");
                }

                Debug.Log($"[MapSpawnService] 꿈틀이 동적 생성 완료: {_spawnedGgumtles.Count}개");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 꿈틀이 동적 생성 실패: {e.Message}");
            }
        }

        private async UniTask SpawnHealPacksAsync(List<HealPackPacket> healPacks, Transform parent)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 힐팩 동적 생성 시작: {healPacks.Count}개");

                var positions = healPacks.Select(h => h.ToVector3()).ToList();
                var spawnedHealPacks = await _addressableLoadService.SpawnMultipleAsync<Features.FieldItem.Views.FieldItemGameObject>(
                    HEALPACK_PREFAB_KEY, positions, parent);

                // ID 및 타입 설정
                for (int i = 0; i < spawnedHealPacks.Count && i < healPacks.Count; i++)
                {
                    var healPackData = healPacks[i];
                    var healPackObject = spawnedHealPacks[i];

                    // ID 및 타입 설정
                    healPackObject.SetId(healPackData.Id);
                    healPackObject.SetType(FieldItemType.HealPack);

                    // 관리 리스트에 추가
                    _spawnedHealPacks.Add(healPackObject);

                    // 이벤트 발생
                    OnObjectSpawned?.Invoke("HealPack", healPackObject.gameObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 힐팩 생성 완료: ID={healPackData.Id}, Position={healPackData.ToVector3()}");
                }

                Debug.Log($"[MapSpawnService] 힐팩 동적 생성 완료: {_spawnedHealPacks.Count}개");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 힐팩 동적 생성 실패: {e.Message}");
            }
        }

        private async UniTask SpawnSpeedPacksAsync(List<SpeedPackPacket> speedPacks, Transform parent)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 스피드팩 동적 생성 시작: {speedPacks.Count}개");

                var positions = speedPacks.Select(s => s.ToVector3()).ToList();
                var spawnedSpeedPacks = await _addressableLoadService.SpawnMultipleAsync<Features.FieldItem.Views.FieldItemGameObject>(
                    SPEEDPACK_PREFAB_KEY, positions, parent);

                // ID 및 타입 설정
                for (int i = 0; i < spawnedSpeedPacks.Count && i < speedPacks.Count; i++)
                {
                    var speedPackData = speedPacks[i];
                    var speedPackObject = spawnedSpeedPacks[i];

                    // ID 및 타입 설정
                    speedPackObject.SetId(speedPackData.Id);
                    speedPackObject.SetType(FieldItemType.SpeedPack);

                    // 관리 리스트에 추가
                    _spawnedSpeedPacks.Add(speedPackObject);

                    // 이벤트 발생
                    OnObjectSpawned?.Invoke("SpeedPack", speedPackObject.gameObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 스피드팩 생성 완료: ID={speedPackData.Id}, Position={speedPackData.ToVector3()}");
                }

                Debug.Log($"[MapSpawnService] 스피드팩 동적 생성 완료: {_spawnedSpeedPacks.Count}개");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 스피드팩 동적 생성 실패: {e.Message}");
            }
        }

        private Transform GetOrCreateParent(string parentName)
        {
            var parent = GameObject.Find(parentName);
            if (parent == null)
            {
                parent = new GameObject(parentName);
                Debug.Log($"[MapSpawnService] 부모 오브젝트 생성: {parentName}");
            }
            return parent.transform;
        }

        // 기존 SetupPreplaced 메서드들은 호환성을 위해 새 스폰 메서드 호출로 변경
        public void SetupPreplacedChests(List<ChestPacket> chests)
        {
            if (chests == null || chests.Count == 0)
                return;

            // 동적 생성 방식으로 변경 - 비동기 호출
            var parent = GetOrCreateParent("Chests");
            _ = SpawnChestsAsync(chests, parent);
        }

        public void SetupPreplacedGgumtles(List<GgumtlePacket> ggumtles)
        {
            if (ggumtles == null || ggumtles.Count == 0)
                return;

            // 동적 생성 방식으로 변경 - 비동기 호출
            var parent = GetOrCreateParent("Ggumtles");
            _ = SpawnGgumtlesAsync(ggumtles, parent);
        }

        public void SetupPreplacedHealPacks(List<HealPackPacket> healPacks)
        {
            if (healPacks == null || healPacks.Count == 0)
                return;

            // 동적 생성 방식으로 변경 - 비동기 호출
            var parent = GetOrCreateParent("FieldItems");
            _ = SpawnHealPacksAsync(healPacks, parent);
        }

        public void SetupPreplacedSpeedPacks(List<SpeedPackPacket> speedPacks)
        {
            if (speedPacks == null || speedPacks.Count == 0)
                return;

            // 동적 생성 방식으로 변경 - 비동기 호출
            var parent = GetOrCreateParent("FieldItems");
            _ = SpawnSpeedPacksAsync(speedPacks, parent);
        }

        private async UniTask ClearAllObjectsAsync()
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[MapSpawnService] 기존 오브젝트들 정리 시작");

                int totalReleased = 0;

                // 상자들 해제
                foreach (var chest in _spawnedChests)
                {
                    if (chest != null && chest.gameObject != null)
                    {
                        _addressableLoadService.ReleaseInstance(chest.gameObject);
                        totalReleased++;
                    }
                }
                _spawnedChests.Clear();

                // 꿈틀이들 해제 (서비스에서도 해제)
                foreach (var ggumtle in _spawnedGgumtles)
                {
                    if (ggumtle != null && ggumtle.gameObject != null)
                    {
                        _ggumtleService.UnregisterGgumtle(ggumtle.GgumtleId.ToString());
                        _addressableLoadService.ReleaseInstance(ggumtle.gameObject);
                        totalReleased++;
                    }
                }
                _spawnedGgumtles.Clear();

                // 힐팩들 해제
                foreach (var healPack in _spawnedHealPacks)
                {
                    if (healPack != null && healPack.gameObject != null)
                    {
                        _addressableLoadService.ReleaseInstance(healPack.gameObject);
                        totalReleased++;
                    }
                }
                _spawnedHealPacks.Clear();

                // 스피드팩들 해제
                foreach (var speedPack in _spawnedSpeedPacks)
                {
                    if (speedPack != null && speedPack.gameObject != null)
                    {
                        _addressableLoadService.ReleaseInstance(speedPack.gameObject);
                        totalReleased++;
                    }
                }
                _spawnedSpeedPacks.Clear();

                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 기존 오브젝트들 정리 완료: {totalReleased}개 해제");

                // 한 프레임 대기 (정리 완료 보장)
                await UniTask.Yield();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 오브젝트 정리 중 오류: {e.Message}");
            }
        }

        // 나머지 기존 메서드들은 동적 생성 방식으로 변경됨
        public void SpawnItems(List<HealPackPacket> healPacks, List<SpeedPackPacket> speedPacks)
        {
            try
            {
                if (healPacks != null && healPacks.Count > 0)
                    SetupPreplacedHealPacks(healPacks);

                if (speedPacks != null && speedPacks.Count > 0)
                    SetupPreplacedSpeedPacks(speedPacks);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSpawnService] 아이템 스폰 실패: {e.Message}");
            }
        }

        public bool RemoveGgumtle(int id)
        {
            try
            {
                var ggumtleToRemove = _spawnedGgumtles.FirstOrDefault(g => g != null && g.GgumtleId == id);

                if (ggumtleToRemove != null)
                {
                    // GgumtleService에서 해제
                    _ggumtleService.UnregisterGgumtle(id.ToString());

                    // Addressable 해제
                    _addressableLoadService.ReleaseInstance(ggumtleToRemove.gameObject);

                    // 리스트에서 제거
                    _spawnedGgumtles.Remove(ggumtleToRemove);

                    OnObjectRemoved?.Invoke("Ggumtle", ggumtleToRemove.gameObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 꿈틀이 제거 완료: ID={id}");

                    return true;
                }

                Debug.LogWarning($"[MapSpawnService] 제거할 꿈틀이를 찾을 수 없음: ID={id}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 꿈틀이 제거 실패: ID={id}, {e.Message}");
                return false;
            }
        }

        public void ClearAllObjects()
        {
            // 비동기 메서드 호출
            _ = ClearAllObjectsAsync();
        }

        public int GetSpawnedObjectCount(string objectType)
        {
            return objectType switch
            {
                "Ggumtle" => _spawnedGgumtles.Count(g => g != null),
                "Chest" => _spawnedChests.Count(c => c != null),
                "HealPack" => _spawnedHealPacks.Count(h => h != null),
                "SpeedPack" => _spawnedSpeedPacks.Count(s => s != null),
                _ => 0
            };
        }

        // 유틸리티 메서드들 (리플렉션 사용)
        private void SetChestId(Features.Chest.Views.ChestGameObject chestObject, int id)
        {
            try
            {
                var chestIdField = typeof(Features.Chest.Views.ChestGameObject).GetField("chestId",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (chestIdField != null)
                {
                    chestIdField.SetValue(chestObject, id);
                    Debug.Log($"[MapSpawnService] ChestId 설정 완료: {id}");
                }
                else
                {
                    Debug.LogWarning($"[MapSpawnService] chestId 필드를 찾을 수 없음: {id}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] ChestId 설정 실패: {id}, Error: {e.Message}");
            }
        }

        private void SetGgumtleId(Features.Ggumtle.Views.GgumtleGameObject ggumtleObject, int id)
        {
            try
            {
                var ggumtleIdField = typeof(Features.Ggumtle.Views.GgumtleGameObject).GetField("ggumtleId",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (ggumtleIdField != null)
                {
                    ggumtleIdField.SetValue(ggumtleObject, id);
                    Debug.Log($"[MapSpawnService] GgumtleId 설정 완료: {id}");
                }
                else
                {
                    Debug.LogWarning($"[MapSpawnService] ggumtleId 필드를 찾을 수 없음: {id}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] GgumtleId 설정 실패: {id}, Error: {e.Message}");
            }
        }
    }
}
