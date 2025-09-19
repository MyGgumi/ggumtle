using Features.Game.Models;
using Features.Game.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using DI;
using Cysharp.Threading.Tasks;

namespace Features.Game.Managers
{
    /// <summary>
    /// 게임 전체 관리 및 서비스 초기화를 담당하는 매니저
    /// Feature 기반 구조에서 게임 상태와 씬 전환을 조율하는 역할
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("서비스 설정")]
        [SerializeField] private bool autoInitializeServices = true;
        [SerializeField] private bool enableDebugLogs = true;

        // Feature Services 의존성 주입
        private IGameStateService _gameStateService;
        private ISceneTransitionService _sceneTransitionService;

        [Inject]
        public void Construct(
            IGameStateService gameStateService,
            ISceneTransitionService sceneTransitionService)
        {
            _gameStateService = gameStateService;
            _sceneTransitionService = sceneTransitionService;
        }

        void Awake()
        {
            // DontDestroyOnLoad 설정
            DontDestroyOnLoad(gameObject);

            if (enableDebugLogs)
                Debug.Log("[GameManager] GameManager 초기화 완료");

            if (autoInitializeServices)
            {
                InitializeServices();
            }
        }

        void Start()
        {
            // 게임 상태 이벤트 구독
            if (_gameStateService != null)
            {
                _gameStateService.OnStateChanged += OnGameStateChanged;

                if (enableDebugLogs)
                    Debug.Log($"[GameManager] 초기 게임 상태: {_gameStateService.CurrentState}");
            }
        }

        void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_gameStateService != null)
            {
                _gameStateService.OnStateChanged -= OnGameStateChanged;
            }
        }

        /// <summary>
        /// 게임에 필요한 모든 서비스들을 초기화
        /// </summary>
        private void InitializeServices()
        {
            if (enableDebugLogs)
                Debug.Log("[GameManager] 서비스 초기화 시작");

            // VContainer LifetimeScope 초기화
            InitializeVContainer();

            if (enableDebugLogs)
                Debug.Log("[GameManager] 모든 서비스 초기화 완료");
        }

        /// <summary>
        /// VContainer LifetimeScope 초기화
        /// </summary>
        private void InitializeVContainer()
        {
            // Scene에 GameLifetimeScope가 없으면 생성
            var existingScope = FindFirstObjectByType<GameLifetimeScope>();
            if (existingScope == null)
            {
                var scopeObject = new GameObject("GameLifetimeScope");
                scopeObject.AddComponent<GameLifetimeScope>();
                DontDestroyOnLoad(scopeObject); // 중요: DontDestroyOnLoad 설정

                if (enableDebugLogs)
                    Debug.Log("[GameManager] GameLifetimeScope 생성 및 DontDestroyOnLoad 설정 완료");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("[GameManager] GameLifetimeScope 이미 존재함");
            }
        }

        /// <summary>
        /// 게임 상태 변경 이벤트 처리
        /// </summary>
        private void OnGameStateChanged(GameState previousState, GameState newState)
        {
            if (enableDebugLogs)
                Debug.Log($"[GameManager] 게임 상태 변경 감지: {previousState} → {newState}");

            // 상태별 추가 처리 로직이 필요하면 여기에 추가
        }

        #region Public API - 씬 전환 메서드들

        /// <summary>
        /// Loading 씬으로 전환
        /// </summary>
        public async UniTask TransitionToLoading()
        {
            // 서비스가 주입되지 않은 경우 Container에서 직접 가져오기
            if (_gameStateService == null || _sceneTransitionService == null)
            {
                Debug.Log("[GameManager] 서비스 재주입 시도");
                try
                {
                    var lifetimeScope = VContainer.Unity.LifetimeScope.Find<VContainer.Unity.LifetimeScope>();
                    if (lifetimeScope != null)
                    {
                        _gameStateService = lifetimeScope.Container.Resolve<IGameStateService>();
                        _sceneTransitionService = lifetimeScope.Container.Resolve<ISceneTransitionService>();
                        Debug.Log($"[GameManager] 서비스 재주입 완료: GameState={_gameStateService != null}, SceneTransition={_sceneTransitionService != null}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GameManager] 서비스 재주입 실패: {e.Message}");
                }
            }

            if (_gameStateService == null || _sceneTransitionService == null)
            {
                Debug.LogError("[GameManager] 필요한 서비스가 주입되지 않음");
                return;
            }

            // Loading 상태로 전환 (순서: Lobby → Loading → InGame)
            _gameStateService.SetState(GameState.Loading);

            try
            {
                await _sceneTransitionService.LoadSceneAsync("Loading");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GameManager] Loading 씬 로드 실패, Main 씬으로 직접 전환: {e.Message}");
                // Loading 씬이 없으면 바로 Main으로
                _gameStateService.SetState(GameState.InGame);
                await _sceneTransitionService.LoadSceneAsync("Main");
            }
        }

        /// <summary>
        /// Main 씬으로 전환
        /// </summary>
        public async UniTask TransitionToMain()
        {
            // 서비스가 주입되지 않은 경우 Container에서 직접 가져오기
            if (_gameStateService == null || _sceneTransitionService == null)
            {
                Debug.Log("[GameManager] 서비스 재주입 시도");
                try
                {
                    var lifetimeScope = VContainer.Unity.LifetimeScope.Find<VContainer.Unity.LifetimeScope>();
                    if (lifetimeScope != null)
                    {
                        _gameStateService = lifetimeScope.Container.Resolve<IGameStateService>();
                        _sceneTransitionService = lifetimeScope.Container.Resolve<ISceneTransitionService>();
                        Debug.Log($"[GameManager] 서비스 재주입 완료: GameState={_gameStateService != null}, SceneTransition={_sceneTransitionService != null}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GameManager] 서비스 재주입 실패: {e.Message}");
                }
            }

            if (_gameStateService == null || _sceneTransitionService == null)
            {
                Debug.LogError("[GameManager] 필요한 서비스가 주입되지 않음. 직접 씬 전환 시도");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
                return;
            }

            // 현재 상태 확인 후 적절한 전환
            var currentState = _gameStateService.CurrentState;
            Debug.Log($"[GameManager] TransitionToMain - 현재 상태: {currentState}");

            if (currentState == GameState.Loading)
            {
                // Loading → InGame 전환 (허용됨)
                _gameStateService.SetState(GameState.InGame);
                await _sceneTransitionService.LoadSceneAsync("Main");
            }
            else if (currentState == GameState.Lobby)
            {
                // Lobby → Loading → InGame 순서로 전환
                Debug.Log("[GameManager] Lobby에서 바로 Main으로 전환. Loading 상태를 거쳐서 전환");
                _gameStateService.SetState(GameState.Loading);
                _gameStateService.SetState(GameState.InGame);
                await _sceneTransitionService.LoadSceneAsync("Main");
            }
            else
            {
                // 다른 상태에서는 바로 InGame으로 전환
                _gameStateService.SetState(GameState.InGame);
                await _sceneTransitionService.LoadSceneAsync("Main");
            }
        }

        /// <summary>
        /// Lobby 씬으로 전환
        /// </summary>
        public async UniTask TransitionToLobby()
        {
            if (_gameStateService == null || _sceneTransitionService == null)
            {
                Debug.LogError("[GameManager] 필요한 서비스가 주입되지 않음");
                return;
            }

            _gameStateService.SetState(GameState.Lobby);
            await _sceneTransitionService.LoadSceneAsync("Lobby");
        }

        /// <summary>
        /// 게임 종료 상태로 전환
        /// </summary>
        public void SetGameOver()
        {
            _gameStateService?.SetState(GameState.GameOver);
        }

        /// <summary>
        /// 현재 게임 상태 가져오기
        /// </summary>
        public GameState GetCurrentGameState()
        {
            return _gameStateService?.CurrentState ?? GameState.Lobby;
        }

        #endregion

        #region Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogGameState()
        {
            Debug.Log($"[GameManager] Debug State:");
            Debug.Log($"  - Current State: {_gameStateService?.CurrentState}");
            Debug.Log($"  - Current Scene: {_sceneTransitionService?.GetCurrentSceneName()}");
            Debug.Log($"  - Services Injected: {_gameStateService != null && _sceneTransitionService != null}");
        }

        #endregion
    }
}