using StarterAssets;
using UnityEngine;

// 단순화된 상호작용 매니저
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    [Header("플레이어 참조")]
    public ThirdPersonController playerController;
    public StarterAssetsInputs playerInput;

    [Header("상호작용 핸들러들")]
    public ChestInteractionHandler chestHandler;

    private bool isInteracting = false;

    void Awake()
    {
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
        // 자동으로 컴포넌트 찾기
        if (playerController == null)
            playerController = FindFirstObjectByType<ThirdPersonController>();

        if (playerInput == null)
            playerInput = FindFirstObjectByType<StarterAssetsInputs>();

        // 핸들러들 초기화
        if (chestHandler == null)
            chestHandler = GetComponent<ChestInteractionHandler>();

        SetPlayerMovement(true);
    }

    public void BeginInteraction()
    {
        isInteracting = true;
        SetPlayerMovement(false);
    }

    public void EndCurrentInteraction()
    {
        isInteracting = false;
        SetPlayerMovement(true);

        // UI 매니저에게 모든 오버레이 닫기 요청
        UIManager.Instance?.CloseAllOverlays();
    }

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

    public bool IsInteracting()
    {
        return isInteracting;
    }

    // 모바일용 닫기 버튼
    public void OnCloseButtonPressed()
    {
        EndCurrentInteraction();
    }

    // 모바일용 상호작용 버튼
    public void TryNearestInteraction()
    {
        Debug.Log("[InteractionManager] TryNearestInteraction 호출됨");
        
        // 가장 가까운 상호작용 가능한 객체 찾기
        InteractableChest nearestChest = FindNearestInteractableChest();
        if (nearestChest != null)
        {
            Debug.Log($"[InteractionManager] 가장 가까운 상자 찾음: {nearestChest.chestName}");
            nearestChest.TryInteract();
        }
        else
        {
            Debug.LogWarning("[InteractionManager] 상호작용 가능한 상자를 찾을 수 없습니다");
        }
    }

    private InteractableChest FindNearestInteractableChest()
    {
        if (playerController == null)
        {
            Debug.LogWarning("[InteractionManager] playerController가 null입니다");
            return null;
        }

        InteractableChest[] chests = FindObjectsOfType<InteractableChest>();
        Debug.Log($"[InteractionManager] 씬에서 {chests.Length}개의 상자를 찾음");

        InteractableChest nearestChest = null;
        float nearestDistance = float.MaxValue;

        foreach (InteractableChest chest in chests)
        {
            if (chest.isOpen)
            {
                Debug.Log($"[InteractionManager] {chest.chestName}은 이미 열려있음");
                continue;
            }

            float distance = Vector3.Distance(playerController.transform.position, chest.transform.position);
            Debug.Log($"[InteractionManager] {chest.chestName}까지 거리: {distance:F2}, 상호작용 범위: {chest.interactionRange}");
            
            if (distance <= chest.interactionRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestChest = chest;
            }
        }

        return nearestChest;
    }
}
