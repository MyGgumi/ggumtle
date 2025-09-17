using System.Collections;
using UnityEngine;
using Models;
using Services;
using Config;
using Interfaces;
using Managers;

public class InteractableGgumtle : MonoBehaviour, IUIInteractable
{
    [Header("꿈틀이 설정")]
    public string ggumtleName = "꿈틀이";
    public string ggumtleId; // 고유 ID

    [Header("Trigger 설정")]
    [SerializeField] private bool autoSetupTrigger = true; // Trigger Collider 자동 설정

    // 설정값들은 GgumtleService에서 관리하므로 제거

    [Header("애니메이션")]
    public Animator ggumtleAnimator;
    public string diggingAnimationTrigger = "Dig";
    public string feedingAnimationTrigger = "Feed";
    public string purifiedAnimationTrigger = "Purify";

    // UI 힌트는 더 이상 필요 없음 (트리거 기반 UI로 대체됨)

    [Header("이펙트")]
    public ParticleSystem diggingEffect;
    public ParticleSystem purificationEffect;

    // Service 참조
    private GgumtleData ggumtleData;

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

        // Trigger Collider 자동 설정
        Debug.Log($"[InteractableGgumtle] autoSetupTrigger: {autoSetupTrigger}");
        if (autoSetupTrigger)
        {
            SetupTriggerCollider();
        }
        else
        {
            Debug.LogWarning($"[InteractableGgumtle] autoSetupTrigger가 false여서 Trigger Collider 설정 안함: {gameObject.name}");
        }

        // Service 초기화
        InitializeService();

        // UIInteractionManager에 등록
        RegisterToUIManager();
    }

    // Update 제거 - 더 이상 아이콘 업데이트나 범위 체크 불필요

    private void InitializeService()
    {
        Debug.Log($"[InteractableGgumtle] {ggumtleName} Service 연결 시도");

        // Service 인스턴스 획득
        if (GgumtleService.Instance == null)
        {
            Debug.LogWarning("[InteractableGgumtle] GgumtleService를 찾을 수 없습니다.");
            StartCoroutine(WaitForServiceAndInitialize());
            return;
        }

        // Service에 꿈틀이 등록
        GgumtleService.Instance.RegisterGgumtle(ggumtleId, ggumtleName, transform.position);
        ggumtleData = GgumtleService.Instance.GetGgumtleData(ggumtleId);

        if (ggumtleData == null)
        {
            Debug.LogError($"[InteractableGgumtle] {ggumtleName} 데이터를 가져올 수 없습니다!");
            return;
        }

        // Service 이벤트 구독
        GgumtleService.Instance.OnGgumtleStateChanged += OnStateChanged;

        Debug.Log($"[InteractableGgumtle] {ggumtleName} Service 연결 완료 - ID: {ggumtleId}, 상태: {ggumtleData.currentState}");
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

    private void RegisterToUIManager()
    {
        if (UIInteractionManager.Instance != null)
        {
            UIInteractionManager.Instance.RegisterInteractable(this);
            Debug.Log($"[InteractableGgumtle] UIInteractionManager에 등록: {ggumtleName}");
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
        Debug.Log($"[InteractableGgumtle] UIInteractionManager에 등록: {ggumtleName}");
    }

    void OnDestroy()
    {
        // UIInteractionManager에서 해제
        if (UIInteractionManager.Instance != null)
        {
            UIInteractionManager.Instance.UnregisterInteractable(this);
        }

        // Service 이벤트 구독 해제
        if (GgumtleService.Instance != null)
        {
            GgumtleService.Instance.OnGgumtleStateChanged -= OnStateChanged;
            GgumtleService.Instance.UnregisterGgumtle(ggumtleId);
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
                StartCoroutine(DestroyAfterPurification());
                break;
        }
    }


    #endregion




    private IEnumerator DestroyAfterPurification()
    {
        yield return new WaitForSeconds(3f); // 정화 애니메이션/이펙트 시간

        Debug.Log($"[InteractableGgumtle] {ggumtleName} 정화 완료 - 오브젝트 제거");
        Destroy(gameObject);
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        float range = InteractionConfig.GgumtleInteractionRange;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    #region IUIInteractable 구현
    public bool CanInteract()
    {
        if (ggumtleData == null)
        {
            // Service 초기화가 완료되지 않았으면 상호작용 불가
            return false;
        }

        // Trigger 방식에서는 이미 범위 안에 있다는 것이 확실하므로 거리 검사 제거
        bool canInteract = ggumtleData.CanInteract();
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

    public void OnInteract()
    {
        // UIInteractionManager에서 홀드가 필요한지 확인하고 처리하므로
        // 즉시 실행 상호작용은 없음
        Debug.Log($"[InteractableGgumtle] OnInteract 호출됨 - 홀드 필요");
    }

    public bool RequiresHold()
    {
        // 모든 꿈틀이 상호작용은 홀드 필요
        return true;
    }

    public void OnHoldStart()
    {
        if (GgumtleService.Instance != null)
        {
            GgumtleService.Instance.StartHold(ggumtleId);
        }
    }

    public void OnHoldProgress(float progress)
    {
        if (GgumtleService.Instance != null)
        {
            GgumtleService.Instance.UpdateHoldProgress(ggumtleId, progress);
        }
    }

    public void OnHoldComplete()
    {
        if (GgumtleService.Instance != null)
        {
            GgumtleService.Instance.CompleteHold(ggumtleId);
        }
    }

    public void OnHoldCancelled()
    {
        if (GgumtleService.Instance != null)
        {
            GgumtleService.Instance.CancelHold(ggumtleId);
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
        return InteractionConfig.GgumtleInteractionRange;
    }

    public Transform GetTransform()
    {
        return transform;
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
            Debug.Log($"[InteractableGgumtle] Trigger Collider 자동 생성: {gameObject.name}");
        }

        // Config에서 범위 가져오기
        float configRange = InteractionConfig.GgumtleInteractionRange;
        triggerCollider.radius = configRange;
        Debug.Log($"[InteractableGgumtle] 트리거 반지름 설정: {configRange} (Config에서 가져옴)");

        // 꿈틀이가 땅에 박혀있을 때는 Trigger의 중심을 위로 올림
        triggerCollider.center = Vector3.up * (InteractionConfig.GgumtleBuriedHeightOffset * 0.5f);

        // GameObject를 상호작용 레이어로 설정 (Layer 7 = Interaction)
        if (gameObject.layer != 7)
        {
            gameObject.layer = 7;
            Debug.Log($"[InteractableGgumtle] 레이어를 상호작용 레이어(7)로 변경: {gameObject.name}");
        }

        Debug.Log($"[InteractableGgumtle] Trigger 설정 완료 - 범위: {configRange}, 레이어: {gameObject.layer}");
    }


    #endregion
}