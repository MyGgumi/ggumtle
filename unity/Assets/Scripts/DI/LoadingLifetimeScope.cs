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
            // parent 설정을 base.Awake() 이전에 완료해야 함
            SetupParentReference();
            base.Awake();
        }

        private void SetupParentReference()
        {
            GameLifetimeScope gameLifetimeScope = null;

            // 먼저 현재 씬에서 찾기
            gameLifetimeScope = FindFirstObjectByType<GameLifetimeScope>();

            // 없으면 DontDestroyOnLoad 씬에서 찾기
            if (gameLifetimeScope == null)
            {
                var allGameObjects = UnityEngine.Object.FindObjectsOfType<GameLifetimeScope>(true);
                if (allGameObjects.Length > 0)
                {
                    gameLifetimeScope = allGameObjects[0];
                }
            }

            // parent 설정 (base.Awake() 이전에 설정해야 함)
            if (gameLifetimeScope != null && gameLifetimeScope != this)
            {
                parentReference = new ParentReference { Object = gameLifetimeScope };
                UnityEngine.Debug.Log("[LoadingLifetimeScope] GameLifetimeScope를 parent로 설정 완료");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[LoadingLifetimeScope] GameLifetimeScope를 찾을 수 없음");
            }
        }

        protected override void Configure(IContainerBuilder builder)
        {
            UnityEngine.Debug.Log("[LoadingLifetimeScope] Configure 시작");

            // Loading 씬에 있는 컴포넌트들 등록
            builder.RegisterComponentInHierarchy<LoadingSceneManager>();

            // Loading 씬 전용 ViewModel 등록
            builder.Register<Features.Scenes.Loading.ViewModels.LoadingViewModel>(Lifetime.Scoped);

            UnityEngine.Debug.Log("[LoadingLifetimeScope] Configure 완료");
        }
    }
}
