using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using UnityEngine;

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

    [Header("UI Toolkit 연결")]
    private PlayerActionManager playerActionManager;

    // 홀드 상호작용 상태
    private bool isHoldingInteraction = false;
    private IInteractable currentHoldInteractable;

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

            // 이전 홀드 상태가 있으면 종료
            if (isHoldingInteraction && currentHoldInteractable != null)
            {
                Debug.Log(
                    $"[InteractionManager] 객체 전환으로 인한 홀드 종료: {currentHoldInteractable.GetInteractableName()}"
                );
                OnInteractionHoldEnd();
            }

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

                    // 홀드 중인 객체가 범위를 벗어나면 홀드도 종료
                    if (isHoldingInteraction && currentHoldInteractable == interactable)
                    {
                        Debug.Log(
                            $"[InteractionManager] 범위 이탈로 인한 홀드 종료: {interactable.GetInteractableName()}"
                        );
                        OnInteractionHoldEnd();
                    }

                    interactable.OnInteractionEnd();
                    EndInteraction(interactable);
                }
            }
        }
    }
    #endregion

    #region 홀드 상호작용 제어
    /// <summary>
    /// 상호작용 홀드 시작
    /// </summary>
    public void OnInteractionHoldStart()
    {
        if (currentNearbyInteractable != null && currentNearbyInteractable.CanInteract())
        {
            Debug.Log(
                $"[InteractionManager] 홀드 시작: {currentNearbyInteractable.GetInteractableName()}"
            );

            isHoldingInteraction = true;
            currentHoldInteractable = currentNearbyInteractable;

            // 꿈틀이 타입인지 확인하고 홀드 시작 알림
            if (currentHoldInteractable is InteractableGgumtle ggumtle)
            {
                ggumtle.OnInteractionStart();
            }
            else
            {
                // 일반 상호작용 객체는 기존 방식으로 처리
                currentHoldInteractable.Interact();
            }
        }
    }

    /// <summary>
    /// 상호작용 홀드 종료
    /// </summary>
    public void OnInteractionHoldEnd()
    {
        if (isHoldingInteraction && currentHoldInteractable != null)
        {
            Debug.Log(
                $"[InteractionManager] 홀드 종료: {currentHoldInteractable.GetInteractableName()}"
            );

            // 꿈틀이 타입인지 확인하고 홀드 해제 알림
            if (currentHoldInteractable is InteractableGgumtle ggumtle)
            {
                ggumtle.OnInteractionRelease();
            }
        }

        // 홀드 상태는 항상 초기화 (상태 불일치 방지)
        isHoldingInteraction = false;
        currentHoldInteractable = null;

        Debug.Log("[InteractionManager] 홀드 상태 완전 초기화");
    }

    /// <summary>
    /// 현재 홀드 중인지 확인
    /// </summary>
    public bool IsHolding()
    {
        return isHoldingInteraction;
    }

    /// <summary>
    /// 현재 홀드 중인 객체 반환
    /// </summary>
    public IInteractable GetCurrentHoldInteractable()
    {
        return currentHoldInteractable;
    }
    #endregion

    #region UI 버튼 제어
    /// <summary>
    /// 상호작용 버튼의 표시/숨김 상태 업데이트
    /// </summary>
    private void UpdateInteractionButtonVisibility()
    {
        // 상호작용 가능한 객체가 근처에 있을 때만 버튼 표시
        bool shouldShow =
            currentNearbyInteractable != null && currentNearbyInteractable.CanInteract();

        // UI Toolkit 버튼 제어
        if (playerActionManager != null)
        {
            playerActionManager.UpdateInteractionButtonVisibility(shouldShow);
            Debug.Log(
                $"[InteractionManager] UI Toolkit 상호작용 버튼 {(shouldShow ? "표시" : "숨김")}"
            );
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
    /// UI Toolkit PlayerActionManager 연결
    /// </summary>
    public void SetPlayerActionManager(PlayerActionManager actionManager)
    {
        playerActionManager = actionManager;
        Debug.Log("[InteractionManager] PlayerActionManager 연결 완료");

        // 즉시 버튼 상태 업데이트
        UpdateInteractionButtonVisibility();
    }

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
        if (currentNearbyInteractable != null && currentNearbyInteractable.CanInteract())
        {
            Debug.Log(
                $"[InteractionManager] 레거시 상호작용으로 {currentNearbyInteractable.GetInteractableName()} 상호작용"
            );
            currentNearbyInteractable.Interact();
        }
    }
    #endregion
}
