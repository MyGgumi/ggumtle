using System.Collections.Generic;
using UnityEngine;
using Config;
using Interfaces;
using Managers;

// 클릭 기반 상자 상호작용
public class InteractableChest : MonoBehaviour, IUIInteractable
{
    [Header("상자 설정")]
    public string chestName = "상자";
    public string chestId; // 고유 상자 ID (Inspector에서 설정하거나 자동 생성)

    public string ChestId => chestId;
    public bool isOpen = false;

    [Header("Trigger 설정")]
    [SerializeField] private bool autoSetupTrigger = true; // Trigger Collider 자동 설정

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

    // private ChestInteractionHandler handler; // DEPRECATED: ChestViewModel로 대체
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

        // Trigger Collider 자동 설정
        Debug.Log($"[InteractableChest] autoSetupTrigger: {autoSetupTrigger}");
        if (autoSetupTrigger)
        {
            SetupTriggerCollider();
        }
        else
        {
            Debug.LogWarning($"[InteractableChest] autoSetupTrigger가 false여서 Trigger Collider 설정 안함: {gameObject.name}");
        }

        // 매니저 초기화 대기 후 등록
        StartCoroutine(RegisterChestWhenReady());

        // UIInteractionManager에 등록
        RegisterToUIManager();

        // 핸들러 찾기 - DEPRECATED
        // handler = FindFirstObjectByType<ChestInteractionHandler>();

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

    void OnDestroy()
    {
        // UIInteractionManager에서 해제
        if (UIInteractionManager.Instance != null)
        {
            UIInteractionManager.Instance.UnregisterInteractable(this);
        }
    }

    private void RegisterToUIManager()
    {
        if (UIInteractionManager.Instance != null)
        {
            UIInteractionManager.Instance.RegisterInteractable(this);
            Debug.Log($"[InteractableChest] UIInteractionManager에 등록: {chestName}");
        }
        else
        {
            StartCoroutine(WaitForUIManagerAndRegister());
        }
    }

    private System.Collections.IEnumerator WaitForUIManagerAndRegister()
    {
        while (UIInteractionManager.Instance == null)
        {
            yield return null;
        }

        UIInteractionManager.Instance.RegisterInteractable(this);
        Debug.Log($"[InteractableChest] UIInteractionManager에 등록: {chestName}");
    }

    private void UpdateInteractionIcon()
    {
        if (playerTransform == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool isInRange = distance <= InteractionConfig.ChestInteractionRange;
        bool shouldShow = !isOpen && isInRange;

        // 상호작용 아이콘 업데이트 (로컬 표시용)
        if (interactionIcon != null && interactionIcon.activeInHierarchy != shouldShow)
        {
            interactionIcon.SetActive(shouldShow);
        }

        // InteractionViewModel이 이제 중앙에서 거리 계산을 처리하므로
        // 개별 객체에서는 nearby interaction 등록/해제를 하지 않음
    }

    // UI 버튼에서 호출되는 상호작용 메서드 (레거시)
    public void TryInteract()
    {
        OnInteract();
    }

    // 레거시 호환성을 위한 메서드
    public void Interact()
    {
        OnInteract();
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null)
            return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= InteractionConfig.ChestInteractionRange;
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

        // 핸들러에게 닫기 알림 - DEPRECATED
        // if (handler != null)
        // {
        //     handler.HandleChestClose();
        // }

        // InteractionViewModel이 중앙에서 관리하므로 개별 제거 불필요

        Debug.Log($"[InteractableChest] 상자 닫힘 완료: {chestName}, isOpen: {isOpen}");
    }

    // 아이템 관리 메서드들 - DEPRECATED: ChestViewModel 사용
    public void AddItem(ChestItem item)
    {
        // DEPRECATED: ChestViewModel.Instance.UpdateSlot() 사용
        Debug.LogWarning("[InteractableChest] AddItem은 deprecated입니다. ChestViewModel 사용하세요.");
    }

    public void RemoveItem(ChestItem item)
    {
        // DEPRECATED: ChestViewModel.Instance.TakeItemFromSlot() 사용
        Debug.LogWarning("[InteractableChest] RemoveItem은 deprecated입니다. ChestViewModel 사용하세요.");
    }

    public void RemoveItemAt(int index)
    {
        // DEPRECATED: ChestViewModel.Instance.TakeItemFromSlot() 사용
        Debug.LogWarning("[InteractableChest] RemoveItemAt은 deprecated입니다. ChestViewModel 사용하세요.");
    }

    /// <summary>
    /// 현재 상자의 아이템 리스트 조회 - ChestViewModel 사용
    /// </summary>
    public List<ChestItem> GetChestItems()
    {
        if (ViewModels.UI.ChestViewModel.Instance != null)
        {
            var chest = ViewModels.UI.ChestViewModel.Instance.GetChest(chestId);
            if (chest != null)
            {
                var items = new List<ChestItem>();
                foreach (var slot in chest.slots)
                {
                    if (!slot.isEmpty)
                    {
                        // ChestSlot을 ChestItem으로 변환 (임시)
                        items.Add(new ChestItem(slot.itemId, null, slot.count, ""));
                    }
                }
                return items;
            }
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
    /// 테스트용 더미 아이템 초기화 - DEPRECATED
    /// </summary>
    private void InitializeTestItems()
    {
        Debug.Log($"[InteractableChest] 테스트 더미 아이템 초기화 - DEPRECATED");
        return; // 서버에서 처리하므로 비활성화

        // DEPRECATED - 서버에서 처리
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
        Gizmos.DrawWireSphere(transform.position, InteractionConfig.ChestInteractionRange);
    }

    #region IUIInteractable 구현
    /// <summary>
    /// 상호작용 가능한지 확인
    /// </summary>
    public bool CanInteract()
    {
        // Trigger 방식에서는 이미 범위 안에 있다는 것이 확실하므로 항상 true
        // 상자 상태와 관계없이 상호작용 가능 (열린 상자는 닫기 가능)
        return true;
    }

    public string GetInteractionText()
    {
        return isOpen ? $"{chestName} 닫기" : $"{chestName} 열기";
    }

    /// <summary>
    /// 상호작용 실행
    /// </summary>
    public void OnInteract()
    {
        Debug.Log($"[InteractableChest] ⚡ OnInteract() 호출됨! - {chestName}, isOpen: {isOpen}");

        // 상호작용 가능 체크
        if (!CanInteract())
        {
            Debug.Log("[InteractableChest] 너무 멀어서 상자를 조작할 수 없습니다.");
            return;
        }

        if (isOpen)
        {
            // 상자가 이미 열려있으면 닫기
            Debug.Log($"[InteractableChest] 상자 닫기: {chestName}");
            CloseChest();

            // MVVM 패턴: ChestViewModel에 닫기 알림
            var chestViewModel = ViewModels.UI.ChestViewModel.Instance;
            if (chestViewModel != null)
            {
                Debug.Log($"[InteractableChest] ChestViewModel.CloseChest() 호출");
                chestViewModel.CloseChest();
            }
            else
            {
                Debug.LogWarning($"[InteractableChest] ChestViewModel.Instance가 null입니다!");
            }
        }
        else
        {
            // 상자가 닫혀있으면 열기
            Debug.Log($"[InteractableChest] 상자 열기: {chestName}");

            // 즉시 상태 업데이트 (서버 응답 대기하지 않음)
            isOpen = true;
            Debug.Log($"[InteractableChest] isOpen을 true로 즉시 설정함: {chestName}");

            // 서버에 상자 열기 요청 (실시간 멀티플레이어)
            if (ServerSyncManager.Instance != null)
            {
                ServerSyncManager.Instance.RequestOpenChest(chestId);
                Debug.Log($"[InteractableChest] ServerSyncManager.RequestOpenChest() 호출");
            }

            // MVVM 패턴: ChestViewModel 직접 호출
            var chestViewModel = ViewModels.UI.ChestViewModel.Instance;
            if (chestViewModel != null)
            {
                Debug.Log($"[InteractableChest] ChestViewModel.OpenChest() 호출: {chestId}");
                bool success = chestViewModel.OpenChest(chestId);
                if (!success)
                {
                    Debug.LogWarning($"[InteractableChest] ChestViewModel.OpenChest() 실패: {chestId}");
                }
                OpenChest(); // 상자 애니메이션 재생
            }
            else
            {
                Debug.LogError($"[InteractableChest] ChestViewModel.Instance가 null입니다!");
            }
        }
    }

    public bool RequiresHold()
    {
        // 상자는 클릭 기반 상호작용
        return false;
    }

    public float GetHoldDuration()
    {
        return 0f;
    }

    public void OnHoldStart() { }
    public void OnHoldProgress(float progress) { }
    public void OnHoldComplete() { }
    public void OnHoldCancelled() { }

    public float GetInteractionRange()
    {
        return InteractionConfig.ChestInteractionRange;
    }

    public Transform GetTransform()
    {
        return transform;
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
        return isOpen ? InteractionConfig.ChestInteractionRange * 1.5f : InteractionConfig.ChestInteractionRange;
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
    /// 홀드 시작 - 상자는 홀드가 필요하지 않으므로 빈 구현
    /// </summary>
    public void OnHoldStart()
    {
        // 상자는 홀드가 필요하지 않음
    }

    /// <summary>
    /// 홀드 진행 - 상자는 홀드가 필요하지 않으므로 빈 구현
    /// </summary>
    public void OnHoldProgress(float progress)
    {
        // 상자는 홀드가 필요하지 않음
    }

    /// <summary>
    /// 홀드 완료 - 상자는 홀드가 필요하지 않으므로 빈 구현
    /// </summary>
    public void OnHoldComplete()
    {
        // 상자는 홀드가 필요하지 않음
    }

    /// <summary>
    /// 홀드 취소 - 상자는 홀드가 필요하지 않으므로 빈 구현
    /// </summary>
    public void OnHoldCancelled()
    {
        // 상자는 홀드가 필요하지 않음
    }

    /// <summary>
    /// 홀드 필요 시간 - 상자는 홀드가 필요하지 않으므로 0 반환
    /// </summary>
    public float GetHoldDuration()
    {
        return 0f; // 홀드 불필요
    }

    /// <summary>
    /// 매니저가 준비될 때까지 대기 후 상자 등록
    /// </summary>
    private System.Collections.IEnumerator RegisterChestWhenReady()
    {
        // ChestViewModel이 초기화될 때까지 대기
        while (ViewModels.UI.ChestViewModel.Instance == null)
        {
            yield return null;
        }

        // 추가 안전장치: 1프레임 더 대기
        yield return null;

        // ChestViewModel에 상자 등록
        var chestViewModel = ViewModels.UI.ChestViewModel.Instance;
        if (chestViewModel != null)
        {
            chestViewModel.RegisterChest(chestId, chestName, transform.position, gameObject);
            Debug.Log($"[InteractableChest] {chestId} ChestViewModel 등록 완료");
        }
        else
        {
            Debug.LogWarning($"[InteractableChest] ChestViewModel.Instance가 null이어서 {chestId} 등록 실패");
        }
    }

    /// <summary>
    /// Trigger Collider 자동 설정 (새로운 상호작용 시스템용)
    /// </summary>
    private void SetupTriggerCollider()
    {
        // 기존 Trigger Collider가 있는지 확인
        SphereCollider triggerCollider = null;
        var colliders = GetComponents<Collider>();

        foreach (var col in colliders)
        {
            if (col.isTrigger && col is SphereCollider sphere)
            {
                triggerCollider = sphere;
                break;
            }
        }

        // Trigger Collider가 없으면 새로 생성
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            Debug.Log($"[InteractableChest] Trigger Collider 자동 생성: {gameObject.name}");
        }

        // 상호작용 범위로 설정
        triggerCollider.radius = InteractionConfig.ChestInteractionRange;

        // GameObject를 상호작용 레이어로 설정 (Layer 7 = Interaction)
        if (gameObject.layer != 7)
        {
            gameObject.layer = 7;
            Debug.Log($"[InteractableChest] 레이어를 상호작용 레이어(7)로 변경: {gameObject.name}");
        }

        Debug.Log($"[InteractableChest] Trigger 설정 완료 - 범위: {InteractionConfig.ChestInteractionRange}, 레이어: {gameObject.layer}");
    }

    #endregion
}
