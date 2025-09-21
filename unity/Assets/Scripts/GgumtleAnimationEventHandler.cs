using UnityEngine;

public class GgumtleAnimationEventHandler : MonoBehaviour
{
    private Animator animator;
    private ParticleSystem purified_effect;
    private ParticleSystem after_pullup_effect;
    private ParticleSystem dig_dirt_effect;
    private ParticleSystem gumttle_feeding_effect;
    private AudioSource audioSource;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }

        // 현재 오브젝트(Baloon_Ghost_v2_1)의 Main 하위에서 이펙트 찾기
        Transform mainTransform = transform.Find("Main");
        if (mainTransform != null)
        {
            Transform purifiedTransform = mainTransform.Find("purified_effect");
            if (purifiedTransform != null)
            {
                purified_effect = purifiedTransform.GetComponent<ParticleSystem>();
                Debug.Log("purified_effect 찾음");
            }

            // After_pullup_effect/Center 찾기
            Transform afterPullupTransform = mainTransform.Find("After_pullup_effect");
            if (afterPullupTransform != null)
            {
                Transform centerTransform = afterPullupTransform.Find("Center");
                if (centerTransform != null)
                {
                    after_pullup_effect = centerTransform.GetComponent<ParticleSystem>();
                    Debug.Log("After_pullup_effect/Center 찾음");
                }
                else
                {
                    // After_pullup_effect 자체에 ParticleSystem이 있을 수도 있음
                    after_pullup_effect = afterPullupTransform.GetComponent<ParticleSystem>();
                    if (after_pullup_effect != null)
                    {
                        Debug.Log("After_pullup_effect 찾음");
                    }
                }
            }

            // Dig_dirt_effect 찾기
            Transform digDirtTransform = mainTransform.Find("Dig_dirt_effect");
            if (digDirtTransform != null)
            {
                dig_dirt_effect = digDirtTransform.GetComponent<ParticleSystem>();
                if (dig_dirt_effect != null)
                {
                    Debug.Log("Dig_dirt_effect 찾음");
                }
            }

            // Gumttle_feeding_effect 찾기
            Transform feedingEffectTransform = mainTransform.Find("Gumttle_feeding_effect");
            if (feedingEffectTransform != null)
            {
                gumttle_feeding_effect = feedingEffectTransform.GetComponent<ParticleSystem>();
                if (gumttle_feeding_effect != null)
                {
                    Debug.Log("Gumttle_feeding_effect 찾음");
                }
            }
        }
        else
        {
            Debug.LogWarning("Main 오브젝트를 찾을 수 없습니다!");
        }

        // AudioSource 찾기
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }

        if (purified_effect == null)
            Debug.LogWarning("purified_effect를 찾을 수 없습니다!");
        if (after_pullup_effect == null)
            Debug.LogWarning("After_pullup_effect를 찾을 수 없습니다!");
    }

    public void OnPurifyEnd()
    {
        Debug.Log("꿈틀이오브젝트 정화 후 없앰");
        if (purified_effect != null)
        {
            purified_effect.Stop();
            purified_effect.gameObject.SetActive(false);
        }
        Destroy(gameObject); // 실제로 사라지게
    }

    // 이펙트 실행
    public void PlayPurifiedEffect()
    {
        if (purified_effect != null)
        {
            purified_effect.gameObject.SetActive(true);
            purified_effect.Play();
        }
    }

    // After_pullup_effect 실행
    public void PlayAfterPullupEffect()
    {
        if (after_pullup_effect != null)
        {
            after_pullup_effect.gameObject.SetActive(true);
            after_pullup_effect.Play();

            // 풀업 사운드 재생
            if (audioSource != null)
            {
                AudioClip pullupClip = Resources.Load<AudioClip>("pullup_ggumtle");
                if (pullupClip != null)
                {
                    audioSource.PlayOneShot(pullupClip);
                    Debug.Log("After_pullup 사운드 재생: pullup_ggumtle");
                }
                else
                {
                    Debug.LogWarning("pullup_ggumtle 오디오 클립을 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning("AudioSource가 없습니다!");
            }
        }
    }

    private bool wasDigging = false;
    private bool wasEating = false;

    void Update()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Digging 상태 체크
            bool isDigging = stateInfo.IsName("Digging");

            // Eating 상태 체크
            bool isEating = stateInfo.IsName("eat");

            // Digging 시작할 때
            if (isDigging && !wasDigging)
            {
                if (dig_dirt_effect != null)
                {
                    var main = dig_dirt_effect.main;
                    main.loop = true;

                    dig_dirt_effect.gameObject.SetActive(true);
                    dig_dirt_effect.Play();
                    Debug.Log("Digging 시작 - Dig_dirt_effect 재생 (반복)");
                }
            }
            // Digging 끝날 때
            else if (!isDigging && wasDigging)
            {
                if (dig_dirt_effect != null)
                {
                    dig_dirt_effect.Stop();
                    dig_dirt_effect.gameObject.SetActive(false);
                    Debug.Log("Digging 종료 - Dig_dirt_effect 정지");
                }
            }

            // Eating 시작할 때
            if (isEating && !wasEating)
            {
                if (gumttle_feeding_effect != null)
                {
                    var main = gumttle_feeding_effect.main;
                    main.loop = true;

                    gumttle_feeding_effect.gameObject.SetActive(true);
                    gumttle_feeding_effect.Play();
                    Debug.Log("Eating 시작 - Gumttle_feeding_effect 재생 (반복)");
                }
            }
            // Eating 끝날 때
            else if (!isEating && wasEating)
            {
                if (gumttle_feeding_effect != null)
                {
                    gumttle_feeding_effect.Stop();
                    gumttle_feeding_effect.gameObject.SetActive(false);
                    Debug.Log("Eating 종료 - Gumttle_feeding_effect 정지");
                }
            }

            wasDigging = isDigging;
            wasEating = isEating;
        }
    }
}
