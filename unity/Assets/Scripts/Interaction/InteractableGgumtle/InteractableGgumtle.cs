using System.Collections;
using UnityEngine;

public enum GgumtleState
{
    Buried,    // 땅에 묻혀있음 (초기 상태)
    Digging,   // 파내는 중 (애니메이션)
    Feeding,   // 먹이 주기 가능 상태
    Purified   // 정화 완료 (제거됨)
}

public class InteractableGgumtle : MonoBehaviour, IInteractable
{
    [Header("꿈틀이 설정")]
    public string ggumtleName = "꿈틀이";
    public string ggumtleId; // 고유 ID
    public float interactionRange = 2.0f;
    public GgumtleState currentState = GgumtleState.Buried;

    [Header("상호작용 설정")]
    public float diggingHoldTime = 3f; // 파내기에 필요한 홀드 시간 (초)
    
    [Header("먹이 설정")]
    public int maxFoodRequired = 30; // 정화에 필요한 총 먹이량
    public int feedingRate = 1; // 0.5초당 먹이 개수 (먹이 인벤토리에서 소모)
    public int currentFoodAmount = 0; // 현재 먹은 먹이량
    public string acceptableFoodType = "Mushroom"; // 받을 수 있는 먹이 종류

    [Header("애니메이션")]
    public Animator ggumtleAnimator;
    public string diggingAnimationTrigger = "Dig";
    public string feedingAnimationTrigger = "Feed";
    public string purifiedAnimationTrigger = "Purify";

    [Header("UI 힌트")]
    public GameObject interactionIcon;
    public GameObject feedingIcon; // 먹이주기 상태일 때 표시할 아이콘

    [Header("이펙트")]
    public ParticleSystem diggingEffect;
    public ParticleSystem purificationEffect;

    private Transform playerTransform;
    private bool isFeedingContinuously = false;
    private bool isDiggingHold = false;
    private bool isInteractionHeld = false; // 현재 상호작용 버튼이 눌려있는 상태
    private Coroutine feedingCoroutine;
    private Coroutine diggingCoroutine;
    private float lastFeedTime = 0f; // 마지막 먹이 준 시간

    void Start()
    {
        Debug.Log($"[InteractableGgumtle] Start() 호출됨 - GameObject: {gameObject.name}");

        // 꿈틀이 ID 자동 생성
        if (string.IsNullOrEmpty(ggumtleId))
        {
            ggumtleId = $"Ggumtle_{transform.position.x}_{transform.position.z}_{GetInstanceID()}";
        }

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        if (ggumtleAnimator == null)
            ggumtleAnimator = GetComponent<Animator>();

        InitializeState();
    }

    void Update()
    {
        UpdateInteractionIcon();
        UpdateHoldInteractions();
    }

    private void InitializeState()
    {
        // 초기 상태 설정
        currentState = GgumtleState.Buried;
        currentFoodAmount = 0;

        // UI 아이콘 초기화
        if (interactionIcon != null)
            interactionIcon.SetActive(false);
        if (feedingIcon != null)
            feedingIcon.SetActive(false);

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 초기화 완료 - 상태: {currentState}");
    }

    private void UpdateInteractionIcon()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool isInRange = distance <= interactionRange;

        // 상태에 따른 아이콘 표시
        switch (currentState)
        {
            case GgumtleState.Buried:
                if (interactionIcon != null)
                    interactionIcon.SetActive(isInRange);
                if (feedingIcon != null)
                    feedingIcon.SetActive(false);
                break;

            case GgumtleState.Feeding:
                if (interactionIcon != null)
                    interactionIcon.SetActive(false);
                if (feedingIcon != null)
                    feedingIcon.SetActive(isInRange);
                break;

            default:
                if (interactionIcon != null)
                    interactionIcon.SetActive(false);
                if (feedingIcon != null)
                    feedingIcon.SetActive(false);
                break;
        }
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= interactionRange;
    }

    private void UpdateHoldInteractions()
    {
        // 플레이어가 범위를 벗어나면 모든 홀드 상호작용 중단
        if (!IsPlayerInRange())
        {
            StopAllHoldInteractions();
            return;
        }

        // InteractionManager를 통해 상호작용 버튼 상태 확인
        // 실제로는 InteractionManager에서 홀드 상태를 알려줘야 하지만,
        // 임시로 여기서 처리
        CheckHoldState();
    }

    private void CheckHoldState()
    {
        // InteractionManager를 통해 홀드 상태 확인
        if (InteractionManager.Instance != null)
        {
            bool managerIsHolding = InteractionManager.Instance.IsHolding();
            var currentHoldObject = InteractionManager.Instance.GetCurrentHoldInteractable();
            
            // InteractionManager의 홀드 상태와 동기화
            if (managerIsHolding && currentHoldObject == this)
            {
                isInteractionHeld = true;
            }
            else if (!managerIsHolding)
            {
                isInteractionHeld = false;
            }
        }
    }

    private void StopAllHoldInteractions()
    {
        if (isDiggingHold)
        {
            StopDiggingHold();
        }
        if (isFeedingContinuously)
        {
            StopFeeding();
        }
        isInteractionHeld = false;
    }

    private void StartDiggingHold()
    {
        if (isDiggingHold) return;
        
        Debug.Log($"[InteractableGgumtle] {ggumtleName} 파내기 홀드 시작");
        
        isDiggingHold = true;
        currentState = GgumtleState.Digging;

        // 파내기 애니메이션
        if (ggumtleAnimator != null && !string.IsNullOrEmpty(diggingAnimationTrigger))
        {
            ggumtleAnimator.SetTrigger(diggingAnimationTrigger);
        }

        // 파내기 이펙트
        if (diggingEffect != null)
        {
            diggingEffect.Play();
        }

        // 홀드 시간 체크 코루틴 시작
        if (diggingCoroutine != null)
        {
            StopCoroutine(diggingCoroutine);
        }
        diggingCoroutine = StartCoroutine(DiggingHoldProgress());
    }

    private void StopDiggingHold()
    {
        if (!isDiggingHold) return;

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 파내기 홀드 중단");
        
        isDiggingHold = false;
        
        // 다시 묻힌 상태로 복귀
        currentState = GgumtleState.Buried;

        // 파내기 이펙트 중단
        if (diggingEffect != null)
        {
            diggingEffect.Stop();
        }

        // 코루틴 중단
        if (diggingCoroutine != null)
        {
            StopCoroutine(diggingCoroutine);
            diggingCoroutine = null;
        }
    }

    private IEnumerator DiggingHoldProgress()
    {
        float holdTime = 0f;
        
        while (isDiggingHold && holdTime < diggingHoldTime)
        {
            // 플레이어가 범위를 벗어나면 중단
            if (!IsPlayerInRange() || !isInteractionHeld)
            {
                StopDiggingHold();
                yield break;
            }

            holdTime += Time.deltaTime;
            
            // 진행도 표시 (선택적)
            float progress = holdTime / diggingHoldTime;
            Debug.Log($"[InteractableGgumtle] 파내기 진행도: {progress:P0}");

            yield return null;
        }

        // 홀드 완료
        if (isDiggingHold && holdTime >= diggingHoldTime)
        {
            CompleteDigging();
        }
    }

    private void CompleteDigging()
    {
        Debug.Log($"[InteractableGgumtle] {ggumtleName} 파내기 완료");
        
        isDiggingHold = false;
        currentState = GgumtleState.Feeding;

        // 파내기 이펙트 중단
        if (diggingEffect != null)
        {
            diggingEffect.Stop();
        }

        // 먹이주기 애니메이션으로 전환
        if (ggumtleAnimator != null && !string.IsNullOrEmpty(feedingAnimationTrigger))
        {
            ggumtleAnimator.SetTrigger(feedingAnimationTrigger);
        }

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 먹이주기 가능 상태로 전환");
    }


    private void StartFeeding()
    {
        if (isFeedingContinuously) return;

        // 플레이어가 먹이를 가지고 있는지 확인
        if (!CanPlayerFeed())
        {
            Debug.Log($"[InteractableGgumtle] 플레이어가 Mushroom을(를) 가지고 있지 않습니다");
            return;
        }

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 먹이주기 시작");
        isFeedingContinuously = true;
        lastFeedTime = Time.time;

        // 지속적 먹이주기 코루틴 시작
        if (feedingCoroutine != null)
        {
            StopCoroutine(feedingCoroutine);
        }
        feedingCoroutine = StartCoroutine(ContinuousFeeding());
    }

    private void StopFeeding()
    {
        if (!isFeedingContinuously) return;

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 먹이주기 중단");
        isFeedingContinuously = false;

        if (feedingCoroutine != null)
        {
            StopCoroutine(feedingCoroutine);
            feedingCoroutine = null;
        }
    }

    private IEnumerator ContinuousFeeding()
    {
        while (isFeedingContinuously && currentState == GgumtleState.Feeding)
        {
            // 플레이어가 범위를 벗어나거나 홀드가 해제되면 먹이주기 중단
            if (!IsPlayerInRange() || !isInteractionHeld)
            {
                StopFeeding();
                yield break;
            }

            // 플레이어가 먹이를 가지고 있는지 확인
            if (!CanPlayerFeed())
            {
                Debug.Log($"[InteractableGgumtle] 플레이어의 Mushroom이 모두 소모됨 - 먹이주기 중단");
                StopFeeding();
                yield break;
            }

            // 0.5초마다 feedingRate만큼 먹이 소모 및 추가
            float currentTime = Time.time;
            if (currentTime - lastFeedTime >= 0.5f) // 0.5초마다
            {
                // 플레이어 인벤토리에서 먹이 제거
                if (TryConsumeFoodFromPlayer(feedingRate))
                {
                    currentFoodAmount += feedingRate;
                    lastFeedTime = currentTime;

                    Debug.Log($"[InteractableGgumtle] 먹이 {feedingRate}개 소모, 현재 먹이량: {currentFoodAmount}/{maxFoodRequired}");

                    // 정화 조건 확인
                    if (currentFoodAmount >= maxFoodRequired)
                    {
                        StartPurification();
                        yield break;
                    }
                }
                else
                {
                    Debug.Log($"[InteractableGgumtle] 플레이어 인벤토리에서 Mushroom 소모 실패");
                    StopFeeding();
                    yield break;
                }
            }

            yield return null; // 매 프레임마다 실행
        }
    }

    private void StartPurification()
    {
        Debug.Log($"[InteractableGgumtle] {ggumtleName} 정화 시작!");
        
        currentState = GgumtleState.Purified;
        StopFeeding();

        // 정화 애니메이션
        if (ggumtleAnimator != null && !string.IsNullOrEmpty(purifiedAnimationTrigger))
        {
            ggumtleAnimator.SetTrigger(purifiedAnimationTrigger);
        }

        // 정화 이펙트
        if (purificationEffect != null)
        {
            purificationEffect.Play();
        }

        // InteractionManager에게 상호작용 종료 알림
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.EndInteraction(this);
        }

        // 일정 시간 후 제거
        StartCoroutine(DestroyAfterPurification());
    }

    private IEnumerator DestroyAfterPurification()
    {
        yield return new WaitForSeconds(3f); // 정화 애니메이션/이펙트 시간

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 정화 완료 - 오브젝트 제거");
        Destroy(gameObject);
    }

    /// <summary>
    /// 플레이어가 먹이를 줄 수 있는지 확인
    /// </summary>
    private bool CanPlayerFeed()
    {
        if (FeedingInventory.Instance == null) return false;
        return FeedingInventory.Instance.GetMushroomCount() > 0;
    }

    /// <summary>
    /// 먹이 인벤토리에서 먹이 소모 시도
    /// </summary>
    private bool TryConsumeFoodFromPlayer(int amount)
    {
        if (FeedingInventory.Instance == null) return false;

        if (FeedingInventory.Instance.HasEnoughMushrooms(amount))
        {
            bool success = FeedingInventory.Instance.RemoveMushrooms(amount);
            if (success)
            {
                Debug.Log($"[InteractableGgumtle] 먹이 인벤토리에서 Mushroom {amount}개 소모");
            }
            return success;
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    #region IInteractable 구현
    public bool CanInteract()
    {
        switch (currentState)
        {
            case GgumtleState.Buried:
                return IsPlayerInRange();
            case GgumtleState.Digging:
                // 파내기 중에도 상호작용 가능 (홀드 유지용)
                return IsPlayerInRange();
            case GgumtleState.Feeding:
                return IsPlayerInRange();
            case GgumtleState.Purified:
            default:
                return false;
        }
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        Debug.Log($"[InteractableGgumtle] Interact 호출됨 - 상태: {currentState}");
        
        // Digging 상태에서는 Interact 호출 무시 (중복 방지)
        if (currentState == GgumtleState.Digging)
        {
            Debug.Log($"[InteractableGgumtle] 파내기 진행 중 - Interact 무시");
            return;
        }
        
        // 기본 Interact는 홀드 시작으로 처리
        OnInteractionStart();
    }

    // 상호작용 시작 (홀드 시작)
    public void OnInteractionStart()
    {
        if (!CanInteract()) return;

        isInteractionHeld = true;
        InteractionManager.Instance?.BeginInteraction(this);

        switch (currentState)
        {
            case GgumtleState.Buried:
                StartDiggingHold();
                break;

            case GgumtleState.Feeding:
                StartFeeding();
                break;
        }
    }

    // 상호작용 해제 (홀드 해제)
    public void OnInteractionRelease()
    {
        Debug.Log($"[InteractableGgumtle] 상호작용 해제 - 상태: {currentState}");
        
        isInteractionHeld = false;
        
        switch (currentState)
        {
            case GgumtleState.Digging:
                if (isDiggingHold)
                {
                    StopDiggingHold();
                    InteractionManager.Instance?.EndInteraction(this);
                }
                break;

            case GgumtleState.Feeding:
                if (isFeedingContinuously)
                {
                    StopFeeding();
                    InteractionManager.Instance?.EndInteraction(this);
                }
                break;
        }
    }

    public void OnInteractionEnd()
    {
        Debug.Log($"[InteractableGgumtle] {ggumtleName} 상호작용 종료");
        
        // 모든 홀드 상호작용 중단
        StopAllHoldInteractions();
        
        // 상태 완전 초기화
        isInteractionHeld = false;
        Debug.Log($"[InteractableGgumtle] {ggumtleName} 상태 완전 초기화");
    }

    public float GetInteractionRange()
    {
        return interactionRange;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public string GetInteractableName()
    {
        switch (currentState)
        {
            case GgumtleState.Buried:
                return $"{ggumtleName} (파내기)";
            case GgumtleState.Feeding:
                return $"{ggumtleName} (먹이주기 {currentFoodAmount}/{maxFoodRequired})";
            default:
                return ggumtleName;
        }
    }
    #endregion
}