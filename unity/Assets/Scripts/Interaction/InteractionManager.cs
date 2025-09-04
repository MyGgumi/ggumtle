using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 내 모든 상호작용을 관리하는 중앙 매니저
/// - 거리 기반 상호작용 버튼 표시/숨김
/// - 상자 열기/닫기 자동화
/// - 플레이어 이동 제어 (현재는 자유 이동 허용)
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    [Header("플레이어 참조")]
    public ThirdPersonController playerController;
    public StarterAssetsInputs playerInput;

    [Header("상호작용 핸들러")]
    public ChestInteractionHandler chestHandler;

    [Header("모바일 UI")]
    [Tooltip("MobileCanvas의 VirtualButton_Open을 할당하세요")]
    public Button mobileInteractionButton;

    // 상호작용 상태
    private bool isInteracting = false;
    private InteractableChest currentNearbyChest;      // 현재 범위 내에 있는 가장 가까운 상자
    private InteractableChest currentInteractingChest; // 현재 열려있는 상자 (UI가 표시된 상자)

    #region Unity 생명주기
    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeComponents();
        SetupMobileUI();
        SetPlayerMovement(true);
    }

    void Update()
    {
        // 매 프레임마다 상호작용 상태 업데이트
        UpdateNearbyChest();
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 필요한 컴포넌트들을 자동으로 찾아서 할당
    /// </summary>
    private void InitializeComponents()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<ThirdPersonController>();

        if (playerInput == null)
            playerInput = FindFirstObjectByType<StarterAssetsInputs>();

        if (chestHandler == null)
            chestHandler = GetComponent<ChestInteractionHandler>();
    }

    /// <summary>
    /// 모바일 UI 버튼 설정
    /// </summary>
    private void SetupMobileUI()
    {
        if (mobileInteractionButton != null)
        {
            mobileInteractionButton.gameObject.SetActive(false);
            mobileInteractionButton.onClick.AddListener(OnInteractionButtonPressed);
        }
    }
    #endregion

    #region 상호작용 제어
    /// <summary>
    /// 상호작용 시작 (상자 열기 등)
    /// </summary>
    public void BeginInteraction()
    {
        isInteracting = true;
        // 플레이어 이동은 자유롭게 허용
    }

    /// <summary>
    /// 현재 상호작용 종료 (UI 닫기, 상자 닫기)
    /// </summary>
    public void EndCurrentInteraction()
    {
        Debug.Log($"[InteractionManager] 상호작용 종료: {currentInteractingChest?.chestName ?? "null"}");
        
        isInteracting = false;
        
        // 상자 닫기
        if (currentInteractingChest != null)
        {
            Debug.Log($"[InteractionManager] {currentInteractingChest.chestName} 상자 닫기 전 - isOpen: {currentInteractingChest.isOpen}");
            currentInteractingChest.CloseChest();
            Debug.Log($"[InteractionManager] {currentInteractingChest.chestName} 상자 닫기 후 - isOpen: {currentInteractingChest.isOpen}");
        }
        
        currentInteractingChest = null;

        // UI 매니저에게 모든 오버레이 닫기 요청
        UIManager.Instance?.CloseAllOverlays();
        
        // 버튼 상태 즉시 업데이트
        UpdateInteractionButtonVisibility();
        
        Debug.Log("[InteractionManager] 상호작용 종료 완료");
    }

    /// <summary>
    /// 현재 상호작용 중인지 확인
    /// </summary>
    public bool IsInteracting()
    {
        return isInteracting;
    }
    #endregion

    #region 상호작용 업데이트
    /// <summary>
    /// 매 프레임마다 가까운 상자 확인 및 상호작용 상태 업데이트
    /// </summary>
    private void UpdateNearbyChest()
    {
        InteractableChest nearestChest = FindNearestInteractableChest();
        
        // 디버그: 거리 측정 (1초마다)
        LogDistanceDebugInfo();
        
        // 가까운 상자가 변경되었을 때 버튼 상태 업데이트
        if (nearestChest != currentNearbyChest)
        {
            Debug.Log($"[InteractionManager] 가까운 상자 변경: {currentNearbyChest?.chestName ?? "없음"} -> {nearestChest?.chestName ?? "없음"}");
            currentNearbyChest = nearestChest;
            UpdateInteractionButtonVisibility();
        }

        // 상호작용 중인 상자에서 멀어지면 자동 종료
        CheckInteractionDistance();
    }

    /// <summary>
    /// 디버그용: 상자들과의 거리 정보 출력 (1초마다)
    /// </summary>
    private void LogDistanceDebugInfo()
    {
        if (Time.time % 1f < 0.02f && playerController != null)
        {
            InteractableChest[] chests = FindObjectsByType<InteractableChest>(FindObjectsSortMode.None);
            foreach (InteractableChest chest in chests)
            {
                float distance = Vector3.Distance(playerController.transform.position, chest.transform.position);
                Debug.Log($"[거리측정] {chest.chestName}까지 거리: {distance:F2}m, 상호작용 범위: {chest.interactionRange}m, 범위내: {distance <= chest.interactionRange}");
            }
        }
    }

    /// <summary>
    /// 상호작용 중인 상자와의 거리 체크해서 자동 종료
    /// </summary>
    private void CheckInteractionDistance()
    {
        if (isInteracting && currentInteractingChest != null && playerController != null)
        {
            float distanceToInteractingChest = Vector3.Distance(
                playerController.transform.position, 
                currentInteractingChest.transform.position
            );
            
            Debug.Log($"[상호작용체크] {currentInteractingChest.chestName}와의 거리: {distanceToInteractingChest:F2}m, 범위: {currentInteractingChest.interactionRange}m");
            
            if (distanceToInteractingChest > currentInteractingChest.interactionRange)
            {
                Debug.Log($"[InteractionManager] {currentInteractingChest.chestName}에서 멀어져서 상호작용 종료 (거리: {distanceToInteractingChest:F2}m)");
                EndCurrentInteraction();
            }
        }
    }
    #endregion

    #region UI 버튼 제어
    /// <summary>
    /// 상호작용 버튼의 표시/숨김 상태 업데이트
    /// </summary>
    private void UpdateInteractionButtonVisibility()
    {
        if (mobileInteractionButton == null)
        {
            Debug.LogWarning("[InteractionManager] mobileInteractionButton이 null입니다! Inspector에서 VirtualButton_Open을 할당해주세요.");
            return;
        }

        // 닫힌 상자가 근처에 있을 때만 버튼 표시
        bool shouldShow = currentNearbyChest != null && !currentNearbyChest.isOpen;
        
        Debug.Log($"[버튼체크] currentNearbyChest: {currentNearbyChest?.chestName ?? "null"}, isOpen: {currentNearbyChest?.isOpen}, shouldShow: {shouldShow}");
        
        if (mobileInteractionButton.gameObject.activeInHierarchy != shouldShow)
        {
            mobileInteractionButton.gameObject.SetActive(shouldShow);
            Debug.Log($"[InteractionManager] 상호작용 버튼 {(shouldShow ? "표시" : "숨김")}");
        }
    }

    /// <summary>
    /// 모바일 상호작용 버튼이 눌렸을 때 호출
    /// </summary>
    private void OnInteractionButtonPressed()
    {
        if (currentNearbyChest != null)
        {
            Debug.Log($"[InteractionManager] 상호작용 버튼으로 {currentNearbyChest.chestName} 열기");
            currentInteractingChest = currentNearbyChest; // 상호작용 중인 상자로 설정
            currentNearbyChest.TryInteract();
        }
    }
    #endregion

    #region 유틸리티 메서드
    /// <summary>
    /// 플레이어 이동 활성화/비활성화 (현재는 사용하지 않음)
    /// </summary>
    private void SetPlayerMovement(bool enabled)
    {
        if (playerController != null)
        {
            playerController.SetMovementEnabled(enabled);
        }

        if (playerInput != null)
        {
            playerInput.enabled = enabled;
        }
    }

    /// <summary>
    /// 플레이어 근처에서 가장 가까운 닫힌 상자 찾기
    /// </summary>
    private InteractableChest FindNearestInteractableChest()
    {
        if (playerController == null)
            return null;

        InteractableChest[] chests = FindObjectsByType<InteractableChest>(FindObjectsSortMode.None);
        InteractableChest nearestChest = null;
        float nearestDistance = float.MaxValue;

        foreach (InteractableChest chest in chests)
        {
            // 이미 열린 상자는 제외
            if (chest.isOpen)
                continue;

            float distance = Vector3.Distance(playerController.transform.position, chest.transform.position);
            
            // 상호작용 범위 내에 있는 가장 가까운 상자 찾기
            if (distance <= chest.interactionRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestChest = chest;
            }
        }

        return nearestChest;
    }
    #endregion

    #region 공개 메서드 (다른 스크립트에서 호출)
    /// <summary>
    /// 모바일용 닫기 버튼에서 호출
    /// </summary>
    public void OnCloseButtonPressed()
    {
        EndCurrentInteraction();
    }

    /// <summary>
    /// 레거시 메서드 - UICanvasControllerInput에서 호출
    /// </summary>
    public void TryNearestInteraction()
    {
        OnInteractionButtonPressed();
    }
    #endregion
}
