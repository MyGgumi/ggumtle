using Features.Map.Services;
using Features.Room.Services;
using Features.Map.Utils;
using Networks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Scenes.Main.Initializers
{
    /// <summary>
    /// Main 씬 초기화 담당 Entry Point
    /// RoomStorage에서 데이터를 확인하고 MapSpawnService를 통해 초기 오브젝트들을 스폰
    /// </summary>
    public class MainSceneInitializer : IStartable
    {
        private readonly IMapSpawnService _mapSpawnService;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public MainSceneInitializer(IMapSpawnService mapSpawnService)
        {
            _mapSpawnService = mapSpawnService;

            if (_enableDebugLogs)
                Debug.Log("[MainSceneInitializer] 의존성 주입 완료");
        }

        public void Start()
        {
            if (_enableDebugLogs)
                Debug.Log("[MainSceneInitializer] Main 씬 초기화 시작");

            // 항상 기존 오브젝트들 정리 (씬에 미리 배치된 것들)
            ClearExistingSpawnedObjects();

            // RoomStorage에서 데이터 확인
            var room = RoomStorage.Instance.Room;
            if (room == null || !room.IsInitialized())
            {
                Debug.LogWarning("[MainSceneInitializer] Room 데이터가 없거나 초기화되지 않음 - 빈 맵으로 시작");

                // 데이터가 없어도 Main 씬 시스템들은 초기화해야 함
                InitializeMainSceneSystems();
                return;
            }

            // Loading 씬에서 온 경우 오브젝트 스폰은 건너뛰고 시스템만 초기화
            if (WasLoadedFromLoadingScene())
            {
                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] Loading 씬에서 전환됨 - 오브젝트는 이미 스폰됨, 시스템만 초기화");

                InitializeMainSceneSystems();
                return;
            }

            // 직접 Main 씬 시작인 경우 - 오브젝트 스폰까지 모두 수행
            if (_enableDebugLogs)
                Debug.Log("[MainSceneInitializer] 직접 Main 씬 시작 - 전체 초기화 진행");

            // RoomData로 변환
            var roomData = RoomDataConverter.ConvertToRoomData(room);
            if (roomData == null || !RoomDataConverter.ValidateRoomData(roomData))
            {
                Debug.LogWarning("[MainSceneInitializer] RoomData 변환 또는 검증 실패 - 빈 맵으로 시작");
                InitializeMainSceneSystems();
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[MainSceneInitializer] RoomData 확인 완료: " +
                         $"Chests={roomData.Chests?.Count ?? 0}, " +
                         $"Ggumtles={roomData.Ggumtles?.Count ?? 0}, " +
                         $"HealPacks={roomData.HealPacks?.Count ?? 0}, " +
                         $"SpeedPacks={roomData.SpeedPacks?.Count ?? 0}");
            }

            // MapSpawnService를 통한 오브젝트 스폰 시작
            InitializeMapObjects(roomData);

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
            var spawnedGgumtles = GameObject.FindObjectsOfType<Features.Ggumtle.Views.GgumtleGameObject>();
            var spawnedChests = GameObject.FindObjectsOfType<InteractableChest>();

            bool hasSpawnedObjects = spawnedGgumtles.Length > 0 || spawnedChests.Length > 0;

            if (_enableDebugLogs && hasSpawnedObjects)
            {
                Debug.Log($"[MainSceneInitializer] Loading에서 스폰된 오브젝트 감지: 꿈틀이={spawnedGgumtles.Length}개, 상자={spawnedChests.Length}개");
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
            var playerGameObject = GameObject.FindFirstObjectByType<Features.Player.Views.PlayerGameObject>();
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
            var hudController = GameObject.FindFirstObjectByType<UniversalHUDController>();
            if (hudController != null)
            {
                // UI 시스템이 활성화되어 있는지 확인
                if (!hudController.gameObject.activeInHierarchy)
                {
                    hudController.gameObject.SetActive(true);
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
            var keyboardController = GameObject.FindFirstObjectByType<Features.MobileControls.Testing.KeyboardDebugController>();
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

            var mobileControlsView = GameObject.FindFirstObjectByType<Features.MobileControls.Views.MobileControlsView>();
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

        private async void InitializeMapObjects(Features.Room.Models.RoomData roomData)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] 맵 오브젝트 초기화 시작");

                // 스폰 데이터 준비
                _mapSpawnService.PrepareSpawnData(roomData);

                // 모든 오브젝트 스폰
                await _mapSpawnService.SpawnAllObjectsAsync();

                if (_enableDebugLogs)
                    Debug.Log("[MainSceneInitializer] 맵 오브젝트 초기화 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainSceneInitializer] 맵 오브젝트 초기화 실패: {e.Message}");
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
                    Debug.Log("[MainSceneInitializer] Loading 씬에서 전환됨 - 스폰된 오브젝트들 보호");

                // 기존 꿈틀이들 찾아서 제거 (단, Loading에서 스폰된 것들은 보호)
                var existingGgumtles = GameObject.FindObjectsOfType<Features.Ggumtle.Views.GgumtleGameObject>();
                foreach (var ggumtle in existingGgumtles)
                {
                    // 플레이어 자신의 꿈틀이는 제거하지 않음
                    if (ggumtle.name.Contains("Player") || ggumtle.name.Contains("LocalPlayer"))
                    {
                        if (_enableDebugLogs)
                            Debug.Log($"[MainSceneInitializer] 플레이어 꿈틀이는 유지: {ggumtle.name}");
                        continue;
                    }

                    // Loading 씬에서 스폰된 꿈틀이들 보호 (이름이 Ggumtle_숫자 형태)
                    if (protectSpawnedObjects && ggumtle.name.StartsWith("Ggumtle_"))
                    {
                        if (_enableDebugLogs)
                            Debug.Log($"[MainSceneInitializer] Loading에서 스폰된 꿈틀이 보호: {ggumtle.name}");
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
                    if ((obj.name.Contains("Chest") || obj.name.Contains("HealPack") || obj.name.Contains("SpeedPack"))
                        && !obj.name.Contains("UI") && !obj.name.Contains("Canvas") && !obj.name.Contains("System"))
                    {
                        // Loading 씬에서 스폰된 오브젝트들 보호 (이름이 Chest_숫자, HealPack_숫자 등)
                        if (protectSpawnedObjects &&
                            (obj.name.StartsWith("Chest_") || obj.name.StartsWith("HealPack_") || obj.name.StartsWith("SpeedPack_")))
                        {
                            if (_enableDebugLogs)
                                Debug.Log($"[MainSceneInitializer] Loading에서 스폰된 오브젝트 보호: {obj.name}");
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
    }
}