using System.Collections;
using UnityEngine;
using Models;
using Services;

public class InteractableGgumtle : MonoBehaviour, IInteractable
{
    [Header("꿈틀이 설정")]
    public string ggumtleName = "꿈틀이";
    public string ggumtleId; // 고유 ID
    public float interactionRange = 2.0f;

    // 설정값들은 GgumtleService에서 관리하므로 제거

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

    // 서비스 참조
    private GgumtleService ggumtleService;
    private GgumtleData ggumtleData;
    private Transform playerTransform;

    void Start()
    {
        Debug.Log($"[InteractableGgumtle] Start() 호출됨 - GameObject: {gameObject.name}");

        // 꿈틀이 ID 자동 생성
        if (string.IsNullOrEmpty(ggumtleId))
        {
            ggumtleId = $"Ggumtle_{transform.position.x}_{transform.position.z}_{GetInstanceID()}";
        }

        if (ggumtleAnimator == null)
            ggumtleAnimator = GetComponent<Animator>();

        // 서비스 초기화
        InitializeService();
    }

    void Update()
    {
        UpdateInteractionIcon();
    }

    private void InitializeService()
    {
        Debug.Log($"[InteractableGgumtle] {ggumtleName} InitializeService 시작");

        // 서비스 인스턴스 획득
        ggumtleService = GgumtleService.Instance;
        if (ggumtleService == null)
        {
            Debug.LogError("[InteractableGgumtle] GgumtleService가 없습니다! 코루틴으로 대기합니다.");
            StartCoroutine(WaitForServiceAndInitialize());
            return;
        }

        // 서비스에 꿈틀이 등록
        ggumtleService.RegisterGgumtle(ggumtleId, ggumtleName, transform.position);
        ggumtleData = ggumtleService.GetGgumtleData(ggumtleId);

        if (ggumtleData == null)
        {
            Debug.LogError($"[InteractableGgumtle] {ggumtleName} ggumtleData를 가져올 수 없습니다!");
            return;
        }

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // 서비스 이벤트 구독
        ggumtleService.OnGgumtleStateChanged += OnStateChanged;
        ggumtleService.OnGgumtleFoodAdded += OnFoodAdded;
        ggumtleService.OnGgumtlePurified += OnPurified;

        // UI 아이콘 초기화
        if (interactionIcon != null)
            interactionIcon.SetActive(false);
        if (feedingIcon != null)
            feedingIcon.SetActive(false);

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 서비스 등록 완료 - ID: {ggumtleId}, 상태: {ggumtleData.currentState}");
    }

    private System.Collections.IEnumerator WaitForServiceAndInitialize()
    {
        Debug.Log($"[InteractableGgumtle] {ggumtleName} GgumtleService 대기 중...");

        // GgumtleService가 준비될 때까지 대기
        while (GgumtleService.Instance == null)
        {
            yield return null;
        }

        // 추가 안전 대기
        yield return new WaitForSeconds(0.1f);

        InitializeService();
    }

    void OnDestroy()
    {
        // 서비스 이벤트 구독 해제
        if (ggumtleService != null)
        {
            ggumtleService.OnGgumtleStateChanged -= OnStateChanged;
            ggumtleService.OnGgumtleFoodAdded -= OnFoodAdded;
            ggumtleService.OnGgumtlePurified -= OnPurified;

            // 서비스에서 꿈틀이 등록 해제
            ggumtleService.UnregisterGgumtle(ggumtleId);
        }
    }

    #region Service Event Handlers

    private void OnStateChanged(string id, GgumtleState previousState, GgumtleState newState)
    {
        if (id != ggumtleId) return;

        Debug.Log($"[InteractableGgumtle] 상태 변경: {previousState} → {newState}");

        // 애니메이션 처리
        switch (newState)
        {
            case GgumtleState.Digging:
                if (ggumtleAnimator != null && !string.IsNullOrEmpty(diggingAnimationTrigger))
                {
                    ggumtleAnimator.SetTrigger(diggingAnimationTrigger);
                }
                if (diggingEffect != null)
                {
                    diggingEffect.Play();
                }
                break;

            case GgumtleState.Emerging:
                // 나오는 중 애니메이션 (추후 추가)
                if (diggingEffect != null)
                {
                    diggingEffect.Stop();
                }
                Debug.Log($"[InteractableGgumtle] {ggumtleName} 나오는 중... (2초간 애니메이션)");
                break;

            case GgumtleState.Feeding:
                if (ggumtleAnimator != null && !string.IsNullOrEmpty(feedingAnimationTrigger))
                {
                    ggumtleAnimator.SetTrigger(feedingAnimationTrigger);
                }
                Debug.Log($"[InteractableGgumtle] {ggumtleName} 먹이주기 가능 상태");
                break;

            case GgumtleState.Purified:
                if (purificationEffect != null)
                {
                    purificationEffect.Play();
                }
                break;
        }

        // UI 업데이트를 위해 InteractionViewModel에게 알림 (Digging, Emerging 상태 제외)
        if (newState != GgumtleState.Digging && newState != GgumtleState.Emerging)
        {
            UpdateNearbyInteractionUI();
        }
    }

    private void OnFoodAdded(string id, int currentAmount, int maxAmount)
    {
        if (id != ggumtleId) return;

        Debug.Log($"[InteractableGgumtle] 먹이 추가: {currentAmount}/{maxAmount}");

        // UI 텍스트만 업데이트 (전체 UI 제거/재추가 방지)
        if (ViewModels.UI.InteractionViewModel.Instance != null)
        {
            var newText = GetInteractionText();
            bool updated = ViewModels.UI.InteractionViewModel.Instance.UpdateNearbyInteractionText(
                Models.InteractionType.Feeding, gameObject, newText);

            if (updated)
            {
                Debug.Log($"[InteractableGgumtle] UI 텍스트 업데이트 완료: {newText}");

                // 먹이주기 후 프로그레스 바 리셋 요청
                ViewModels.UI.InteractionViewModel.Instance.ResetFeedingProgressBar();
            }
        }
    }

    private void OnPurified(string id)
    {
        if (id != ggumtleId) return;

        Debug.Log($"[InteractableGgumtle] 정화 완료: {id}");

        // 일정 시간 후 오브젝트 제거
        StartCoroutine(DestroyAfterPurification());
    }

    #endregion

    private void UpdateInteractionIcon()
    {
        if (playerTransform == null || ggumtleData == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool isInRange = distance <= ggumtleData.interactionRange;

        // 상태에 따른 아이콘 표시
        switch (ggumtleData.currentState)
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
        if (playerTransform == null || ggumtleData == null) return false;
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= ggumtleData.interactionRange;
    }



    // CompleteDigging은 서비스에서 처리하므로 제거

    private void UpdateNearbyInteractionUI()
    {
        if (ggumtleData == null) return;

        // InteractionViewModel에게 상태 변경 알림
        if (ViewModels.UI.InteractionViewModel.Instance != null)
        {
            // 상태에 따른 올바른 타입 설정
            Models.InteractionType interactionType;
            switch (ggumtleData.currentState)
            {
                case GgumtleState.Buried:
                    interactionType = Models.InteractionType.Dig;
                    break;
                case GgumtleState.Feeding:
                    interactionType = Models.InteractionType.Feeding;
                    break;
                default:
                    return; // Digging, Purified 상태에서는 상호작용 불가
            }

            var text = GetInteractionText();
            if (string.IsNullOrEmpty(text)) return;

            // 기존 상호작용 제거 후 새로 추가 (텍스트 업데이트)
            ViewModels.UI.InteractionViewModel.Instance.RemoveNearbyInteraction(Models.InteractionType.Dig, gameObject);
            ViewModels.UI.InteractionViewModel.Instance.RemoveNearbyInteraction(Models.InteractionType.Feeding, gameObject);

            ViewModels.UI.InteractionViewModel.Instance.AddNearbyInteraction(interactionType, text, gameObject);
            Debug.Log($"[InteractableGgumtle] UI 업데이트: {text}, 타입: {interactionType}");
        }
    }


    // FeedOnce 제거 - 이제 홀드 방식으로 통일
    // StopFeeding 제거 - 홀드 기반으로 변경됨
    // ContinuousFeeding은 GgumtleService에서 처리하므로 제거

    // StartPurification은 OnPurified 이벤트에서 처리

    private IEnumerator DestroyAfterPurification()
    {
        yield return new WaitForSeconds(3f); // 정화 애니메이션/이펙트 시간

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 정화 완료 - 오브젝트 제거");
        Destroy(gameObject);
    }

    // 아래 메서드들은 이제 GgumtleService에서 처리됨 (DEPRECATED)
    // CanPlayerFeed(), TryConsumeFoodFromPlayer() 메서드들은 GgumtleService로 이동됨

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float range = ggumtleData?.interactionRange ?? 2f;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    #region IInteractable 구현
    public bool CanInteract()
    {
        if (ggumtleData == null)
        {
            // Service 초기화가 완료되지 않았으면 상호작용 불가
            return false;
        }

        bool canInteract = ggumtleData.CanInteract() && IsPlayerInRange();
        return canInteract;
    }

    public string GetInteractionText()
    {
        if (ggumtleData == null)
        {
            // Service 초기화가 완료되지 않았으면 빈 문자열 반환
            return "";
        }

        return ggumtleData.GetInteractionText();
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        Debug.Log($"[InteractableGgumtle] Interact 호출됨 - 상태: {ggumtleData.currentState}");

        // 즉시 실행되는 상호작용은 없음 - 모든 상호작용이 홀드 필요
        Debug.Log($"[InteractableGgumtle] 모든 상호작용이 홀드 필요 - Handler에서 처리됨");
    }

    public void OnInteractionEnd()
    {
        Debug.Log($"[InteractableGgumtle] {ggumtleName} 상호작용 종료");

        // 홀드 중이었다면 취소
        if (ggumtleData != null && ggumtleData.isHoldInProgress)
        {
            OnHoldCancelled();
        }

        // 홀드 기반으로 변경되어 별도 중단 불필요

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 상태 완전 초기화");
    }

    #region IInteractable Hold Methods

    public void OnHoldStart()
    {
        // MVVM 패턴: ViewModel을 통해 서비스 호출
        if (ViewModels.UI.InteractionViewModel.Instance != null)
        {
            ViewModels.UI.InteractionViewModel.Instance.StartGgumtleHold(ggumtleId);
        }
    }

    public void OnHoldProgress(float progress)
    {
        // MVVM 패턴: ViewModel을 통해 서비스 호출
        if (ViewModels.UI.InteractionViewModel.Instance != null)
        {
            ViewModels.UI.InteractionViewModel.Instance.UpdateGgumtleHoldProgress(ggumtleId, progress);
        }
    }

    public void OnHoldComplete()
    {
        // MVVM 패턴: ViewModel을 통해 서비스 호출
        if (ViewModels.UI.InteractionViewModel.Instance != null)
        {
            ViewModels.UI.InteractionViewModel.Instance.CompleteGgumtleHold(ggumtleId);
        }
    }

    public void OnHoldCancelled()
    {
        // MVVM 패턴: ViewModel을 통해 서비스 호출
        if (ViewModels.UI.InteractionViewModel.Instance != null)
        {
            ViewModels.UI.InteractionViewModel.Instance.CancelGgumtleHold(ggumtleId);
        }
    }

    public float GetHoldDuration()
    {
        if (ggumtleData == null) return 3f;

        // 상태에 따라 다른 홀드 시간 반환
        switch (ggumtleData.currentState)
        {
            case GgumtleState.Buried:
            case GgumtleState.Digging:
                return ggumtleData.diggingHoldTime; // 파내기: 3초
            case GgumtleState.Feeding:
                return 0.5f; // 먹이주기: 0.5초마다 반복
            default:
                return 3f;
        }
    }

    #endregion

    public float GetInteractionRange()
    {
        if (ggumtleData == null) return 2f;
        return ggumtleData.interactionRange;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public string GetInteractableName()
    {
        if (ggumtleData == null) return ggumtleName;
        return ggumtleData.GetInteractableName();
    }

    // SendFeedingStatusToServer는 GgumtleService에서 처리하므로 제거

    #endregion
}