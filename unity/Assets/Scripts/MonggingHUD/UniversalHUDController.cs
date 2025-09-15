using System;
using System.Collections;
using StarterAssets;
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
    public StarterAssetsInputs starterAssetsInputs;

    private UIDocument _doc;
    private VisualElement _root;

    [Header("Manager Components")]
    public GameTimeManager gameTimeManager;
    public PlayerListManager playerListManager;
    public ResourceManager resourceManager;
    public HealthBarManager healthBarManager;
    public HUDInteractionManager interactionManager;
    public NotificationBannerManager notificationManager;
    public ChatManager chatManager;
    public PlayerInventoryManager playerInventoryManager;
    public ChestBoxUIManager chestBoxUIManager;

    [Header("Mobile Control Managers")]
    public MobileInputManager mobileInputManager;
    public PlayerActionManager playerActionManager;

    void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        EnsureManagerComponents();
        ConfigureUIForRole(currentPlayerRole);
    }

    void Start()
    {
        InitializeManagers();
        InitializeSprites();

        // 매니저 초기화 후 이벤트 구독
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void EnsureManagerComponents()
    {
        if (gameTimeManager == null)
            gameTimeManager =
                gameObject.GetComponent<GameTimeManager>()
                ?? gameObject.AddComponent<GameTimeManager>();
        if (playerListManager == null)
            playerListManager =
                gameObject.GetComponent<PlayerListManager>()
                ?? gameObject.AddComponent<PlayerListManager>();
        if (resourceManager == null)
            resourceManager =
                gameObject.GetComponent<ResourceManager>()
                ?? gameObject.AddComponent<ResourceManager>();
        if (healthBarManager == null)
            healthBarManager =
                gameObject.GetComponent<HealthBarManager>()
                ?? gameObject.AddComponent<HealthBarManager>();
        if (interactionManager == null)
            interactionManager =
                gameObject.GetComponent<HUDInteractionManager>()
                ?? gameObject.AddComponent<HUDInteractionManager>();
        if (notificationManager == null)
            notificationManager =
                gameObject.GetComponent<NotificationBannerManager>()
                ?? gameObject.AddComponent<NotificationBannerManager>();
        if (chatManager == null)
            chatManager =
                gameObject.GetComponent<ChatManager>() ?? gameObject.AddComponent<ChatManager>();
        if (playerInventoryManager == null)
            playerInventoryManager =
                gameObject.GetComponent<PlayerInventoryManager>()
                ?? gameObject.AddComponent<PlayerInventoryManager>();
        if (chestBoxUIManager == null)
            chestBoxUIManager =
                gameObject.GetComponent<ChestBoxUIManager>()
                ?? gameObject.AddComponent<ChestBoxUIManager>();
        if (mobileInputManager == null)
            mobileInputManager =
                gameObject.GetComponent<MobileInputManager>()
                ?? gameObject.AddComponent<MobileInputManager>();
        if (playerActionManager == null)
            playerActionManager =
                gameObject.GetComponent<PlayerActionManager>()
                ?? gameObject.AddComponent<PlayerActionManager>();
    }

    private void InitializeManagers()
    {
        gameTimeManager?.Initialize(_root);
        playerListManager?.Initialize(_root);
        resourceManager?.Initialize(_root);
        healthBarManager?.Initialize(_root);
        interactionManager?.Initialize(_root);
        notificationManager?.Initialize(_root);
        chatManager?.Initialize(_root);
        playerInventoryManager?.Initialize(_root);
        chestBoxUIManager?.Initialize(_root);

        // 모바일 컨트롤 매니저 초기화
        mobileInputManager?.Initialize(_root);
        playerActionManager?.Initialize(_root);

        // StarterAssetsInputs 연결
        if (mobileInputManager != null)
        {
            mobileInputManager.starterAssetsInputs = starterAssetsInputs;
        }
        if (playerActionManager != null)
            playerActionManager.starterAssetsInputs = starterAssetsInputs;

        // InteractionManager와 PlayerActionManager 연결
        if (playerActionManager != null && InteractionManager.Instance != null)
        {
            InteractionManager.Instance.SetPlayerActionManager(playerActionManager);
            Debug.Log(
                "[UniversalHUDController] InteractionManager와 PlayerActionManager 연결 완료"
            );
        }
        else if (InteractionManager.Instance == null)
        {
            Debug.LogWarning(
                "[UniversalHUDController] InteractionManager.Instance를 찾을 수 없습니다!"
            );
        }

        // 체력바 강제 표시 (CSS 클래스 충돌 해결)
        healthBarManager?.SetHealthBarVisibility(true);

        Debug.Log("[UniversalHUDController] 모든 매니저 초기화 완료");
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
        HUDEvents.OnInteractionCompleted += OnInteractionCompleted;

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
        HUDEvents.OnInteractionCompleted -= OnInteractionCompleted;

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

        resourceManager?.SetSprites(lightJelly, backgroundItemSlot);
        playerListManager?.SetSprites(
            iconMongingDefault,
            iconMongingFaint,
            iconMongingDead,
            iconMongingEscape
        );
        playerInventoryManager?.SetSprites(backgroundItemSlot);
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

    // ====== Public API (각 매니저로 위임) ======
    public void SetTimeRemaining(TimeSpan t) => gameTimeManager?.SetTimeRemaining(t);

    public void SetStatusMessage(string message) => gameTimeManager?.SetStatusMessage(message);

    public void SetLightCount(int count) => resourceManager?.SetLightCount(count);

    public void SetGgumtleProgress(int level, float progress) =>
        gameTimeManager?.SetGgumtleProgress(level, progress);

    public void UpdatePlayer(
        int playerId,
        string nickname,
        string colorTheme,
        string status = "default",
        bool isOnline = true,
        bool isHost = false
    )
    {
        playerListManager?.UpdatePlayer(playerId, nickname, colorTheme, status, isOnline, isHost);
    }

    public void ShowBanner(string msg, float seconds = 2.2f) =>
        notificationManager?.ShowBanner(msg, seconds);

    public void SetHealth(int currentHP, int maxHP = 100) =>
        healthBarManager?.SetHealth(currentHP, maxHP);

    public void SetFaintState(bool isFainted, float reviveTime = 5f) =>
        healthBarManager?.SetFaintState(isFainted, reviveTime);

    public void SetInteractionUI(
        bool show,
        string text = "",
        float progress = 0f,
        bool isCountdown = false
    )
    {
        // 몽둥이는 상호작용 불가
        if (currentPlayerRole == PlayerRole.Mongdung && show)
        {
            Debug.Log("[UniversalHUD] 몽둥이는 상호작용할 수 없습니다.");
            return;
        }

        interactionManager?.SetInteractionUI(show, text, progress, isCountdown);
    }

    public void SetCustomInteraction(string text, float progress = 0f, bool isCountdown = false)
    {
        // 몽둥이는 상호작용 불가
        if (currentPlayerRole == PlayerRole.Mongdung)
        {
            Debug.Log("[UniversalHUD] 몽둥이는 상호작용할 수 없습니다.");
            return;
        }

        interactionManager?.SetCustomInteraction(text, progress, isCountdown);
    }

    // 상호작용 가능 여부 체크
    public bool CanInteract()
    {
        return currentPlayerRole == PlayerRole.Mongging;
    }

    public bool UseInventoryItem(int slotNumber) =>
        resourceManager?.UseInventoryItem(slotNumber) ?? false;

    public bool AddInventoryItem(int slotNumber, int amount = 1) =>
        resourceManager?.AddInventoryItem(slotNumber, amount) ?? false;

    public void SetInventoryItemCount(int slotNumber, int count) =>
        resourceManager?.SetInventoryItemCount(slotNumber, count);

    public int GetInventoryItemCount(int slotNumber) =>
        resourceManager?.GetInventoryItemCount(slotNumber) ?? 0;

    public void InitializeInventoryForTesting() => resourceManager?.InitializeForTesting();

    // 상자 UI 관리 API
    public void ShowChestInventory(InteractableChest chest) =>
        chestBoxUIManager?.ShowChestInventory(chest);

    public void HideChestBox() => chestBoxUIManager?.HideChestBox();

    public string GetCurrentChestId() => chestBoxUIManager?.GetCurrentChestId() ?? "";

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
                resourceManager?.SetLightAreaVisibility(true);
                resourceManager?.SetSlotVisibility(2, true);
                resourceManager?.SetSlotVisibility(3, true);
                healthBarManager?.SetHealthBarVisibility(true);
                chatManager?.SetChatIconVisibility(true);
                gameTimeManager?.SetStatusMessage("• 꿈 속을 탈출하세요.");
                break;
            case PlayerRole.Mongdung:
                resourceManager?.SetLightAreaVisibility(false);
                resourceManager?.SetSlotVisibility(2, false);
                resourceManager?.SetSlotVisibility(3, false);
                healthBarManager?.SetHealthBarVisibility(false);
                chatManager?.SetChatIconVisibility(false);
                gameTimeManager?.SetStatusMessage("• 몽깅이를 제압해 꿈 속에 가두세요.");
                break;
        }
    }

    public PlayerRole GetCurrentRole() => currentPlayerRole;

    // ====== 이벤트 핸들러들 ======
    private void OnGgumtleProgressChanged(int newLevel)
    {
        // 실제 UI 업데이트 (이벤트 발생 안함)
        gameTimeManager?.SetGgumtleProgressInternal(newLevel, 1.0f);

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
            notificationManager?.ShowBanner("[꿈틀 정화 +1] 맵이 조금 밝아집니다.");
        }

        Debug.Log($"[UniversalHUD] 꿈틀 진행도 변경: {newLevel}단계");
    }

    private IEnumerator Show3rdStageBanners()
    {
        // 첫 번째 배너
        notificationManager?.ShowBanner("[꿈틀 정화 +1] 맵이 조금 밝아집니다.");

        // 3초 대기 (첫 번째 배너가 표시되는 시간)
        yield return new WaitForSeconds(3f);

        // 두 번째 배너 (탈출구 메시지)
        notificationManager?.ShowBanner("탈출구가 열렸습니다. 어서 나가세요!");

        Debug.Log("[UniversalHUD] 3단계 배너 순차 표시 완료");
    }

    private void OnLightCountChanged(int newCount)
    {
        // 실제 UI 업데이트 (이벤트 발생 안함)
        resourceManager?.SetLightCountInternal(newCount);

        // 배너 메시지
        notificationManager?.ShowBanner($"[빛 획득] 빛을 {newCount}개 얻었습니다!");
        Debug.Log($"[UniversalHUD] 빛 개수 변경: {newCount}");
    }

    private void OnFaintStateChanged(bool isFainted)
    {
        if (isFainted)
        {
            // UI 업데이트
            healthBarManager?.SetFaintState(true, 5f);
            notificationManager?.ShowBanner(
                "기절! 제한 시간 내 동료 몽깅이의 도움이 필요합니다.",
                3f
            );

            // 기절 상호작용 트리거
            HUDEvents.TriggerFaintInteraction(5f);
        }
        else
        {
            // 기절 해제 시 UI 업데이트
            healthBarManager?.SetFaintState(false, 0f);
            interactionManager?.SetInteractionUI(false);
        }

        Debug.Log($"[UniversalHUD] 기절 상태 변경: {isFainted}");
    }

    private void OnPlayerDeath()
    {
        notificationManager?.ShowBanner("사망하였습니다.");
        Debug.Log("[UniversalHUD] 플레이어 사망");
    }

    private void OnPlayerRevived()
    {
        notificationManager?.ShowBanner("부활하였습니다");
        Debug.Log("[UniversalHUD] 플레이어 부활");
    }

    private void OnInteractionCompleted(InteractionType type, bool success)
    {
        if (success)
        {
            string message = type switch
            {
                InteractionType.Dig => "땅파기 완료!",
                InteractionType.Revive => "구조 완료!",
                InteractionType.Feeding => "먹이주기 완료!",
                InteractionType.Faint => "시간 만료!",
                _ => "상호작용이 완료되었습니다!",
            };

            notificationManager?.ShowBanner(message);
        }

        Debug.Log($"[UniversalHUD] 상호작용 완료: {type} (성공: {success})");
    }

    private void OnTimeWarning(int minutes)
    {
        string message = minutes switch
        {
            3 => "남은 3분 안에 탈출하세요.",
            1 => "남은 1분 안에 탈출하세요.",
            _ => $"남은 {minutes}분 안에 탈출하세요.",
        };

        notificationManager?.ShowBanner(message);
        Debug.Log($"[UniversalHUD] 시간 경고: {minutes}분");
    }

    private void OnTimeUp()
    {
        notificationManager?.ShowBanner("사망하였습니다.");
        Debug.Log("[UniversalHUD] 시간 종료");
    }

    // ====== 인벤토리 이벤트 핸들러들 ======
    private void OnItemObtained(string itemName, int quantity)
    {
        notificationManager?.ShowBanner($"[아이템 획득] {itemName} {quantity}개를 얻었습니다!");
        Debug.Log($"[UniversalHUD] 아이템 획득: {itemName} x{quantity}");
    }

    private void OnItemUsed(string itemName, int quantity)
    {
        notificationManager?.ShowBanner($"[아이템 사용] {itemName} 사용");
        Debug.Log($"[UniversalHUD] 아이템 사용: {itemName} x{quantity}");
    }

    private void OnSlotSwapped(int slot1, int slot2)
    {
        Debug.Log($"[UniversalHUD] 슬롯 교체: {slot1} ↔ {slot2}");
    }
}
