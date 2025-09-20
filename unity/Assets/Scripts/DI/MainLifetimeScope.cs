using Features.Ggumtle.Messages;
using Features.Ggumtle.NetworkSources;
using Features.Ggumtle.Services;
using Features.Ggumtle.ViewModels;
using Features.Ggumtle.Views;
using Features.MobileControls.Messages;
using Features.Map.Services;
using Features.MobileControls.Services;
using Features.MobileControls.Testing;
using Features.MobileControls.ViewModels;
using Features.Player.Services;
using Features.Player.Views;
using Features.Scenes.Main.Initializers;
using MessagePipe;
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
        protected override void Configure(IContainerBuilder builder)
        {
            UnityEngine.Debug.Log("[MainLifetimeScope] Configure 시작");

            // MessagePipe 옵션 가져오기 (부모에서 상속)
            var options = builder.RegisterMessagePipe();

            // Main 씬 전용 메시지 타입들 등록
            builder.RegisterMessageBroker<GgumtleLeftMessage>(options);
            builder.RegisterMessageBroker<GgumtleStateChangedMessage>(options);
            builder.RegisterMessageBroker<GgumtleDetectedMessage>(options);
            builder.RegisterMessageBroker<GgumtlePurifiedMessage>(options);
            builder.RegisterMessageBroker<GgumtleHoldProgressMessage>(options);
            builder.RegisterMessageBroker<GgumtleFoodAddedMessage>(options);
            builder.RegisterMessageBroker<NotificationMessage>(options);

            // 네트워크 이벤트 메시지 타입들 등록
            builder.RegisterMessageBroker<GgumtleDiggingDoneMessage>(options);
            builder.RegisterMessageBroker<GgumtleJellyForceQuitMessage>(options);
            builder.RegisterMessageBroker<GgumtleSpawnMessage>(options);
            builder.RegisterMessageBroker<GgumtleNirvanaMessage>(options);

            builder.RegisterMessageBroker<JoystickInputMessage>(options);
            builder.RegisterMessageBroker<JoystickEndMessage>(options);
            builder.RegisterMessageBroker<MobileButtonPressedMessage>(options);
            builder.RegisterMessageBroker<MobileButtonReleasedMessage>(options);
            builder.RegisterMessageBroker<InteractHoldStartMessage>(options);
            builder.RegisterMessageBroker<InteractHoldEndMessage>(options);
            builder.RegisterMessageBroker<InteractButtonVisibilityMessage>(options);
            builder.RegisterMessageBroker<CameraTouchMessage>(options);
            builder.RegisterMessageBroker<MobileControlSettingsMessage>(options);
            builder.RegisterMessageBroker<MobileButtonStateMessage>(options);
            builder.RegisterMessageBroker<MobileInputMessage>(options);

            // Main 씬 컴포넌트들 등록
            builder.RegisterComponentInHierarchy<PlayerGameObject>();
            builder.RegisterComponentInHierarchy<GgumtleGameObject>();
            builder.RegisterComponentInHierarchy<UniversalHUDController>();
            builder.RegisterComponentInHierarchy<Features.MobileControls.Views.MobileControlsView>();
            builder.RegisterComponentInHierarchy<Features.Ggumtle.Views.GgumtleUIView>();
            builder.RegisterComponentInHierarchy<Interaction.InteractionTriggerDetector>();


            // Main 씬 전용 NetworkSources 등록
            builder.Register<IGgumtleNetworkSource, GgumtleNetworkSource>(Lifetime.Scoped);

            // Main 씬 전용 NetworkEventHandlers 등록
            builder.Register<GgumtleNetworkEventHandler>(Lifetime.Scoped);

            // Main 씬 전용 Services 등록 (씬 생명주기와 동일하게 Scoped)
            builder.Register<PlayerMovementService>(Lifetime.Scoped);
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService 등록 시도");
            builder.Register<MobileInputService>(Lifetime.Scoped);
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService 등록 완료");
            builder.Register<IGgumtleService, GgumtleServiceImpl>(Lifetime.Scoped);
            builder.Register<IMapSpawnService, MapSpawnServiceImpl>(Lifetime.Scoped);

            // Main 씬 전용 ViewModels 등록 (씬 생명주기와 동일하게 Scoped)
            builder.Register<GgumtleViewModel>(Lifetime.Scoped).AsSelf();
            builder.Register<MobileControlsViewModel>(Lifetime.Scoped).AsSelf();

            // Entry Points 등록 (씬 시작 시 자동 실행)
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService EntryPoint 등록 시도");
            builder.RegisterEntryPoint<MobileInputService>();
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService EntryPoint 등록 완료");
            builder.RegisterEntryPoint<MainSceneInitializer>();

            UnityEngine.Debug.Log("[MainLifetimeScope] Configure 완료");
        }
    }
}
