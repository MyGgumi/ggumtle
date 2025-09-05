using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 내 모든 상호작용을 관리하는 중앙 매니저
/// - 거리 기반 상호작용 버튼 표시/숨김
/// - 범용적인 상호작용 시스템
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    [Header("플레이어 참조")]
    public ThirdPersonController playerController;
    public StarterAssetsInputs playerInput;

    [Header("모바일 UI")]
    public Button mobileInteractionButton;

    // 상호작용 상태
    private bool isInteracting = false;
    private IInteractable currentNearbyInteractable; // 현재 범위 내 상호작용 가능한 객체
    private List<IInteractable> currentInteractingObjects = new List<IInteractable>(); // 현재 상호작용 중인 객체들
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
        UpdateNearbyInteractables();
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
    /// 상호작용 시작
    /// </summary>
    public void BeginInteraction(IInteractable interactable)
    {
        if (!currentInteractingObjects.Contains(interactable))
        {
            currentInteractingObjects.Add(interactable);
        }
        isInteracting = currentInteractingObjects.Count > 0;
        Debug.Log($"[InteractionManager] 상호작용 시작: {interactable?.GetInteractableName()}");
    }

    /// <summary>
    /// 특정 상호작용 종료
    /// </summary>
    public void EndInteraction(IInteractable interactable)
    {
        if (currentInteractingObjects.Contains(interactable))
        {
            currentInteractingObjects.Remove(interactable);
            Debug.Log(
                $"[InteractionManager] 개별 상호작용 종료: {interactable?.GetInteractableName()}"
            );
        }

        isInteracting = currentInteractingObjects.Count > 0;

        // 상호작용 중인 객체가 없으면 UI도 정리
        if (!isInteracting)
        {
            UIManager.Instance?.CloseAllOverlays();
            UpdateInteractionButtonVisibility();
        }
    }

    /// <summary>
    /// 현재 모든 상호작용 종료
    /// </summary>
    public void EndCurrentInteraction()
    {
        Debug.Log($"[InteractionManager] 모든 상호작용 종료");

        // 모든 상호작용 중인 객체들에게 종료 알림
        for (int i = currentInteractingObjects.Count - 1; i >= 0; i--)
        {
            var interactable = currentInteractingObjects[i];
            if (interactable != null)
            {
                interactable.OnInteractionEnd();
            }
        }

        currentInteractingObjects.Clear();
        isInteracting = false;

        // UI 매니저에게 모든 오버레이 닫기 요청
        UIManager.Instance?.CloseAllOverlays();

        // 버튼 상태 즉시 업데이트
        UpdateInteractionButtonVisibility();

        Debug.Log("[InteractionManager] 모든 상호작용 종료 완료");
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
    /// 매 프레임마다 가까운 상호작용 가능한 객체 확인 및 상호작용 상태 업데이트
    /// </summary>
    private void UpdateNearbyInteractables()
    {
        IInteractable nearestInteractable = FindNearestInteractable();

        // 가까운 객체가 변경되었을 때 버튼 상태 업데이트
        if (nearestInteractable != currentNearbyInteractable)
        {
            Debug.Log(
                $"[InteractionManager] 가까운 상호작용 객체 변경: {currentNearbyInteractable?.GetInteractableName() ?? "없음"} -> {nearestInteractable?.GetInteractableName() ?? "없음"}"
            );
            currentNearbyInteractable = nearestInteractable;
            UpdateInteractionButtonVisibility();
        }

        // 상호작용 중인 객체들과의 거리 체크
        CheckInteractionDistance();
    }

    /// <summary>
    /// 상호작용 중인 객체들과의 거리 체크해서 자동 종료
    /// </summary>
    private void CheckInteractionDistance()
    {
        if (!isInteracting || playerController == null)
            return;

        for (int i = currentInteractingObjects.Count - 1; i >= 0; i--)
        {
            var interactable = currentInteractingObjects[i];
            if (interactable != null && interactable.GetTransform() != null)
            {
                float distance = Vector3.Distance(
                    playerController.transform.position,
                    interactable.GetTransform().position
                );

                if (distance > interactable.GetInteractionRange())
                {
                    Debug.Log(
                        $"[InteractionManager] {interactable.GetInteractableName()}에서 멀어져서 상호작용 종료 (거리: {distance:F2}m)"
                    );
                    interactable.OnInteractionEnd();
                    EndInteraction(interactable);
                }
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
            Debug.LogWarning(
                "[InteractionManager] mobileInteractionButton이 null입니다! Inspector에서 VirtualButton_Open을 할당해주세요."
            );
            return;
        }

        // 상호작용 가능한 객체가 근처에 있을 때만 버튼 표시
        bool shouldShow =
            currentNearbyInteractable != null && currentNearbyInteractable.CanInteract();

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
        if (currentNearbyInteractable != null && currentNearbyInteractable.CanInteract())
        {
            Debug.Log(
                $"[InteractionManager] 상호작용 버튼으로 {currentNearbyInteractable.GetInteractableName()} 상호작용"
            );
            currentNearbyInteractable.Interact();
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
    /// 플레이어 근처에서 가장 가까운 상호작용 가능한 객체 찾기
    /// </summary>
    private IInteractable FindNearestInteractable()
    {
        if (playerController == null)
            return null;

        IInteractable[] interactables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IInteractable>()
            .ToArray();

        IInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (IInteractable interactable in interactables)
        {
            if (!interactable.CanInteract() || interactable.GetTransform() == null)
                continue;

            float distance = Vector3.Distance(
                playerController.transform.position,
                interactable.GetTransform().position
            );

            // 상호작용 범위 내에 있는 가장 가까운 객체 찾기
            if (distance <= interactable.GetInteractionRange() && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = interactable;
            }
        }

        return nearest;
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
