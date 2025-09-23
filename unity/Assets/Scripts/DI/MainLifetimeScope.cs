using Features.Chat.Messages;
using Features.Chat.Services;
using Features.Chat.ViewModels;
using Features.Chest.Messages;
using Features.Chest.Services;
using Features.Chest.ViewModels;
using Features.Chest.Views;
using Features.FieldItem.Views;
using Features.GameInfo.Messages;
using Features.GameInfo.Services;
using Features.GameInfo.ViewModels;
using Features.Ggumtle.Messages;
using Features.Ggumtle.NetworkSources;
using Features.Ggumtle.Services;
using Features.Ggumtle.ViewModels;
using Features.Ggumtle.Views;
using Features.Inventory.Messages;
using Features.Inventory.NetworkSources;
using Features.Inventory.Services;
using Features.Inventory.ViewModels;
using Features.Inventory.Views;
using Features.MainGame.Messages;
using Features.MainGame.Services;
using Features.Map.Services;
using Features.MobileControls.Messages;
using Features.MobileControls.Services;
using Features.MobileControls.Testing;
using Features.MobileControls.ViewModels;
using Features.Notification.Messages;
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
using Features.Scenes.Main.Initializers;
using Features.UI.Services;
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
            builder.RegisterMessageBroker<GgumtlePurifiedMessage>(options);
            builder.RegisterMessageBroker<GgumtleHoldProgressMessage>(options);
            builder.RegisterMessageBroker<GgumtleFoodAddedMessage>(options);

            // Chest Messages
            builder.RegisterMessageBroker<ChestDetectedMessage>(options);
            builder.RegisterMessageBroker<ChestLeftMessage>(options);
            builder.RegisterMessageBroker<ChestOpenedMessage>(options);
            builder.RegisterMessageBroker<ChestClosedMessage>(options);
            builder.RegisterMessageBroker<Features.Notification.Messages.NotificationMessage>(
                options
            );

            // Inventory Messages
            builder.RegisterMessageBroker<ItemAddedMessage>(options);
            builder.RegisterMessageBroker<ItemRemovedMessage>(options);
            builder.RegisterMessageBroker<ItemUsedMessage>(options);
            builder.RegisterMessageBroker<FieldItemUsedMessage>(options);
            builder.RegisterMessageBroker<InventorySyncMessage>(options);
            builder.RegisterMessageBroker<SlotChangedMessage>(options);
            builder.RegisterMessageBroker<ItemTransferredMessage>(options);
            builder.RegisterMessageBroker<InventorySlotClickedMessage>(options);
            builder.RegisterMessageBroker<InventoryToggleMessage>(options);

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

            // MainGame Messages
            builder.RegisterMessageBroker<GameInitializedMessage>(options);
            builder.RegisterMessageBroker<GameStartedMessage>(options);
            builder.RegisterMessageBroker<GamePhaseChangedMessage>(options);
            builder.RegisterMessageBroker<WinConditionMetMessage>(options);
            builder.RegisterMessageBroker<GameEndedMessage>(options);
            builder.RegisterMessageBroker<PlayerEliminatedMessage>(options);
            builder.RegisterMessageBroker<GameStateSyncMessage>(options);
            builder.RegisterMessageBroker<GamePausedMessage>(options);

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

            // Main 씬 컴포넌트들 등록 (씬에 미리 배치된 것들만)
            builder.RegisterComponentInHierarchy<PlayerGameObject>();
            builder.RegisterComponentInHierarchy<Features.UI.Views.HUDInitializer>();

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

            // 모든 컴포넌트들은 Addressable 동적 생성 방식으로 처리

            // InteractionTriggerDetector는 별도로 주입 처리
            builder.RegisterComponentInHierarchy<Interaction.InteractionTriggerDetector>();

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

            // Main 씬 전용 NetworkEventHandlers 등록
            builder.Register<GgumtleNetworkEventHandler>(Lifetime.Scoped);
            builder.Register<Features.Chest.NetworkSources.ChestNetworkEventHandler>(
                Lifetime.Scoped
            );

            // Main 씬 전용 Services 등록 (씬 생명주기와 동일하게 Scoped)
            builder.Register<PlayerMovementService>(Lifetime.Scoped);
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService 등록 시도");
            builder.Register<MobileInputService>(Lifetime.Scoped);
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService 등록 완료");
            builder.Register<IGgumtleService, GgumtleServiceImpl>(Lifetime.Scoped);
            builder.Register<IChestService, ChestServiceImpl>(Lifetime.Scoped);
            builder.Register<IMapSpawnService, MapSpawnServiceImpl>(Lifetime.Scoped);
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
            builder.Register<IUIAssetService, UIAssetServiceImpl>(Lifetime.Scoped);
            // Legacy InventoryModel and InventoryService removed

            // Main 씬 전용 ViewModels 등록 (씬 생명주기와 동일하게 Scoped)
            builder.Register<GgumtleViewModel>(Lifetime.Scoped);
            builder.Register<Features.Chest.ViewModels.ChestViewModel>(Lifetime.Scoped);
            builder.Register<Features.Inventory.ViewModels.InventoryViewModel>(Lifetime.Scoped);
            builder.Register<Features.Feeding.ViewModels.FeedingViewModel>(Lifetime.Scoped);
            builder.Register<MobileControlsViewModel>(Lifetime.Scoped);
            builder.Register<GameInfoViewModel>(Lifetime.Scoped);
            builder.Register<PlayerListViewModel>(Lifetime.Scoped);
            builder.Register<NotificationViewModel>(Lifetime.Scoped);
            builder.Register<ChatViewModel>(Lifetime.Scoped);
            builder.Register<PlayerHealthViewModel>(Lifetime.Scoped);

            // Entry Points 등록 (씬 시작 시 자동 실행)
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService EntryPoint 등록 시도");
            builder.RegisterEntryPoint<MobileInputService>();
            UnityEngine.Debug.Log("[MainLifetimeScope] MobileInputService EntryPoint 등록 완료");
            builder.RegisterEntryPoint<MainSceneInitializer>();
            builder.RegisterEntryPoint<MainGameServiceImpl>();
            UnityEngine.Debug.Log("[MainLifetimeScope] MainGameService EntryPoint 등록 완료");

            UnityEngine.Debug.Log("[MainLifetimeScope] Configure 완료 - Addressable 동적 생성 방식 사용");
        }
    }
}
