using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using System;
using DI;

namespace Networks
{
    /// <summary>
    /// Static NetworkEventHandler와 VContainer MessagePipe를 연결하는 Bridge
    /// DontDestroyOnLoad로 생성되어 모든 씬에서 현재 활성화된 LifetimeScope를 찾아 연결
    /// </summary>
    public class MessagePipeBridge : MonoBehaviour
    {
        private static MessagePipeBridge _instance;
        public static MessagePipeBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[MessagePipeBridge] Instance가 없음! GameLifetimeScope에서 생성되었는지 확인하세요.");
                }
                return _instance;
            }
        }

        private bool _enableDebugLogs = true;

        void Awake()
        {
            // 싱글톤 패턴
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MessagePipeBridge] 중복 인스턴스 제거");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_enableDebugLogs)
                Debug.Log("[MessagePipeBridge] 초기화 완료 (DontDestroyOnLoad)");
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Static Handler에서 현재 활성화된 씬의 MessagePipe로 메시지 발행
        /// </summary>
        public void PublishMessage<T>(T message)
        {
            try
            {
                var currentScope = FindCurrentActiveScope();
                if (currentScope == null)
                {
                    Debug.LogError($"[MessagePipeBridge] 활성화된 LifetimeScope를 찾을 수 없음! {typeof(T).Name} 메시지 발행 실패");
                    return;
                }

                var publisher = currentScope.Container.Resolve<IPublisher<T>>();
                publisher.Publish(message);

                if (_enableDebugLogs)
                    Debug.Log($"[MessagePipeBridge] {typeof(T).Name} 메시지 발행 성공 (Scope: {currentScope.GetType().Name})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MessagePipeBridge] {typeof(T).Name} 메시지 발행 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 현재 활성화된 LifetimeScope 찾기 (우선순위 순서)
        /// </summary>
        private LifetimeScope FindCurrentActiveScope()
        {
            // 1. MainLifetimeScope 우선 (Main 씬이 활성화된 경우)
            var mainScope = FindFirstObjectByType<MainLifetimeScope>();
            if (mainScope != null)
            {
                if (_enableDebugLogs && Time.frameCount % 300 == 0) // 5초마다 한 번씩만 로그
                    Debug.Log("[MessagePipeBridge] MainLifetimeScope 사용 중");
                return mainScope;
            }

            // 2. LobbyLifetimeScope (Lobby 씬이 활성화된 경우)
            var lobbyScope = FindFirstObjectByType<LobbyLifetimeScope>();
            if (lobbyScope != null)
            {
                if (_enableDebugLogs && Time.frameCount % 300 == 0)
                    Debug.Log("[MessagePipeBridge] LobbyLifetimeScope 사용 중");
                return lobbyScope;
            }

            // 3. GameLifetimeScope (최후 수단, 항상 존재해야 함)
            var gameScope = FindFirstObjectByType<GameLifetimeScope>();
            if (gameScope != null)
            {
                if (_enableDebugLogs && Time.frameCount % 300 == 0)
                    Debug.Log("[MessagePipeBridge] GameLifetimeScope 사용 중");
                return gameScope;
            }

            Debug.LogError("[MessagePipeBridge] 어떤 LifetimeScope도 찾을 수 없음!");
            return null;
        }

        /// <summary>
        /// 디버그용: 현재 사용 가능한 스코프들 확인
        /// </summary>
        [ContextMenu("Show Available Scopes")]
        public void ShowAvailableScopes()
        {
            var mainScope = FindFirstObjectByType<MainLifetimeScope>();
            var lobbyScope = FindFirstObjectByType<LobbyLifetimeScope>();
            var gameScope = FindFirstObjectByType<GameLifetimeScope>();

            Debug.Log($"[MessagePipeBridge] 사용 가능한 스코프들:");
            Debug.Log($"  - MainLifetimeScope: {mainScope != null}");
            Debug.Log($"  - LobbyLifetimeScope: {lobbyScope != null}");
            Debug.Log($"  - GameLifetimeScope: {gameScope != null}");
            Debug.Log($"  - 현재 선택된 스코프: {FindCurrentActiveScope()?.GetType().Name ?? "없음"}");
        }

        /// <summary>
        /// 디버그 로그 활성화/비활성화
        /// </summary>
        public void SetDebugLogging(bool enable)
        {
            _enableDebugLogs = enable;
        }
    }
}