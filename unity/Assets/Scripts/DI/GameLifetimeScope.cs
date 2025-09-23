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

            // MessagePipeBridge 동적 생성 및 등록
            builder.RegisterBuildCallback(container =>
            {
                CreateMessagePipeBridge(container);
            });



            // 로비 관련 메시지 타입들 등록
            builder.RegisterMessageBroker<TokenVerifiedMessage>(options);
            builder.RegisterMessageBroker<LobbyUIStateMessage>(options);



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



            // Services 등록 (순수 C# 클래스)

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

            // NetworkSources for new features
            builder.Register<
                Features.Room.NetworkSources.IRoomNetworkSource,
                Features.Room.NetworkSources.RoomNetworkSource
            >(Lifetime.Singleton);

            // ViewModels 등록 (전역 ViewModels만)

            // Views 등록 (GameObject에 붙은 컴포넌트들) - 씬별 컴포넌트는 각 씬의 LifetimeScope에서 등록
            // builder.RegisterComponentInHierarchy<Features.Player.Views.PlayerGameObject>(); // Main 씬에만 존재


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

        /// <summary>
        /// MessagePipeBridge를 동적으로 생성하고 DontDestroyOnLoad 설정
        /// </summary>
        private void CreateMessagePipeBridge(VContainer.IObjectResolver container)
        {
            try
            {
                // MessagePipeBridge GameObject 생성
                var bridgeObject = new UnityEngine.GameObject("MessagePipeBridge");
                var bridge = bridgeObject.AddComponent<Networks.MessagePipeBridge>();

                // DontDestroyOnLoad 설정
                UnityEngine.Object.DontDestroyOnLoad(bridgeObject);

                UnityEngine.Debug.Log("[GameLifetimeScope] MessagePipeBridge 동적 생성 완료");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[GameLifetimeScope] MessagePipeBridge 생성 실패: {e.Message}");
            }
        }
    }

    public class GameInitializer : IStartable
    {
        private readonly Features.Game.Services.IGameStateService _gameStateService;
        private readonly Features.Room.Services.IRoomService _roomService;
        private readonly Features.Map.Services.IAddressableLoadService _addressableLoadService;

        [Inject]
        public GameInitializer(
            Features.Game.Services.IGameStateService gameStateService,
            Features.Room.Services.IRoomService roomService,
            Features.Map.Services.IAddressableLoadService addressableLoadService
        )
        {
            _gameStateService = gameStateService;
            _roomService = roomService;
            _addressableLoadService = addressableLoadService;
        }

        public void Start()
        {
            UnityEngine.Debug.Log("[GameInitializer] VContainer DI 초기화 완료");
            UnityEngine.Debug.Log(
                $"[GameInitializer] GameStateService: {_gameStateService != null}"
            );
            UnityEngine.Debug.Log($"[GameInitializer] RoomService: {_roomService != null}");
            UnityEngine.Debug.Log(
                $"[GameInitializer] AddressableLoadService: {_addressableLoadService != null}"
            );

            // Game state 초기화 (Lobby 상태로 시작)
            if (_gameStateService != null)
            {
                _gameStateService.SetState(Features.Game.Models.GameState.Lobby);
                UnityEngine.Debug.Log("[GameInitializer] 게임 상태를 Lobby로 초기화");
            }
        }
    }
}
