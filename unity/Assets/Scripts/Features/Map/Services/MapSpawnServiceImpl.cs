using Cysharp.Threading.Tasks;
using Features.Room.Models;
using Features.Map.Utils;
using Features.Ggumtle.Services;
using Features.Chest.Views;
using Features.FieldItem.Views;
using Features.FieldItem.Models;
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
    /// Addressables를 통해 프리팹을 로딩하고 방 데이터를 기반으로 오브젝트들을 스폰
    /// </summary>
    public class MapSpawnServiceImpl : IMapSpawnService
    {
        private readonly IAddressableLoadService _addressableLoadService;
        private readonly IGgumtleService _ggumtleService;
        private readonly bool _enableDebugLogs = true; // 스폰 로그 활성화 - 디버깅용
        // preplaced 방식으로 변경됨 - 더이상 스폰 방식 사용하지 않음

        // 설정할 데이터
        private RoomData _roomData;

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
                    Debug.Log($"  - 상자 ID: {chest.Id}, 위치: ({chest.X}, {chest.Y}, {chest.Z})");
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
                    Debug.Log("[MapSpawnService] 모든 오브젝트 스폰 시작 (EntryPoint에서 호출)");

                // 미리 배치된 오브젝트들 설정
                if (_roomData.Chests != null && _roomData.Chests.Count > 0)
                    SetupPreplacedChests(_roomData.Chests);

                if (_roomData.Ggumtles != null && _roomData.Ggumtles.Count > 0)
                    SetupPreplacedGgumtles(_roomData.Ggumtles);

                if (_roomData.HealPacks != null && _roomData.HealPacks.Count > 0)
                    SetupPreplacedHealPacks(_roomData.HealPacks);

                if (_roomData.SpeedPacks != null && _roomData.SpeedPacks.Count > 0)
                    SetupPreplacedSpeedPacks(_roomData.SpeedPacks);

                if (_enableDebugLogs)
                    Debug.Log("[MapSpawnService] 모든 오브젝트 스폰 완료");

                OnAllObjectsSpawned?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSpawnService] 오브젝트 스폰 중 오류: {e.Message}");
            }
        }

        public void SetupPreplacedChests(List<ChestPacket> chests)
        {
            if (chests == null || chests.Count == 0)
                return;

            try
            {
                Debug.Log($"[MapSpawnService] 미리 배치된 상자 설정 시작: {chests.Count}개");

                // Chests 부모 오브젝트 찾기
                var chestsParent = GameObject.Find("Chests");
                if (chestsParent == null)
                {
                    Debug.LogWarning("[MapSpawnService] 'Chests' 부모 오브젝트를 찾을 수 없음");
                    return;
                }

                // Chests 하위의 ChestGameObject들만 찾기
                var preplacedChests = chestsParent.GetComponentsInChildren<ChestGameObject>(true)
                    .OrderBy(c => ExtractIndexFromUnityName(c.name))
                    .ToList();

                if (preplacedChests.Count == 0)
                {
                    Debug.LogWarning("[MapSpawnService] Chests 하위에 ChestGameObject를 찾을 수 없음");
                    return;
                }

                // 필요한 수만큼 상자 활성화 및 설정
                for (int i = 0; i < chests.Count && i < preplacedChests.Count; i++)
                {
                    var chestData = chests[i];
                    var chestObject = preplacedChests[i];

                    // 위치 설정
                    var position = chestData.ToVector3();
                    chestObject.transform.position = position;

                    // ID 설정 (리플렉션 사용)
                    SetChestId(chestObject, chestData.Id.ToString());

                    // 활성화 (VContainer가 자동으로 의존성 주입)
                    chestObject.gameObject.SetActive(true);

                    Debug.Log($"[MapSpawnService] 상자 설정 완료: ID={chestData.Id}, Position={position}");
                }

                // 사용하지 않는 상자들은 비활성화 유지
                for (int i = chests.Count; i < preplacedChests.Count; i++)
                {
                    preplacedChests[i].gameObject.SetActive(false);
                }

                Debug.Log($"[MapSpawnService] 미리 배치된 상자 설정 완료: {Math.Min(chests.Count, preplacedChests.Count)}개 활성화");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 미리 배치된 상자 설정 실패: {e.Message}");
            }
        }

        public void SetupPreplacedGgumtles(List<GgumtlePacket> ggumtles)
        {
            if (ggumtles == null || ggumtles.Count == 0)
                return;

            try
            {
                Debug.Log($"[MapSpawnService] 미리 배치된 꿈틀이 설정 시작: {ggumtles.Count}개");

                // Ggumtles 부모 오브젝트 찾기
                var ggumtlesParent = GameObject.Find("Ggumtles");
                if (ggumtlesParent == null)
                {
                    Debug.LogWarning("[MapSpawnService] 'Ggumtles' 부모 오브젝트를 찾을 수 없음");
                    return;
                }

                // Ggumtles 하위의 GgumtleGameObject들만 찾기
                var preplacedGgumtles = ggumtlesParent.GetComponentsInChildren<Features.Ggumtle.Views.GgumtleGameObject>(true)
                    .OrderBy(g => ExtractIndexFromUnityName(g.name))
                    .ToList();

                if (preplacedGgumtles.Count == 0)
                {
                    Debug.LogWarning("[MapSpawnService] Ggumtles 하위에 GgumtleGameObject를 찾을 수 없음");
                    return;
                }

                // 필요한 수만큼 꿈틀이 활성화 및 설정
                for (int i = 0; i < ggumtles.Count && i < preplacedGgumtles.Count; i++)
                {
                    var ggumtleData = ggumtles[i];
                    var ggumtleObject = preplacedGgumtles[i];

                    // 위치 설정
                    var position = ggumtleData.ToVector3();
                    ggumtleObject.transform.position = position;

                    // ID 설정 (리플렉션 사용)
                    SetGgumtleId(ggumtleObject, ggumtleData.Id);

                    // 활성화 (VContainer가 자동으로 의존성 주입)
                    ggumtleObject.gameObject.SetActive(true);

                    Debug.Log($"[MapSpawnService] 꿈틀이 설정 완료: ID={ggumtleData.Id}, Position={position}");
                }

                // 사용하지 않는 꿈틀이들은 비활성화 유지
                for (int i = ggumtles.Count; i < preplacedGgumtles.Count; i++)
                {
                    preplacedGgumtles[i].gameObject.SetActive(false);
                }

                Debug.Log($"[MapSpawnService] 미리 배치된 꿈틀이 설정 완료: {Math.Min(ggumtles.Count, preplacedGgumtles.Count)}개 활성화");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 미리 배치된 꿈틀이 설정 실패: {e.Message}");
            }
        }

        public void SetupPreplacedHealPacks(List<HealPackPacket> healPacks)
        {
            SetupPreplacedFieldItems(healPacks?.ConvertAll(h => new { Id = h.Id, Position = h.ToVector3() }),
                                    FieldItemType.HealPack, "HealPack");
        }

        public void SetupPreplacedSpeedPacks(List<SpeedPackPacket> speedPacks)
        {
            SetupPreplacedFieldItems(speedPacks?.ConvertAll(s => new { Id = s.Id, Position = s.ToVector3() }),
                                    FieldItemType.SpeedPack, "SpeedPack");
        }

        private void SetupPreplacedFieldItems<T>(List<T> items, FieldItemType itemType, string itemTypeName) where T : class
        {
            if (items == null || items.Count == 0)
                return;

            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 미리 배치된 {itemTypeName} 설정 시작: {items.Count}개");

                // FieldItems 부모 오브젝트 찾기
                var fieldItemsParent = GameObject.Find("FieldItems");
                if (fieldItemsParent == null)
                {
                    Debug.LogWarning("[MapSpawnService] 'FieldItems' 부모 오브젝트를 찾을 수 없음");
                    return;
                }

                // FieldItems 하위의 FieldItemGameObject들만 찾기
                var preplacedItems = fieldItemsParent.GetComponentsInChildren<FieldItemGameObject>(true)
                    .OrderBy(f => ExtractIndexFromUnityName(f.name))
                    .ToList();

                if (preplacedItems.Count == 0)
                {
                    Debug.LogWarning($"[MapSpawnService] FieldItems 하위에 FieldItemGameObject를 찾을 수 없음");
                    return;
                }

                // 필요한 수만큼 아이템 활성화 및 설정
                for (int i = 0; i < items.Count && i < preplacedItems.Count; i++)
                {
                    var itemData = items[i];
                    var fieldItemObject = preplacedItems[i];

                    // 리플렉션을 사용해서 Id와 Position 가져오기
                    var idProperty = itemData.GetType().GetProperty("Id");
                    var positionProperty = itemData.GetType().GetProperty("Position");

                    if (idProperty != null && positionProperty != null)
                    {
                        var id = (int)idProperty.GetValue(itemData);
                        var position = (Vector3)positionProperty.GetValue(itemData);

                        // 위치 설정
                        fieldItemObject.transform.position = position;

                        // ID 및 타입 설정
                        fieldItemObject.SetId(id);
                        fieldItemObject.SetType(itemType);

                        // 활성화
                        fieldItemObject.gameObject.SetActive(true);

                        if (_enableDebugLogs)
                            Debug.Log($"[MapSpawnService] {itemTypeName} 설정 완료: ID={id}, Position={position}");
                    }
                }

                // 사용하지 않는 아이템들은 비활성화 유지
                for (int i = items.Count; i < preplacedItems.Count; i++)
                {
                    preplacedItems[i].gameObject.SetActive(false);
                }

                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 미리 배치된 {itemTypeName} 설정 완료: {Math.Min(items.Count, preplacedItems.Count)}개 활성화");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 미리 배치된 {itemTypeName} 설정 실패: {e.Message}");
            }
        }

        // SpawnGgumtlesAsync 메서드는 preplaced 방식으로 대체됨 - SetupPreplacedGgumtles 사용

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
                Debug.LogWarning($"[MapSpawnService] 아이템 스폰 실패: {e.Message} - 다른 오브젝트 스폰은 계속 진행");
                // throw를 제거하여 전체 프로세스가 중단되지 않도록 함
            }
        }

        // SpawnGgumtleAsync 메서드는 preplaced 방식으로 대체됨 - SetupPreplacedGgumtles에서 처리

        public bool RemoveGgumtle(int id)
        {
            try
            {
                // Preplaced 방식에서는 비활성화만 함
                var ggumtlesParent = GameObject.Find("Ggumtles");
                if (ggumtlesParent != null)
                {
                    var ggumtleObject = ggumtlesParent.GetComponentsInChildren<Features.Ggumtle.Views.GgumtleGameObject>(true)
                        .FirstOrDefault(g => GetGgumtleId(g) == id);

                    if (ggumtleObject != null)
                    {
                        // GgumtleService에서 해제
                        _ggumtleService.UnregisterGgumtle(id);

                        // 오브젝트 비활성화
                        ggumtleObject.gameObject.SetActive(false);

                        OnObjectRemoved?.Invoke("Ggumtle", ggumtleObject.gameObject);

                        if (_enableDebugLogs)
                            Debug.Log($"[MapSpawnService] 꿈틀이 제거 완료: ID={id}");

                        return true;
                    }
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
            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[MapSpawnService] 모든 오브젝트 정리 시작 (preplaced 방식)");

                int totalDeactivated = 0;

                // Preplaced 방식에서는 모든 오브젝트를 비활성화
                var chestsParent = GameObject.Find("Chests");
                if (chestsParent != null)
                {
                    var chests = chestsParent.GetComponentsInChildren<ChestGameObject>(true);
                    foreach (var chest in chests)
                    {
                        chest.gameObject.SetActive(false);
                        totalDeactivated++;
                    }
                }

                var ggumtlesParent = GameObject.Find("Ggumtles");
                if (ggumtlesParent != null)
                {
                    var ggumtles = ggumtlesParent.GetComponentsInChildren<Features.Ggumtle.Views.GgumtleGameObject>(true);
                    foreach (var ggumtle in ggumtles)
                    {
                        ggumtle.gameObject.SetActive(false);
                        totalDeactivated++;
                    }
                }

                var fieldItemsParent = GameObject.Find("FieldItems");
                if (fieldItemsParent != null)
                {
                    var fieldItems = fieldItemsParent.GetComponentsInChildren<FieldItemGameObject>(true);
                    foreach (var fieldItem in fieldItems)
                    {
                        fieldItem.gameObject.SetActive(false);
                        totalDeactivated++;
                    }
                }

                if (_enableDebugLogs)
                    Debug.Log($"[MapSpawnService] 모든 오브젝트 정리 완료: {totalDeactivated}개 비활성화");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] 오브젝트 정리 중 오류: {e.Message}");
            }
        }

        public int GetSpawnedObjectCount(string objectType)
        {
            // Preplaced 방식에서는 활성화된 오브젝트 개수 반환
            switch (objectType)
            {
                case "Ggumtle":
                    var ggumtlesParent = GameObject.Find("Ggumtles");
                    if (ggumtlesParent != null)
                    {
                        return ggumtlesParent.GetComponentsInChildren<Features.Ggumtle.Views.GgumtleGameObject>(false).Length;
                    }
                    break;
                case "Chest":
                    var chestsParent = GameObject.Find("Chests");
                    if (chestsParent != null)
                    {
                        return chestsParent.GetComponentsInChildren<ChestGameObject>(false).Length;
                    }
                    break;
                case "HealPack":
                case "SpeedPack":
                    var fieldItemsParent = GameObject.Find("FieldItems");
                    if (fieldItemsParent != null)
                    {
                        return fieldItemsParent.GetComponentsInChildren<FieldItemGameObject>(false).Length;
                    }
                    break;
            }

            return 0;
        }

        private void SetChestId(ChestGameObject chestObject, string id)
        {
            try
            {
                var chestIdField = typeof(ChestGameObject).GetField("chestId",
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

        private int GetGgumtleId(Features.Ggumtle.Views.GgumtleGameObject ggumtleObject)
        {
            try
            {
                var ggumtleIdField = typeof(Features.Ggumtle.Views.GgumtleGameObject).GetField("ggumtleId",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (ggumtleIdField != null)
                {
                    return (int)ggumtleIdField.GetValue(ggumtleObject);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapSpawnService] GgumtleId 가져오기 실패: {e.Message}");
            }
            return -1;
        }

        private int ExtractIndexFromUnityName(string name)
        {
            // "ChestGameObject" -> 0 (첫 번째)
            // "ChestGameObject(1)" -> 1 (두 번째)
            // "FieldItemGameObject" -> 0 (첫 번째)
            // "FieldItemGameObject(1)" -> 1 (두 번째)
            if (name == "ChestGameObject" || name == "GgumtleGameObject" || name == "FieldItemGameObject")
                return 0;

            var match = System.Text.RegularExpressions.Regex.Match(name, @"\((\d+)\)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
            {
                return index;
            }

            return 0;
        }
    }
}