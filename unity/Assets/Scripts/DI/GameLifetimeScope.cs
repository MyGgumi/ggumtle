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
            builder.RegisterMessageBroker<Messages.GgumtleDetectedMessage>(options);
            builder.RegisterMessageBroker<Messages.GgumtleLeftMessage>(options);
            builder.RegisterMessageBroker<Messages.GgumtleStateChangedMessage>(options);
            builder.RegisterMessageBroker<Messages.GgumtlePurifiedMessage>(options);
            builder.RegisterMessageBroker<Messages.GgumtleHoldProgressMessage>(options);
            builder.RegisterMessageBroker<Messages.GgumtleFoodAddedMessage>(options);
            builder.RegisterMessageBroker<Messages.NotificationMessage>(options);

            // Services 등록 (순수 C# 클래스)
            builder.Register<Services.IGgumtleService, Services.GgumtleServiceImpl>(
                Lifetime.Singleton
            );

            // ViewModels 등록
            builder.Register<ViewModels.GgumtleViewModel>(Lifetime.Singleton);

            // Views는 Self-Resolving 패턴 사용 (수동 주입 불필요)

            // Entry Point 등록
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }

    public class GameInitializer : IStartable
    {
        private readonly Services.IGgumtleService _ggumtleService;

        [Inject]
        public GameInitializer(Services.IGgumtleService ggumtleService)
        {
            _ggumtleService = ggumtleService;
        }

        public void Start()
        {
            UnityEngine.Debug.Log("[GameInitializer] VContainer DI 초기화 완료");

            // Views는 각자 Self-Resolving으로 의존성 해결

            // 씬에 있는 모든 꿈틀이 자동 등록 (추후 구현)
            // var ggumtles = UnityEngine.GameObject.FindObjectsOfType<InteractableGgumtle>();
            // foreach (var ggumtle in ggumtles)
            // {
            //     _ggumtleService.RegisterGgumtle(ggumtle.GgumtleId, ggumtle.name, ggumtle.transform.position);
            // }
        }
    }
}
