using System.Collections.Generic;
using UnityEngine;

// 클릭 기반 상자 상호작용
public class InteractableChest : MonoBehaviour
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

    // UI 버튼에서 호출되는 상호작용 메서드

    public void TryInteract()
    {
        // 이미 다른 상호작용 중이면 무시
        if (InteractionManager.Instance != null && InteractionManager.Instance.IsInteracting())
        {
            Debug.Log("다른 상호작용이 진행 중입니다.");
            return;
        }

        // 이미 열려있으면 무시
        if (isOpen)
        {
            Debug.Log("상자가 이미 열려있습니다.");
            return;
        }

        // 거리 체크
        if (!IsPlayerInRange())
        {
            Debug.Log("너무 멀어서 상자를 열 수 없습니다.");
            // TODO: UI로 "너무 멀다" 메시지 표시
            return;
        }

        // 상호작용 실행
        if (handler != null)
        {
            handler.HandleChestInteraction(this);
        }
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

    // Gizmos로 상호작용 범위 표시
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
