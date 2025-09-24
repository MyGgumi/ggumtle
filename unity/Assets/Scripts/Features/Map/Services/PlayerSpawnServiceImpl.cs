using Cysharp.Threading.Tasks;
using Features.Room.Models;
using Features.Map.Utils;
using Features.Player.Views;
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
    /// 플레이어 스폰 서비스 구현체
    /// Addressables를 통해 플레이어 프리팹을 동적 생성하고 VContainer 의존성 주입
    /// </summary>
    public class PlayerSpawnServiceImpl : IPlayerSpawnService
    {
        private readonly IAddressableLoadService _addressableLoadService;
        private readonly bool _enableDebugLogs = true;

        // 설정할 데이터
        private RoomData _roomData;

        // 동적 생성된 플레이어들 관리
        private readonly List<PlayerGameObject> _spawnedMonggings = new();
        private readonly List<PlayerGameObject> _spawnedMongdungs = new();
        private readonly Dictionary<long, PlayerGameObject> _playerIdToGameObject = new();

        // Addressable 키 상수
        private const string MONGGING_PREFAB_KEY = "Mongging";
        private const string MONGDUNG_PREFAB_KEY = "Mongdung";

        public event Action<string, GameObject> OnPlayerSpawned;
        public event Action<string, GameObject> OnPlayerRemoved;
        public event Action OnAllPlayersSpawned;

        [Inject]
        public PlayerSpawnServiceImpl(IAddressableLoadService addressableLoadService)
        {
            _addressableLoadService = addressableLoadService ?? throw new ArgumentNullException(nameof(addressableLoadService));

            if (_enableDebugLogs)
                Debug.Log("[PlayerSpawnService] 초기화 완료");
        }

        public void PrepareSpawnData(RoomData roomData)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerSpawnService] ===== PrepareSpawnData 메서드 시작 =====");
                Debug.Log($"[PlayerSpawnService] roomData 매개변수: {roomData != null}");
            }

            _roomData = roomData ?? throw new ArgumentNullException(nameof(roomData));

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerSpawnService] 스폰 데이터 준비 완료:");
                Debug.Log($"  - 플레이어: {_roomData.Players?.Count ?? 0}명");

                if (_roomData.Players != null && _roomData.Players.Count > 0)
                {
                    Debug.Log($"[PlayerSpawnService] 플레이어 상세 정보:");
                    foreach (var player in _roomData.Players)
                    {
                        Debug.Log($"  - 플레이어 ID: {player.Id}, 타입: {(player.IsMongging ? "Mongging" : "Mongdung")}, 로컬: {player.IsMine}, 위치: {player.Position}");
                    }
                }
            }
        }

        public async UniTask SpawnAllPlayersAsync()
        {
            if (_roomData == null)
            {
                Debug.LogError("[PlayerSpawnService] 방 데이터가 준비되지 않음");
                return;
            }

            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[PlayerSpawnService] 모든 플레이어 동적 생성 시작");

                // 기존 플레이어들 정리
                await ClearAllPlayersAsync();

                // 부모 오브젝트 준비
                var playersParent = GetOrCreateParent("Players");

                // 플레이어 스폰
                if (_roomData.Players != null && _roomData.Players.Count > 0)
                    await SpawnPlayersAsync(_roomData.Players, playersParent);

                if (_enableDebugLogs)
                    Debug.Log($"[PlayerSpawnService] 모든 플레이어 동적 생성 완료 - Mongging:{_spawnedMonggings.Count}, Mongdung:{_spawnedMongdungs.Count}");

                OnAllPlayersSpawned?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSpawnService] 플레이어 동적 생성 중 오류: {e.Message}");
            }
        }

        public async UniTask SpawnPlayersAsync(List<PlayerPacket> players)
        {
            var playersParent = GetOrCreateParent("Players");
            await SpawnPlayersAsync(players, playersParent);
        }

        private async UniTask SpawnPlayersAsync(List<PlayerPacket> players, Transform parent)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[PlayerSpawnService] 플레이어 동적 생성 시작: {players.Count}개");

                // Mongging과 Mongdung을 분리하여 처리
                var monggingPlayers = players.Where(p => p.IsMongging).ToList();
                var mongdungPlayers = players.Where(p => !p.IsMongging).ToList();

                // Mongging 플레이어들 스폰
                if (monggingPlayers.Count > 0)
                {
                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSpawnService] Mongging 스폰 시작: {monggingPlayers.Count}개, Addressable Key: '{MONGGING_PREFAB_KEY}'");

                    var positions = monggingPlayers.Select(p => p.ToVector3()).ToList();

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[PlayerSpawnService] Mongging 위치 정보:");
                        for (int i = 0; i < positions.Count; i++)
                        {
                            Debug.Log($"  - [{i}] {positions[i]}");
                        }
                    }

                    var spawnedMonggings = await _addressableLoadService.SpawnMultipleAsync<PlayerGameObject>(
                        MONGGING_PREFAB_KEY, positions, parent);

                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSpawnService] Mongging Addressable 스폰 결과: {spawnedMonggings?.Count ?? 0}개");

                    for (int i = 0; i < spawnedMonggings.Count && i < monggingPlayers.Count; i++)
                    {
                        var playerData = monggingPlayers[i];
                        var playerObject = spawnedMonggings[i];

                        // 플레이어 ID 설정 및 관리 리스트에 추가
                        SetupPlayerObject(playerObject, playerData);
                        _spawnedMonggings.Add(playerObject);
                        _playerIdToGameObject[playerData.Id] = playerObject;

                        // 이벤트 발생
                        OnPlayerSpawned?.Invoke("Mongging", playerObject.gameObject);

                        if (_enableDebugLogs)
                            Debug.Log($"[PlayerSpawnService] Mongging 생성 완료: ID={playerData.Id}, Position={playerData.ToVector3()}, 로컬={playerData.IsMine}");
                    }
                }

                // Mongdung 플레이어들 스폰
                if (mongdungPlayers.Count > 0)
                {
                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSpawnService] Mongdung 스폰 시작: {mongdungPlayers.Count}개, Addressable Key: '{MONGDUNG_PREFAB_KEY}'");

                    var positions = mongdungPlayers.Select(p => p.ToVector3()).ToList();

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[PlayerSpawnService] Mongdung 위치 정보:");
                        for (int i = 0; i < positions.Count; i++)
                        {
                            Debug.Log($"  - [{i}] {positions[i]}");
                        }
                    }

                    var spawnedMongdungs = await _addressableLoadService.SpawnMultipleAsync<PlayerGameObject>(
                        MONGDUNG_PREFAB_KEY, positions, parent);

                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSpawnService] Mongdung Addressable 스폰 결과: {spawnedMongdungs?.Count ?? 0}개");

                    for (int i = 0; i < spawnedMongdungs.Count && i < mongdungPlayers.Count; i++)
                    {
                        var playerData = mongdungPlayers[i];
                        var playerObject = spawnedMongdungs[i];

                        // 플레이어 ID 설정 및 관리 리스트에 추가
                        SetupPlayerObject(playerObject, playerData);
                        _spawnedMongdungs.Add(playerObject);
                        _playerIdToGameObject[playerData.Id] = playerObject;

                        // 이벤트 발생
                        OnPlayerSpawned?.Invoke("Mongdung", playerObject.gameObject);

                        if (_enableDebugLogs)
                            Debug.Log($"[PlayerSpawnService] Mongdung 생성 완료: ID={playerData.Id}, Position={playerData.ToVector3()}, 로컬={playerData.IsMine}");
                    }
                }

                if (_enableDebugLogs)
                    Debug.Log($"[PlayerSpawnService] 플레이어 동적 생성 완료: Mongging {_spawnedMonggings.Count}개, Mongdung {_spawnedMongdungs.Count}개");

            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSpawnService] 플레이어 동적 생성 실패: {e.Message}");
            }
        }

        private void SetupPlayerObject(PlayerGameObject playerObject, PlayerPacket playerData)
        {
            // 플레이어 GameObject에 ID와 기타 정보 설정
            playerObject.name = $"Player_{playerData.Id}_{(playerData.IsMongging ? "Mongging" : "Mongdung")}";

            // 로컬 플레이어인 경우 카메라 설정
            if (playerData.IsMine)
            {
                SetupLocalPlayerCamera(playerObject);
            }
        }

        private void SetupLocalPlayerCamera(PlayerGameObject playerObject)
        {
            // SkyboxTransitionManager를 통해 카메라를 플레이어 뷰로 전환
            var skyboxManager = Features.Game.Services.SkyboxTransitionManager.Instance;
            if (skyboxManager != null)
            {
                skyboxManager.SetPlayerViewMode();
                if (_enableDebugLogs)
                    Debug.Log($"[PlayerSpawnService] 로컬 플레이어 카메라 설정 완료: {playerObject.name}");
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnService] SkyboxTransitionManager를 찾을 수 없습니다");
            }
        }

        private Transform GetOrCreateParent(string parentName)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PlayerSpawnService] 부모 오브젝트 찾기 시도: {parentName}");

            var parent = GameObject.Find(parentName);
            if (parent == null)
            {
                parent = new GameObject(parentName);
                if (_enableDebugLogs)
                    Debug.Log($"[PlayerSpawnService] 부모 오브젝트 생성 완료: {parentName}");
            }
            else
            {
                if (_enableDebugLogs)
                    Debug.Log($"[PlayerSpawnService] 기존 부모 오브젝트 사용: {parentName}");
            }

            if (_enableDebugLogs)
                Debug.Log($"[PlayerSpawnService] 부모 오브젝트 Transform 반환: {parent.transform}");

            return parent.transform;
        }

        public bool RemovePlayer(long playerId)
        {
            try
            {
                if (_playerIdToGameObject.TryGetValue(playerId, out var playerObject))
                {
                    // 리스트에서 제거
                    _spawnedMonggings.Remove(playerObject);
                    _spawnedMongdungs.Remove(playerObject);
                    _playerIdToGameObject.Remove(playerId);

                    // Addressable 해제
                    _addressableLoadService.ReleaseInstance(playerObject.gameObject);

                    string playerType = playerObject.name.Contains("Mongging") ? "Mongging" : "Mongdung";
                    OnPlayerRemoved?.Invoke(playerType, playerObject.gameObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSpawnService] 플레이어 제거 완료: ID={playerId}");

                    return true;
                }

                Debug.LogWarning($"[PlayerSpawnService] 제거할 플레이어를 찾을 수 없음: ID={playerId}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSpawnService] 플레이어 제거 실패: ID={playerId}, {e.Message}");
                return false;
            }
        }

        public void ClearAllPlayers()
        {
            // 비동기 메서드 호출
            _ = ClearAllPlayersAsync();
        }

        private async UniTask ClearAllPlayersAsync()
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[PlayerSpawnService] 기존 플레이어들 정리 시작");

                int totalReleased = 0;

                // Mongging 플레이어들 해제
                foreach (var player in _spawnedMonggings)
                {
                    if (player != null && player.gameObject != null)
                    {
                        _addressableLoadService.ReleaseInstance(player.gameObject);
                        totalReleased++;
                    }
                }
                _spawnedMonggings.Clear();

                // Mongdung 플레이어들 해제
                foreach (var player in _spawnedMongdungs)
                {
                    if (player != null && player.gameObject != null)
                    {
                        _addressableLoadService.ReleaseInstance(player.gameObject);
                        totalReleased++;
                    }
                }
                _spawnedMongdungs.Clear();

                // ID 매핑 정리
                _playerIdToGameObject.Clear();

                if (_enableDebugLogs)
                    Debug.Log($"[PlayerSpawnService] 기존 플레이어들 정리 완료: {totalReleased}개 해제");

                // 한 프레임 대기 (정리 완료 보장)
                await UniTask.Yield();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSpawnService] 플레이어 정리 중 오류: {e.Message}");
            }
        }

        public GameObject GetLocalPlayer()
        {
            var localPlayer = _playerIdToGameObject.Values.FirstOrDefault(p =>
            {
                // PlayerGameObject에서 로컬 플레이어 확인하는 방법이 필요
                // 현재는 이름으로 확인 (실제로는 PlayerPacket의 IsMine 정보를 저장해야 함)
                return p.name.EndsWith("_Mine") || p.gameObject.CompareTag("LocalPlayer");
            });

            return localPlayer?.gameObject;
        }

        public GameObject GetPlayer(long playerId)
        {
            return _playerIdToGameObject.TryGetValue(playerId, out var playerObject)
                ? playerObject.gameObject
                : null;
        }

        public int GetSpawnedPlayerCount(string playerType)
        {
            return playerType switch
            {
                "Mongging" => _spawnedMonggings.Count(p => p != null),
                "Mongdung" => _spawnedMongdungs.Count(p => p != null),
                _ => 0
            };
        }
    }
}