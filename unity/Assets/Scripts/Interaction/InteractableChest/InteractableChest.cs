using System.Collections.Generic;
using UnityEngine;

// 클릭 기반 상자 상호작용
public class InteractableChest : MonoBehaviour, IInteractable
{
    [Header("상자 설정")]
    public string chestName = "상자";
    public string chestId; // 고유 상자 ID (Inspector에서 설정하거나 자동 생성)
    public float interactionRange = 2.0f;
    public bool isOpen = false;

    // 기존 chestItems는 호환성을 위해 유지하되, 실제로는 ChestInventoryManager 사용

    [Header("애니메이션")]
    public Animator chestAnimator;
    public string openAnimationTrigger = "Open";
    public string closeAnimationTrigger = "Close";

    [Header("UI 힌트")]
    public GameObject interactionIcon; // 상호작용 힌트 아이콘 (선택사항)

    [Header("테스트용 더미 아이템")]
    public bool useTestItems = true; // 테스트 아이템 사용 여부
    public Sprite[] testItemSprites; // 테스트용 아이템 스프라이트들

    private ChestInteractionHandler handler;
    private Transform playerTransform;

    void Start()
    {
        Debug.Log(
            $"[InteractableChest] Start() 호출됨 - GameObject: {gameObject.name}, 초기 chestId: '{chestId}'"
        );

        // 상자 ID 자동 생성 (Inspector에서 설정하지 않은 경우)
        if (string.IsNullOrEmpty(chestId))
        {
            chestId = $"Chest_{transform.position.x}_{transform.position.z}_{GetInstanceID()}";
            Debug.Log($"[InteractableChest] 자동 생성된 chestId: {chestId}");
        }
        else
        {
            Debug.Log($"[InteractableChest] Inspector에서 설정된 chestId: {chestId}");
        }

        // 매니저 초기화 대기 후 등록
        StartCoroutine(RegisterChestWhenReady());

        // 핸들러 찾기
        handler = FindFirstObjectByType<ChestInteractionHandler>();

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        if (chestAnimator == null)
            chestAnimator = GetComponent<Animator>();

        // 상호작용 아이콘 초기화
        if (interactionIcon != null)
            interactionIcon.SetActive(!isOpen);

        // 서버 시스템을 사용하므로 로컬 테스트 아이템 생성 비활성화
        // 테스트 아이템 초기화
        // if (useTestItems && GetChestItemCount() == 0)
        // {
        //     InitializeTestItems();
        // }
    }

    void Update()
    {
        // 상호작용 아이콘 표시 여부 결정 (선택사항)
        UpdateInteractionIcon();
    }

    private void UpdateInteractionIcon()
    {
        if (interactionIcon == null || playerTransform == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool shouldShow = !isOpen && distance <= interactionRange;

        if (interactionIcon.activeInHierarchy != shouldShow)
        {
            interactionIcon.SetActive(shouldShow);
        }
    }

    // UI 버튼에서 호출되는 상호작용 메서드 (레거시)
    public void TryInteract()
    {
        Interact();
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null)
            return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= interactionRange;
    }

    public void OpenChest()
    {
        if (isOpen)
            return;

        isOpen = true;

        // 상호작용 아이콘 숨기기
        if (interactionIcon != null)
            interactionIcon.SetActive(false);

        // 애니메이션 재생
        if (chestAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
        {
            chestAnimator.SetTrigger(openAnimationTrigger);
        }

        Debug.Log($"상자 열림: {chestName}");
    }

    public void CloseChest()
    {
        Debug.Log($"[InteractableChest] CloseChest 호출됨 - {chestName}, 현재 isOpen: {isOpen}");

        if (!isOpen)
        {
            Debug.Log($"[InteractableChest] {chestName}는 이미 닫혀있음");
            return;
        }

        isOpen = false;
        Debug.Log($"[InteractableChest] {chestName} isOpen을 false로 설정함");

        // 상호작용 아이콘 다시 표시 (거리 체크는 Update에서)
        if (interactionIcon != null && IsPlayerInRange())
            interactionIcon.SetActive(true);

        // 애니메이션 재생
        if (chestAnimator != null && !string.IsNullOrEmpty(closeAnimationTrigger))
        {
            chestAnimator.SetTrigger(closeAnimationTrigger);
        }

        // 핸들러에게 닫기 알림
        if (handler != null)
        {
            handler.HandleChestClose();
        }

        // InteractionManager에게 상호작용 종료 알림
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.EndInteraction(this);
        }

        Debug.Log($"[InteractableChest] 상자 닫힘 완료: {chestName}, isOpen: {isOpen}");
    }

    // 아이템 관리 메서드들 (ChestInventoryManager 연동)
    public void AddItem(ChestItem item)
    {
        if (ChestInventoryManager.Instance != null)
        {
            ChestInventoryManager.Instance.AddItemToChest(chestId, item);
        }
    }

    public void RemoveItem(ChestItem item)
    {
        // 특정 아이템 제거는 인덱스 기반으로 처리
        var items = GetChestItems();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemName == item.itemName)
            {
                RemoveItemAt(i);
                break;
            }
        }
    }

    public void RemoveItemAt(int index)
    {
        if (ChestInventoryManager.Instance != null)
        {
            ChestInventoryManager.Instance.RemoveItemFromChest(chestId, index);
        }
    }

    /// <summary>
    /// ChestInventoryManager에서 현재 상자의 아이템 리스트 조회
    /// </summary>
    public List<ChestItem> GetChestItems()
    {
        if (ChestInventoryManager.Instance != null)
        {
            return ChestInventoryManager.Instance.GetChestItems(chestId);
        }
        return new List<ChestItem>();
    }

    /// <summary>
    /// 현재 상자의 아이템 개수 반환
    /// </summary>
    public int GetChestItemCount()
    {
        return GetChestItems().Count;
    }

    /// <summary>
    /// 호환성을 위한 chestItems 프로퍼티 (읽기 전용)
    /// </summary>
    public List<ChestItem> chestItems => GetChestItems();

    /// <summary>
    /// 테스트용 더미 아이템 초기화 (새로운 시스템 사용)
    /// </summary>
    private void InitializeTestItems()
    {
        Debug.Log($"[InteractableChest] 테스트 더미 아이템 초기화: {chestId}");

        if (GlobalItemManager.Instance == null || ChestInventoryManager.Instance == null)
        {
            Debug.LogWarning(
                "[InteractableChest] GlobalItemManager 또는 ChestInventoryManager가 없습니다!"
            );
            return;
        }

        // GlobalItemManager를 통해 아이템 생성 (메타데이터 기반)
        var testItemNames = new[]
        {
            "Apple",
            "Stone",
            "Apple",
            "Stone",
            "Apple",
            "Stone",
            "Apple",
            "Stone",
            "Apple",
        };
        var testQuantities = new[] { 4, 4, 3, 3, 2, 2, 1, 1, 5 };

        for (int i = 0; i < testItemNames.Length; i++)
        {
            // ItemDatabase에 해당 아이템이 있는지 확인 후 생성
            var chestItem = GlobalItemManager.Instance.CreateChestItem(
                testItemNames[i],
                testQuantities[i]
            );
            if (chestItem != null)
            {
                AddItem(chestItem);
            }
            else
            {
                // ItemDatabase에 없으면 임시로 기존 방식으로 생성
                var fallbackItem = new ChestItem(
                    testItemNames[i],
                    GetTestSprite(i),
                    testQuantities[i],
                    $"{testItemNames[i]} 설명"
                );
                AddItem(fallbackItem);
                Debug.LogWarning(
                    $"[InteractableChest] ItemDatabase에 '{testItemNames[i]}' 없음, 임시 아이템 생성"
                );
            }
        }

        Debug.Log(
            $"[InteractableChest] {testItemNames.Length}개 테스트 아이템 초기화 완료 (9개 슬롯 모두 채움)"
        );
    }

    /// <summary>
    /// 테스트용 스프라이트 가져오기 (인덱스 범위 체크 포함)
    /// </summary>
    private Sprite GetTestSprite(int index)
    {
        if (testItemSprites != null && index >= 0 && index < testItemSprites.Length)
        {
            return testItemSprites[index];
        }
        return null; // 스프라이트가 없으면 null 반환
    }

    // Gizmos로 상호작용 범위 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    #region IInteractable 구현
    /// <summary>
    /// 상호작용 가능한지 확인
    /// </summary>
    public bool CanInteract()
    {
        // 플레이어가 범위 안에 있으면 상호작용 가능 (열린 상자는 닫기 가능)
        return IsPlayerInRange();
    }

    /// <summary>
    /// 상호작용 실행
    /// </summary>
    public void Interact()
    {
        // 이미 다른 상호작용 중이면 무시
        if (InteractionManager.Instance != null && InteractionManager.Instance.IsInteracting())
        {
            Debug.Log("다른 상호작용이 진행 중입니다.");
            return;
        }

        // 상호작용 가능 체크
        if (!CanInteract())
        {
            Debug.Log("너무 멀어서 상자를 조작할 수 없습니다.");
            return;
        }

        // 상호작용 시작 알림
        InteractionManager.Instance?.BeginInteraction(this);

        if (isOpen)
        {
            // 상자가 이미 열려있으면 닫기
            Debug.Log($"상자 닫기: {chestName}");
            CloseChest();
            
            // 닫기 핸들러 호출
            if (handler != null)
            {
                handler.HandleChestClose(this);
            }
        }
        else
        {
            // 상자가 닫혀있으면 열기
            // 서버에 상자 열기 요청 (실시간 멀티플레이어)
            if (ServerSyncManager.Instance != null)
            {
                ServerSyncManager.Instance.RequestOpenChest(chestId);
            }

            // 상호작용 실행
            if (handler != null)
            {
                handler.HandleChestInteraction(this);
            }
        }
    }

    /// <summary>
    /// 상호작용 종료 (거리 멀어지거나 강제 종료시)
    /// </summary>
    public void OnInteractionEnd()
    {
        CloseChest();
    }

    /// <summary>
    /// 상호작용 범위 반환 (상자가 열려있으면 범위를 더 넓게)
    /// </summary>
    public float GetInteractionRange()
    {
        // 상자가 열려있으면 범위를 1.5배 넓게 해서 UI가 안 꺼지도록
        return isOpen ? interactionRange * 1.5f : interactionRange;
    }

    /// <summary>
    /// Transform 반환
    /// </summary>
    public Transform GetTransform()
    {
        return transform;
    }

    /// <summary>
    /// 상호작용 객체 이름 반환
    /// </summary>
    public string GetInteractableName()
    {
        return chestName;
    }

    /// <summary>
    /// 매니저가 준비될 때까지 대기 후 상자 등록
    /// </summary>
    private System.Collections.IEnumerator RegisterChestWhenReady()
    {
        // 매니저들이 초기화될 때까지 대기
        while (ChestInventoryManager.Instance == null)
        {
            yield return null;
        }

        // 추가 안전장치: 1프레임 더 대기
        yield return null;

        // 상자 등록
        ChestInventoryManager.Instance.RegisterChest(chestId);
        Debug.Log($"[InteractableChest] {chestId} 등록 완료 (지연 등록)");
    }
    #endregion
}
