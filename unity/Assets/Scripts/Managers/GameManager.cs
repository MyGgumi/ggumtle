using DI;
using Networks;
using Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Managers
{
    /// <summary>
    /// 게임 전체 관리 및 서비스 초기화를 담당하는 매니저
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("서비스 설정")]
        [SerializeField]
        private bool autoInitializeServices = true;

        [SerializeField]
        private bool enableDebugLogs = true;

        void Awake()
        {
            // 싱글톤 설정
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (enableDebugLogs)
                    Debug.Log("[GameManager] GameManager 초기화 완료");

                if (autoInitializeServices)
                {
                    InitializeServices();
                }
            }
            else
            {
                Destroy(gameObject);
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

            // 추후 다른 서비스들도 여기서 초기화
            // InitializeInventoryService();
            // InitializeAudioService();
            // etc...

            if (enableDebugLogs)
                Debug.Log("[GameManager] 모든 서비스 초기화 완료");
        }

        /// <summary>
        /// VContainer LifetimeScope 초기화
        /// </summary>
        private void InitializeVContainer()
        {
            // Scene에 GameLifetimeScope가 없으면 생성
            var existingScope = FindObjectOfType<GameLifetimeScope>();
            if (existingScope == null)
            {
                var scopeObject = new GameObject("GameLifetimeScope");
                scopeObject.AddComponent<GameLifetimeScope>();

                if (enableDebugLogs)
                    Debug.Log("[GameManager] GameLifetimeScope 생성 완료");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("[GameManager] GameLifetimeScope 이미 존재함");
            }
        }

        /// <summary>
        /// 게임 종료 시 정리 작업
        /// </summary>
        void OnApplicationQuit()
        {
            if (enableDebugLogs)
                Debug.Log("[GameManager] 게임 종료 - 정리 작업 수행");
        }

        #region Public API

        /// <summary>
        /// 서비스를 수동으로 초기화 (필요한 경우)
        /// </summary>
        public void ManualInitializeServices()
        {
            InitializeServices();
        }

        #endregion

        #region Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogGameState()
        {
            Debug.Log($"[GameManager] Debug State:");
            Debug.Log($"  - IGgumtleService: ✅ VContainer로 관리됨");
            // 추후 다른 서비스 상태도 추가
        }

        #endregion
    }
}
