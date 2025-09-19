using Cysharp.Threading.Tasks;
using Features.Game.Managers;
using Features.Map.Services;
using Features.Room.Services;
using Features.Scenes.Loading.ViewModels;
using R3;
using UnityEngine;
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

        [Header("UI References")]
        [SerializeField]
        private UIDocument uiDocument; // InteractionUI.uxml을 담는 UIDocument

        // UI Elements
        private Label _interactionLabel;
        private VisualElement _progressFill;
        private VisualElement _interactionUI;

        // 의존성 주입
        private LoadingViewModel _viewModel;
        private GameManager _gameManager;
        private IAddressableLoadService _addressableLoadService;
        private IRoomService _roomService;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(
            LoadingViewModel viewModel,
            GameManager gameManager,
            IAddressableLoadService addressableLoadService,
            IRoomService roomService
        )
        {
            Debug.Log("[LoadingSceneManager] Construct 호출됨!");
            _viewModel = viewModel;
            _gameManager = gameManager;
            _addressableLoadService = addressableLoadService;
            _roomService = roomService;
        }

        async void Start()
        {
            Debug.Log("[LoadingSceneManager] 로딩 씬 시작");

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

            Debug.Log("[LoadingSceneManager] UI 초기화 완료");
        }

        /// <summary>
        /// 로딩 프로세스 시작
        /// </summary>
        private async UniTask StartLoadingProcess()
        {
            try
            {
                Debug.Log("[LoadingSceneManager] 로딩 프로세스 시작");

                // 1단계: 서버에서 맵 데이터 가져오기 (0% → 20%)
                Debug.Log("🗺️ [LoadingSceneManager] === 1단계: 맵 데이터 로딩 시작 ===");
                for (float progress = 0f; progress <= 0.2f; progress += 0.05f)
                {
                    UpdateProgress(progress);
                    await UniTask.Delay(100);
                }

                bool mapLoadSuccess = await LoadMapData();
                if (mapLoadSuccess)
                {
                    Debug.Log("✅ [LoadingSceneManager] 1단계 성공: 맵 데이터 로딩 완료");
                }
                else
                {
                    Debug.LogError("❌ [LoadingSceneManager] 1단계 실패: 맵 데이터 로딩 실패");
                }

                // 2단계: 서버에서 플레이어 데이터 가져오기 (20% → 40%)
                Debug.Log("👥 [LoadingSceneManager] === 2단계: 플레이어 데이터 로딩 시작 ===");
                for (float progress = 0.2f; progress <= 0.4f; progress += 0.05f)
                {
                    UpdateProgress(progress);
                    await UniTask.Delay(100);
                }

                bool playerLoadSuccess = await LoadPlayerData();
                if (playerLoadSuccess)
                {
                    Debug.Log("✅ [LoadingSceneManager] 2단계 성공: 플레이어 데이터 로딩 완료");
                }
                else
                {
                    Debug.LogError("❌ [LoadingSceneManager] 2단계 실패: 플레이어 데이터 로딩 실패");
                }

                // 3단계: Addressable 에셋 로딩 (40% → 80%)
                Debug.Log("📦 [LoadingSceneManager] === 3단계: 에셋 로딩 시작 ===");
                UpdateProgress(0.4f);

                bool assetLoadSuccess = await LoadAddressableAssets();
                if (assetLoadSuccess)
                {
                    Debug.Log("✅ [LoadingSceneManager] 3단계 성공: 에셋 로딩 완료");
                }
                else
                {
                    Debug.LogError("❌ [LoadingSceneManager] 3단계 실패: 에셋 로딩 실패");
                }

                // 4단계: 게임 시작 준비 (80% → 95%)
                Debug.Log("⚙️ [LoadingSceneManager] === 4단계: 게임 시작 준비 ===");
                for (float progress = 0.8f; progress <= 0.95f; progress += 0.05f)
                {
                    UpdateProgress(progress);
                    await UniTask.Delay(100);
                }
                Debug.Log("✅ [LoadingSceneManager] 4단계 성공: 게임 시작 준비 완료");

                // 5단계: 완료 (95% → 100%)
                Debug.Log("🎯 [LoadingSceneManager] === 5단계: 로딩 완료 ===");
                UpdateProgress(1.0f);
                await UniTask.Delay(500);
                Debug.Log("🎉 [LoadingSceneManager] 전체 로딩 프로세스 성공!");

                // Main 씬으로 전환
                await TransitionToMain();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingSceneManager] 로딩 프로세스 실패: {e.Message}");
                UpdateProgress(1.0f); // 실패해도 100%로 설정
                await UniTask.Delay(1000);
                await TransitionToMain(); // 실패해도 Main으로 이동
            }
        }

        /// <summary>
        /// 맵 데이터 로딩 (더미)
        /// </summary>
        private async UniTask<bool> LoadMapData()
        {
            try
            {
                Debug.Log("[LoadingSceneManager] 맵 데이터 로딩 시작");
                // 실제로는 RoomNetworkSource에서 데이터를 받아와야 하지만 여기서는 시뮬레이션
                await UniTask.Delay(500);
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
                Debug.Log("[LoadingSceneManager] 플레이어 데이터 로딩 시작");
                // 실제로는 RoomNetworkSource에서 데이터를 받아와야 하지만 여기서는 시뮬레이션
                await UniTask.Delay(500);
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
                            // 40% ~ 80% 범위에서 진행률 표시
                            var scaledProgress = 0.4f + (progress * 0.4f);
                            UpdateProgress(scaledProgress);
                        }
                    );

                    Debug.Log("[LoadingSceneManager] Addressable 에셋 로딩 완료");
                    return true;
                }
                else
                {
                    Debug.LogWarning(
                        "[LoadingSceneManager] AddressableLoadService를 찾을 수 없음. 더미 로딩으로 진행"
                    );
                    // 더미 진행률 시뮬레이션
                    for (float progress = 0.4f; progress <= 0.8f; progress += 0.1f)
                    {
                        UpdateProgress(progress);
                        await UniTask.Delay(200);
                    }
                    Debug.Log("[LoadingSceneManager] 더미 에셋 로딩 완료");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LoadingSceneManager] Addressable 에셋 로딩 실패: {e.Message}");
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

            Debug.Log($"[LoadingSceneManager] 진행률: {percentage:F0}%");
        }

        /// <summary>
        /// Main 씬으로 전환
        /// </summary>
        private async UniTask TransitionToMain()
        {
            try
            {
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
                Debug.Log("[LoadingSceneManager] 마지막 수단으로 직접 Main 씬 로드");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
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
