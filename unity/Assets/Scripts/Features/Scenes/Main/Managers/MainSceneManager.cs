using Features.Map.Services;
using Features.Room.Services;
using Features.Game.Services;
using Networks;
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
        [SerializeField] private bool enableDebugLogs = true;

        // 의존성 주입
        private SkyboxTransitionManager _skyboxManager;

        [Inject]
        public void Construct(SkyboxTransitionManager skyboxManager)
        {
            _skyboxManager = skyboxManager;
        }

        void Start()
        {
            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 메인 씬 시작");

            Debug.Log("[MainSceneManager] SkyboxTransitionManager 직접 접근 시도");
            var skyboxManager = SkyboxTransitionManager.Instance;
            if (skyboxManager != null)
            {
                Debug.Log("[MainSceneManager] SkyboxTransitionManager 카메라 설정만 초기화");
                skyboxManager.SetupCameras();
                // 카메라 모드는 LoadingBackgroundController에서 결정하도록 함
                Debug.Log("[MainSceneManager] 카메라 모드는 로딩 상태에 따라 결정됨");
            }
            else
            {
                Debug.LogError("[MainSceneManager] SkyboxTransitionManager.Instance가 null입니다!");
            }

            // 메인 씬의 기본 스카이박스를 그대로 사용 (별도 설정 불필요)

            // 방 상태 확인
            CheckRoomStatus();

            // 이벤트 구독
            SubscribeToServices();

            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 메인 씬 초기화 완료");
        }

        void OnDestroy()
        {
            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 메인 씬 종료");

            // 서비스 이벤트 구독 해제
            UnsubscribeFromServices();
        }

        /// <summary>
        /// 방 상태 확인
        /// </summary>
        private void CheckRoomStatus()
        {
            var room = RoomStorage.Instance.Room;
            if (room == null)
            {
                Debug.LogWarning("[MainSceneManager] RoomStorage에 방 데이터가 없음 - 아직 초기화 전임");
                return;
            }

            if (!room.IsInitialized())
            {
                Debug.LogWarning("[MainSceneManager] 방이 완전히 초기화되지 않음");
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log("[MainSceneManager] 방 상태 확인 완료");
                Debug.Log($"[MainSceneManager] 플레이어 수: {room.players?.Count ?? 0}명");
            }
        }

        /// <summary>
        /// 서비스 이벤트 구독
        /// </summary>
        private void SubscribeToServices()
        {
            // 현재 구독할 이벤트가 없음 - 플레이어 스폰은 MainSceneInitializer에서 처리

            if (enableDebugLogs)
                Debug.Log("[MainSceneManager] 서비스 이벤트 구독 완료");
        }

        /// <summary>
        /// 서비스 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromServices()
        {
            // 현재 구독 중인 이벤트가 없음
        }


        #region Skybox Setup

        /// <summary>
        /// 게임용 스카이박스 설정 (메인 씬의 기본 스카이박스 사용)
        /// </summary>
        private void SetupGameSkybox()
        {
            if (_skyboxManager != null)
            {
                // 메인 씬의 기본 스카이박스를 게임 스카이박스로 사용
                Material mainSceneSkybox = RenderSettings.skybox;

                _skyboxManager.SetGameSkybox(mainSceneSkybox);

                if (enableDebugLogs)
                    Debug.Log($"[MainSceneManager] 메인 씬 기본 스카이박스를 게임 스카이박스로 설정: {mainSceneSkybox?.name ?? "NULL"}");
            }
        }

        #endregion

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugRoomInfo()
        {
            var room = RoomStorage.Instance.Room;
            if (room != null)
            {
                Debug.Log($"[MainSceneManager] Room Debug - PlayerCount: {room.players?.Count ?? 0}, Initialized: {room.IsInitialized()}");
            }
            else
            {
                Debug.Log("[MainSceneManager] Room Debug - No room data in RoomStorage");
            }
        }

        #endregion
    }
}