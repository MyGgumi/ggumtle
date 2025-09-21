using UnityEngine;
using VContainer;
using Features.Player.Services;
using System.Collections;
using System.Collections.Generic;

public class MonggingAnimationEventHandler : MonoBehaviour
{
    private Animator animator;
    private PlayerMovementService movementService;

    [Header("Particle Effects - 자동으로 찾음")]
    private ParticleSystem interactEffect;  // 힐, 아이템 사용 공통
    private ParticleSystem feedEffect;
    private ParticleSystem getHealedEffect;
    private ParticleSystem takeDamageEffect;
    private ParticleSystem downEffect;
    private ParticleSystem respawnExplosionEffect;  // 부활 이펙트


    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;


    void Start()
    {
        // Animator 찾기
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        

       
            // 모든 하위 오브젝트 이름 출력 (디버깅용)
            Debug.Log("[MonggingAnimationEventHandler] 모든 하위 오브젝트:");
            foreach (Transform child in transform)
            {
                Debug.Log(" - " + child.name);
            }
        }

        // 하위 오브젝트에서 파티클 이펙트들 찾기
        FindParticleEffects();

        // 모든 이펙트 초기 비활성화
        DisableAllParticleEffects();

        // PlayerMovementService 연결
        var lifetimeScope = FindObjectOfType<DI.GameLifetimeScope>();
        if (lifetimeScope != null)
        {
            movementService = lifetimeScope.Container.Resolve<PlayerMovementService>();

            if (movementService != null)
            {
                // isMoving 상태 변경 이벤트 구독
                movementService.MovingStateChanged += OnMovingStateChanged;

                // 초기 상태 설정
                animator.SetBool("IsMoving", movementService.IsMoving);

                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] PlayerMovementService 연결 완료");
            }
        }
    }

    private void OnMovingStateChanged(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);

            if (enableDebugLogs)
                Debug.Log($"[MonggingAnimationEventHandler] IsMoving: {isMoving}");
        }
    }

    // 피격 애니메이션 트리거 (이펙트는 Update()에서 자동 처리)
    public void TriggerTakeDamage()
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
            if (enableDebugLogs)
                Debug.Log("[MonggingAnimationEventHandler] TakeDamage 트리거!");
        }
    }

    // 다운 애니메이션 트리거 (한 번만 실행)
    public void TriggerDown()
    {
        if (animator != null)
        {
            animator.SetTrigger("Down");
            if (enableDebugLogs)
                Debug.Log("[MonggingAnimationEventHandler] Down 트리거 - 애니메이션 한 번 실행");
        }
    }


    // 죽음 트리거 (이펙트는 추후 추가 가능)
    public void TriggerDie()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
            if (enableDebugLogs)
                Debug.Log("[MonggingAnimationEventHandler] Die 트리거!");
        }
    }

    // 힐 받기 상태 설정 (이펙트는 Update()에서 자동 처리)
    public void SetGettingHealed(bool isGettingHealed)
    {
        if (animator != null)
        {
            animator.SetBool("IsGettingHealed", isGettingHealed);
            if (enableDebugLogs)
                Debug.Log($"[MonggingAnimationEventHandler] IsGettingHealed 파라미터: {isGettingHealed}");
        }
    }

    // ========== 일회성 상호작용 (Trigger) - 이펙트는 Update()에서 자동 처리 ==========
    public void TriggerHeal()
    {
        if (animator != null)
        {
            animator.SetTrigger("Interact");
            if (enableDebugLogs)
                Debug.Log("[MonggingAnimationEventHandler] Interact 트리거 (힐)");
        }
    }

    public void TriggerUseItem()
    {
        if (animator != null)
        {
            animator.SetTrigger("Interact");
            if (enableDebugLogs)
                Debug.Log("[MonggingAnimationEventHandler] Interact 트리거 (아이템 사용)");
        }
    }

    // ========== 지속형 상호작용 (Bool) - 이펙트는 Update()에서 자동 처리 ==========

    // 기본 상호작용 (이펙트 없음, 확장성용)
    public void SetInteracting(bool isInteracting)
    {
        if (animator != null)
        {
            animator.SetBool("IsInteracting", isInteracting);
            if (enableDebugLogs)
                Debug.Log($"[MonggingAnimationEventHandler] IsInteracting 파라미터: {isInteracting}");
        }
    }

    public void SetDigging(bool isDigging)
    {
        if (animator != null)
        {
            animator.SetBool("IsDigging", isDigging);
            if (enableDebugLogs)
                Debug.Log($"[MonggingAnimationEventHandler] IsDigging 파라미터: {isDigging}");
        }
    }

    public void SetFeeding(bool isFeeding)
    {
        if (animator != null)
        {
            animator.SetBool("IsFeeding", isFeeding);
            if (enableDebugLogs)
                Debug.Log($"[MonggingAnimationEventHandler] IsFeeding 파라미터: {isFeeding}");
        }
    }

    // ========== 유틸리티 함수 ==========

    // 일회성 이펙트 재생 (피격, 다운 등) - 하위 파티클도 모두 재생
    private void PlayOneTimeEffect(GameObject effect)
    {
        if (effect != null)
        {
            effect.SetActive(true);

            // 자신과 하위 오브젝트의 모든 ParticleSystem 찾아서 재생
            ParticleSystem[] allParticles = effect.GetComponentsInChildren<ParticleSystem>();
            float maxDuration = 0f;

            foreach (var ps in allParticles)
            {
                if (ps != null)
                {
                    ps.Play();
                    // 가장 긴 재생 시간 계산
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    if (duration > maxDuration)
                        maxDuration = duration;
                }
            }

            if (allParticles.Length > 0)
            {
                // 가장 긴 파티클 재생 시간만큼 기다린 후 비활성화
                StartCoroutine(DisableEffectAfterTime(effect, maxDuration));
                if (enableDebugLogs)
                    Debug.Log($"[MonggingAnimationEventHandler] 일회성 이펙트 재생: {effect.name} (하위 파티클 {allParticles.Length}개)");
            }
            else
            {
                // ParticleSystem이 없으면 2초 후 비활성화
                StartCoroutine(DisableEffectAfterTime(effect, 2f));
                if (enableDebugLogs)
                    Debug.Log($"[MonggingAnimationEventHandler] 일회성 이펙트 재생: {effect.name} (파티클 없음)");
            }
        }
    }

    private System.Collections.IEnumerator DisableEffectAfterTime(GameObject effect, float time)
    {
        yield return new WaitForSeconds(time);
        if (effect != null)
        {
            effect.SetActive(false);
        }
    }

    // 파티클 이펙트들 자동으로 찾기
    private void FindParticleEffects()
    {
        // 하위 오브젝트에서 이름으로 파티클 시스템 찾기
        interactEffect = FindParticleByName("interact_effect");  // 힐, 아이템 사용 공통
        feedEffect = FindParticleByName("feed_effect");
        getHealedEffect = FindParticleByName("get_healed_effect");
        takeDamageEffect = FindParticleByName("take_damage_effect");
        downEffect = FindParticleByName("down_effect");
        respawnExplosionEffect = FindParticleByName("RespawnExplosion_effect");

        Debug.Log($"[MonggingAnimationEventHandler] 파티클 이펙트 찾기 완료");
    }

    private ParticleSystem FindParticleByName(string effectName)
    {
        Transform effectTransform = transform.Find(effectName);
        if (effectTransform == null)
        {
            // 하위 오브젝트들을 재귀적으로 탐색
            effectTransform = FindInChildren(transform, effectName);
        }

        if (effectTransform != null)
        {
            ParticleSystem ps = effectTransform.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Debug.Log($"[MonggingAnimationEventHandler] {effectName} 찾음");
                return ps;
            }
        }

        Debug.LogWarning($"[MonggingAnimationEventHandler] {effectName}를 찾을 수 없습니다!");
        return null;
    }

    private Transform FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private void DisableAllParticleEffects()
    {
        if (interactEffect != null) interactEffect.gameObject.SetActive(false);
        if (feedEffect != null) feedEffect.gameObject.SetActive(false);
        if (getHealedEffect != null) getHealedEffect.gameObject.SetActive(false);
        if (takeDamageEffect != null) takeDamageEffect.gameObject.SetActive(false);
        if (downEffect != null) downEffect.gameObject.SetActive(false);
        if (respawnExplosionEffect != null) respawnExplosionEffect.gameObject.SetActive(false);
    }

    // 이전 상태 추적 변수들
    private bool wasInInteractState = false;  // Interact State 추적
    private bool wasInInteractingState = false;  // Interacting State 추적
    private bool wasInteracting = false;      // IsInteracting Bool 추적
    private bool wasDigging = false;
    private bool wasFeeding = false;
    private bool wasInDownState = false;      // Down State 추적
    private bool wasGettingHealed = false;
    private bool wasTakingDamage = false;
    private bool wasReviving = false;
    private bool wasDying = false;

    // 오디오 재생 간격 제어
    private float lastInteractingAudioTime = 0f;
    private float interactingAudioInterval = 1f; // 1초 간격

    void Update()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);


            // Trigger 상태들 (State 이름으로 체크)
            bool isInInteractState = stateInfo.IsName("Interact");  // 힐/아이템 사용 트리거
            bool isInInteractingState = stateInfo.IsName("Interacting");  // 지속적 상호작용 상태
            bool isTakingDamage = stateInfo.IsName("Take Damage");
            bool isReviving = stateInfo.IsName("Revive");
            bool isDying = stateInfo.IsName("Die");
            bool isInDownState = stateInfo.IsName("Down");  // Down 애니메이션 트리거

            // Boolean 파라미터들 (직접 체크)
            bool isInteracting = animator.GetBool("IsInteracting");  // 지속적 상호작용
            bool isDigging = animator.GetBool("IsDigging");
            bool isFeeding = animator.GetBool("IsFeeding");
            bool isGettingHealed = animator.GetBool("IsGettingHealed");

            // Interact State (힐, 아이템 사용 트리거) - 애니메이션과 동기화
            if (isInInteractState && !wasInInteractState)
            {
                if (interactEffect != null)
                {
                    interactEffect.gameObject.SetActive(true);
                    // 하위 파티클도 모두 재생
                    ParticleSystem[] allParticles = interactEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                        {
                            var main = ps.main;
                            main.loop = false;  // 루프 끄기
                            ps.Play();
                        }
                    }
                    if (enableDebugLogs)
                        Debug.Log("[MonggingAnimationEventHandler] Interact State 시작 - interact_effect 재생");
                }
            }
            else if (!isInInteractState && wasInInteractState)
            {
                // Interact State 종료 시 이펙트도 즉시 정지
                if (interactEffect != null)
                {
                    ParticleSystem[] allParticles = interactEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                            ps.Stop();
                    }
                    interactEffect.gameObject.SetActive(false);
                    if (enableDebugLogs)
                        Debug.Log("[MonggingAnimationEventHandler] Interact State 종료 - interact_effect 정지");
                }
            }

            // Interacting State (지속적 상호작용) - 이펙트 재생
            if (isInInteractingState && !wasInInteractingState)
            {
                // 상호작용 이펙트 시작 (지속)
                if (interactEffect != null)
                {
                    interactEffect.gameObject.SetActive(true);
                    // 하위 파티클도 모두 재생
                    ParticleSystem[] allParticles = interactEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                        {
                            var main = ps.main;
                            main.loop = true;  // 지속적 루프
                            ps.Play();
                        }
                    }

                    // 오디오 첫 재생 및 타이머 초기화
                    AudioSource audioSource = interactEffect.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.Play();
                        lastInteractingAudioTime = Time.time;
                    }

                    if (enableDebugLogs)
                        Debug.Log($"[MonggingAnimationEventHandler] Interacting State 시작 - interactEffect 지속 재생");
                }
            }
            else if (isInInteractingState && wasInInteractingState)
            {
                // Interacting State 지속 중 - 1초마다 오디오 재생
                if (interactEffect != null && Time.time - lastInteractingAudioTime >= interactingAudioInterval)
                {
                    AudioSource audioSource = interactEffect.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.Play();
                        lastInteractingAudioTime = Time.time;
                        if (enableDebugLogs)
                            Debug.Log("[MonggingAnimationEventHandler] Interacting 오디오 재생 (1초 간격)");
                    }
                }
            }
            else if (!isInInteractingState && wasInInteractingState)
            {
                // 상호작용 이펙트 종료
                if (interactEffect != null)
                {
                    // 하위 파티클도 모두 정지
                    ParticleSystem[] allParticles = interactEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                            ps.Stop();
                    }
                    interactEffect.gameObject.SetActive(false);
                    if (enableDebugLogs)
                        Debug.Log("[MonggingAnimationEventHandler] Interacting State 종료");
                }
            }

            // IsInteracting Bool (확장성용)
            if (isInteracting && !wasInteracting)
            {
                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] IsInteracting Bool 시작");
            }
            else if (!isInteracting && wasInteracting)
            {
                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] IsInteracting Bool 종료");
            }

            // Digging 상태
            if (isDigging && !wasDigging)
            {
                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] Digging 시작");
                // 땅파기 이펙트는 추후 추가 가능
            }
            else if (!isDigging && wasDigging)
            {
                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] Digging 종료");
            }

            // Feeding 상태
            if (isFeeding && !wasFeeding)
            {
                if (feedEffect != null)
                {
                    feedEffect.gameObject.SetActive(true);
                    // 하위 파티클도 모두 재생
                    ParticleSystem[] allParticles = feedEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                        {
                            var main = ps.main;
                            main.loop = true;
                            ps.Play();
                        }
                    }
                    if (enableDebugLogs)
                        Debug.Log($"[MonggingAnimationEventHandler] Feeding 시작 - feedEffect 재생 (하위 파티클 {allParticles.Length}개, 반복)");
                }
            }
            else if (!isFeeding && wasFeeding)
            {
                if (feedEffect != null)
                {
                    // 하위 파티클도 모두 정지
                    ParticleSystem[] allParticles = feedEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                            ps.Stop();
                    }
                    feedEffect.gameObject.SetActive(false);
                    if (enableDebugLogs)
                        Debug.Log("[MonggingAnimationEventHandler] Feeding 종료");
                }
            }

            // Down 상태 (애니메이션은 한번, 이펙트는 지속)
            if (isInDownState)
            {
                // Down State에 있는 동안 이펙트 계속 재생
                if (!wasInDownState)
                {
                    // Down 시작 - 이펙트 시작
                    if (downEffect != null)
                    {
                        downEffect.gameObject.SetActive(true);
                        // 하위 파티클도 모두 재생
                        ParticleSystem[] allParticles = downEffect.GetComponentsInChildren<ParticleSystem>();
                        foreach (var ps in allParticles)
                        {
                            if (ps != null)
                            {
                                var main = ps.main;
                                main.loop = true;
                                ps.Play();
                            }
                        }
                        if (enableDebugLogs)
                            Debug.Log($"[MonggingAnimationEventHandler] Down 시작 - 이펙트 재생 (하위 파티클 {allParticles.Length}개)");
                    }
                }
            }
            else
            {
                // Down State 벗어남 - 이펙트 정지
                if (wasInDownState)
                {
                    if (downEffect != null)
                    {
                        // 하위 파티클도 모두 정지
                        ParticleSystem[] allParticles = downEffect.GetComponentsInChildren<ParticleSystem>();
                        foreach (var ps in allParticles)
                        {
                            if (ps != null)
                                ps.Stop();
                        }
                        downEffect.gameObject.SetActive(false);
                        if (enableDebugLogs)
                            Debug.Log("[MonggingAnimationEventHandler] Down 종료 - 이펙트 정지");
                    }
                }
            }

            // TakeDamage 상태 (일회성 이펙트)
            if (isTakingDamage && !wasTakingDamage)
            {
                if (takeDamageEffect != null)
                {
                    PlayOneTimeEffect(takeDamageEffect.gameObject);
                    if (enableDebugLogs)
                        Debug.Log("[MonggingAnimationEventHandler] TakeDamage 시작 - takeDamageEffect 일회성 재생");
                }
            }

            // Revive 상태
            if (isReviving && !wasReviving)
            {
                // RespawnExplosion 이펙트 재생
                if (respawnExplosionEffect != null)
                {
                    PlayOneTimeEffect(respawnExplosionEffect.gameObject);
                    if (enableDebugLogs)
                        Debug.Log("[MonggingAnimationEventHandler] Revive 시작 - RespawnExplosion_effect 재생");
                }
            }
            else if (!isReviving && wasReviving)
            {
                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] Revive 종료");
            }

            // Die 상태
            if (isDying && !wasDying)
            {
                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] Die 시작");
                // Die 이펙트는 추후 추가 가능
            }
            else if (!isDying && wasDying)
            {
                if (enableDebugLogs)
                    Debug.Log("[MonggingAnimationEventHandler] Die 종료");
            }

            // GetHealed 상태 (Bool 파라미터)
            if (isGettingHealed && !wasGettingHealed)
            {
                if (getHealedEffect != null)
                {
                    getHealedEffect.gameObject.SetActive(true);
                    // 하위 파티클도 모두 재생
                    ParticleSystem[] allParticles = getHealedEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                            ps.Play();
                    }
                    if (enableDebugLogs)
                        Debug.Log($"[MonggingAnimationEventHandler] GetHealed 시작 (하위 파티클 {allParticles.Length}개)");
                }
            }
            else if (!isGettingHealed && wasGettingHealed)
            {
                if (getHealedEffect != null)
                {
                    // 하위 파티클도 모두 정지
                    ParticleSystem[] allParticles = getHealedEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in allParticles)
                    {
                        if (ps != null)
                            ps.Stop();
                    }
                    getHealedEffect.gameObject.SetActive(false);
                    if (enableDebugLogs)
                        Debug.Log("[MonggingAnimationEventHandler] GetHealed 종료");
                }
            }

            // 이전 상태 업데이트
            wasInInteractState = isInInteractState;
            wasInInteractingState = isInInteractingState;
            wasInteracting = isInteracting;
            wasDigging = isDigging;
            wasFeeding = isFeeding;
            wasInDownState = isInDownState;
            wasGettingHealed = isGettingHealed;
            wasTakingDamage = isTakingDamage;
            wasReviving = isReviving;
            wasDying = isDying;
        }
    }

    void OnDestroy()
    {
        if (movementService != null)
        {
            movementService.MovingStateChanged -= OnMovingStateChanged;
        }
    }
}