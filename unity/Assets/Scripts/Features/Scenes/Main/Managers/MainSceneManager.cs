using Features.Map.Services;
using Features.Room.Services;
using UnityEngine;
using VContainer;

namespace Features.Scenes.Main.Managers
{
    /// <summary>
    /// 메인 게임 씬을 관리하는 매니저
    /// 게임 플레이 시작과 종료를 처리
    /// </summary>
    public class MainSceneManager : MonoBehaviour
    {
        [Header("게임 설정")]
        [SerializeField] private bool autoSpawnObjects = true;
        [SerializeField] private bool enableDebugLogs = true;

        // 의존성 주입
        private IRoomService _roomService;
        private IMapSpawnService _mapSpawnService;
        private IAddressableLoadService _addressableLoadService;

        [Inject]
        public void Construct(
            IRoomService roomService,
            IMapSpawnService mapSpawnService,
            IAddressableLoadService addressableLoadService)
        {
            _roomService = roomService;
            _mapSpawnService = mapSpawnService;
            _addressableLoadService = addressableLoadService;
        }

        async void Start()
        {
            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 메인 씬 시작");

            // 방 상태 확인
            CheckRoomStatus();

            // 이벤트 구독
            SubscribeToServices();

            // 맵 오브젝트 스폰
            if (autoSpawnObjects)
            {
                await SpawnGameObjects();
            }

            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 메인 씬 초기화 완료");
        }

        void OnDestroy()
        {
            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 메인 씬 종료");

            // 서비스 이벤트 구독 해제
            UnsubscribeFromServices();

            // Addressable 에셋 해제
            CleanupAssets();
        }

        /// <summary>
        /// 방 상태 확인
        /// </summary>
        private void CheckRoomStatus()
        {
            if (_roomService?.CurrentRoom == null)
            {
                Debug.LogError("[MainSceneManager] 방 데이터가 없음");
                return;
            }

            if (!_roomService.IsRoomInitialized())
            {
                Debug.LogError("[MainSceneManager] 방이 완전히 초기화되지 않음");
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log("[MainSceneManager] 방 상태 확인 완료");
                _roomService.CurrentRoom.DebugLogRoomInfo();
            }
        }

        /// <summary>
        /// 서비스 이벤트 구독
        /// </summary>
        private void SubscribeToServices()
        {
            if (_mapSpawnService != null)
            {
                _mapSpawnService.OnObjectSpawned += OnObjectSpawned;
                _mapSpawnService.OnObjectRemoved += OnObjectRemoved;
                _mapSpawnService.OnAllObjectsSpawned += OnAllObjectsSpawned;
            }

            if (_roomService != null)
            {
                // 런타임 이벤트 구독 (추후 필요시)
                // _roomService.OnSomethingHappened += OnSomethingHappened;
            }

            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 서비스 이벤트 구독 완료");
        }

        /// <summary>
        /// 서비스 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromServices()
        {
            if (_mapSpawnService != null)
            {
                _mapSpawnService.OnObjectSpawned -= OnObjectSpawned;
                _mapSpawnService.OnObjectRemoved -= OnObjectRemoved;
                _mapSpawnService.OnAllObjectsSpawned -= OnAllObjectsSpawned;
            }
        }

        /// <summary>
        /// 게임 오브젝트 스폰
        /// </summary>
        private async Cysharp.Threading.Tasks.UniTask SpawnGameObjects()
        {
            if (_roomService?.CurrentRoom == null || _mapSpawnService == null)
            {
                Debug.LogError("[MainSceneManager] 필요한 서비스가 주입되지 않음");
                return;
            }

            try
            {
                if (enableDebugLogs)
                    Debug.Log("[MainSceneManager] 게임 오브젝트 스폰 시작");

                var roomData = _roomService.CurrentRoom;

                // 방 데이터로부터 스폰 데이터 준비
                _mapSpawnService.PrepareSpawnData(roomData);

                // 모든 오브젝트 스폰
                await _mapSpawnService.SpawnAllObjectsAsync();

                if (enableDebugLogs)
                    Debug.Log("[MainSceneManager] 게임 오브젝트 스폰 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainSceneManager] 게임 오브젝트 스폰 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 에셋 정리
        /// </summary>
        private void CleanupAssets()
        {
            try
            {
                // 맵 오브젝트 정리
                _mapSpawnService?.ClearAllObjects();

                // Addressable 에셋 해제
                _addressableLoadService?.ReleaseAssetsWithTag("InGame");

                if (enableDebugLogs)
                    Debug.Log("[MainSceneManager] 에셋 정리 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainSceneManager] 에셋 정리 중 오류: {e.Message}");
            }
        }

        #region 스폰 이벤트 핸들러

        /// <summary>
        /// 오브젝트 스폰 완료 이벤트 핸들러
        /// </summary>
        private void OnObjectSpawned(string objectType, GameObject spawnedObject)
        {
            if (enableDebugLogs)
                Debug.Log($"[MainSceneManager] 오브젝트 스폰: {objectType} - {spawnedObject.name}");
        }

        /// <summary>
        /// 오브젝트 제거 완료 이벤트 핸들러
        /// </summary>
        private void OnObjectRemoved(string objectType, GameObject removedObject)
        {
            if (enableDebugLogs)
                Debug.Log($"[MainSceneManager] 오브젝트 제거: {objectType} - {removedObject.name}");
        }

        /// <summary>
        /// 모든 오브젝트 스폰 완료 이벤트 핸들러
        /// </summary>
        private void OnAllObjectsSpawned()
        {
            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 모든 오브젝트 스폰 완료");

            // 게임 플레이 준비 완료
            // 여기서 추가적인 게임 시작 로직 실행 가능
            StartGameplay();
        }

        /// <summary>
        /// 실제 게임플레이 시작
        /// </summary>
        private void StartGameplay()
        {
            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 게임플레이 시작!");

            // 플레이어 활성화, UI 표시, 게임 타이머 시작 등
            // 필요한 게임 시작 로직들을 여기에 추가
        }

        #endregion

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugSpawnObjects()
        {
            _ = SpawnGameObjects();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugClearObjects()
        {
            _mapSpawnService?.ClearAllObjects();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugRoomInfo()
        {
            _roomService?.CurrentRoom?.DebugLogRoomInfo();
        }

        #endregion
    }
}