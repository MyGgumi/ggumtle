using Cysharp.Threading.Tasks;
using Features.Room.Models;
using Features.Map.Utils;
using Features.Ggumtle.Services;
using Networks.Rooms.Domains;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Map.Services
{
    /// <summary>
    /// 맵 오브젝트 스폰 서비스 구현체
    /// Addressables를 통해 프리팹을 로딩하고 방 데이터를 기반으로 오브젝트들을 스폰
    /// </summary>
    public class MapSpawnServiceImpl : IMapSpawnService
    {
        private readonly IAddressableLoadService _addressableLoadService;
        private readonly IGgumtleService _ggumtleService;
        private readonly bool _enableDebugLogs = false; // 스폰 로그 비활성화
        private readonly bool _useFixedGgumtlePositions = true; // 꿈틀이 고정 위치 사용 여부

        // 스폰된 오브젝트들을 타입별로 관리
        private readonly Dictionary<string, List<GameObject>> _spawnedObjects = new();
        private readonly Dictionary<int, GameObject> _spawnedGgumtles = new(); // ID로 꿈틀이 관리

        // 스폰할 데이터
        private RoomData _roomData;

        // Addressable 키 상수 (실제 프리팹 이름과 맞춤)
        private const string GGUMTLE_PREFAB_KEY = "Ggumtle";
        private const string CHEST_PREFAB_KEY = "InteractableChest";
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

            InitializeObjectContainers();

            if (_enableDebugLogs)
                Debug.Log("[MapSpawnService] 초기화 완료");
        }

        /// <summary>
        /// 오브젝트 컨테이너 초기화
        /// </summary>
        private void InitializeObjectContainers()
        {
            _spawnedObjects["Ggumtle"] = new List<GameObject>();
            _spawnedObjects["Chest"] = new List<GameObject>();
            _spawnedObjects["HealPack"] = new List<GameObject>();
            _spawnedObjects["SpeedPack"] = new List<GameObject>();
        }

        public void PrepareSpawnData(RoomData roomData)
        {
            _roomData = roomData ?? throw new ArgumentNullException(nameof(roomData));

            if (_enableDebugLogs)
            {
                Debug.Log("[MapSpawnService] 스폰 데이터 준비 완료:");
                Debug.Log($"  - 상자: {_roomData.Chests?.Count ?? 0}개");
                Debug.Log($"  - 꿈틀이: {_roomData.Ggumtles?.Count ?? 0}개");
                Debug.Log($"  - 힐팩: {_roomData.HealPacks?.Count ?? 0}개");
                Debug.Log($"  - 스피드팩: {_roomData.SpeedPacks?.Count ?? 0}개");
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
                    Debug.Log("[MapSpawnService] 모든 오브젝트 스폰 시작");

                // 모든 오브젝트를 병렬로 스폰
                var tasks = new List<UniTask>();

                if (_roomData.Chests != null && _roomData.Chests.Count > 0)
                    tasks.Add(SpawnChestsAsync(_roomData.Chests));

                if (_roomData.Ggumtles != null && _roomData.Ggumtles.Count > 0)
                    tasks.Add(SpawnGgumtlesAsync(_roomData.Ggumtles));

                if ((_roomData.HealPacks != null && _roomData.HealPacks.Count > 0) ||
                    (_roomData.SpeedPacks != null && _roomData.SpeedPacks.Count > 0))
                    tasks.Add(SpawnItemsAsync(_roomData.HealPacks, _roomData.SpeedPacks));

                await UniTask.WhenAll(tasks);

                if (_enableDebugLogs)
                    Debug.Log("[MapSpawnService] 모든 오브젝트 스폰 완료");

                OnAllObjectsSpawned?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSpawnService] 오브젝트 스폰 중 오류: {e.Message} - 가능한 오브젝트만 스폰하고 계속 진행");
                // throw를 제거하여 전체 프로세스가 중단되지 않도록 함
            }
        }

        public async UniTask SpawnChestsAsync(List<ChestPacket> chests)
        {
            if (chests == null || chests.Count == 0)
                return;

            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 상자 스폰 시작: {chests.Count}개");

                var chestPrefab = await _addressableLoadService.LoadAssetAsync<GameObject>(CHEST_PREFAB_KEY);
                if (chestPrefab == null)
                {
                    Debug.LogWarning($"[MapSpawnService] 상자 프리팹을 찾을 수 없음: {CHEST_PREFAB_KEY} - 상자 스폰을 건너뜀");
                    return;
                }

                foreach (var chest in chests)
                {
                    var position = chest.ToVector3();
                    var chestObject = await _addressableLoadService.InstantiateAsync(
                        CHEST_PREFAB_KEY, position, Quaternion.identity);

                    if (chestObject != null)
                    {
                        chestObject.name = $"Chest_{chest.Id}";
                        _spawnedObjects["Chest"].Add(chestObject);
                        OnObjectSpawned?.Invoke("Chest", chestObject);

                        if (_enableDebugLogs)
                            Debug.Log($"[MapSpawnService] 상자 스폰 완료: ID={chest.Id}, Position={position}");
                    }

                    // 한 프레임 대기 (성능 최적화)
                    await UniTask.Yield();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSpawnService] 상자 스폰 실패: {e.Message} - 다른 오브젝트 스폰은 계속 진행");
                // throw를 제거하여 전체 프로세스가 중단되지 않도록 함
            }
        }

        public async UniTask SpawnGgumtlesAsync(List<GgumtlePacket> ggumtles)
        {
            if (ggumtles == null || ggumtles.Count == 0)
                return;

            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 꿈틀이 스폰 시작: {ggumtles.Count}개");

                var ggumtlePrefab = await _addressableLoadService.LoadAssetAsync<GameObject>(GGUMTLE_PREFAB_KEY);
                if (ggumtlePrefab == null)
                {
                    Debug.LogWarning($"[MapSpawnService] 꿈틀이 프리팹을 찾을 수 없음: {GGUMTLE_PREFAB_KEY} - 꿈틀이 스폰을 건너뜀");
                    return;
                }

                // 고정 위치 사용 여부에 따라 분기
                if (_useFixedGgumtlePositions)
                {
                    await SpawnGgumtlesWithFixedPositions(ggumtles);
                }
                else
                {
                    await SpawnGgumtlesWithServerPositions(ggumtles);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSpawnService] 꿈틀이 스폰 실패: {e.Message} - 다른 오브젝트 스폰은 계속 진행");
                // throw를 제거하여 전체 프로세스가 중단되지 않도록 함
            }
        }

        /// <summary>
        /// 서버에서 받은 위치 그대로 꿈틀이 스폰
        /// </summary>
        private async UniTask SpawnGgumtlesWithServerPositions(List<GgumtlePacket> ggumtles)
        {
            Debug.Log($"[SERVER_ROOM_DATA] 서버 위치 그대로 꿈틀이 스폰 시작: {ggumtles.Count}개");

            foreach (var ggumtle in ggumtles)
            {
                var serverPosition = ggumtle.ToVector3();
                Debug.Log($"[SERVER_ROOM_DATA] 꿈틀이 스폰: ID={ggumtle.Id}, 서버 원본 좌표=({ggumtle.X}, {ggumtle.Y}, {ggumtle.Z}), Unity 변환 좌표={serverPosition}");

                var ggumtleObject = await SpawnGgumtleAsync(ggumtle.Id, serverPosition);

                if (ggumtleObject != null)
                {
                    Debug.Log($"[SERVER_ROOM_DATA] 꿈틀이 스폰 완료: ID={ggumtle.Id}, 최종 GameObject 위치={ggumtleObject.transform.position}");
                }

                // 한 프레임 대기 (성능 최적화)
                await UniTask.Yield();
            }

            Debug.Log("[SERVER_ROOM_DATA] 꿈틀이 서버 위치로 스폰 완료");
        }

        /// <summary>
        /// 고정된 위치에 꿈틀이 스폰 (서버 위치 무시)
        /// </summary>
        private async UniTask SpawnGgumtlesWithFixedPositions(List<GgumtlePacket> ggumtles)
        {
            // 고정 위치 배열
            Vector3[] fixedPositions = {
                new Vector3(52.7443886f, 5.37099981f, 11.4316998f),
                new Vector3(53.8800011f, 5.37099981f, 15.6700001f),
                new Vector3(48.75f, 5.37099981f, 12.5f)
            };

            for (int i = 0; i < ggumtles.Count && i < fixedPositions.Length; i++)
            {
                var ggumtle = ggumtles[i];
                var fixedPosition = fixedPositions[i];

                Debug.Log($"[SERVER_ROOM_DATA] 꿈틀이 고정 위치 스폰: ID={ggumtle.Id}, 서버 원본 좌표=({ggumtle.X}, {ggumtle.Y}, {ggumtle.Z}), 서버 Unity 변환={ggumtle.ToVector3()}, 고정 위치={fixedPosition}");

                var ggumtleObject = await SpawnGgumtleAsync(ggumtle.Id, fixedPosition);

                // 한 프레임 대기 (성능 최적화)
                await UniTask.Yield();
            }

            if (_enableDebugLogs)
                Debug.Log($"[MapSpawnService] 꿈틀이 고정 위치 스폰 완료: {Math.Min(ggumtles.Count, fixedPositions.Length)}개");
        }

        public async UniTask SpawnItemsAsync(List<HealPackPacket> healPacks, List<SpeedPackPacket> speedPacks)
        {
            try
            {
                var tasks = new List<UniTask>();

                if (healPacks != null && healPacks.Count > 0)
                    tasks.Add(SpawnHealPacksAsync(healPacks));

                if (speedPacks != null && speedPacks.Count > 0)
                    tasks.Add(SpawnSpeedPacksAsync(speedPacks));

                await UniTask.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSpawnService] 아이템 스폰 실패: {e.Message} - 다른 오브젝트 스폰은 계속 진행");
                // throw를 제거하여 전체 프로세스가 중단되지 않도록 함
            }
        }

        private async UniTask SpawnHealPacksAsync(List<HealPackPacket> healPacks)
        {
            if (_enableDebugLogs)
                Debug.Log($"[MapSpawnService] 힐팩 스폰 시작: {healPacks.Count}개");

            var healPackPrefab = await _addressableLoadService.LoadAssetAsync<GameObject>(HEALPACK_PREFAB_KEY);
            if (healPackPrefab == null)
            {
                Debug.LogWarning($"[MapSpawnService] 힐팩 프리팹을 찾을 수 없음: {HEALPACK_PREFAB_KEY} - 힐팩 스폰을 건너뜀");
                return;
            }

            foreach (var healPack in healPacks)
            {
                var position = healPack.ToVector3();
                var healPackObject = await _addressableLoadService.InstantiateAsync(
                    HEALPACK_PREFAB_KEY, position, Quaternion.identity);

                if (healPackObject != null)
                {
                    healPackObject.name = $"HealPack_{healPack.Id}";
                    _spawnedObjects["HealPack"].Add(healPackObject);
                    OnObjectSpawned?.Invoke("HealPack", healPackObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 힐팩 스폰 완료: ID={healPack.Id}, Position={position}");
                }

                await UniTask.Yield();
            }
        }

        private async UniTask SpawnSpeedPacksAsync(List<SpeedPackPacket> speedPacks)
        {
            if (_enableDebugLogs)
                Debug.Log($"[MapSpawnService] 스피드팩 스폰 시작: {speedPacks.Count}개");

            var speedPackPrefab = await _addressableLoadService.LoadAssetAsync<GameObject>(SPEEDPACK_PREFAB_KEY);
            if (speedPackPrefab == null)
            {
                Debug.LogWarning($"[MapSpawnService] 스피드팩 프리팹을 찾을 수 없음: {SPEEDPACK_PREFAB_KEY} - 스피드팩 스폰을 건너뜀");
                return;
            }

            foreach (var speedPack in speedPacks)
            {
                var position = speedPack.ToVector3();
                var speedPackObject = await _addressableLoadService.InstantiateAsync(
                    SPEEDPACK_PREFAB_KEY, position, Quaternion.identity);

                if (speedPackObject != null)
                {
                    speedPackObject.name = $"SpeedPack_{speedPack.Id}";
                    _spawnedObjects["SpeedPack"].Add(speedPackObject);
                    OnObjectSpawned?.Invoke("SpeedPack", speedPackObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 스피드팩 스폰 완료: ID={speedPack.Id}, Position={position}");
                }

                await UniTask.Yield();
            }
        }

        public async UniTask<GameObject> SpawnGgumtleAsync(int id, Vector3 position)
        {
            try
            {
                // 이미 스폰된 꿈틀이인지 확인
                if (_spawnedGgumtles.ContainsKey(id))
                {
                    Debug.LogWarning($"[MapSpawnService] 이미 스폰된 꿈틀이: ID={id}");
                    return _spawnedGgumtles[id];
                }

                var ggumtleObject = await _addressableLoadService.InstantiateAsync(
                    GGUMTLE_PREFAB_KEY, position, Quaternion.identity);

                if (ggumtleObject != null)
                {
                    ggumtleObject.name = $"Ggumtle_{id}";

                    // VContainer 의존성 주입 (프리팹 인스턴스에 주입)
                    var mainLifetimeScope = GameObject.FindFirstObjectByType<DI.MainLifetimeScope>();
                    if (mainLifetimeScope != null && mainLifetimeScope.Container != null)
                    {
                        mainLifetimeScope.Container.InjectGameObject(ggumtleObject);
                        if (_enableDebugLogs)
                            Debug.Log($"[MapSpawnService] GgumtleGameObject VContainer 의존성 주입 완료: ID={id}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MapSpawnService] MainLifetimeScope를 찾을 수 없어 의존성 주입 실패: ID={id}");
                    }

                    // 꿈틀이 ID 설정
                    var ggumtleComponent = ggumtleObject.GetComponent<Features.Ggumtle.Views.GgumtleGameObject>();
                    if (ggumtleComponent != null)
                    {
                        // GgumtleGameObject의 ID 설정 (리플렉션 사용)
                        var idField = typeof(Features.Ggumtle.Views.GgumtleGameObject)
                            .GetField("ggumtleId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        idField?.SetValue(ggumtleComponent, id);
                    }

                    // GgumtleService에 등록은 GgumtleGameObject에서 자동으로 처리됨

                    _spawnedObjects["Ggumtle"].Add(ggumtleObject);
                    _spawnedGgumtles[id] = ggumtleObject;
                    OnObjectSpawned?.Invoke("Ggumtle", ggumtleObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[MapSpawnService] 꿈틀이 스폰 완료: ID={id}, Position={position}");
                }

                return ggumtleObject;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 꿈틀이 스폰 실패: ID={id}, {e.Message}");
                throw;
            }
        }

        public bool RemoveGgumtle(int id)
        {
            try
            {
                if (!_spawnedGgumtles.TryGetValue(id, out var ggumtleObject))
                {
                    Debug.LogWarning($"[MapSpawnService] 제거할 꿈틀이를 찾을 수 없음: ID={id}");
                    return false;
                }

                // GgumtleService에서 해제
                _ggumtleService.UnregisterGgumtle(id);

                // 스폰된 오브젝트 리스트에서 제거
                _spawnedObjects["Ggumtle"].Remove(ggumtleObject);
                _spawnedGgumtles.Remove(id);

                // Addressable 인스턴스 해제
                _addressableLoadService.ReleaseInstance(ggumtleObject);

                OnObjectRemoved?.Invoke("Ggumtle", ggumtleObject);

                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 꿈틀이 제거 완료: ID={id}");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 꿈틀이 제거 실패: ID={id}, {e.Message}");
                return false;
            }
        }

        public void ClearAllObjects()
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[MapSpawnService] 모든 오브젝트 정리 시작");

                int totalRemoved = 0;

                foreach (var objectType in _spawnedObjects.Keys)
                {
                    var objects = _spawnedObjects[objectType];
                    totalRemoved += objects.Count;

                    foreach (var obj in objects)
                    {
                        if (obj != null)
                        {
                            _addressableLoadService.ReleaseInstance(obj);
                        }
                    }

                    objects.Clear();
                }

                _spawnedGgumtles.Clear();

                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 모든 오브젝트 정리 완료: {totalRemoved}개 제거");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 오브젝트 정리 중 오류: {e.Message}");
            }
        }

        public int GetSpawnedObjectCount(string objectType)
        {
            if (_spawnedObjects.TryGetValue(objectType, out var objects))
            {
                return objects.Count;
            }

            return 0;
        }
    }
}