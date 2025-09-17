using UnityEngine;
using Services;

namespace Managers
{
    /// <summary>
    /// 게임 전체 관리 및 서비스 초기화를 담당하는 매니저
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("서비스 설정")]
        [SerializeField] private bool autoInitializeServices = true;
        [SerializeField] private bool enableDebugLogs = true;

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

            // GgumtleService 초기화
            InitializeGgumtleService();


            // 추후 다른 서비스들도 여기서 초기화
            // InitializeInventoryService();
            // InitializeAudioService();
            // etc...

            if (enableDebugLogs)
                Debug.Log("[GameManager] 모든 서비스 초기화 완료");
        }

        /// <summary>
        /// GgumtleService 초기화
        /// </summary>
        private void InitializeGgumtleService()
        {
            if (GgumtleService.Instance == null)
            {
                var serviceObject = new GameObject("GgumtleService");
                serviceObject.AddComponent<GgumtleService>();

                if (enableDebugLogs)
                    Debug.Log("[GameManager] GgumtleService 생성 완료");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("[GameManager] GgumtleService 이미 존재함");
            }
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

        /// <summary>
        /// 특정 서비스만 다시 초기화
        /// </summary>
        public void ReinitializeGgumtleService()
        {
            InitializeGgumtleService();
        }

        #endregion

        #region Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogGameState()
        {
            Debug.Log($"[GameManager] Debug State:");
            Debug.Log($"  - GgumtleService: {(GgumtleService.Instance != null ? "✅ 활성" : "❌ 비활성")}");
            // 추후 다른 서비스 상태도 추가
        }

        #endregion
    }
}