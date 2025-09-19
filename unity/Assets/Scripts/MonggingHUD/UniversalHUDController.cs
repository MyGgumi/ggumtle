using System;
using System.Collections;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UniversalHUDController : MonoBehaviour
{
    [Header("UI Sprites")]
    public Sprite ggumtle;
    public Sprite lightJelly;
    public Sprite iconMongingDefault;
    public Sprite iconMongingFaint;
    public Sprite iconMongingDead;
    public Sprite iconMongingEscape;
    public Sprite iconQuickChat;
    public Sprite iconHp;
    public Sprite backgroundItemSlot;

    [Header("Player Role Configuration")]
    public PlayerRole currentPlayerRole = PlayerRole.Mongging;

    [Header("Input System Integration")]
    private UIDocument _doc;
    private VisualElement _root;

    [Header("MVVM ViewModels")]
    public MVVM.UI.GameTimeViewModel gameTimeViewModel;
    public MVVM.UI.HealthBarViewModel healthBarViewModel;
    public MVVM.UI.ResourceViewModel resourceViewModel;
    public MVVM.UI.ChatViewModel chatViewModel;
    public MVVM.UI.PlayerListViewModel playerListViewModel;
    public MVVM.UI.NotificationViewModel notificationViewModel;
    public ViewModels.UI.InventoryViewModel inventoryViewModel;

    public ViewModels.UI.ChestViewModel chestViewModel;

    [Header("MVVM Views")]
    public Views.GameTimeView gameTimeView;
    public Views.HealthBarView healthBarView;
    public Views.ResourceView resourceView;
    public Views.ChatView chatView;
    public Views.PlayerListView playerListView;
    public Views.NotificationView notificationView;
    public Views.InventoryView inventoryView;

    public Views.ChestView chestView;
    public Features.Ggumtle.Views.GgumtleUIView ggumtleUIView;
    public Features.MobileControls.Views.MobileControlsView mobileControlsView;

    [Header("Debug Input")]
    private Features.MobileControls.Testing.KeyboardDebugController keyboardDebugController;

    // MessagePipe 구독 관리
    private readonly CompositeDisposable _messageSubscriptions = new();

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        EnsureManagerComponents();
        SetupInputSystem();
        SetupMobileControlsSystem();
        ConfigureUIForRole(currentPlayerRole);
    }

    void Start()
    {
        InitializeManagers();
        InitializeInputControllers();
        InitializeViewModels();
        InitializeMobileControls();
        InitializeSprites();

        // 매니저 초기화 후 이벤트 구독
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();

        // MessagePipe 구독 해제
        _messageSubscriptions.Dispose();
        Debug.Log("[UniversalHUDController] MessagePipe 구독 해제 완료");
    }

    #region 입력 시스템 설정

    private void SetupInputSystem()
    {
        // 키보드 디버그 컨트롤러 추가 (테스트용)
        keyboardDebugController =
            gameObject.GetComponent<Features.MobileControls.Testing.KeyboardDebugController>();
        if (keyboardDebugController == null)
        {
            keyboardDebugController =
                gameObject.AddComponent<Features.MobileControls.Testing.KeyboardDebugController>();
            Debug.Log(
                "[UniversalHUDController] 키보드 디버그 컨트롤러 추가 (WASD 이동, Space 점프)"
            );
        }
    }

    private void InitializeInputControllers()
    {
        Debug.Log("[UniversalHUDController] 입력 컨트롤러 초기화 완료");
    }

    #endregion

    private void InitializeViewModels()
    {
        Debug.Log("[UniversalHUDController] ViewModel들 초기화 완료");
    }

    #region Mobile Controls System

    private void SetupMobileControlsSystem()
    {
        // MobileControlsView는 EnsureManagerComponents에서 생성됨
        if (mobileControlsView == null)
        {
            Debug.LogError("[UniversalHUDController] MobileControlsView가 생성되지 않음");
        }
        else
        {
            Debug.Log("[UniversalHUDController] Mobile Controls 시스템 컴포넌트 설정 완료");
        }
    }

    private void InitializeMobileControls()
    {
        if (mobileControlsView != null)
        {
            // MobileControlsView 디버그 비활성화
            mobileControlsView.SetDebugLogging(false);

            // MobileControlsView 초기화
            mobileControlsView.Initialize(_root);
            Debug.Log("[UniversalHUDController] MobileControlsView 초기화 완료");
        }
        else
        {
            Debug.LogError("[UniversalHUDController] MobileControlsView를 초기화할 수 없음");
        }
    }

    #endregion

    private void EnsureManagerComponents()
    {
        // MVVM ViewModels 컴포넌트 확보
        if (gameTimeViewModel == null)
            gameTimeViewModel =
                gameObject.GetComponent<MVVM.UI.GameTimeViewModel>()
                ?? gameObject.AddComponent<MVVM.UI.GameTimeViewModel>();
        if (healthBarViewModel == null)
            healthBarViewModel =
                gameObject.GetComponent<MVVM.UI.HealthBarViewModel>()
                ?? gameObject.AddComponent<MVVM.UI.HealthBarViewModel>();
        if (resourceViewModel == null)
            resourceViewModel =
                gameObject.GetComponent<MVVM.UI.ResourceViewModel>()
                ?? gameObject.AddComponent<MVVM.UI.ResourceViewModel>();
        if (chatViewModel == null)
            chatViewModel =
                gameObject.GetComponent<MVVM.UI.ChatViewModel>()
                ?? gameObject.AddComponent<MVVM.UI.ChatViewModel>();
        if (playerListViewModel == null)
            playerListViewModel =
                gameObject.GetComponent<MVVM.UI.PlayerListViewModel>()
                ?? gameObject.AddComponent<MVVM.UI.PlayerListViewModel>();
        if (notificationViewModel == null)
            notificationViewModel =
                gameObject.GetComponent<MVVM.UI.NotificationViewModel>()
                ?? gameObject.AddComponent<MVVM.UI.NotificationViewModel>();
        if (inventoryViewModel == null)
            inventoryViewModel =
                gameObject.GetComponent<ViewModels.UI.InventoryViewModel>()
                ?? gameObject.AddComponent<ViewModels.UI.InventoryViewModel>();
        if (chestViewModel == null)
            chestViewModel =
                gameObject.GetComponent<ViewModels.UI.ChestViewModel>()
                ?? gameObject.AddComponent<ViewModels.UI.ChestViewModel>();

        // MVVM Views 컴포넌트 확보
        if (gameTimeView == null)
            gameTimeView =
                gameObject.GetComponent<Views.GameTimeView>()
                ?? gameObject.AddComponent<Views.GameTimeView>();
        if (healthBarView == null)
            healthBarView =
                gameObject.GetComponent<Views.HealthBarView>()
                ?? gameObject.AddComponent<Views.HealthBarView>();
        if (resourceView == null)
            resourceView =
                gameObject.GetComponent<Views.ResourceView>()
                ?? gameObject.AddComponent<Views.ResourceView>();
        if (chatView == null)
            chatView =
                gameObject.GetComponent<Views.ChatView>()
                ?? gameObject.AddComponent<Views.ChatView>();
        if (playerListView == null)
            playerListView =
                gameObject.GetComponent<Views.PlayerListView>()
                ?? gameObject.AddComponent<Views.PlayerListView>();
        if (notificationView == null)
            notificationView =
                gameObject.GetComponent<Views.NotificationView>()
                ?? gameObject.AddComponent<Views.NotificationView>();
        if (inventoryView == null)
            inventoryView =
                gameObject.GetComponent<Views.InventoryView>()
                ?? gameObject.AddComponent<Views.InventoryView>();
        if (chestView == null)
            chestView =
                gameObject.GetComponent<Views.ChestView>()
                ?? gameObject.AddComponent<Views.ChestView>();
        if (ggumtleUIView == null)
            ggumtleUIView =
                gameObject.GetComponent<Features.Ggumtle.Views.GgumtleUIView>()
                ?? gameObject.AddComponent<Features.Ggumtle.Views.GgumtleUIView>();
        if (mobileControlsView == null)
        {
            // 씬에서 MobileControlsView 찾기 (별도 GameObject에 배치됨)
            mobileControlsView = FindFirstObjectByType<Features.MobileControls.Views.MobileControlsView>();
            if (mobileControlsView == null)
            {
                Debug.LogError("[UniversalHUDController] 씬에서 MobileControlsView를 찾을 수 없습니다! MobileControlsView GameObject가 씬에 배치되어 있는지 확인하세요.");
            }
            else
            {
                Debug.Log($"[UniversalHUDController] MobileControlsView 찾기 완료: {mobileControlsView.gameObject.name}");
            }
        }
    }

    private void InitializeManagers()
    {
        // MVVM 시스템 초기화
        InitializeMVVMSystem();

        Debug.Log("[UniversalHUDController] 모든 매니저 초기화 완료");
    }

    private void InitializeMVVMSystem()
    {
        // MVVM View-ViewModel 연결 및 초기화
        gameTimeView?.Initialize(_root, gameTimeViewModel);
        healthBarView?.Initialize(_root, healthBarViewModel);
        resourceView?.Initialize(_root, resourceViewModel);
        chatView?.Initialize(_root, chatViewModel);
        playerListView?.Initialize(_root, playerListViewModel);
        notificationView?.Initialize(_root, notificationViewModel);
        inventoryView?.Initialize(_root, inventoryViewModel);

        chestView?.Initialize(_root, chestViewModel);

        // GgumtleUIView 초기화 (View가 직접 ViewModel을 해결)
        if (ggumtleUIView != null)
        {
            Debug.Log("[UniversalHUDController] GgumtleUIView 초기화 시작");
            ggumtleUIView.Initialize(_root);
            Debug.Log("[UniversalHUDController] GgumtleUIView 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[UniversalHUDController] GgumtleUIView가 null입니다");
        }

        // 체력바 강제 표시 (CSS 클래스 충돌 해결)
        healthBarView?.SetHealthBarVisibility(true);

        Debug.Log("[UniversalHUDController] MVVM 시스템 초기화 완료");
    }

    private void SubscribeToEvents()
    {
        HUDEvents.OnGgumtleProgressChanged += OnGgumtleProgressChanged;
        HUDEvents.OnTimeWarning += OnTimeWarning;
        HUDEvents.OnTimeUp += OnTimeUp;
        HUDEvents.OnLightCountChanged += OnLightCountChanged;
        HUDEvents.OnFaintStateChanged += OnFaintStateChanged;
        HUDEvents.OnPlayerDeath += OnPlayerDeath;
        HUDEvents.OnPlayerRevived += OnPlayerRevived;

        // 인벤토리 이벤트 구독
        HUDEvents.OnItemObtained += OnItemObtained;
        HUDEvents.OnItemUsed += OnItemUsed;
        HUDEvents.OnSlotSwapped += OnSlotSwapped;
    }

    private void UnsubscribeFromEvents()
    {
        HUDEvents.OnGgumtleProgressChanged -= OnGgumtleProgressChanged;
        HUDEvents.OnTimeWarning -= OnTimeWarning;
        HUDEvents.OnTimeUp -= OnTimeUp;
        HUDEvents.OnLightCountChanged -= OnLightCountChanged;
        HUDEvents.OnFaintStateChanged -= OnFaintStateChanged;
        HUDEvents.OnPlayerDeath -= OnPlayerDeath;
        HUDEvents.OnPlayerRevived -= OnPlayerRevived;

        // 인벤토리 이벤트 구독 해제
        HUDEvents.OnItemObtained -= OnItemObtained;
        HUDEvents.OnItemUsed -= OnItemUsed;
        HUDEvents.OnSlotSwapped -= OnSlotSwapped;
    }

    private void InitializeSprites()
    {
        SetSpriteToElement("ggumtle", ggumtle);
        SetSpriteToElement("chatIconArea", iconQuickChat);
        SetSpriteToElement("hpIcon", iconHp);

        // MVVM ViewModels에 스프라이트 설정
        resourceViewModel?.SetSprites(lightJelly, backgroundItemSlot);
        playerListViewModel?.SetSprites(
            iconMongingDefault,
            iconMongingFaint,
            iconMongingDead,
            iconMongingEscape
        );
    }

    private void SetSpriteToElement(string elementName, Sprite sprite)
    {
        var element = _root.Q<VisualElement>(elementName);
        if (element != null && sprite != null)
        {
            element.style.backgroundImage = new StyleBackground(sprite);
            element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        }
    }

    // ====== Public API (MVVM ViewModels로 위임) ======
    public void SetTimeRemaining(TimeSpan t)
    {
        if (gameTimeViewModel != null)
            gameTimeViewModel.CurrentTime = t;
    }

    public void SetStatusMessage(string message)
    {
        if (gameTimeViewModel != null)
            gameTimeViewModel.StatusMessage = message;
    }

    public void SetLightCount(int count)
    {
        if (resourceViewModel != null)
            resourceViewModel.LightCount = count;
    }

    public void SetGgumtleProgress(int level, float progress)
    {
        if (gameTimeViewModel != null)
            gameTimeViewModel.SetGgumtleProgress(level, progress);
    }

    public void UpdatePlayer(
        int playerId,
        string nickname,
        string colorTheme,
        string status = "default",
        bool isOnline = true,
        bool isHost = false
    )
    {
        playerListViewModel?.UpdatePlayer(playerId, nickname, colorTheme, status, isOnline, isHost);
    }

    public void ShowBanner(string msg, float seconds = 2.2f)
    {
        notificationViewModel?.ShowNotification(msg, seconds);
    }

    public void SetHealth(int currentHP, int maxHP = 100)
    {
        if (healthBarViewModel != null)
            healthBarViewModel.SetHealth(currentHP, maxHP);
    }

    public void SetFaintState(bool isFainted, float reviveTime = 5f)
    {
        if (healthBarViewModel != null)
        {
            if (isFainted)
            {
                healthBarViewModel.SetHealth(0);
            }
            else
            {
                healthBarViewModel.RevivePlayer();
            }
        }
    }

    // public void SetInteractionUI(
    //     bool show,
    //     string text = "",
    //     float progress = 0f,
    //     bool isCountdown = false
    // )
    // {
    //     // 몽둥이는 상호작용 불가
    //     if (currentPlayerRole == PlayerRole.Mongdung && show)
    //     {
    //         Debug.Log("[UniversalHUD] 몽둥이는 상호작용할 수 없습니다.");
    //         return;
    //     }

    //     if (interactionViewModel != null)
    //     {
    //         if (show)
    //         {
    //             interactionViewModel.ShowInteractionUI(text, Models.InteractionType.Custom);
    //         }
    //         else
    //         {
    //             interactionViewModel.HideInteractionUI();
    //         }
    //     }
    // }

    // public void SetCustomInteraction(string text, float progress = 0f, bool isCountdown = false)
    // {
    //     // 몽둥이는 상호작용 불가
    //     if (currentPlayerRole == PlayerRole.Mongdung)
    //     {
    //         Debug.Log("[UniversalHUD] 몽둥이는 상호작용할 수 없습니다.");
    //         return;
    //     }

    //     if (interactionViewModel != null)
    //     {
    //         interactionViewModel.ShowInteractionUI(text, Models.InteractionType.Custom);
    //     }
    // }

    // 상호작용 가능 여부 체크
    public bool CanInteract()
    {
        return currentPlayerRole == PlayerRole.Mongging;
    }

    public bool UseInventoryItem(int slotNumber) =>
        inventoryViewModel?.UsePlayerSlot(slotNumber) ?? false;

    public bool AddInventoryItem(int slotNumber, int amount = 1) =>
        inventoryViewModel?.AddToPlayerSlot(slotNumber, "", amount) ?? false;

    public void SetInventoryItemCount(int slotNumber, int count) =>
        inventoryViewModel?.AddToPlayerSlot(slotNumber, "", count);

    public int GetInventoryItemCount(int slotNumber) =>
        inventoryViewModel?.GetPlayerSlotCount(slotNumber) ?? 0;

    public void InitializeInventoryForTesting() => resourceView?.InitializeForTesting();

    // 상자 UI 관리 API
    public void ShowChestInventory(InteractableChest chest)
    {
        if (chestViewModel != null && chest != null)
        {
            chestViewModel.ShowChestUI(chest.ChestId);
        }
    }

    public void HideChestBox()
    {
        if (chestViewModel != null)
        {
            chestViewModel.HideChestUI();
        }
    }

    public string GetCurrentChestId() => chestViewModel?.CurrentChestId ?? "";

    // 역할 관리
    public void ChangePlayerRole(PlayerRole newRole)
    {
        if (currentPlayerRole != newRole)
        {
            currentPlayerRole = newRole;
            ConfigureUIForRole(newRole);
            Debug.Log($"[UniversalHUD] 플레이어 역할이 {newRole}로 변경됨");
        }
    }

    private void ConfigureUIForRole(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Mongging:
                resourceView?.SetLightAreaVisibility(true);
                resourceView?.SetSlotVisibility(2, true);
                resourceView?.SetSlotVisibility(3, true);
                healthBarView?.SetHealthBarVisibility(true);
                chatView?.SetChatIconVisibility(true);
                if (gameTimeViewModel != null)
                    gameTimeViewModel.StatusMessage = "• 꿈 속을 탈출하세요.";
                break;
            case PlayerRole.Mongdung:
                resourceView?.SetLightAreaVisibility(false);
                resourceView?.SetSlotVisibility(2, false);
                resourceView?.SetSlotVisibility(3, false);
                healthBarView?.SetHealthBarVisibility(false);
                chatView?.SetChatIconVisibility(false);
                if (gameTimeViewModel != null)
                    gameTimeViewModel.StatusMessage = "• 몽깅이를 제압해 꿈 속에 가두세요.";
                break;
        }
    }

    public PlayerRole GetCurrentRole() => currentPlayerRole;

    // ====== 입력 시스템 API ======


    // ====== 이벤트 핸들러들 ======
    private void OnGgumtleProgressChanged(int newLevel)
    {
        // 실제 UI 업데이트 (MVVM 방식)
        if (gameTimeView != null)
            gameTimeView.SetGgumtleProgressInternal(newLevel, 1.0f);

        // 배너 메시지
        if (newLevel == 3)
        {
            // 3단계일 때는 특별 메시지 (코루틴으로 순차 표시)
            StartCoroutine(Show3rdStageBanners());
            Debug.Log($"[UniversalHUD] 꿈틀 3단계 완료! 탈출구 개방");
        }
        else
        {
            // 일반 메시지
            notificationViewModel?.ShowNotification("[꿈틀 정화 +1] 맵이 조금 밝아집니다.");
        }

        Debug.Log($"[UniversalHUD] 꿈틀 진행도 변경: {newLevel}단계");
    }

    private IEnumerator Show3rdStageBanners()
    {
        // 첫 번째 배너
        notificationViewModel?.ShowNotification("[꿈틀 정화 +1] 맵이 조금 밝아집니다.");

        // 3초 대기 (첫 번째 배너가 표시되는 시간)
        yield return new WaitForSeconds(3f);

        // 두 번째 배너 (탈출구 메시지)
        notificationViewModel?.ShowNotification("탈출구가 열렸습니다. 어서 나가세요!");

        Debug.Log("[UniversalHUD] 3단계 배너 순차 표시 완료");
    }

    private void OnLightCountChanged(int newCount)
    {
        // 실제 UI 업데이트 (MVVM 방식)
        resourceView?.UpdateFromFeedingInventory();

        // 배너 메시지
        notificationViewModel?.ShowNotification($"[빛 획득] 빛을 {newCount}개 얻었습니다!");
        Debug.Log($"[UniversalHUD] 빛 개수 변경: {newCount}");
    }

    private void OnFaintStateChanged(bool isFainted)
    {
        if (isFainted)
        {
            // UI 업데이트 (MVVM 방식)
            if (healthBarViewModel != null)
                healthBarViewModel.SetHealth(0);
            notificationViewModel?.ShowNotification(
                "기절! 제한 시간 내 동료 몽깅이의 도움이 필요합니다.",
                3f
            );

            // 기절 상호작용 트리거
            HUDEvents.TriggerFaintInteraction(5f);
        }
        else
        {
            // 기절 해제 시 UI 업데이트 (MVVM 방식)
            if (healthBarViewModel != null)
                healthBarViewModel.RevivePlayer();
        }

        Debug.Log($"[UniversalHUD] 기절 상태 변경: {isFainted}");
    }

    private void OnPlayerDeath()
    {
        notificationViewModel?.ShowNotification("사망하였습니다.");
        Debug.Log("[UniversalHUD] 플레이어 사망");
    }

    private void OnPlayerRevived()
    {
        notificationViewModel?.ShowNotification("부활하였습니다");
        Debug.Log("[UniversalHUD] 플레이어 부활");
    }

    // private void OnInteractionCompleted(Models.InteractionType type, bool success) // InteractionType 삭제로 인해 주석 처리
    // {
    //     if (success)
    //     {
    //         string message = type switch
    //         {
    //             Models.InteractionType.Dig => "땅파기 완료!",
    //             Models.InteractionType.Revive => "구조 완료!",
    //             Models.InteractionType.Feeding => "먹이주기 완료!",
    //             Models.InteractionType.Faint => "시간 만료!",
    //             _ => "상호작용이 완료되었습니다!",
    //         };

    //         notificationViewModel?.ShowNotification(message);
    //     }

    //     Debug.Log($"[UniversalHUD] 상호작용 완료: {type} (성공: {success})");
    // }

    private void OnTimeWarning(int minutes)
    {
        string message = minutes switch
        {
            3 => "남은 3분 안에 탈출하세요.",
            1 => "남은 1분 안에 탈출하세요.",
            _ => $"남은 {minutes}분 안에 탈출하세요.",
        };

        notificationViewModel?.ShowNotification(message);
        Debug.Log($"[UniversalHUD] 시간 경고: {minutes}분");
    }

    private void OnTimeUp()
    {
        notificationViewModel?.ShowNotification("사망하였습니다.");
        Debug.Log("[UniversalHUD] 시간 종료");
    }

    // ====== 인벤토리 이벤트 핸들러들 ======
    private void OnItemObtained(string itemName, int quantity)
    {
        notificationViewModel?.ShowNotification(
            $"[아이템 획득] {itemName} {quantity}개를 얻었습니다!"
        );
        Debug.Log($"[UniversalHUD] 아이템 획득: {itemName} x{quantity}");
    }

    private void OnItemUsed(string itemName, int quantity)
    {
        notificationViewModel?.ShowNotification($"[아이템 사용] {itemName} 사용");
        Debug.Log($"[UniversalHUD] 아이템 사용: {itemName} x{quantity}");
    }

    private void OnSlotSwapped(int slot1, int slot2)
    {
        Debug.Log($"[UniversalHUD] 슬롯 교체: {slot1} ↔ {slot2}");
    }
}
