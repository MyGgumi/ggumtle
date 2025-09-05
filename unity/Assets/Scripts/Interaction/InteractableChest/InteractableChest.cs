using System.Collections.Generic;
using UnityEngine;

// 클릭 기반 상자 상호작용
public class InteractableChest : MonoBehaviour, IInteractable
{
    [Header("상자 설정")]
    public string chestName = "상자";
    public float interactionRange = 2.0f;
    public bool isOpen = false;
    public List<ChestItem> chestItems = new List<ChestItem>();

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
            
        // 테스트 아이템 초기화
        if (useTestItems && chestItems.Count == 0)
        {
            InitializeTestItems();
        }
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

    // 아이템 관리 메서드들
    public void AddItem(ChestItem item)
    {
        chestItems.Add(item);
    }

    public void RemoveItem(ChestItem item)
    {
        chestItems.Remove(item);
    }

    public void RemoveItemAt(int index)
    {
        if (index >= 0 && index < chestItems.Count)
            chestItems.RemoveAt(index);
    }

    /// <summary>
    /// 테스트용 더미 아이템 초기화 (나중에 서버에서 받아올 데이터로 교체)
    /// </summary>
    private void InitializeTestItems()
    {
        Debug.Log("[InteractableChest] 테스트 더미 아이템 초기화");
        
        // 기본 테스트 아이템들
        var testItems = new[]
        {
            new ChestItem("포션", GetTestSprite(0), 5, "체력을 회복하는 물약"),
            new ChestItem("마나 포션", GetTestSprite(1), 3, "마나를 회복하는 물약"),
            new ChestItem("검", GetTestSprite(2), 1, "날카로운 검"),
            new ChestItem("방패", GetTestSprite(3), 1, "든든한 방패"),
            new ChestItem("코인", GetTestSprite(4), 50, "반짝이는 금화")
        };

        chestItems.AddRange(testItems);
        
        Debug.Log($"[InteractableChest] {testItems.Length}개 테스트 아이템 추가됨");
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
        // 이미 열려있거나, 플레이어가 범위 밖에 있으면 상호작용 불가
        return !isOpen && IsPlayerInRange();
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
            if (isOpen)
                Debug.Log("상자가 이미 열려있습니다.");
            else
                Debug.Log("너무 멀어서 상자를 열 수 없습니다.");
            return;
        }

        // 상호작용 시작 알림
        InteractionManager.Instance?.BeginInteraction(this);

        // 상호작용 실행
        if (handler != null)
        {
            handler.HandleChestInteraction(this);
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
    /// 상호작용 범위 반환
    /// </summary>
    public float GetInteractionRange()
    {
        return interactionRange;
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
    #endregion
}
