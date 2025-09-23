using Cysharp.Threading.Tasks;
using Features.Game.Managers;
using Features.Game.Services;
using Features.Map.Services;
using Features.Map.Utils;
using Features.Room.Services;
using Features.Scenes.Loading.ViewModels;
using Networks;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

namespace Features.Scenes.Loading.Managers
{
    /// <summary>
    /// 로딩 씬을 관리하는 매니저
    /// InteractionUI.uxml을 활용하여 로딩 진행상황을 표시하고 인게임 데이터를 다운로드
    /// </summary>
    public class LoadingSceneManager : MonoBehaviour
    {
        [Header("로딩 설정")]
        [SerializeField]
        private string assetTag = "InGame";

        [SerializeField]
        private float loadingDelay = 2f; // 로딩 시뮬레이션 시간

        [Header("디버그 설정")]
        [SerializeField]
        private bool enableProgressLogs = false; // 진행률 로그 활성화

        [SerializeField]
        private bool enableDetailedLogs = false; // 상세 로그 활성화

        [Header("UI References")]
        [SerializeField]
        private UIDocument uiDocument; // InteractionUI.uxml을 담는 UIDocument

        // UI Elements
        private Label _interactionLabel;
        private VisualElement _progressFill;
        private VisualElement _interactionUI;
        private VisualElement _backgroundPanel;

        // 의존성 주입
        private LoadingViewModel _viewModel;
        private GameManager _gameManager;
        private IAddressableLoadService _addressableLoadService;
        private IRoomService _roomService;
        private readonly CompositeDisposable _disposables = new();

        // 진행률 이벤트
        public static event System.Action<float> OnProgressUpdated;
        public static event System.Action OnMainSceneLoadCompleted;

        [Inject]
        public void Construct(
            LoadingViewModel viewModel,
            GameManager gameManager,
            IAddressableLoadService addressableLoadService,
            IRoomService roomService
        )
        {
            if (enableDetailedLogs)
                Debug.Log("[LoadingSceneManager] Construct 호출됨!");
            _viewModel = viewModel;
            _gameManager = gameManager;
            _addressableLoadService = addressableLoadService;
            _roomService = roomService;
        }

        async void Start()
        {
            if (enableDetailedLogs)
                Debug.Log("[LoadingSceneManager] 로딩 씬 시작");

            // Audio Listener 중복 문제 해결: Loading 씬의 Audio Listener 비활성화
            DisableLoadingSceneAudioListener();

            // UI 초기화
            InitializeUI();

            // 로딩 프로세스 시작
            await StartLoadingProcess();
        }

        void OnDestroy()
        {
            // R3 구독 해제
            _disposables.Dispose();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            if (uiDocument == null)
            {
                Debug.LogError("[LoadingSceneManager] UIDocument가 설정되지 않음");
                return;
            }

            var root = uiDocument.rootVisualElement;

            // 전체 화면 배경 패널 생성
            CreateFullScreenBackground(root);

            _interactionUI = root.Q<VisualElement>("interactionUI");
            _interactionLabel = root.Q<Label>("interactionLabel");
            _progressFill = root.Q<VisualElement>("progressFill");

            if (_interactionUI == null || _interactionLabel == null || _progressFill == null)
            {
                Debug.LogError("[LoadingSceneManager] UI Elements를 찾을 수 없음");
                return;
            }

            // UI 표시
            _interactionUI.style.display = DisplayStyle.Flex;
            UpdateProgress(0f);

            if (enableDetailedLogs)
                Debug.Log("[LoadingSceneManager] UI 초기화 완료");
        }

        /// <summary>
        /// 전체 화면을 덮는 배경 패널 생성
        /// </summary>
        private void CreateFullScreenBackground(VisualElement root)
        {
            _backgroundPanel = new VisualElement();
            _backgroundPanel.name = "loading-background";

            // 전체 화면을 덮도록 설정
            _backgroundPanel.style.position = Position.Absolute;
            _backgroundPanel.style.top = 0;
            _backgroundPanel.style.left = 0;
            _backgroundPanel.style.right = 0;
            _backgroundPanel.style.bottom = 0;
            _backgroundPanel.style.width = Length.Percent(100);
            _backgroundPanel.style.height = Length.Percent(100);

            // 배경색 설정 (어두운 색으로)
           // _backgroundPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // 어두운 회색

            // 루트의 첫 번째 자식으로 추가 (다른 UI들 뒤에)
            root.Insert(0, _backgroundPanel);

            if (enableDetailedLogs)
                Debug.Log("[LoadingSceneManager] 전체 화면 배경 생성 완료");
        }

        /// <summary>
        /// Main 씬의 UI들을 화면에서 숨김 (로직은 계속 작동)
        /// </summary>
        private void DisableMainSceneUI()
        {
            var mainScene = SceneManager.GetSceneByName("Main");
            if (mainScene.isLoaded)
            {
                var rootObjects = mainScene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    // Features.UI.Views.HUDInitializer가 있는 GameObject 찾기
                    var hudInitializer = rootObj.GetComponentInChildren<Features.UI.Views.HUDInitializer>();
                    if (hudInitializer != null)
                    {
                        var uiDocument = hudInitializer.GetComponent<UIDocument>();
                        if (uiDocument != null)
                        {
                            // UI는 작동하되 화면에만 안 보이게 함
                            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
                            if (enableDetailedLogs)
                                Debug.Log($"[LoadingSceneManager] Main 씬 UI 화면에서 숨김: {hudInitializer.name}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Main 씬의 UI들을 화면에 다시 표시
        /// </summary>
        private void EnableMainSceneUI()
        {
            var mainScene = SceneManager.GetSceneByName("Main");
            if (mainScene.isLoaded)
            {
                var rootObjects = mainScene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    // Features.UI.Views.HUDInitializer가 있는 GameObject 찾기
                    var hudInitializer = rootObj.GetComponentInChildren<Features.UI.Views.HUDInitializer>();
                    if (hudInitializer != null)
                    {
                        var uiDocument = hudInitializer.GetComponent<UIDocument>();
                        if (uiDocument != null)
                        {
                            // UI를 화면에 다시 표시
                            uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
                            if (enableDetailedLogs)
                                Debug.Log($"[LoadingSceneManager] Main 씬 UI 화면에 표시: {hudInitializer.name}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 로딩 프로세스 시작
        /// </summary>
        private async UniTask StartLoadingProcess()
        {
            try
            {
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 로딩 프로세스 시작");

                // 1단계: RoomStorage에서 데이터 확인 (0% → 10%)
                if (enableDetailedLogs)
                    Debug.Log("📋 [LoadingSceneManager] === 1단계: Room 데이터 확인 ===");
                await UpdateProgressSmoothly(0.0f, 0.05f, 200);

                var room = RoomStorage.Instance.Room;
                if (room == null || !room.IsInitialized())
                {
                    Debug.LogError("❌ [LoadingSceneManager] Room 데이터가 없거나 초기화되지 않음!");
                    // Lobby로 돌아가기
                    await TransitionToLobby();
                    return;
                }

                await UpdateProgressSmoothly(0.05f, 0.1f, 300);
                if (enableDetailedLogs)
                    Debug.Log($"✅ [LoadingSceneManager] Room 데이터 확인 완료: " +
                             $"Ggumtles={room.ggumtles?.Count ?? 0}, " +
                             $"Chests={room.chests?.Count ?? 0}");

                // 2단계: Addressable 에셋 로딩 (10% → 50%)
                if (enableDetailedLogs)
                    Debug.Log("📦 [LoadingSceneManager] === 2단계: 에셋 로딩 시작 ===");
                await UpdateProgressSmoothly(0.1f, 0.15f, 200);

                bool assetLoadSuccess = await LoadAddressableAssets();
                if (assetLoadSuccess)
                {
                    if (enableDetailedLogs)
                        Debug.Log("✅ [LoadingSceneManager] 2단계 성공: 에셋 로딩 완료");
                }
                else
                {
                    Debug.LogWarning("⚠️ [LoadingSceneManager] 에셋 로딩 실패 - 계속 진행");
                    // 에셋 로딩 실패 시에도 50%로 설정 (LoadAddressableAssets 내부에서 이미 처리됨)
                }

                // 3단계: 로딩 진행률 표시 (50% → 70%)
                if (enableDetailedLogs)
                    Debug.Log("🎬 [LoadingSceneManager] === 3단계: 로딩 진행률 시뮬레이션 ===");
                await UpdateProgressSmoothly(0.5f, 0.7f, 800);

                // 4단계: Main 씬 Additive 로딩 (70% → 80%)
                if (enableDetailedLogs)
                    Debug.Log("🎯 [LoadingSceneManager] === 4단계: Main 씬 백그라운드 로딩 ===");
                await UpdateProgressSmoothly(0.7f, 0.75f, 300);

                // Main 씬을 Additive로 로딩
                var mainSceneLoad = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Additive);

                // 로딩 즉시 UI 숨기기 (깜빡임 방지)
                mainSceneLoad.completed += _ => DisableMainSceneUI();

                while (!mainSceneLoad.isDone)
                {
                    await UniTask.Yield();
                }

                // ⭐ Main Scene을 Active Scene으로 설정
                var mainScene = SceneManager.GetSceneByName("Main");
                if (mainScene.IsValid())
                {
                    SceneManager.SetActiveScene(mainScene);
                    if (enableDetailedLogs)
                        Debug.Log("[LoadingSceneManager] Main을 Active Scene으로 설정 완료");
                }
                else
                {
                    Debug.LogError("[LoadingSceneManager] Main Scene을 찾을 수 없음!");
                }

                await UpdateProgressSmoothly(0.75f, 0.8f, 300);
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] Main 씬 Additive 로딩 완료");

                // 메인 씬 로딩 완료 이벤트 발행
                OnMainSceneLoadCompleted?.Invoke();

                // Main 씬 UI가 이미 숨겨져 있음

                // 5단계: 오브젝트 스폰 (80% → 90%)
                if (enableDetailedLogs)
                    Debug.Log("🏗️ [LoadingSceneManager] === 5단계: 오브젝트 스폰 ===");
                await UpdateProgressSmoothly(0.8f, 0.85f, 300);
                await SpawnObjectsInMainScene(room);
                await UpdateProgressSmoothly(0.85f, 0.9f, 300);

                // 6단계: 최종 전환 (90% → 100%)
                if (enableDetailedLogs)
                    Debug.Log("🎬 [LoadingSceneManager] === 6단계: 씬 전환 완료 ===");
                await UpdateProgressSmoothly(0.9f, 1.0f, 500);

                // Main 씬 UI 다시 표시
                EnableMainSceneUI();

                // 카메라를 플레이어 모드로 전환 (스카이박스는 메인 씬 기본값 유지)
                TransitionToPlayerModeOnly();

                // Loading 씬 언로드
                SceneManager.UnloadSceneAsync("Loading");

                if (enableDetailedLogs)
                    Debug.Log("🎉 [LoadingSceneManager] 전체 로딩 프로세스 성공!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingSceneManager] 로딩 프로세스 실패: {e.Message}");
                Debug.LogError($"[LoadingSceneManager] 스택 트레이스: {e.StackTrace}");
                UpdateProgress(1.0f);
                await UniTask.Delay(1000);
                await TransitionToLobby(); // 실패 시 로비로 복귀
            }
        }

        /// <summary>
        /// 맵 데이터 로딩 (더미)
        /// </summary>
        private async UniTask<bool> LoadMapData()
        {
            try
            {
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 맵 데이터 로딩 시작");
                // 실제로는 RoomNetworkSource에서 데이터를 받아와야 하지만 여기서는 시뮬레이션
                await UniTask.Delay(500);
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 맵 데이터 로딩 완료");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoadingSceneManager] 맵 데이터 로딩 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 플레이어 데이터 로딩 (더미)
        /// </summary>
        private async UniTask<bool> LoadPlayerData()
        {
            try
            {
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 플레이어 데이터 로딩 시작");
                // 실제로는 RoomNetworkSource에서 데이터를 받아와야 하지만 여기서는 시뮬레이션
                await UniTask.Delay(500);
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 플레이어 데이터 로딩 완료");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoadingSceneManager] 플레이어 데이터 로딩 실패: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Addressable 에셋 로딩
        /// </summary>
        private async UniTask<bool> LoadAddressableAssets()
        {
            try
            {
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] Addressable 에셋 로딩 시작");

                // AddressableLoadService가 주입되지 않은 경우 VContainer에서 찾기 시도
                if (_addressableLoadService == null)
                {
                    Debug.LogWarning(
                        "[LoadingSceneManager] AddressableLoadService가 주입되지 않음. VContainer에서 찾기 시도..."
                    );
                    try
                    {
                        var lifetimeScope =
                            VContainer.Unity.LifetimeScope.Find<VContainer.Unity.LifetimeScope>();
                        if (lifetimeScope != null)
                        {
                            _addressableLoadService =
                                lifetimeScope.Container.Resolve<IAddressableLoadService>();
                            Debug.Log(
                                "[LoadingSceneManager] VContainer에서 AddressableLoadService 찾기 성공"
                            );
                        }
                    }
                    catch (System.Exception resolveEx)
                    {
                        Debug.LogWarning(
                            $"[LoadingSceneManager] VContainer에서 AddressableLoadService 찾기 실패: {resolveEx.Message}"
                        );
                    }
                }

                if (_addressableLoadService != null)
                {
                    // 진행률 콜백과 함께 에셋 로딩
                    await _addressableLoadService.LoadAssetsWithTagAsync(
                        assetTag,
                        (progress) =>
                        {
                            // 15% ~ 50% 범위에서 진행률 표시 (2단계 범위에 맞춤)
                            var scaledProgress = 0.15f + (progress * 0.35f);
                            UpdateProgress(scaledProgress);
                        }
                    );

                    if (enableDetailedLogs)
                        Debug.Log("[LoadingSceneManager] Addressable 에셋 로딩 완료");
                    return true;
                }
                else
                {
                    Debug.LogWarning(
                        "[LoadingSceneManager] AddressableLoadService를 찾을 수 없음. 에셋 로딩 단계 건너뛰기"
                    );
                    // AddressableLoadService 없을 때 진행률만 업데이트하여 다음 단계로 진행
                    for (float progress = 0.15f; progress <= 0.5f; progress += 0.07f)
                    {
                        UpdateProgress(progress);
                        await UniTask.Delay(200);
                    }
                    if (enableDetailedLogs)
                        Debug.Log("[LoadingSceneManager] 에셋 로딩 단계 완료 (서비스 없음)");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoadingSceneManager] Addressable 에셋 로딩 실패: {e.Message}");
                // 실패 시에도 진행률을 50%로 설정하여 다음 단계로 진행
                UpdateProgress(0.5f);
                return false;
            }
        }

        /// <summary>
        /// 진행률 업데이트
        /// </summary>
        private void UpdateProgress(float progress)
        {
            var percentage = Mathf.Clamp01(progress) * 100f;

            // 프로그레스 바 업데이트
            if (_progressFill != null)
            {
                _progressFill.style.width = Length.Percent(percentage);
            }

            // 텍스트를 %로 업데이트
            if (_interactionLabel != null)
            {
                _interactionLabel.text = $"{percentage:F0}%";
            }

            // 진행률 이벤트 발행
            OnProgressUpdated?.Invoke(progress);

            if (enableProgressLogs)
                Debug.Log($"[LoadingSceneManager] 진행률: {percentage:F0}%");
        }

        /// <summary>
        /// 부드러운 진행률 업데이트
        /// </summary>
        private async UniTask UpdateProgressSmoothly(float fromProgress, float toProgress, int durationMs)
        {
            float elapsed = 0f;
            float duration = durationMs / 1000f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentProgress = Mathf.Lerp(fromProgress, toProgress, t);
                UpdateProgress(currentProgress);
                await UniTask.Yield();
            }

            // 최종 값으로 확실히 설정
            UpdateProgress(toProgress);
        }


        /// <summary>
        /// Main 씬에서 오브젝트 생성
        /// </summary>
        private async UniTask SpawnObjectsInMainScene(Networks.Rooms.Room room)
        {
            try
            {
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] Main 씬에서 오브젝트 생성 시작");

                // RoomData로 변환
                var roomData = RoomDataConverter.ConvertToRoomData(room);
                if (roomData == null || !RoomDataConverter.ValidateRoomData(roomData))
                {
                    Debug.LogWarning("[LoadingSceneManager] RoomData 변환 또는 검증 실패");
                    return;
                }

                // Main 씬의 MainLifetimeScope에서 MapSpawnService 해결 시도
                IMapSpawnService mapSpawnService = null;

                try
                {
                    // Main 씬의 MainLifetimeScope를 직접 찾기
                    var mainLifetimeScopes = UnityEngine.Object.FindObjectsByType<DI.MainLifetimeScope>(UnityEngine.FindObjectsSortMode.None);
                    var mainLifetimeScope = mainLifetimeScopes.Length > 0 ? mainLifetimeScopes[0] : null;

                    if (mainLifetimeScope != null && mainLifetimeScope.Container != null)
                    {
                        mapSpawnService = mainLifetimeScope.Container.Resolve<IMapSpawnService>();
                        if (enableDetailedLogs)
                            Debug.Log("[LoadingSceneManager] MainLifetimeScope에서 IMapSpawnService 찾기 성공");
                    }
                    else
                    {
                        Debug.LogWarning("[LoadingSceneManager] MainLifetimeScope를 찾을 수 없거나 Container가 초기화되지 않음");

                        // 대안: LifetimeScope.Find로 시도
                        var anyLifetimeScope = VContainer.Unity.LifetimeScope.Find<VContainer.Unity.LifetimeScope>();
                        if (anyLifetimeScope != null)
                        {
                            mapSpawnService = anyLifetimeScope.Container.Resolve<IMapSpawnService>();
                            if (enableDetailedLogs)
                                Debug.Log("[LoadingSceneManager] 대안 방법으로 IMapSpawnService 찾기 성공");
                        }
                    }
                }
                catch (System.Exception resolveEx)
                {
                    Debug.LogWarning($"[LoadingSceneManager] IMapSpawnService 찾기 실패: {resolveEx.Message}");
                }

                if (mapSpawnService != null)
                {
                    // MapSpawnService를 통한 오브젝트 생성 (이제 Main Scene에 직접 스폰됨)
                    mapSpawnService.PrepareSpawnData(roomData);
                    await mapSpawnService.SpawnAllObjectsAsync();

                    if (enableDetailedLogs)
                        Debug.Log("[LoadingSceneManager] MapSpawnService를 통한 오브젝트 생성 완료 - Main Scene에 직접 스폰됨");
                }
                else
                {
                    Debug.LogWarning("[LoadingSceneManager] IMapSpawnService를 찾을 수 없음 - Main 씬의 MainLifetimeScope 초기화 대기 필요");

                    // RoomStorage에 데이터를 저장해두고 MainSceneInitializer가 처리하도록 함
                    if (enableDetailedLogs)
                        Debug.Log("[LoadingSceneManager] MainSceneInitializer가 오브젝트 생성을 담당하도록 위임");
                }

                await UniTask.Delay(200); // 오브젝트 생성 대기
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingSceneManager] Main 씬 오브젝트 생성 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로비 씬으로 복귀
        /// </summary>
        private async UniTask TransitionToLobby()
        {
            try
            {
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 로비 씬으로 복귀 시작");

                // GameManager를 통한 로비 전환 시도
                if (_gameManager != null)
                {
                    // GameManager에 TransitionToLobby 메서드가 있는지 확인
                    var lobbyMethod = _gameManager.GetType().GetMethod("TransitionToLobby");
                    if (lobbyMethod != null)
                    {
                        await (UniTask)lobbyMethod.Invoke(_gameManager, null);
                        return;
                    }
                }

                // 직접 로비 씬 로드
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 직접 로비 씬 로드");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingSceneManager] 로비 씬 전환 실패: {e.Message}");
                // 마지막 수단으로 직접 로비 씬 로드
                UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
            }
        }

        /// <summary>
        /// Main 씬으로 전환
        /// </summary>
        private async UniTask TransitionToMain()
        {
            try
            {
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] Main 씬으로 전환 시작");

                // GameManager가 주입되지 않은 경우 직접 찾기 시도
                if (_gameManager == null)
                {
                    Debug.LogWarning(
                        "[LoadingSceneManager] GameManager가 주입되지 않음. 직접 찾기 시도..."
                    );

                    // VContainer에서 GameManager 찾기 시도
                    try
                    {
                        var lifetimeScope =
                            VContainer.Unity.LifetimeScope.Find<VContainer.Unity.LifetimeScope>();
                        if (lifetimeScope != null)
                        {
                            _gameManager = lifetimeScope.Container.Resolve<GameManager>();
                            if (enableDetailedLogs)
                                Debug.Log("[LoadingSceneManager] VContainer에서 GameManager 찾기 성공");
                        }
                    }
                    catch (System.Exception resolveEx)
                    {
                        Debug.LogWarning(
                            $"[LoadingSceneManager] VContainer에서 GameManager 찾기 실패: {resolveEx.Message}"
                        );

                        // GameObject.FindFirstObjectByType으로 찾기 시도
                        _gameManager = FindFirstObjectByType<GameManager>();
                        if (_gameManager != null)
                        {
                            Debug.Log(
                                "[LoadingSceneManager] FindFirstObjectByType으로 GameManager 찾기 성공"
                            );
                        }
                    }
                }

                if (_gameManager != null)
                {
                    await _gameManager.TransitionToMain();
                }
                else
                {
                    Debug.LogError(
                        "[LoadingSceneManager] GameManager를 찾을 수 없음. 직접 씬 전환 시도"
                    );
                    // 직접 씬 전환 시도
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingSceneManager] Main 씬 전환 실패: {e.Message}");
                Debug.LogError($"[LoadingSceneManager] 스택 트레이스: {e.StackTrace}");

                // 마지막 수단으로 직접 씬 전환
                if (enableDetailedLogs)
                    Debug.Log("[LoadingSceneManager] 마지막 수단으로 직접 Main 씬 로드");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }

        /// <summary>
        /// Loading 씬의 Audio Listener를 비활성화하여 중복 문제 해결
        /// </summary>
        private void DisableLoadingSceneAudioListener()
        {
            try
            {
                var audioListeners = GameObject.FindObjectsOfType<AudioListener>();
                if (enableDetailedLogs)
                    Debug.Log($"[LoadingSceneManager] 총 Audio Listener 개수: {audioListeners.Length}");

                // Loading 씬의 Audio Listener들을 비활성화
                foreach (var listener in audioListeners)
                {
                    // Loading 씬에 있는 Audio Listener인지 확인
                    if (listener.gameObject.scene.name == "Loading" ||
                        listener.gameObject.name.Contains("Loading") ||
                        listener.GetComponent<LoadingSceneManager>() != null ||
                        listener.transform.parent?.GetComponent<LoadingSceneManager>() != null)
                    {
                        if (enableDetailedLogs)
                            Debug.Log($"[LoadingSceneManager] Loading 씬의 Audio Listener 비활성화: {listener.gameObject.name}");
                        listener.enabled = false;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoadingSceneManager] Audio Listener 비활성화 중 오류: {e.Message}");
            }
        }

        // ProtectSpawnedObjects 메서드 제거됨
        // 이제 Active Scene이 Main으로 설정되어 있어 DontDestroyOnLoad가 불필요함

        /// <summary>
        /// 플레이어 모드로 카메라 전환 (스카이박스 변경 포함)
        /// </summary>
        private void TransitionToPlayerMode()
        {
            try
            {
                var skyboxManager = SkyboxTransitionManager.Instance;
                if (skyboxManager != null)
                {
                    // 플레이어 뷰 모드로 전환
                    skyboxManager.SetPlayerViewMode();

                    // 게임 스카이박스로 전환 (페이드 효과 포함)
                    skyboxManager.TransitionToGameSkybox();

                    if (enableDetailedLogs)
                        Debug.Log("[LoadingSceneManager] 플레이어 모드로 카메라 전환 완료");
                }
                else
                {
                    Debug.LogWarning("[LoadingSceneManager] SkyboxTransitionManager.Instance가 null입니다");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingSceneManager] 카메라 전환 중 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 모드로 카메라만 전환 (스카이박스는 메인 씬 기본값 유지)
        /// </summary>
        private void TransitionToPlayerModeOnly()
        {
            try
            {
                var skyboxManager = SkyboxTransitionManager.Instance;
                if (skyboxManager != null)
                {
                    // 플레이어 뷰 모드로 전환 (스카이박스 변경 없음)
                    skyboxManager.SetPlayerViewMode();

                    if (enableDetailedLogs)
                        Debug.Log("[LoadingSceneManager] 플레이어 모드로 카메라만 전환 완료 (메인 씬 스카이박스 유지)");
                }
                else
                {
                    Debug.LogWarning("[LoadingSceneManager] SkyboxTransitionManager.Instance가 null입니다");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingSceneManager] 카메라 전환 중 오류: {e.Message}");
            }
        }

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugSkipLoading()
        {
            _ = TransitionToMain();
        }

        #endregion
    }
}
