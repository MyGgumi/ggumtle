using Features.Ggumtle.Messages;
using Features.MobileControls.Messages;
using Features.Scenes.Lobby.Messages;
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

            // 로비 관련 메시지 타입들 등록
            builder.RegisterMessageBroker<TokenVerifiedMessage>(options);
            builder.RegisterMessageBroker<LobbyUIStateMessage>(options);

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

            // NetworkApi 등록 (팩토리 패턴으로 안전하게 처리)
            builder.Register<Networks.NetworkApi>(
                container =>
                {
                    var existingNetworkApi =
                        UnityEngine.Object.FindObjectOfType<Networks.NetworkApi>();
                    if (existingNetworkApi != null)
                    {
                        return existingNetworkApi;
                    }

                    // Client GameObject 생성
                    var clientObject = new UnityEngine.GameObject("Client");
                    var client = clientObject.AddComponent<Networks.Client>();

                    // NetworkApi GameObject 생성 및 Client 연결
                    var networkApiObject = new UnityEngine.GameObject("NetworkApi");
                    var networkApi = networkApiObject.AddComponent<Networks.NetworkApi>();

                    // Reflection을 사용하여 private client 필드 설정
                    var clientField = typeof(Networks.NetworkApi).GetField(
                        "client",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    );
                    clientField?.SetValue(networkApi, client);

                    UnityEngine.Debug.Log(
                        "[GameLifetimeScope] NetworkApi와 Client GameObject 팩토리에서 생성 완료"
                    );
                    return networkApi;
                },
                Lifetime.Singleton
            );

            // NetworkSources 등록
            builder.Register<
                Features.Ggumtle.NetworkSources.IGgumtleNetworkSource,
                Features.Ggumtle.NetworkSources.GgumtleNetworkSource
            >(Lifetime.Singleton);

            // NetworkEventHandlers 등록
            builder.Register<Features.Ggumtle.NetworkSources.GgumtleNetworkEventHandler>(
                Lifetime.Singleton
            );

            // Services 등록 (순수 C# 클래스)
            builder.Register<
                Features.Ggumtle.Services.IGgumtleService,
                Features.Ggumtle.Services.GgumtleServiceImpl
            >(Lifetime.Singleton);
            builder.Register<Features.MobileControls.Services.MobileInputService>(
                Lifetime.Singleton
            );
            builder.Register<Features.Player.Services.PlayerMovementService>(Lifetime.Singleton);

            // Game Management Services
            builder.Register<
                Features.Game.Services.IGameStateService,
                Features.Game.Services.GameStateServiceImpl
            >(Lifetime.Singleton);
            builder.Register<
                Features.Game.Services.ISceneTransitionService,
                Features.Game.Services.SceneTransitionServiceImpl
            >(Lifetime.Singleton);

            // Room Services
            builder.Register<
                Features.Room.Services.IRoomService,
                Features.Room.Services.RoomServiceImpl
            >(Lifetime.Singleton);

            // Map Services
            builder.Register<
                Features.Map.Services.IAddressableLoadService,
                Features.Map.Services.AddressableLoadServiceImpl
            >(Lifetime.Singleton);
            builder.Register<
                Features.Map.Services.IMapSpawnService,
                Features.Map.Services.MapSpawnServiceImpl
            >(Lifetime.Singleton);

            // NetworkSources for new features
            builder.Register<
                Features.Scenes.Lobby.NetworkSources.ILobbyNetworkSource,
                Features.Scenes.Lobby.NetworkSources.LobbyNetworkSource
            >(Lifetime.Singleton);
            builder.Register<
                Features.Room.NetworkSources.IRoomNetworkSource,
                Features.Room.NetworkSources.RoomNetworkSource
            >(Lifetime.Singleton);

            // ViewModels 등록
            builder.Register<Features.Ggumtle.ViewModels.GgumtleViewModel>(Lifetime.Singleton);
            builder.Register<Features.MobileControls.ViewModels.MobileControlsViewModel>(
                Lifetime.Singleton
            );
            builder.Register<Features.Scenes.Lobby.ViewModels.LobbyViewModel>(Lifetime.Singleton);
            builder.Register<Features.Scenes.Loading.ViewModels.LoadingViewModel>(
                Lifetime.Singleton
            );

            // Views 등록 (GameObject에 붙은 컴포넌트들) - 씬별 컴포넌트는 각 씬의 LifetimeScope에서 등록
            // builder.RegisterComponentInHierarchy<Features.Player.Views.PlayerGameObject>(); // Main 씬에만 존재

            // KeyboardDebugController는 팩토리 패턴으로 안전하게 등록
            builder.Register<Features.MobileControls.Testing.KeyboardDebugController>(
                container =>
                {
                    var existingController =
                        UnityEngine.Object.FindObjectOfType<Features.MobileControls.Testing.KeyboardDebugController>();
                    return existingController; // null이어도 상관없음 (씬에 없을 수 있음)
                },
                Lifetime.Singleton
            );

            // Managers 등록 (Factory 패턴으로 안전하게 처리)
            builder.Register<Features.Game.Managers.GameManager>(
                container =>
                {
                    var existingGameManager =
                        UnityEngine.Object.FindObjectOfType<Features.Game.Managers.GameManager>();
                    if (existingGameManager != null)
                    {
                        return existingGameManager;
                    }

                    // GameManager GameObject 생성
                    var gameManagerObject = new UnityEngine.GameObject("GameManager");
                    var gameManager =
                        gameManagerObject.AddComponent<Features.Game.Managers.GameManager>();

                    UnityEngine.Debug.Log(
                        "[GameLifetimeScope] GameManager GameObject 팩토리에서 생성 완료"
                    );
                    return gameManager;
                },
                Lifetime.Singleton
            );

            // Entry Point 등록
            builder.RegisterEntryPoint<GameInitializer>();
        }
    }

    public class GameInitializer : IStartable
    {
        private readonly Features.Ggumtle.Services.IGgumtleService _ggumtleService;
        private readonly Features.MobileControls.Services.MobileInputService _mobileInputService;
        private readonly Features.Player.Services.PlayerMovementService _playerMovementService;
        private readonly Features.Game.Services.IGameStateService _gameStateService;
        private readonly Features.Room.Services.IRoomService _roomService;
        private readonly Features.Map.Services.IAddressableLoadService _addressableLoadService;

        [Inject]
        public GameInitializer(
            Features.Ggumtle.Services.IGgumtleService ggumtleService,
            Features.MobileControls.Services.MobileInputService mobileInputService,
            Features.Player.Services.PlayerMovementService playerMovementService,
            Features.Game.Services.IGameStateService gameStateService,
            Features.Room.Services.IRoomService roomService,
            Features.Map.Services.IAddressableLoadService addressableLoadService
        )
        {
            _ggumtleService = ggumtleService;
            _mobileInputService = mobileInputService;
            _playerMovementService = playerMovementService;
            _gameStateService = gameStateService;
            _roomService = roomService;
            _addressableLoadService = addressableLoadService;
        }

        public void Start()
        {
            UnityEngine.Debug.Log("[GameInitializer] VContainer DI 초기화 완료");
            UnityEngine.Debug.Log(
                $"[GameInitializer] MobileInputService: {_mobileInputService != null}"
            );
            UnityEngine.Debug.Log(
                $"[GameInitializer] PlayerMovementService: {_playerMovementService != null}"
            );
            UnityEngine.Debug.Log(
                $"[GameInitializer] GameStateService: {_gameStateService != null}"
            );
            UnityEngine.Debug.Log($"[GameInitializer] RoomService: {_roomService != null}");
            UnityEngine.Debug.Log(
                $"[GameInitializer] AddressableLoadService: {_addressableLoadService != null}"
            );

            // 디버그 로그 비활성화
            if (_mobileInputService != null)
                _mobileInputService.enableDebugLogs = false;
            if (_playerMovementService != null)
                _playerMovementService.enableDebugLogs = false;

            // Game state 초기화 (Lobby 상태로 시작)
            if (_gameStateService != null)
            {
                _gameStateService.SetState(Features.Game.Models.GameState.Lobby);
                UnityEngine.Debug.Log("[GameInitializer] 게임 상태를 Lobby로 초기화");
            }

            // 씬에 있는 모든 꿈틀이 자동 등록 (추후 구현)
            // var ggumtles = UnityEngine.GameObject.FindObjectsOfType<InteractableGgumtle>();
            // foreach (var ggumtle in ggumtles)
            // {
            //     _ggumtleService.RegisterGgumtle(ggumtle.GgumtleId, ggumtle.name, ggumtle.transform.position);
            // }
        }
    }
}
