using Features.Player.Services;
using Features.PlayerList.Models;
using Features.UI.Services;
using Features.Mongdung.Views;
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
        private PlayerManagerService _playerManagerService;

        // Feature UI Views
        private Features.GameInfo.Views.GameInfoUIView _gameInfoUIView;
        private Features.PlayerHealth.Views.PlayerHealthUIView _playerHealthUIView;
        private Features.Chat.Views.ChatUIView _chatUIView;
        private Features.PlayerList.Views.PlayerListUIView _playerListUIView;
        private Features.Notification.Views.NotificationUIView _notificationUIView;
        private Features.Ggumtle.Views.GgumtleUIView _ggumtleUIView;
        private Features.MobileControls.Views.MobileControlsView _mobileControlsView;
        private Features.Inventory.Views.InventoryUIView _inventoryUIView;
        private Features.Chest.Views.ChestUIView _chestUIView;
        private Features.Feeding.Views.FeedingUIView _feedingUIView;
        private MongdungUIView _mongdungUIView;

        [Inject]
        public void Construct(IPlayerRoleService playerRoleService, IUIAssetService uiAssetService, PlayerManagerService playerManagerService)
        {
            _playerRoleService = playerRoleService;
            _uiAssetService = uiAssetService;
            _playerManagerService = playerManagerService;
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

            // 플레이어 스폰 완료 후 역할별 UI 설정을 위해 지연 호출
            StartCoroutine(DelayedRoleBasedUISetup());

            UnityEngine.Debug.Log("[HUDInitializer] HUD 초기화 완료");
        }

        private System.Collections.IEnumerator DelayedRoleBasedUISetup()
        {
            // 몇 프레임 대기하여 플레이어 스폰이 완료되도록 함
            yield return new WaitForSeconds(0.5f);

            Debug.Log("[HUDInitializer] 지연된 역할별 UI 설정 시작");
            SetupRoleBasedUI();
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
            _gameInfoUIView = FindFirstObjectByType<Features.GameInfo.Views.GameInfoUIView>();
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
            _mongdungUIView = FindFirstObjectByType<MongdungUIView>();

            UnityEngine.Debug.Log("[HUDInitializer] Feature Views 찾기 완료");
        }

        private void InitializeAllViews()
        {
            // 각 View에 root를 전달하여 초기화
            _gameInfoUIView?.Initialize(_root);
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
            _mongdungUIView?.Initialize(_root);

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
            {
                _uiAssetService?.SetItemSlotBackgroundSprite(backgroundItemSlot);

                // MongdungUIView에도 동일한 배경 적용
                if (_mongdungUIView != null)
                {
                    _mongdungUIView.SetSlotBackgroundSprite(backgroundItemSlot);
                }
            }

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

        /// <summary>
        /// 로컬 플레이어 역할에 따른 UI 표시/숨김 설정
        /// </summary>
        private void SetupRoleBasedUI()
        {
            Debug.Log("[HUDInitializer] SetupRoleBasedUI 시작");

            if (_playerManagerService == null)
            {
                Debug.LogWarning("[HUDInitializer] PlayerManagerService가 주입되지 않았습니다. 기본 UI 설정을 사용합니다.");
                return;
            }

            var localPlayerRole = _playerManagerService.GetLocalPlayerRole();
            Debug.Log($"[HUDInitializer] 감지된 로컬 플레이어 역할: {localPlayerRole}");
            Debug.Log($"[HUDInitializer] MongdungUIView 존재 여부: {_mongdungUIView != null}");
            Debug.Log($"[HUDInitializer] InventoryUIView 존재 여부: {_inventoryUIView != null}");
            Debug.Log($"[HUDInitializer] PlayerHealthUIView 존재 여부: {_playerHealthUIView != null}");

            if (localPlayerRole == PlayerRole.Mongdung)
            {
                Debug.Log("[HUDInitializer] 몽둥이 플레이어로 감지됨 - SetupMongdungUI 호출");
                // 몽둥이 플레이어: MongdungUIView 표시, Inventory/MobileControls 일부 숨김
                SetupMongdungUI();
            }
            else
            {
                Debug.Log("[HUDInitializer] 몽깅이 플레이어로 감지됨 - SetupMonggingUI 호출");
                // 몽깅이 플레이어: MongdungUIView 숨김, Inventory/MobileControls 표시
                SetupMonggingUI();
            }

            Debug.Log($"[HUDInitializer] 역할별 UI 설정 완료: {localPlayerRole}");
        }

        /// <summary>
        /// 몽둥이 플레이어 UI 설정
        /// </summary>
        private void SetupMongdungUI()
        {
            // MongdungUIView 표시
            if (_mongdungUIView != null)
            {
                _mongdungUIView.SetVisible(true);
                Debug.Log("[HUDInitializer] 몽둥이 UI 활성화");
            }
            else
            {
                Debug.LogWarning("[HUDInitializer] MongdungUIView를 찾을 수 없습니다");
            }

            // Inventory 숨김 (몽둥이는 아이템을 사용하지 않음)
            if (_inventoryUIView != null)
            {
                // GameObject 비활성화 대신 UI 요소 직접 숨김
                var inventoryElement = _root.Q<VisualElement>("inventory");
                if (inventoryElement != null)
                {
                    inventoryElement.style.display = DisplayStyle.None;
                    Debug.Log("[HUDInitializer] 인벤토리 UI 숨김 (몽둥이)");
                }
                else
                {
                    // 백업으로 GameObject 비활성화
                    _inventoryUIView.gameObject.SetActive(false);
                    Debug.Log("[HUDInitializer] 인벤토리 GameObject 비활성화 (몽둥이)");
                }
            }

            // PlayerHealth 숨김 (몽둥이는 체력바가 필요 없음)
            if (_playerHealthUIView != null)
            {
                // GameObject 비활성화 대신 UI 요소 직접 숨김
                var healthElement = _root.Q<VisualElement>("healthBar");
                if (healthElement != null)
                {
                    healthElement.style.display = DisplayStyle.None;
                    Debug.Log("[HUDInitializer] 체력바 UI 숨김 (몽둥이)");
                }
                else
                {
                    // 백업으로 GameObject 비활성화
                    _playerHealthUIView.gameObject.SetActive(false);
                    Debug.Log("[HUDInitializer] 체력바 GameObject 비활성화 (몽둥이)");
                }
            }

            // MobileControls는 조이스틱과 점프는 유지, 상호작용 버튼은 자동으로 숨겨짐 (MobileControlsViewModel에서 처리)
            // MobileControls 자체는 활성화 상태 유지
            if (_mobileControlsView != null)
            {
                Debug.Log("[HUDInitializer] MobileControls 활성화 상태 확인 (몽둥이)");
            }
        }

        /// <summary>
        /// 몽깅이 플레이어 UI 설정
        /// </summary>
        private void SetupMonggingUI()
        {
            // MongdungUIView 숨김
            if (_mongdungUIView != null)
            {
                _mongdungUIView.SetVisible(false);
                Debug.Log("[HUDInitializer] 몽둥이 UI 비활성화");
            }

            // Inventory 표시 (몽깅이는 아이템을 사용함)
            if (_inventoryUIView != null)
            {
                // UI 요소 직접 표시
                var inventoryElement = _root.Q<VisualElement>("inventory");
                if (inventoryElement != null)
                {
                    inventoryElement.style.display = DisplayStyle.Flex;
                    Debug.Log("[HUDInitializer] 인벤토리 UI 표시 (몽깅이)");
                }
                else
                {
                    // 백업으로 GameObject 활성화
                    _inventoryUIView.gameObject.SetActive(true);
                    Debug.Log("[HUDInitializer] 인벤토리 GameObject 활성화 (몽깅이)");
                }
            }

            // PlayerHealth 표시 (몽깅이는 체력바가 필요함)
            if (_playerHealthUIView != null)
            {
                // UI 요소 직접 표시
                var healthElement = _root.Q<VisualElement>("healthBar");
                if (healthElement != null)
                {
                    healthElement.style.display = DisplayStyle.Flex;
                    Debug.Log("[HUDInitializer] 체력바 UI 표시 (몽깅이)");
                }
                else
                {
                    // 백업으로 GameObject 활성화
                    _playerHealthUIView.gameObject.SetActive(true);
                    Debug.Log("[HUDInitializer] 체력바 GameObject 활성화 (몽깅이)");
                }
            }

            // MobileControls는 모든 버튼 활성화 (상호작용 버튼 포함)
            if (_mobileControlsView != null)
            {
                Debug.Log("[HUDInitializer] MobileControls 활성화 상태 확인 (몽깅이)");
            }
        }
    }
}
