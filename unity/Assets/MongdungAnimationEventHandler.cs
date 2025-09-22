using UnityEngine;
using VContainer;
using Features.Player.Services;
using System.Collections;
using System.Collections.Generic;

public class MongdungAnimationEventHandler : MonoBehaviour
{
    private Animator animator;
    private PlayerMovementService movementService;
    private AudioSource audioSource;

    private ParticleSystem attackEffect;
    private ParticleSystem trapSettingEffect;
    private ParticleSystem frightenEffect;

    private ParticleSystem[] allEffects;

    [SerializeField] private bool enableDebugLogs = false;

    private bool wasAttacking = false;
    private bool wasTrapSetting = false;
    private bool wasFrightened = false;

    void Start()
    {
        // Animator 찾기
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();

            if (enableDebugLogs)
            {
                Debug.Log("[MongdungAnimationEventHandler] 모든 하위 오브젝트:");
                foreach (Transform child in transform)
                {
                    Debug.Log(" - " + child.name);
                }
            }
        }

        // AudioSource 찾기
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }

        // 이펙트 찾기
        FindParticleEffects();

        // 모든 이펙트 초기화 (꺼진 상태로)
        InitializeEffects();

        // PlayerMovementService 연결
        var lifetimeScope = FindFirstObjectByType<DI.MainLifetimeScope>();
        if (lifetimeScope != null)
        {
            movementService = lifetimeScope.Container.Resolve<PlayerMovementService>();

            if (movementService != null)
            {
                movementService.MovingStateChanged += OnMovingStateChanged;
                animator.SetBool("IsMoving", movementService.IsMoving);

                if (enableDebugLogs)
                    Debug.Log("[MongdungAnimationEventHandler] PlayerMovementService 연결 완료");
            }
        }
    }

    private void FindParticleEffects()
    {
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            string particleName = particle.name.ToLower();

            if (particleName.Contains("attack"))
            {
                attackEffect = particle;
                if (enableDebugLogs) Debug.Log($"[MongdungAnimationEventHandler] attack_effect 찾음: {particle.name}");
            }
            else if (particleName.Contains("trap") && particleName.Contains("setting"))
            {
                trapSettingEffect = particle;
                if (enableDebugLogs) Debug.Log($"[MongdungAnimationEventHandler] trap_setting_effect 찾음: {particle.name}");
            }
            else if (particleName.Contains("frighten"))
            {
                frightenEffect = particle;
                if (enableDebugLogs) Debug.Log($"[MongdungAnimationEventHandler] frighten_effect 찾음: {particle.name}");
            }
        }

        if (attackEffect == null && enableDebugLogs)
            Debug.LogWarning("[MongdungAnimationEventHandler] attack_effect를 찾을 수 없습니다!");
        if (trapSettingEffect == null && enableDebugLogs)
            Debug.LogWarning("[MongdungAnimationEventHandler] trap_setting_effect를 찾을 수 없습니다!");
        if (frightenEffect == null && enableDebugLogs)
            Debug.LogWarning("[MongdungAnimationEventHandler] frighten_effect를 찾을 수 없습니다!");
    }

    private void OnMovingStateChanged(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);

            if (enableDebugLogs)
                Debug.Log($"[MongdungAnimationEventHandler] IsMoving: {isMoving}");
        }
    }

    void Update()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            bool isAttacking = stateInfo.IsName("Dash Attack In Place");
            bool isTrapSetting = stateInfo.IsName("Trap Setting");
            bool isFrightened = stateInfo.IsName("Frighten");

            if (isAttacking && !wasAttacking)
            {
                PlayEffect(attackEffect, "Attack");
                PlayAttackAudio();
            }
            else if (!isAttacking && wasAttacking)
            {
                StopEffect(attackEffect, "Attack");
            }

            if (isTrapSetting && !wasTrapSetting)
            {
                PlayOneTimeEffect(trapSettingEffect, "Trap Setting");
            }

            if (isFrightened && !wasFrightened)
            {
                PlayOneTimeEffectWithChildren(frightenEffect, "Frighten");
            }

            wasAttacking = isAttacking;
            wasTrapSetting = isTrapSetting;
            wasFrightened = isFrightened;
        }
    }

    private void PlayEffect(ParticleSystem effect, string effectName)
    {
        if (effect != null)
        {
            effect.gameObject.SetActive(true);
            effect.Play();
            if (enableDebugLogs)
                Debug.Log($"[MongdungAnimationEventHandler] {effectName} 이펙트 시작");
        }
    }

    private void StopEffect(ParticleSystem effect, string effectName)
    {
        if (effect != null)
        {
            effect.Stop();
            effect.gameObject.SetActive(false);
            if (enableDebugLogs)
                Debug.Log($"[MongdungAnimationEventHandler] {effectName} 이펙트 정지");
        }
    }

    private void InitializeEffects()
    {
        if (attackEffect != null)
        {
            attackEffect.gameObject.SetActive(false);
            attackEffect.Stop();
        }
        if (trapSettingEffect != null)
        {
            trapSettingEffect.gameObject.SetActive(false);
            trapSettingEffect.Stop();
        }
        if (frightenEffect != null)
        {
            frightenEffect.gameObject.SetActive(false);
            frightenEffect.Stop();

            // frighten_effect의 하위 이펙트들도 초기화
            ParticleSystem[] childEffects = frightenEffect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem child in childEffects)
            {
                child.gameObject.SetActive(false);
                child.Stop();
            }
        }
    }

    private void PlayOneTimeEffect(ParticleSystem effect, string effectName)
    {
        if (effect != null)
        {
            effect.gameObject.SetActive(true);
            effect.Play();
            if (enableDebugLogs)
                Debug.Log($"[MongdungAnimationEventHandler] {effectName} 이펙트 원샷 재생");

            StartCoroutine(StopEffectAfterDuration(effect, effectName));
        }
    }

    private void PlayOneTimeEffectWithChildren(ParticleSystem effect, string effectName)
    {
        if (effect != null)
        {
            // 부모 오브젝트의 모든 하위 ParticleSystem 찾기
            ParticleSystem[] childEffects = effect.transform.GetComponentsInChildren<ParticleSystem>(true);

            if (enableDebugLogs)
                Debug.Log($"[MongdungAnimationEventHandler] {effectName}에서 {childEffects.Length}개 이펙트 찾음");

            foreach (ParticleSystem child in childEffects)
            {
                child.gameObject.SetActive(true);
                child.Play();
                if (enableDebugLogs)
                    Debug.Log($"[MongdungAnimationEventHandler] {child.name} 이펙트 재생");
            }

            StartCoroutine(StopEffectWithChildrenAfterDuration(effect, effectName));
        }
    }

    private IEnumerator StopEffectWithChildrenAfterDuration(ParticleSystem effect, string effectName)
    {
        if (effect != null)
        {
            yield return new WaitForSeconds(effect.main.duration + effect.main.startLifetime.constantMax);

            if (effect != null)
            {
                effect.Stop();
                effect.gameObject.SetActive(false);

                // 하위 이펙트들도 정지
                ParticleSystem[] childEffects = effect.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem child in childEffects)
                {
                    child.Stop();
                    child.gameObject.SetActive(false);
                }

                if (enableDebugLogs)
                    Debug.Log($"[MongdungAnimationEventHandler] {effectName} 이펙트와 하위 이펙트들 자동 정지");
            }
        }
    }

    private IEnumerator StopEffectAfterDuration(ParticleSystem effect, string effectName)
    {
        if (effect != null)
        {
            yield return new WaitForSeconds(effect.main.duration + effect.main.startLifetime.constantMax);

            if (effect != null)
            {
                effect.Stop();
                effect.gameObject.SetActive(false);
                if (enableDebugLogs)
                    Debug.Log($"[MongdungAnimationEventHandler] {effectName} 이펙트 자동 정지");
            }
        }
    }

    private void PlayAttackAudio()
    {
        if (audioSource != null)
        {
            AudioClip[] audioClips = Resources.LoadAll<AudioClip>("");
            AudioClip swingClip = null;

            foreach (AudioClip clip in audioClips)
            {
                if (clip.name == "Swing4-Free-1")
                {
                    swingClip = clip;
                    break;
                }
            }

            if (swingClip != null)
            {
                audioSource.PlayOneShot(swingClip);
                if (enableDebugLogs)
                    Debug.Log("[MongdungAnimationEventHandler] Attack 오디오 재생: Swing4-Free-1");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[MongdungAnimationEventHandler] Swing4-Free-1 오디오 클립을 찾을 수 없습니다!");
            }
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
