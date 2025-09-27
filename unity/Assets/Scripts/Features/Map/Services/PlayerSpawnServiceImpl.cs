using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.Map.Utils;
using Features.Player.Services;
using Features.Player.Views;
using Features.Room.Models;
using Networks.Rooms.Domains;
using Player;
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
        private readonly PlayerManagerService _playerManagerService;
        private readonly bool _enableDebugLogs = true;

        // 설정할 데이터
        private RoomData _roomData;

        // 동적 생성된 플레이어들 관리
        private readonly List<GameObject> _spawnedPlayers = new();
        private readonly Dictionary<long, GameObject> _playerIdToGameObject = new();

        // Addressable 키 상수 (8개 프리팹)
        private const string LOCAL_MONGGING_TANKER_KEY = "Local_Mongging_Tanker";
        private const string LOCAL_MONGGING_HEALER_KEY = "Local_Mongging_Healer";
        private const string LOCAL_MONGGING_WORKER_KEY = "Local_Mongging_Worker";
        private const string LOCAL_MONGDUNG_KEY = "Local_Mongdung";
        private const string REMOTE_MONGGING_TANKER_KEY = "Remote_Mongging_Tanker";
        private const string REMOTE_MONGGING_HEALER_KEY = "Remote_Mongging_Healer";
        private const string REMOTE_MONGGING_WORKER_KEY = "Remote_Mongging_Worker";
        private const string REMOTE_MONGDUNG_KEY = "Remote_Mongdung";

        public event Action<string, GameObject> OnPlayerSpawned;
        public event Action<string, GameObject> OnPlayerRemoved;
        public event Action OnAllPlayersSpawned;

        [Inject]
        public PlayerSpawnServiceImpl(
            IAddressableLoadService addressableLoadService,
            PlayerManagerService playerManagerService
        )
        {
            _addressableLoadService =
                addressableLoadService
                ?? throw new ArgumentNullException(nameof(addressableLoadService));
            _playerManagerService =
                playerManagerService
                ?? throw new ArgumentNullException(nameof(playerManagerService));

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
                    Debug.Log($"[SERVER_PLAYER_DATA] 플레이어 상세 정보:");
                    foreach (var player in _roomData.Players)
                    {
                        string playerType = player.IsMongging
                            ? $"Mongging(Class:{player.ClassId})"
                            : "Mongdung";
                        Debug.Log($"[SERVER_PLAYER_DATA]   - ID: {player.Id}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     타입: {playerType}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     로컬: {player.IsMine}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     위치: {player.Position}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     닉네임: {player.NickName}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     이동속도: {player.MoveSpeed}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     최대HP: {player.MaxHp}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     힐속도: {player.HealSpeed}");
                        Debug.Log($"[SERVER_PLAYER_DATA]     작업속도: {player.WorkSpeed}");
                        Debug.Log($"[SERVER_PLAYER_DATA]   ==================");
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
                    Debug.Log(
                        $"[PlayerSpawnService] 모든 플레이어 동적 생성 완료 - 총 플레이어:{_spawnedPlayers.Count}개"
                    );

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

                foreach (var player in players)
                {
                    await SpawnSinglePlayerAsync(player, parent);
                }

                if (_enableDebugLogs)
                    Debug.Log(
                        $"[PlayerSpawnService] 플레이어 동적 생성 완료: 총 {_spawnedPlayers.Count}개"
                    );
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerSpawnService] 플레이어 동적 생성 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 단일 플레이어 스폰
        /// </summary>
        private async UniTask SpawnSinglePlayerAsync(PlayerPacket player, Transform parent)
        {
            try
            {
                // 프리팹 키 결정
                string prefabKey = GetPrefabKey(player);

                if (_enableDebugLogs)
                    Debug.Log(
                        $"[PlayerSpawnService] 플레이어 스폰: ID={player.Id}, 키={prefabKey}, 위치={player.ToVector3()}"
                    );

                // Addressable로 프리팹 스폰
                var spawnedObjects =
                    await _addressableLoadService.SpawnMultipleAsync<MonoBehaviour>(
                        prefabKey,
                        new List<Vector3> { player.ToVector3() },
                        parent
                    );

                if (spawnedObjects == null || spawnedObjects.Count == 0)
                {
                    Debug.LogError(
                        $"[PlayerSpawnService] 플레이어 스폰 실패: 키='{prefabKey}' 프리팹을 찾을 수 없음"
                    );
                    return;
                }

                var playerObject = spawnedObjects[0].gameObject;

                // 프리팹 방식: 직접 컴포넌트 초기화
                if (player.IsMine)
                {
                    // 로컬 플레이어: PlayerGameObject 초기화
                    var playerGameObject = playerObject.GetComponent<PlayerGameObject>();
                    if (playerGameObject != null)
                    {
                        // 플레이어 ID 설정
                        playerGameObject.SetPlayerId(player.Id);
                        // 서버 속도를 Unity 속도로 변환
                        playerGameObject.SetServerSpeed(player.MoveSpeed);
                        if (_enableDebugLogs)
                            Debug.Log(
                                $"[PlayerSpawnService] 로컬 PlayerGameObject 초기화: ID={player.Id}, 속도={playerGameObject.MoveSpeed}"
                            );
                    }
                }
                else
                {
                    // 원격 플레이어: RemotePlayerGameObject 초기화
                    var remotePlayerGameObject =
                        playerObject.GetComponent<RemotePlayerGameObject>();
                    if (remotePlayerGameObject != null)
                    {
                        remotePlayerGameObject.InitializeFromPacket(player);
                        if (_enableDebugLogs)
                            Debug.Log(
                                $"[PlayerSpawnService] 원격 PlayerGameObject 초기화 완료: ID={player.Id}"
                            );
                    }
                }

                // 레이어 설정 (몽깅이/몽둥이 구분)
                SetPlayerLayer(playerObject, player.IsMongging);

                // GameObject 이름 설정
                string playerType = GetPlayerTypeString(player);
                string localPrefix = player.IsMine ? "Local" : "Remote";
                playerObject.name = $"{localPrefix}_{playerType}_{player.Id}";

                // 관리 리스트에 추가
                _spawnedPlayers.Add(playerObject);
                _playerIdToGameObject[player.Id] = playerObject;

                // PlayerManagerService에 등록
                _playerManagerService.RegisterPlayer(player, playerObject);

                // 이벤트 발생 (playerType 재사용)
                OnPlayerSpawned?.Invoke(playerType, playerObject);

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[PlayerSpawnService] 플레이어 생성 완료: ID={player.Id}, 타입={playerType}, 로컬={player.IsMine}"
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[PlayerSpawnService] 단일 플레이어 스폰 실패: ID={player.Id}, {e.Message}"
                );
            }
        }

        /// <summary>
        /// PlayerPacket 정보로 Addressable 키 결정
        /// </summary>
        private string GetPrefabKey(PlayerPacket player)
        {
            string localPrefix = player.IsMine ? "Local" : "Remote";

            if (player.IsMongging)
            {
                // 몽깅이 - 클래스별 구분 (서버에서 0부터 시작)
                return player.ClassId switch
                {
                    0 => player.IsMine ? LOCAL_MONGGING_TANKER_KEY : REMOTE_MONGGING_TANKER_KEY, // Tanker
                    1 => player.IsMine ? LOCAL_MONGGING_HEALER_KEY : REMOTE_MONGGING_HEALER_KEY, // Healer
                    2 => player.IsMine ? LOCAL_MONGGING_WORKER_KEY : REMOTE_MONGGING_WORKER_KEY, // Worker
                    _ => player.IsMine ? LOCAL_MONGGING_TANKER_KEY : REMOTE_MONGGING_TANKER_KEY, // 기본값
                };
            }
            else
            {
                // 몽둥이
                return player.IsMine ? LOCAL_MONGDUNG_KEY : REMOTE_MONGDUNG_KEY;
            }
        }

        /// <summary>
        /// 플레이어 타입 문자열 반환
        /// </summary>
        private string GetPlayerTypeString(PlayerPacket player)
        {
            if (!player.IsMongging)
                return "Mongdung";

            string result = player.ClassId switch
            {
                0 => "Mongging_Tanker", // 서버에서 0부터 시작
                1 => "Mongging_Healer",
                2 => "Mongging_Worker",
                _ => "Mongging_Unknown",
            };

            // Unknown인 경우 디버깅 로그 출력
            if (result == "Mongging_Unknown")
            {
                Debug.LogWarning(
                    $"[PlayerSpawnService] Unknown ClassId 발견: ID={player.Id}, ClassId={player.ClassId}, IsMongging={player.IsMongging}"
                );
            }

            return result;
        }

        /// <summary>
        /// 플레이어 레이어 설정 (몽깅이/몽둥이 구분)
        /// </summary>
        private void SetPlayerLayer(GameObject playerObject, bool isMongging)
        {
            try
            {
                // 레이어 이름 결정
                string layerName = isMongging ? "Player_Mongging" : "Player_Mongdung";
                int layerIndex = LayerMask.NameToLayer(layerName);

                if (layerIndex == -1)
                {
                    Debug.LogWarning(
                        $"[PlayerSpawnService] 레이어 '{layerName}'를 찾을 수 없음. 기본 Player 레이어 사용"
                    );
                    layerIndex = LayerMask.NameToLayer("Player");
                }

                // 해당 GameObject와 모든 자식에 레이어 적용
                SetLayerRecursively(playerObject, layerIndex);

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[PlayerSpawnService] 레이어 설정 완료: {playerObject.name} → {layerName} (Index: {layerIndex})"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"[PlayerSpawnService] 레이어 설정 실패: {playerObject.name}, {e.Message}"
                );
            }
        }

        /// <summary>
        /// GameObject와 모든 자식에 레이어를 재귀적으로 설정
        /// 단, Interaction 레이어를 유지해야 하는 특정 GameObject는 제외
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null)
                return;

            // FaintedPlayerDetector는 Interaction 레이어(7)를 유지해야 함
            if (obj.name == "FaintedMonggingInteractable")
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[PlayerSpawnService] {obj.name}은 Interaction 레이어 유지: Layer {obj.layer}"
                    );
                }
                return; // 이 GameObject와 그 자식들은 레이어 변경하지 않음
            }

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
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
                    // PlayerManagerService에서 제거
                    _playerManagerService.RemovePlayer(playerId);

                    // 리스트에서 제거
                    _spawnedPlayers.Remove(playerObject);
                    _playerIdToGameObject.Remove(playerId);

                    // Addressable 해제
                    _addressableLoadService.ReleaseInstance(playerObject);

                    string playerType = playerObject.name.Contains("Mongging")
                        ? "Mongging"
                        : "Mongdung";
                    OnPlayerRemoved?.Invoke(playerType, playerObject);

                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSpawnService] 플레이어 제거 완료: ID={playerId}");

                    return true;
                }

                Debug.LogWarning(
                    $"[PlayerSpawnService] 제거할 플레이어를 찾을 수 없음: ID={playerId}"
                );
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[PlayerSpawnService] 플레이어 제거 실패: ID={playerId}, {e.Message}"
                );
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

                // 모든 플레이어들 해제
                foreach (var player in _spawnedPlayers)
                {
                    if (player != null)
                    {
                        _addressableLoadService.ReleaseInstance(player);
                        totalReleased++;
                    }
                }
                _spawnedPlayers.Clear();

                // ID 매핑 정리
                _playerIdToGameObject.Clear();

                if (_enableDebugLogs)
                    Debug.Log(
                        $"[PlayerSpawnService] 기존 플레이어들 정리 완료: {totalReleased}개 해제"
                    );

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
            // PlayerManagerService를 통해 로컬 플레이어 찾기
            var localPlayerInfo = _playerManagerService.GetLocalPlayer();
            return localPlayerInfo?.GameObject;
        }

        public GameObject GetPlayer(long playerId)
        {
            return _playerIdToGameObject.TryGetValue(playerId, out var playerObject)
                ? playerObject
                : null;
        }

        public int GetSpawnedPlayerCount(string playerType)
        {
            return playerType switch
            {
                "Mongging" => _spawnedPlayers.Count(p => p != null && p.name.Contains("Mongging")),
                "Mongdung" => _spawnedPlayers.Count(p => p != null && p.name.Contains("Mongdung")),
                _ => _spawnedPlayers.Count(p => p != null),
            };
        }
    }
}
