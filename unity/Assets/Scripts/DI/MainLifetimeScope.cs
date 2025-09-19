using Features.MobileControls.Testing;
using Features.Ggumtle.Views;
using Features.Player.Views;
using VContainer;
using VContainer.Unity;

namespace DI
{
    /// <summary>
    /// Main 씬 전용 LifetimeScope
    /// GameLifetimeScope를 parent로 하여 Main 씬 컴포넌트들만 추가 등록
    /// </summary>
    public class MainLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            // GameLifetimeScope를 parent로 설정
            var gameLifetimeScope = FindFirstObjectByType<GameLifetimeScope>();
            if (gameLifetimeScope != null && gameLifetimeScope != this)
            {
                autoInjectGameObjects = new System.Collections.Generic.List<UnityEngine.GameObject>();
                parentReference = new ParentReference { Object = gameLifetimeScope };
                UnityEngine.Debug.Log("[MainLifetimeScope] GameLifetimeScope를 parent로 설정 완료");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[MainLifetimeScope] GameLifetimeScope를 찾을 수 없음");
            }

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            UnityEngine.Debug.Log("[MainLifetimeScope] Configure 시작");

            // Main 씬 컴포넌트들 등록
            builder.RegisterComponentInHierarchy<KeyboardDebugController>();

            // GgumtleGameObject들은 여러 개가 있을 수 있으므로 FindObjectsOfType으로 등록
            var ggumtleObjects = UnityEngine.Object.FindObjectsOfType<GgumtleGameObject>();
            foreach (var ggumtle in ggumtleObjects)
            {
                builder.RegisterComponent(ggumtle);
            }

            builder.RegisterComponentInHierarchy<PlayerGameObject>();

            UnityEngine.Debug.Log("[MainLifetimeScope] Configure 완료");
        }
    }
}
