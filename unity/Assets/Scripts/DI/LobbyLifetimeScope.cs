using Features.Scenes.Lobby.Managers;
using VContainer;
using VContainer.Unity;

namespace DI
{
    /// <summary>
    /// Lobby 씬 전용 LifetimeScope
    /// 씬별 컴포넌트들을 등록
    /// </summary>
    public class LobbyLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            // GameLifetimeScope를 parent로 설정
            var gameLifetimeScope = FindFirstObjectByType<GameLifetimeScope>();
            if (gameLifetimeScope != null && gameLifetimeScope != this)
            {
                autoInjectGameObjects = new System.Collections.Generic.List<UnityEngine.GameObject>();
                parentReference = new ParentReference { Object = gameLifetimeScope };
                UnityEngine.Debug.Log("[LobbyLifetimeScope] GameLifetimeScope를 parent로 설정 완료");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[LobbyLifetimeScope] GameLifetimeScope를 찾을 수 없음");
            }

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            UnityEngine.Debug.Log("[LobbyLifetimeScope] Configure 시작");

            // Lobby 씬에 있는 컴포넌트들 등록
            builder.RegisterComponentInHierarchy<LobbySceneManager>();

            UnityEngine.Debug.Log("[LobbyLifetimeScope] Configure 완료");
        }
    }
}
