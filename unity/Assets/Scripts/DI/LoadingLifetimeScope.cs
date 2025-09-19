using Features.Scenes.Loading.Managers;
using VContainer;
using VContainer.Unity;

namespace DI
{
    /// <summary>
    /// Loading 씬 전용 LifetimeScope
    /// 씬별 컴포넌트들을 등록
    /// </summary>
    public class LoadingLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            // GameLifetimeScope를 parent로 설정
            var gameLifetimeScope = FindFirstObjectByType<GameLifetimeScope>();
            if (gameLifetimeScope != null && gameLifetimeScope != this)
            {
                autoInjectGameObjects = new System.Collections.Generic.List<UnityEngine.GameObject>();
                parentReference = new ParentReference { Object = gameLifetimeScope };
                UnityEngine.Debug.Log("[LoadingLifetimeScope] GameLifetimeScope를 parent로 설정 완료");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[LoadingLifetimeScope] GameLifetimeScope를 찾을 수 없음");
            }

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            UnityEngine.Debug.Log("[LoadingLifetimeScope] Configure 시작");

            // Loading 씬에 있는 컴포넌트들 등록
            builder.RegisterComponentInHierarchy<LoadingSceneManager>();

            UnityEngine.Debug.Log("[LoadingLifetimeScope] Configure 완료");
        }
    }
}
