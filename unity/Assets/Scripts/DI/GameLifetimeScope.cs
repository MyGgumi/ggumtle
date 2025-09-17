using Features.Ggumtle.Messages;
using Features.MobileControls.Messages;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // MessagePipe 등록 및 옵션 반환
            var options = builder.RegisterMessagePipe();
            builder.RegisterBuildCallback(c =>
                GlobalMessagePipe.SetProvider(c.AsServiceProvider())
            );

            // 꿈틀이 관련 메시지 타입들을 명시적으로 등록
            builder.RegisterMessageBroker<GgumtleLeftMessage>(options);
            builder.RegisterMessageBroker<GgumtleStateChangedMessage>(options);
            builder.RegisterMessageBroker<GgumtleDetectedMessage>(options);
            builder.RegisterMessageBroker<GgumtlePurifiedMessage>(options);
            builder.RegisterMessageBroker<GgumtleHoldProgressMessage>(options);
            builder.RegisterMessageBroker<GgumtleFoodAddedMessage>(options);
            builder.RegisterMessageBroker<NotificationMessage>(options);

            // MobileControls 관련 메시지 타입들 등록
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

            // Services 등록 (순수 C# 클래스)
            builder.Register<
                Features.Ggumtle.Services.IGgumtleService,
                Features.Ggumtle.Services.GgumtleServiceImpl
            >(Lifetime.Singleton);
            builder.Register<Features.MobileControls.Services.MobileInputService>(
                Lifetime.Singleton
            );
            builder.Register<Features.Player.Services.PlayerMovementService>(
                Lifetime.Singleton
            );

            // ViewModels 등록
            builder.Register<Features.Ggumtle.ViewModels.GgumtleViewModel>(Lifetime.Singleton);
            builder.Register<Features.MobileControls.ViewModels.MobileControlsViewModel>(
                Lifetime.Singleton
            );

            // Views 등록 (GameObject에 붙은 컴포넌트들)
            builder.RegisterComponentInHierarchy<Features.Player.Views.PlayerGameObject>();
            builder.RegisterComponentInHierarchy<Features.MobileControls.Testing.KeyboardDebugController>();

            // Entry Point 등록
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }

    public class GameInitializer : IStartable
    {
        private readonly Features.Ggumtle.Services.IGgumtleService _ggumtleService;
        private readonly Features.MobileControls.Services.MobileInputService _mobileInputService;
        private readonly Features.Player.Services.PlayerMovementService _playerMovementService;

        [Inject]
        public GameInitializer(
            Features.Ggumtle.Services.IGgumtleService ggumtleService,
            Features.MobileControls.Services.MobileInputService mobileInputService,
            Features.Player.Services.PlayerMovementService playerMovementService)
        {
            _ggumtleService = ggumtleService;
            _mobileInputService = mobileInputService;
            _playerMovementService = playerMovementService;
        }

        public void Start()
        {
            UnityEngine.Debug.Log("[GameInitializer] VContainer DI 초기화 완료");
            UnityEngine.Debug.Log($"[GameInitializer] MobileInputService: {_mobileInputService != null}");
            UnityEngine.Debug.Log($"[GameInitializer] PlayerMovementService: {_playerMovementService != null}");

            // 디버그 로그 비활성화
            if (_mobileInputService != null)
                _mobileInputService.enableDebugLogs = false;
            if (_playerMovementService != null)
                _playerMovementService.enableDebugLogs = false;

            // 씬에 있는 모든 꿈틀이 자동 등록 (추후 구현)
            // var ggumtles = UnityEngine.GameObject.FindObjectsOfType<InteractableGgumtle>();
            // foreach (var ggumtle in ggumtles)
            // {
            //     _ggumtleService.RegisterGgumtle(ggumtle.GgumtleId, ggumtle.name, ggumtle.transform.position);
            // }
        }
    }
}
