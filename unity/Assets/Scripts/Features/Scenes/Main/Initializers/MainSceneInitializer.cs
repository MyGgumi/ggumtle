using Features.Chest.Messages;
using Features.Ggumtle.Messages;
using Features.MainGame.Services;
using Features.MainGame.NetworkSources;
using Features.Map.Services;
using Features.Map.Utils;
using Features.Room.Services;
using MessagePipe;
using Networks;
using Networks.Rooms.Domains;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;

namespace Features.Scenes.Main.Initializers
{
    /// <summary>
    /// Main 씬 초기화 담당 Entry Point
    /// RoomStorage에서 데이터를 확인하고 MapSpawnService를 통해 초기 오브젝트들을 스폰
    /// </summary>
    public class MainSceneInitializer : IStartable
    {
        private readonly IMapSpawnService _mapSpawnService;
        private readonly IPlayerSpawnService _playerSpawnService;
        private readonly IMainGameNetworkSource _mainGameNetworkSource;
        private readonly IPublisher<GgumtleDetectedMessage> _ggumtleDetectedPublisher;
        private readonly IPublisher<GgumtleLeftMessage> _ggumtleLeftPublisher;
        private readonly IPublisher<ChestDetectedMessage> _chestDetectedPublisher;
        private readonly IPublisher<ChestLeftMessage> _chestLeftPublisher;
        private readonly bool _enableDebugLogs = false; // 디버그 로그 비활성화

        [Inject]
        public MainSceneInitializer(
            IMapSpawnService mapSpawnService,
            IPlayerSpawnService playerSpawnService,
            IMainGameNetworkSource mainGameNetworkSource,
            IPublisher<GgumtleDetectedMessage> ggumtleDetectedPublisher,
            IPublisher<GgumtleLeftMessage> ggumtleLeftPublisher,
            IPublisher<ChestDetectedMessage> chestDetectedPublisher,
            IPublisher<ChestLeftMessage> chestLeftPublisher
        )
        {
            _mapSpawnService = mapSpawnService;
            _playerSpawnService = playerSpawnService;
            _mainGameNetworkSource = mainGameNetworkSource;
            _ggumtleDetectedPublisher = ggumtleDetectedPublisher;
            _ggumtleLeftPublisher = ggumtleLeftPublisher;
            _chestDetectedPublisher = chestDetectedPublisher;
            _chestLeftPublisher = chestLeftPublisher;

            if (_enableDebugLogs)
                Debug.Log("[MainSceneInitializer] 의존성 주입 완료");
        }

        public async void Start()
        {
            if (_enableDebugLogs)
                Debug.Log("[MainSceneInitializer] Main 씬 초기화 시작");

            // 기존 오브젝트 삭제 로직 제거 - Loading에서 스폰된 오브젝트들 유지
            // ClearExistingSpawnedObjects(); // 제거됨

            // RoomStorage에서 데이터 확인
            var room = RoomStorage.Instance.Room;
            if (room == null || !room.IsInitialized())
            {
                Debug.LogWarning(
                    "[MainSceneInitializer] Room 데이터가 없거나 초기화되지 않음 - 빈 맵으로 시작"
                );

                // 데이터가 없어도 Main 씬 시스템들은 초기화해야 함
                InitializeMainSceneSystems();
                return;
            }

            // VContainer EntryPoint에서 호출되므로 항상 MapSpawn 실행
            if (_enableDebugLogs)
                Debug.Log(
                    "[MainSceneInitializer] VContainer EntryPoint에서 호출 - 전체 초기화 진행"
                );

            // RoomData로 변환
            var roomData = RoomDataConverter.ConvertToRoomData(room);
            if (roomData == null || !RoomDataConverter.ValidateRoomData(roomData))
            {
                Debug.LogWarning(
                    "[MainSceneInitializer] RoomData 변환 또는 검증 실패 - 빈 맵으로 시작"
                );
                InitializeMainSceneSystems();
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log(
                    $"[MainSceneInitializer] RoomData 확인 완료: "
                        + $"Chests={roomData.Chests?.Count ?? 0}, "
                        + $"Ggumtles={roomData.Ggumtles?.Count ?? 0}, "
                        + $"HealPacks={roomData.HealPacks?.Count ?? 0}, "
                        + $"SpeedPacks={roomData.SpeedPacks?.Count ?? 0}"
                );
            }

            // MapSpawnService를 통한 오브젝트 스폰 시작
            InitializeMapObjects(roomData);

            // PlayerSpawnService를 통한 플레이어 스폰 시작
            await InitializePlayerObjects(roomData);

            // Main 씬 시스템들도 초기화
            InitializeMainSceneSystems();
        }

        /// <summary>
        /// Loading 씬에서 전환되었는지 확인
        /// </summary>
        private bool WasLoadedFromLoadingScene()
        {
            // Loading 씬에서 스폰된 오브젝트들이 있는지 확인
            // DontDestroyOnLoad 오브젝트들이 있으면 Loading 씬에서 온 것으로 판단
            var spawnedGgumtles =
                GameObject.FindObjectsOfType<Features.Ggumtle.Views.GgumtleGameObject>();
            var spawnedChests =
                GameObject.FindObjectsOfType<Features.Chest.Views.ChestGameObject>();

            bool hasSpawnedObjects = spawnedGgumtles.Length > 0 || spawnedChests.Length > 0;

            if (_enableDebugLogs && hasSpawnedObjects)
            {
                Debug.Log(
                    $"[MainSceneInitializer] Loading에서 스폰된 오브젝트 감지: 꿈틀이={spawnedGgumtles.Length}개, 상자={spawnedChests.Length}개"
                );
            }

            return hasSpawnedObjects;
        }

        /// <summary>
        /// Main 씬의 핵심 시스템들 초기화 (플레이어, UI, 입력 등)
        /// </summary>
        private void InitializeMainSceneSystems()
        {
            if (_enableDebugLogs)
                Debug.Log("[MainSceneInitializer] Main 씬 시스템 초기화 시작");

            try
            {
                // 플레이어 시스템 확인 및 초기화
                InitializePlayerSystems();

                // UI 시스템 확인 및 초기화
                InitializeUISystems();

                // 입력 시스템 확인 및 초기화
                InitializeInputSystems();

                // InteractionTriggerDetector 수동 주입
                InitializeInteractionDetectors();

                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] Main 씬 시스템 초기화 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainSceneInitializer] Main 씬 시스템 초기화 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 시스템 초기화
        /// </summary>
        private void InitializePlayerSystems()
        {
            var playerGameObject =
                GameObject.FindFirstObjectByType<Features.Player.Views.PlayerGameObject>();
            if (playerGameObject != null)
            {
                // 플레이어 이동 허용
                playerGameObject.SetMovementEnabled(true);
                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] 플레이어 시스템 초기화 완료");
            }
            else
            {
                Debug.LogWarning("[MainSceneInitializer] PlayerGameObject를 찾을 수 없음");
            }
        }

        /// <summary>
        /// UI 시스템 초기화
        /// </summary>
        private void InitializeUISystems()
        {
            var hudInitializer =
                GameObject.FindFirstObjectByType<Features.UI.Views.HUDInitializer>();
            if (hudInitializer != null)
            {
                // UI 시스템이 활성화되어 있는지 확인
                if (!hudInitializer.gameObject.activeInHierarchy)
                {
                    hudInitializer.gameObject.SetActive(true);
                }
                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] UI 시스템 초기화 완료");
            }
            else
            {
                Debug.LogWarning("[MainSceneInitializer] UniversalHUDController를 찾을 수 없음");
            }
        }

        /// <summary>
        /// 입력 시스템 초기화
        /// </summary>
        private void InitializeInputSystems()
        {
            var keyboardController =
                GameObject.FindFirstObjectByType<Features.MobileControls.Testing.KeyboardDebugController>();
            if (keyboardController != null)
            {
                // 키보드 입력 시스템이 활성화되어 있는지 확인
                if (!keyboardController.gameObject.activeInHierarchy)
                {
                    keyboardController.gameObject.SetActive(true);
                }
                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] 키보드 입력 시스템 초기화 완료");
            }

            var mobileControlsView =
                GameObject.FindFirstObjectByType<Features.MobileControls.Views.MobileControlsView>();
            if (mobileControlsView != null)
            {
                // 모바일 입력 시스템이 활성화되어 있는지 확인
                if (!mobileControlsView.gameObject.activeInHierarchy)
                {
                    mobileControlsView.gameObject.SetActive(true);
                }
                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] 모바일 입력 시스템 초기화 완료");
            }
        }

        /// <summary>
        /// InteractionTriggerDetector 수동 주입
        /// </summary>
        private void InitializeInteractionDetectors()
        {
            var detectors = GameObject.FindObjectsOfType<Interaction.InteractionTriggerDetector>();
            foreach (var detector in detectors)
            {
                try
                {
                    // 리플렉션으로 Construct 메서드 호출
                    var constructMethod = detector.GetType().GetMethod("Construct");
                    if (constructMethod != null)
                    {
                        constructMethod.Invoke(
                            detector,
                            new object[] {
                                _ggumtleDetectedPublisher,
                                _ggumtleLeftPublisher,
                                _chestDetectedPublisher,
                                _chestLeftPublisher
                            }
                        );
                        Debug.Log(
                            $"[MainSceneInitializer] InteractionTriggerDetector 수동 주입 완료: {detector.gameObject.name}"
                        );
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[MainSceneInitializer] InteractionTriggerDetector 수동 주입 실패: {e.Message}"
                    );
                }
            }
        }


        private async void InitializeMapObjects(Features.Room.Models.RoomData roomData)
        {
            try
            {
                Debug.Log("[MainSceneInitializer] ===== InitializeMapObjects 시작 =====");
                Debug.Log($"[MainSceneInitializer] roomData: {roomData != null}");
                Debug.Log($"[MainSceneInitializer] _mapSpawnService: {_mapSpawnService != null}");

                if (roomData != null)
                {
                    Debug.Log($"[MainSceneInitializer] RoomData 내용:");
                    Debug.Log($"  - 상자: {roomData.Chests?.Count ?? 0}개");
                    Debug.Log($"  - 꿈틀이: {roomData.Ggumtles?.Count ?? 0}개");
                    Debug.Log($"  - 힐팩: {roomData.HealPacks?.Count ?? 0}개");
                    Debug.Log($"  - 스피드팩: {roomData.SpeedPacks?.Count ?? 0}개");
                }

                // 스폰 데이터 준비
                Debug.Log("[MainSceneInitializer] PrepareSpawnData 호출");
                _mapSpawnService.PrepareSpawnData(roomData);

                // 모든 오브젝트 스폰
                Debug.Log("[MainSceneInitializer] SpawnAllObjectsAsync 호출");
                await _mapSpawnService.SpawnAllObjectsAsync();

                Debug.Log("[MainSceneInitializer] 맵 오브젝트 초기화 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainSceneInitializer] 맵 오브젝트 초기화 실패: {e.Message}");
                Debug.LogError($"[MainSceneInitializer] 스택트레이스: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 기존에 씬에 배치된 스폰 오브젝트들 제거
        /// 단, Loading 씬에서 스폰된 오브젝트들과 중요한 시스템 오브젝트들은 보호
        /// </summary>
        private void ClearExistingSpawnedObjects()
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] 기존 스폰된 오브젝트들 정리 시작");

                // Loading 씬에서 온 경우 스폰된 오브젝트들을 보호해야 함
                bool protectSpawnedObjects = WasLoadedFromLoadingScene();

                if (protectSpawnedObjects && _enableDebugLogs)
                    Debug.Log(
                        "[MainSceneInitializer] Loading 씬에서 전환됨 - 스폰된 오브젝트들 보호"
                    );

                // 기존 꿈틀이들 찾아서 제거 (단, Loading에서 스폰된 것들은 보호)
                var existingGgumtles =
                    GameObject.FindObjectsOfType<Features.Ggumtle.Views.GgumtleGameObject>();
                foreach (var ggumtle in existingGgumtles)
                {
                    // 플레이어 자신의 꿈틀이는 제거하지 않음
                    if (ggumtle.name.Contains("Player") || ggumtle.name.Contains("LocalPlayer"))
                    {
                        if (_enableDebugLogs)
                            Debug.Log(
                                $"[MainSceneInitializer] 플레이어 꿈틀이는 유지: {ggumtle.name}"
                            );
                        continue;
                    }

                    // Loading 씬에서 스폰된 꿈틀이들 보호 (이름이 Ggumtle_숫자 형태)
                    if (protectSpawnedObjects && ggumtle.name.StartsWith("Ggumtle_"))
                    {
                        if (_enableDebugLogs)
                            Debug.Log(
                                $"[MainSceneInitializer] Loading에서 스폰된 꿈틀이 보호: {ggumtle.name}"
                            );
                        continue;
                    }

                    if (_enableDebugLogs)
                        Debug.Log($"[MainSceneInitializer] 기존 꿈틀이 제거: {ggumtle.name}");
                    GameObject.Destroy(ggumtle.gameObject);
                }

                // 기존 상자들과 아이템들 찾아서 제거 (단, Loading에서 스폰된 것들은 보호)
                var allGameObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (var obj in allGameObjects)
                {
                    // 씬에 미리 배치된 UI나 시스템 오브젝트가 아닌 스폰된 오브젝트들만 대상
                    if (
                        (
                            obj.name.Contains("Chest")
                            || obj.name.Contains("HealPack")
                            || obj.name.Contains("SpeedPack")
                        )
                        && !obj.name.Contains("UI")
                        && !obj.name.Contains("Canvas")
                        && !obj.name.Contains("System")
                    )
                    {
                        // Loading 씬에서 스폰된 오브젝트들 보호 (이름이 Chest_숫자, HealPack_숫자 등)
                        if (
                            protectSpawnedObjects
                            && (
                                obj.name.StartsWith("Chest_")
                                || obj.name.StartsWith("HealPack_")
                                || obj.name.StartsWith("SpeedPack_")
                            )
                        )
                        {
                            if (_enableDebugLogs)
                                Debug.Log(
                                    $"[MainSceneInitializer] Loading에서 스폰된 오브젝트 보호: {obj.name}"
                                );
                            continue;
                        }

                        if (_enableDebugLogs)
                            Debug.Log($"[MainSceneInitializer] 기존 오브젝트 제거: {obj.name}");
                        GameObject.Destroy(obj);
                    }
                }

                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] 기존 스폰된 오브젝트들 정리 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainSceneInitializer] 기존 오브젝트 정리 중 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 오브젝트들 초기화 및 스폰
        /// </summary>
        private async UniTask InitializePlayerObjects(Features.Room.Models.RoomData roomData)
        {
            try
            {
                Debug.Log("[MainSceneInitializer] ===== InitializePlayerObjects 시작 =====");
                Debug.Log($"[MainSceneInitializer] roomData: {roomData != null}");
                Debug.Log($"[MainSceneInitializer] _playerSpawnService: {_playerSpawnService != null}");
                Debug.Log($"[MainSceneInitializer] _mainGameNetworkSource: {_mainGameNetworkSource != null}");

                if (roomData?.Players == null || roomData.Players.Count == 0)
                {
                    Debug.LogWarning("[MainSceneInitializer] 플레이어 데이터가 없음 - 플레이어 스폰 건너뜀");
                    return;
                }

                // 플레이어 데이터 상세 로그 (서버 데이터 형식으로)
                Debug.Log($"[MainSceneInitializer] 플레이어 데이터 상세:");
                Debug.Log($"  - 총 플레이어: {roomData.Players.Count}명");

                for (int i = 0; i < roomData.Players.Count; i++)
                {
                    var player = roomData.Players[i];
                    var position = player.ToVector3();
                    var playerType = player.IsMongging ? "Mongging" : "Mongdung";
                    var isLocal = player.IsMine ? "true" : "false";

                    Debug.Log($"[INFO] [PLAYER_SPAWN_DATA] [{i}] ID: {player.Id}, Unity Position: ({position.x:F2}, {position.y:F2}, {position.z:F2}), Type: {playerType}, Local: {isLocal}");
                }

                // 스폰 데이터 준비
                Debug.Log("[MainSceneInitializer] PlayerSpawnService PrepareSpawnData 호출");
                _playerSpawnService.PrepareSpawnData(roomData);

                // 모든 플레이어 스폰
                Debug.Log("[MainSceneInitializer] PlayerSpawnService SpawnAllPlayersAsync 호출");
                await _playerSpawnService.SpawnAllPlayersAsync();

                Debug.Log("[MainSceneInitializer] 플레이어 오브젝트 초기화 완료");

                // 서버에 씬 준비 완료 알림
                await NotifyServerSceneReady();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainSceneInitializer] 플레이어 오브젝트 초기화 실패: {e.Message}");
                Debug.LogError($"[MainSceneInitializer] 스택트레이스: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 서버에 씬 준비 완료 알림
        /// </summary>
        private async UniTask NotifyServerSceneReady()
        {
            try
            {
                Debug.Log("[MainSceneInitializer] 서버에 씬 준비 완료 알림 시작");

                if (_mainGameNetworkSource == null)
                {
                    Debug.LogError("[MainSceneInitializer] MainGameNetworkSource가 주입되지 않음");
                    return;
                }

                var result = await _mainGameNetworkSource.NotifySceneReadyAsync();

                Debug.Log($"[MainSceneInitializer] 서버 씬 준비 완료 응답: Success={result.Success}, Result={result.Result}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainSceneInitializer] 서버 씬 준비 완료 알림 실패: {e.Message}");
            }
        }
    }
}
