using Features.Player.Services;
using Features.PlayerList.Models;
using Features.UI.Services;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Features.UI.Views
{
    [RequireComponent(typeof(UIDocument))]
    public class HUDInitializer : MonoBehaviour
    {
        [Header("UI Sprites - Auto Loaded")]
        private Sprite ggumtle;
        private Sprite lightJelly;
        private Sprite iconMongingDefault;
        private Sprite iconMongingFaint;
        private Sprite iconMongingDead;
        private Sprite iconMongingEscape;
        private Sprite iconQuickChat;
        private Sprite iconHp;
        private Sprite backgroundItemSlot;

        [Header("Player Role Configuration")]
        public PlayerRole currentPlayerRole = PlayerRole.Mongging;

        private UIDocument _doc;
        private VisualElement _root;

        // VContainer 주입
        private IPlayerRoleService _playerRoleService;
        private IUIAssetService _uiAssetService;

        // Feature UI Views
        private Features.GameTime.Views.GameTimeUIView _gameTimeUIView;
        private Features.PlayerHealth.Views.PlayerHealthUIView _playerHealthUIView;
        private Features.Chat.Views.ChatUIView _chatUIView;
        private Features.PlayerList.Views.PlayerListUIView _playerListUIView;
        private Features.Notification.Views.NotificationUIView _notificationUIView;
        private Features.Ggumtle.Views.GgumtleUIView _ggumtleUIView;
        private Features.MobileControls.Views.MobileControlsView _mobileControlsView;
        private Features.Inventory.Views.InventoryUIView _inventoryUIView;
        private Features.Chest.Views.ChestUIView _chestUIView;
        private Features.Feeding.Views.FeedingUIView _feedingUIView;

        [Inject]
        public void Construct(IPlayerRoleService playerRoleService, IUIAssetService uiAssetService)
        {
            _playerRoleService = playerRoleService;
            _uiAssetService = uiAssetService;
            UnityEngine.Debug.Log("[HUDInitializer] VContainer 의존성 주입 완료");
        }

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            FindAllViews();
        }

        void Start()
        {
            LoadSpritesFromResources();
            InitializeAllViews();
            SetInitialPlayerRole();
            InitializeSprites();

            UnityEngine.Debug.Log("[HUDInitializer] HUD 초기화 완료");
        }

        private void LoadSpritesFromResources()
        {
            // Resources 폴더에서 스프라이트 자동 로드
            ggumtle = Resources.Load<Sprite>("Sprites/ggumtle");
            lightJelly = Resources.Load<Sprite>("Sprites/lightJelly");
            iconMongingDefault = Resources.Load<Sprite>("Sprites/icon_monging_default");
            iconMongingFaint = Resources.Load<Sprite>("Sprites/icon_monging_faint");
            iconMongingDead = Resources.Load<Sprite>("Sprites/icon_monging_dead");
            iconMongingEscape = Resources.Load<Sprite>("Sprites/icon_monging_escape");
            iconQuickChat = Resources.Load<Sprite>("Sprites/icon_quick-chat");
            iconHp = Resources.Load<Sprite>("Sprites/icon_hp");
            backgroundItemSlot = Resources.Load<Sprite>("Sprites/background_item-slot");

            UnityEngine.Debug.Log($"[HUDInitializer] 스프라이트 리소스 로드 완료. backgroundItemSlot: {(backgroundItemSlot != null ? backgroundItemSlot.name : "NULL")}");
        }

        private void FindAllViews()
        {
            // 씬에서 Feature UI Views 찾기
            _gameTimeUIView = FindFirstObjectByType<Features.GameTime.Views.GameTimeUIView>();
            _playerHealthUIView =
                FindFirstObjectByType<Features.PlayerHealth.Views.PlayerHealthUIView>();
            _chatUIView = FindFirstObjectByType<Features.Chat.Views.ChatUIView>();
            _playerListUIView = FindFirstObjectByType<Features.PlayerList.Views.PlayerListUIView>();
            _notificationUIView =
                FindFirstObjectByType<Features.Notification.Views.NotificationUIView>();
            _ggumtleUIView = FindFirstObjectByType<Features.Ggumtle.Views.GgumtleUIView>();
            _mobileControlsView =
                FindFirstObjectByType<Features.MobileControls.Views.MobileControlsView>();
            _inventoryUIView = FindFirstObjectByType<Features.Inventory.Views.InventoryUIView>();
            _chestUIView = FindFirstObjectByType<Features.Chest.Views.ChestUIView>();
            _feedingUIView = FindFirstObjectByType<Features.Feeding.Views.FeedingUIView>();

            UnityEngine.Debug.Log("[HUDInitializer] Feature Views 찾기 완료");
        }

        private void InitializeAllViews()
        {
            // 각 View에 root를 전달하여 초기화
            _gameTimeUIView?.Initialize(_root);
            _playerHealthUIView?.Initialize(_root);
            _chatUIView?.Initialize(_root);
            _playerListUIView?.Initialize(_root);
            _notificationUIView?.Initialize(_root);

            // 별도 GameObject의 Views 초기화
            _ggumtleUIView?.Initialize(_root);
            _mobileControlsView?.Initialize(_root);
            _inventoryUIView?.Initialize(_root);
            _chestUIView?.Initialize(_root);
            _feedingUIView?.Initialize(_root);

            // MobileControlsView 디버그 비활성화
            _mobileControlsView?.SetDebugLogging(false);

            Debug.Log("[HUDInitializer] 모든 View 초기화 완료");
        }

        private void SetInitialPlayerRole()
        {
            _playerRoleService?.ChangeRole(currentPlayerRole);
            Debug.Log($"[HUDInitializer] 초기 플레이어 역할: {currentPlayerRole}");
        }

        private void InitializeSprites()
        {
            if (ggumtle != null)
                _uiAssetService?.SetGgumtleSprite(ggumtle);
            if (lightJelly != null)
                _uiAssetService?.SetLightJellySprite(lightJelly);
            if (iconQuickChat != null)
                _uiAssetService?.SetQuickChatIconSprite(iconQuickChat);
            if (iconHp != null)
                _uiAssetService?.SetHpIconSprite(iconHp);
            if (backgroundItemSlot != null)
                _uiAssetService?.SetItemSlotBackgroundSprite(backgroundItemSlot);

            if (
                iconMongingDefault != null
                && iconMongingFaint != null
                && iconMongingDead != null
                && iconMongingEscape != null
            )
            {
                _uiAssetService?.SetPlayerStateSprites(
                    iconMongingDefault,
                    iconMongingFaint,
                    iconMongingDead,
                    iconMongingEscape
                );
            }

            UnityEngine.Debug.Log("[HUDInitializer] 스프라이트 초기화 완료");
        }

        // 공개 API - 역할 변경
        public void ChangePlayerRole(PlayerRole newRole)
        {
            currentPlayerRole = newRole;
            _playerRoleService?.ChangeRole(newRole);
            Debug.Log($"[HUDInitializer] 플레이어 역할 변경: {newRole}");
        }

        public PlayerRole GetCurrentRole() => currentPlayerRole;

        public bool CanInteract()
        {
            return _playerRoleService?.CanInteract() ?? false;
        }
    }
}
