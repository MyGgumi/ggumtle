using Features.Chat.Messages;
using Features.Chat.Services;
using Features.Chat.ViewModels;
using Features.Chest.Messages;
using Features.Chest.Services;
using Features.Chest.ViewModels;
using Features.Chest.Views;
using Features.EscapeGate.Messages;
using Features.EscapeGate.Services;
using Features.EscapeGate.NetworkSources;
using Features.FieldItem.Views;
using Features.GameInfo.Messages;
using Features.GameInfo.Services;
using Features.GameInfo.ViewModels;
using Features.GameResult.Messages;
using Features.GameResult.Services;
using Features.GameResult.ViewModels;
using Features.Ggumtle.Messages;
using Features.Ggumtle.NetworkSources;
using Features.Ggumtle.Services;
using Features.Ggumtle.ViewModels;
using Features.Ggumtle.Views;
using Features.Mongdung.Messages;
using Features.Mongdung.NetworkSources;
using Features.Mongdung.Services;
using Features.Mongdung.ViewModels;
using Features.Mongdung.Views;
using Features.Mongging.Messages;
using Features.Mongging.Services;
using Features.Mongging.ViewModels;
using Features.Inventory.Messages;
using Features.Inventory.NetworkSources;
using Features.Inventory.Services;
using Features.Inventory.ViewModels;
using Features.Inventory.Views;
using Features.MainGame.Messages;
using Features.MainGame.NetworkSources;
using Features.MainGame.Services;
using Features.Map.Services;
using Features.MobileControls.Messages;
using Features.MobileControls.Services;
using Features.MobileControls.Testing;
using Features.MobileControls.ViewModels;
using Features.Notification.Messages;
using Features.Player.NetworkSources;
using Features.Player.Services;
using Features.Notification.Services;
using Features.Notification.ViewModels;
using Features.Player.Messages;
using Features.Player.Services;
using Features.Player.Views;
using Features.PlayerHealth.Messages;
using Features.PlayerHealth.Services;
using Features.PlayerHealth.ViewModels;
using Features.PlayerList.Messages;
using Features.PlayerList.Services;
using Features.PlayerList.ViewModels;
using Features.Revival.Messages;
using Features.Revival.Services;
using Features.Revival.ViewModels;
using Features.Revival.Views;
using Features.Revival.Handlers;
using Features.ItemUsage.Messages;
using Features.ItemUsage.Services;
using Features.Scenes.Main.Initializers;
using Features.UI.Services;
using Features.Game.Services;
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
            // Ggumtle Messages
            builder.RegisterMessageBroker<GgumtleLeftMessage>(options);
            builder.RegisterMessageBroker<GgumtleStateChangedMessage>(options);
            builder.RegisterMessageBroker<GgumtleDetectedMessage>(options);
            builder.RegisterMessageBroker<GgumtleHoldProgressMessage>(options);
            builder.RegisterMessageBroker<GgumtleFoodAddedMessage>(options);

            // Mongdung Messages
            builder.RegisterMessageBroker<MongdungActionStartedMessage>(options);
            builder.RegisterMessageBroker<MongdungActionCompletedMessage>(options);
            builder.RegisterMessageBroker<MongdungActionCooldownMessage>(options);
            builder.RegisterMessageBroker<MongdungStateChangedMessage>(options);
            builder.RegisterMessageBroker<MongdungActionBroadcastMessage>(options);
            builder.RegisterMessageBroker<MongdungActionNetworkResponseMessage>(options);
            builder.RegisterMessageBroker<MongdungMovementBlockedMessage>(options);
            builder.RegisterMessageBroker<MongdungActionRequestMessage>(options);
            builder.RegisterMessageBroker<MongdungAttackResponseMessage>(options);
            builder.RegisterMessageBroker<MongdungSkillResponseMessage>(options);
            builder.RegisterMessageBroker<MongdungAttackActionMessage>(options);
            builder.RegisterMessageBroker<MongdungSkillActionMessage>(options);

            // Mongging Messages
            builder.RegisterMessageBroker<MonggingPlayerStateChangedMessage>(options);
            builder.RegisterMessageBroker<MonggingPlayerHitMessage>(options);
            builder.RegisterMessageBroker<MonggingPlayerHealMessage>(options);
            builder.RegisterMessageBroker<MonggingPlayerStatusEffectMessage>(options);
            builder.RegisterMessageBroker<MonggingPlayerRevivedMessage>(options);
            builder.RegisterMessageBroker<MonggingPlayerEscapedMessage>(options);
            builder.RegisterMessageBroker<MonggingPlayerAnimationMessage>(options);
            builder.RegisterMessageBroker<MonggingTeamSyncMessage>(options);
            builder.RegisterMessageBroker<MonggingTeamPlayerUpdatedMessage>(options);
            builder.RegisterMessageBroker<MonggingStateBroadcastMessage>(options);
            builder.RegisterMessageBroker<MonggingTeamInitializedMessage>(options);
            builder.RegisterMessageBroker<MonggingTeamGameOverMessage>(options);
            builder.RegisterMessageBroker<MonggingItemUseMessage>(options);
            builder.RegisterMessageBroker<MonggingRevivalActionMessage>(options);
            builder.RegisterMessageBroker<MonggingInteractionMessage>(options);

            // Chest Messages
            builder.RegisterMessageBroker<ChestDetectedMessage>(options);
            builder.RegisterMessageBroker<ChestLeftMessage>(options);
            builder.RegisterMessageBroker<ChestOpenedMessage>(options);
            builder.RegisterMessageBroker<ChestClosedMessage>(options);
            builder.RegisterMessageBroker<Features.Notification.Messages.NotificationMessage>(
                options
            );

            // Revival Messages
            builder.RegisterMessageBroker<FaintedMonggingDetectedMessage>(options);
            builder.RegisterMessageBroker<FaintedMonggingLeftMessage>(options);

            // EscapeGate Messages
            builder.RegisterMessageBroker<Features.EscapeGate.Messages.EscapeGateDetectedMessage>(options);
            builder.RegisterMessageBroker<Features.EscapeGate.Messages.EscapeGateLeftMessage>(options);
            builder.RegisterMessageBroker<Features.EscapeGate.Messages.EscapeGateOpenedMessage>(options);
            builder.RegisterMessageBroker<Features.EscapeGate.Messages.EscapeAttemptSuccessMessage>(options);

            // Inventory Messages
            builder.RegisterMessageBroker<ItemAddedMessage>(options);
            builder.RegisterMessageBroker<ItemRemovedMessage>(options);
            builder.RegisterMessageBroker<FieldItemUsedMessage>(options);
            builder.RegisterMessageBroker<InventorySyncMessage>(options);
            builder.RegisterMessageBroker<SlotChangedMessage>(options);
            builder.RegisterMessageBroker<ItemTransferredMessage>(options);
            builder.RegisterMessageBroker<InventorySlotClickedMessage>(options);
            builder.RegisterMessageBroker<InventoryToggleMessage>(options);

            // FieldItem Messages
            builder.RegisterMessageBroker<Features.FieldItem.Messages.FieldItemUseRequestMessage>(options);
            builder.RegisterMessageBroker<Features.FieldItem.Messages.FieldItemGlobalUsedMessage>(options);
            builder.RegisterMessageBroker<Features.FieldItem.Messages.HealthChangedMessage>(options);
            builder.RegisterMessageBroker<Features.FieldItem.Messages.SpeedChangedMessage>(options);
            builder.RegisterMessageBroker<Features.FieldItem.Messages.FieldItemRemoveRequestMessage>(options);

            // ItemUsage Messages
            builder.RegisterMessageBroker<Features.ItemUsage.Messages.ItemUsedBroadcastMessage>(options);
            builder.RegisterMessageBroker<Features.ItemUsage.Messages.SelfDefibrillatorUsedMessage>(options);
            builder.RegisterMessageBroker<Features.ItemUsage.Messages.TaserGunUsedMessage>(options);
            builder.RegisterMessageBroker<Features.ItemUsage.Messages.FlashBangUsedMessage>(options);
            builder.RegisterMessageBroker<Features.ItemUsage.Messages.ItemUsageResultMessage>(options);

            // Revival Messages
            builder.RegisterMessageBroker<RevivalProgressMessage>(options);
            builder.RegisterMessageBroker<RevivalCompletedMessage>(options);
            builder.RegisterMessageBroker<DirectRevivalStartRequestMessage>(options);
            builder.RegisterMessageBroker<DirectRevivalCancelRequestMessage>(options);
            builder.RegisterMessageBroker<SelfDefibRevivalStartMessage>(options);
            builder.RegisterMessageBroker<SelfDefibRevivalProgressMessage>(options);
            builder.RegisterMessageBroker<RevivalInteractionStateMessage>(options);
            builder.RegisterMessageBroker<MonggingInteractableStateMessage>(options);

            // 네트워크 이벤트 메시지 타입들 등록
            builder.RegisterMessageBroker<GgumtleDiggingDoneMessage>(options);
            builder.RegisterMessageBroker<GgumtleJellyForceQuitMessage>(options);
            builder.RegisterMessageBroker<GgumtleSpawnMessage>(options);
            builder.RegisterMessageBroker<GgumtleStateBroadcastMessage>(options);
            builder.RegisterMessageBroker<GgumtleRemoveRequestMessage>(options);

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

            // GameInfo Messages
            builder.RegisterMessageBroker<GameInfoChangedMessage>(options);
            builder.RegisterMessageBroker<GameTimeStartedMessage>(options);
            builder.RegisterMessageBroker<GameTimeStoppedMessage>(options);
            builder.RegisterMessageBroker<GameTimeResetMessage>(options);
            builder.RegisterMessageBroker<GameTimeWarningMessage>(options);
            builder.RegisterMessageBroker<GameTimeExpiredMessage>(options);
            builder.RegisterMessageBroker<GgumtleProgressChangedMessage>(options);
            builder.RegisterMessageBroker<GgumtleLevelChangedMessage>(options);
            builder.RegisterMessageBroker<StatusMessageChangedMessage>(options);

            // GameResult Messages
            builder.RegisterMessageBroker<GameResultMessage>(options);

            // MainGame Messages
            builder.RegisterMessageBroker<GameInitializedMessage>(options);
            builder.RegisterMessageBroker<GameStartedMessage>(options);
            builder.RegisterMessageBroker<GamePhaseChangedMessage>(options);
            builder.RegisterMessageBroker<WinConditionMetMessage>(options);
            builder.RegisterMessageBroker<GameEndedMessage>(options);
            builder.RegisterMessageBroker<PlayerEliminatedMessage>(options);
            builder.RegisterMessageBroker<GameStateSyncMessage>(options);
            builder.RegisterMessageBroker<GamePausedMessage>(options);
            builder.RegisterMessageBroker<CountdownCompletedMessage>(options);

            // PlayerList Messages
            builder.RegisterMessageBroker<PlayerUpdatedMessage>(options);
            builder.RegisterMessageBroker<PlayerStatusChangedMessage>(options);
            builder.RegisterMessageBroker<PlayerConnectionChangedMessage>(options);
            builder.RegisterMessageBroker<PlayerHighlightedMessage>(options);
            builder.RegisterMessageBroker<PlayerHostChangedMessage>(options);
            builder.RegisterMessageBroker<PlayerSpritesUpdatedMessage>(options);
            builder.RegisterMessageBroker<PlayerListSyncMessage>(options);
            builder.RegisterMessageBroker<AllPlayersClearedMessage>(options);

            // Notification Messages
            builder.RegisterMessageBroker<NotificationShowMessage>(options);
            builder.RegisterMessageBroker<NotificationHideMessage>(options);
            builder.RegisterMessageBroker<NotificationQueuedMessage>(options);
            builder.RegisterMessageBroker<NotificationExpiredMessage>(options);
            builder.RegisterMessageBroker<NotificationClearedMessage>(options);
            builder.RegisterMessageBroker<NotificationSettingsChangedMessage>(options);

            // Chat Messages
            builder.RegisterMessageBroker<ChatToggledMessage>(options);
            builder.RegisterMessageBroker<MessageAddedMessage>(options);
            builder.RegisterMessageBroker<MessageSentMessage>(options);
            builder.RegisterMessageBroker<MessageReceivedMessage>(options);
            builder.RegisterMessageBroker<QuickChatSentMessage>(options);
            builder.RegisterMessageBroker<ChatHistoryClearedMessage>(options);
            builder.RegisterMessageBroker<QuickChatMessagesChangedMessage>(options);
            builder.RegisterMessageBroker<SystemMessageAddedMessage>(options);
            builder.RegisterMessageBroker<GameEventChatMessage>(options);
            builder.RegisterMessageBroker<PlayerChatEventMessage>(options);
            builder.RegisterMessageBroker<ChatSettingsChangedMessage>(options);
            builder.RegisterMessageBroker<ChatInputStateChangedMessage>(options);
            builder.RegisterMessageBroker<ChatVisibilityChangedMessage>(options);

            // PlayerHealth Messages
            builder.RegisterMessageBroker<HealthChangedMessage>(options);
            builder.RegisterMessageBroker<DamageReceivedMessage>(options);
            builder.RegisterMessageBroker<HealReceivedMessage>(options);
            builder.RegisterMessageBroker<PlayerFaintedMessage>(options);
            builder.RegisterMessageBroker<PlayerRevivedMessage>(options);
            builder.RegisterMessageBroker<PlayerDiedMessage>(options);
            builder.RegisterMessageBroker<FaintTimeUpdatedMessage>(options);
            builder.RegisterMessageBroker<HealthBarUpdatedMessage>(options);
            builder.RegisterMessageBroker<HealthStateChangedMessage>(options);
            builder.RegisterMessageBroker<HealthSettingsChangedMessage>(options);

            // Player Messages
            builder.RegisterMessageBroker<PlayerRoleChangedMessage>(options);
            builder.RegisterMessageBroker<PlayerMoveResponseMessage>(options);
            builder.RegisterMessageBroker<PlayerJumpMessage>(options);
            builder.RegisterMessageBroker<PlayerAnimationStateMessage>(options);

            // PlayerManagerService Messages
            builder.RegisterMessageBroker<Features.Player.Services.PlayerStateChangedMessage>(options);

            // Main 씬 컴포넌트들 등록 (씬에 미리 배치된 것들만)
            builder.RegisterComponentInHierarchy<PlayerGameObject>();
            builder.RegisterComponentInHierarchy<Features.UI.Views.HUDInitializer>();
            builder.RegisterComponentInHierarchy<Features.Scenes.Main.Managers.MainSceneManager>();

            // Feature UI Views 등록
            builder.RegisterComponentInHierarchy<Features.GameInfo.Views.GameInfoUIView>();
            builder.RegisterComponentInHierarchy<Features.PlayerHealth.Views.PlayerHealthUIView>();
            builder.RegisterComponentInHierarchy<Features.Chat.Views.ChatUIView>();
            builder.RegisterComponentInHierarchy<Features.PlayerList.Views.PlayerListUIView>();
            builder.RegisterComponentInHierarchy<Features.Notification.Views.NotificationUIView>();
            builder.RegisterComponentInHierarchy<Features.MobileControls.Views.MobileControlsView>();
            builder.RegisterComponentInHierarchy<Features.Ggumtle.Views.GgumtleUIView>();
            builder.RegisterComponentInHierarchy<Features.Inventory.Views.InventoryUIView>();
            builder.RegisterComponentInHierarchy<Features.Chest.Views.ChestUIView>();
            builder.RegisterComponentInHierarchy<Features.Feeding.Views.FeedingUIView>();
            builder.RegisterComponentInHierarchy<Features.Revival.Views.RevivalUIView>();
            builder.RegisterComponentInHierarchy<Features.Mongdung.Views.MongdungUIView>();
            builder.RegisterComponentInHierarchy<Features.GameResult.Views.GameResultUIView>();

            // 모든 컴포넌트들은 Addressable 동적 생성 방식으로 처리

            // InteractionTriggerDetector는 별도로 주입 처리
            builder.RegisterComponentInHierarchy<Interaction.InteractionTriggerDetector>();

            // EscapeGateGameObject는 동적 생성 시 AddressableLoadService에서 자동 의존성 주입됨
            // RegisterComponentInHierarchy 제거 - 씬에 미리 배치되지 않음

            // NetworkApi 등록 (팩토리 방식으로 싱글톤 인스턴스 사용)
            builder.Register<Networks.NetworkApi>(
                _ =>
                {
                    var instance = Networks.NetworkApi.Instance;
                    if (instance == null)
                    {
                        var go = new UnityEngine.GameObject("NetworkApi");
                        instance = go.AddComponent<Networks.NetworkApi>();
                    }
                    return instance;
                },
                Lifetime.Singleton
            );


            // Main 씬 전용 NetworkSources 등록
            builder.Register<IGgumtleNetworkSource, GgumtleNetworkSource>(Lifetime.Scoped);
            builder.Register<IInventoryNetworkSource, InventoryNetworkSource>(Lifetime.Scoped);
            builder.Register<
                Features.Chest.NetworkSources.IChestNetworkSource,
                Features.Chest.NetworkSources.ChestNetworkSource
            >(Lifetime.Scoped);
            builder.Register<IMainGameNetworkSource, MainGameNetworkSource>(Lifetime.Scoped);
            builder.Register<IPlayerNetworkSource, PlayerNetworkSource>(Lifetime.Scoped);
            builder.Register<IMongdungNetworkSource, MongdungNetworkSource>(Lifetime.Scoped);
            builder.Register<
                Features.EscapeGate.NetworkSources.IEscapeGateNetworkSource,
                Features.EscapeGate.NetworkSources.EscapeGateNetworkSource
            >(Lifetime.Scoped);

            // ItemUsage & Revival NetworkSources 등록
            builder.Register<
                Features.ItemUsage.NetworkSources.IItemUsageNetworkSource,
                Features.ItemUsage.NetworkSources.ItemUsageNetworkSource
            >(Lifetime.Scoped);
            builder.Register<
                Features.Revival.NetworkSources.IRevivalNetworkSource,
                Features.Revival.NetworkSources.RevivalNetworkSource
            >(Lifetime.Scoped);

            // Main 씬 전용 NetworkEventHandlers 등록
            builder.Register<GgumtleNetworkEventHandler>(Lifetime.Scoped);
            builder.Register<Features.Chest.NetworkSources.ChestNetworkEventHandler>(
                Lifetime.Scoped
            );
            builder.Register<MongdungNetworkEventHandler>(Lifetime.Scoped);
            // MonggingNetworkEventHandler, FieldItemNetworkEventHandler는 static 클래스이므로 DI 등록하지 않음

            // Main 씬 전용 Services 등록 (씬 생명주기와 동일하게 Scoped)
            builder.Register<PlayerMovementService>(Lifetime.Scoped);
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService 등록 시도");
            builder.Register<MobileInputService>(Lifetime.Scoped);
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService 등록 완료");
            builder.Register<IGgumtleService, GgumtleServiceImpl>(Lifetime.Scoped);
            builder.Register<IChestService, ChestServiceImpl>(Lifetime.Scoped);
            builder.Register<IMapSpawnService, MapSpawnServiceImpl>(Lifetime.Scoped);
            builder.Register<IPlayerSpawnService, PlayerSpawnServiceImpl>(Lifetime.Scoped);
            builder.Register<IAddressableLoadService, AddressableLoadServiceImpl>(Lifetime.Scoped);
            builder.Register<IInventoryService, InventoryServiceImpl>(Lifetime.Scoped);
            builder.Register<
                Features.Feeding.Services.IFeedingService,
                Features.Feeding.Services.FeedingServiceImpl
            >(Lifetime.Scoped);
            builder.Register<IGameInfoService, GameInfoServiceImpl>(Lifetime.Scoped);
            builder.Register<IMainGameService, MainGameServiceImpl>(Lifetime.Scoped);
            builder.Register<IPlayerListService, PlayerListServiceImpl>(Lifetime.Scoped);
            builder.Register<INotificationService, NotificationServiceImpl>(Lifetime.Scoped);
            builder.Register<IChatService, ChatServiceImpl>(Lifetime.Scoped);
            builder.Register<IPlayerHealthService, PlayerHealthServiceImpl>(Lifetime.Scoped);
            builder.Register<IPlayerRoleService, PlayerRoleServiceImpl>(Lifetime.Scoped);
            builder.Register<IMongdungService, MongdungServiceImpl>(Lifetime.Scoped);
            builder.Register<IUIAssetService, UIAssetServiceImpl>(Lifetime.Scoped);

            // FieldItem Services 등록
            builder.Register<Features.FieldItem.Services.IFieldItemService, Features.FieldItem.Services.FieldItemServiceImpl>(Lifetime.Scoped);
            builder.Register<Features.FieldItem.NetworkSources.IFieldItemNetworkSource, Features.FieldItem.NetworkSources.FieldItemNetworkSource>(Lifetime.Scoped);

            // GameResult Services 등록
            builder.Register<IGameResultService, GameResultService>(Lifetime.Scoped);

            // ItemUsage & Revival Services 등록
            builder.Register<
                Features.ItemUsage.Services.IItemUsageService,
                Features.ItemUsage.Services.ItemUsageServiceImpl
            >(Lifetime.Scoped);
            builder.Register<
                Features.Revival.Services.IRevivalService,
                Features.Revival.Services.RevivalServiceImpl
            >(Lifetime.Scoped);

            // EscapeGate Services 등록
            builder.Register<
                Features.EscapeGate.Services.IEscapeGateService,
                Features.EscapeGate.Services.EscapeGateServiceImpl
            >(Lifetime.Scoped);


            // Revival Handlers 등록
            builder.Register<Features.Revival.Handlers.MonggingInteractableHandler>(Lifetime.Scoped);

            // ItemEffect Manager 등록 (맵에 직접 이펙트 스폰)
            builder.Register<Features.ItemUsage.Managers.ItemEffectManager>(Lifetime.Scoped);

            // Mongging Services
            builder.Register<IMonggingPlayerService, MonggingPlayerServiceImpl>(Lifetime.Scoped);
            builder.Register<IMonggingTeamService, MonggingTeamServiceImpl>(Lifetime.Scoped);
            builder.Register<IMonggingActionService, MonggingActionServiceImpl>(Lifetime.Scoped);

            // PlayerManagerService 등록 (새로 추가)
            builder.Register<PlayerManagerService>(Lifetime.Scoped);

            // Legacy InventoryModel and InventoryService removed

            // Main 씬 전용 ViewModels 등록 (씬 생명주기와 동일하게 Scoped)
            builder.Register<GgumtleViewModel>(Lifetime.Scoped);
            builder.Register<MongdungViewModel>(Lifetime.Scoped);
            builder.Register<Features.Chest.ViewModels.ChestViewModel>(Lifetime.Scoped);
            builder.Register<Features.Inventory.ViewModels.InventoryViewModel>(Lifetime.Scoped);
            builder.Register<Features.Feeding.ViewModels.FeedingViewModel>(Lifetime.Scoped);
            builder.Register<MobileControlsViewModel>(Lifetime.Scoped);
            builder.Register<GameInfoViewModel>(Lifetime.Scoped);
            builder.Register<PlayerListViewModel>(Lifetime.Scoped);
            builder.Register<NotificationViewModel>(Lifetime.Scoped);
            builder.Register<ChatViewModel>(Lifetime.Scoped);
            builder.Register<PlayerHealthViewModel>(Lifetime.Scoped);

            // Revival ViewModel 등록
            builder.Register<Features.Revival.ViewModels.RevivalViewModel>(Lifetime.Scoped);

            // GameResult ViewModel 등록
            builder.Register<GameResultViewModel>(Lifetime.Scoped);

            // Mongging ViewModels
            builder.Register<MonggingPlayerViewModel>(Lifetime.Scoped);
            builder.Register<MonggingTeamViewModel>(Lifetime.Scoped);

            // Entry Points 등록 (씬 시작 시 자동 실행)
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService EntryPoint 등록 시도");
            builder.RegisterEntryPoint<MobileInputService>();
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService EntryPoint 등록 완료");
            builder.RegisterEntryPoint<MainSceneInitializer>();
            builder.RegisterEntryPoint<MainGameServiceImpl>();
            UnityEngine.Debug.Log("[MainLifetimeScope] MainGameService EntryPoint 등록 완료");
            builder.RegisterEntryPoint<Features.EscapeGate.Services.EscapeGateServiceImpl>();
            UnityEngine.Debug.Log("[MainLifetimeScope] EscapeGateService EntryPoint 등록 완료");

            // FieldItem EntryPoint 등록
            builder.RegisterEntryPoint<Features.FieldItem.Services.FieldItemServiceImpl>();
            UnityEngine.Debug.Log("[MainLifetimeScope] FieldItemService EntryPoint 등록 완료");

            // Mongging EntryPoint 등록
            builder.RegisterEntryPoint<MonggingTeamServiceImpl>();
            UnityEngine.Debug.Log("[MainLifetimeScope] MonggingTeamService EntryPoint 등록 완료");

            // Revival Handlers EntryPoint 등록 (자동 활성화)
            builder.RegisterEntryPoint<Features.Revival.Handlers.MonggingInteractableHandler>();
            UnityEngine.Debug.Log("[MainLifetimeScope] MonggingInteractableHandler EntryPoint 등록 완료");

            // ItemEffect Manager EntryPoint 등록 (자동 활성화)
            builder.RegisterEntryPoint<Features.ItemUsage.Managers.ItemEffectManager>();
            UnityEngine.Debug.Log("[MainLifetimeScope] ItemEffectManager EntryPoint 등록 완료");

            // NetworkEventHandlers 초기화
            builder.RegisterBuildCallback(container =>
            {
                // MainGameNetworkEventHandler에 MainGameService와 NotificationService, GameResultPublisher 주입
                var mainGameService = container.Resolve<IMainGameService>();
                var notificationService = container.Resolve<INotificationService>();
                var gameResultPublisher = container.Resolve<IPublisher<Features.GameResult.Messages.GameResultMessage>>();
                MainGameNetworkEventHandler.Initialize(mainGameService, notificationService, gameResultPublisher);
                UnityEngine.Debug.Log("[MainLifetimeScope] MainGameNetworkEventHandler 초기화 완료");

                // EscapeGateNetworkEventHandler에 Publisher 주입
                var escapeGateOpenedPublisher = container.Resolve<IPublisher<Features.EscapeGate.Messages.EscapeGateOpenedMessage>>();
                Features.EscapeGate.NetworkSources.EscapeGateNetworkEventHandler.Initialize(escapeGateOpenedPublisher);
                UnityEngine.Debug.Log("[MainLifetimeScope] EscapeGateNetworkEventHandler 초기화 완료");

                // ItemUsageNetworkEventHandler에 Publisher 주입
                var itemUsedBroadcastPublisher = container.Resolve<IPublisher<Features.ItemUsage.Messages.ItemUsedBroadcastMessage>>();
                Features.ItemUsage.NetworkSources.ItemUsageNetworkEventHandler.Initialize(itemUsedBroadcastPublisher);
                UnityEngine.Debug.Log("[MainLifetimeScope] ItemUsageNetworkEventHandler 초기화 완료");
            });

            UnityEngine.Debug.Log("[MainLifetimeScope] Configure 완료 - Addressable 동적 생성 방식 사용");
        }
    }
}
