using UnityEngine;
using System.Collections;

public class GrowthMonggingController : MonoBehaviour
{
    [Header("성장 몽깅 캐릭터들")]
    public GameObject growthHpMongging;    // GrowthHpMongging
    public GameObject growthJobMongging;   // GrowthJobMongging
    public GameObject growthHealMongging;  // GrowthHealMongging

    [Header("파티클 시스템들")]
    public ParticleSystem growStart;           // GrowStart 파티클
    public ParticleSystem growSuccess;         // GrowSuccess 파티클
    public ParticleSystem growSuccessBackground; // GrowSuccessBackground 파티클
    public ParticleSystem growFailed;          // GrowFailed 파티클

    public enum GrowthType
    {
        HpMongging = 0,
        JobMongging = 1,
        HealMongging = 2
    }

    private GrowthType currentGrowthType = GrowthType.HpMongging; // 기본값: HP 타입

    // Awake에서 파티클 PlayOnAwake를 먼저 비활성화
    void Awake()
    {
        Debug.Log("Awake: 파티클 PlayOnAwake 비활성화 시작");
        
        // 즉시 모든 파티클 시스템 찾아서 PlayOnAwake 비활성화
        ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in allParticles)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.playOnAwake = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // 완전히 정지하고 파티클 제거
                Debug.Log($"Awake에서 파티클 비활성화: {ps.name}");
            }
        }
        
        Debug.Log("Awake: 파티클 PlayOnAwake 비활성화 완료");
    }

    // 파티클 색상 설정
    void SetupParticleColors()
    {
        // GrowFailed 파티클을 회색으로 설정
        if (growFailed != null)
        {
            var main = growFailed.main;
            main.startColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 회색
            Debug.Log("GrowFailed 파티클 색상을 회색으로 설정");
        }

        // GrowSuccessBackground 파티클을 민트색으로 설정
        if (growSuccessBackground != null)
        {
            var main = growSuccessBackground.main;
            main.startColor = new Color(0.4f, 0.9f, 0.8f, 1f); // 민트색 (RGB: 102, 230, 204)
            Debug.Log("GrowSuccessBackground 파티클 색상을 민트색으로 설정");
        }
    }

    #region 초기화
    public void Initialize()
    {
        // 자동으로 자식 오브젝트 찾기 (수동 할당이 안 되어 있을 경우)
        if (growthHpMongging == null || growthJobMongging == null || growthHealMongging == null)
        {
            FindGrowthCharacters();
        }

        // 초기 상태: HP 타입만 활성화
        SetGrowthType(GrowthType.HpMongging);
        
        // 모든 파티클 시스템 다시 한번 정지 (안전장치)
        StopAllParticles();
        
        Debug.Log("GrowthMonggingController 초기화 완료 - HP 타입으로 시작, 파티클 정지");
    }

    // 자동으로 성장 캐릭터들 찾기
    void FindGrowthCharacters()
    {
        Debug.Log("=== 성장 캐릭터 찾기 시작 ===");
        Transform[] children = GetComponentsInChildren<Transform>();
        Debug.Log($"총 자식 오브젝트 수: {children.Length}");
        
        foreach (Transform child in children)
        {
            if (child == this.transform) continue; // 자기 자신은 제외
            
            string childName = child.name.ToLower();
            Debug.Log($"자식 오브젝트 확인: {child.name} -> {childName}");
            
            if (childName.Contains("growthhp"))
            {
                growthHpMongging = child.gameObject;
                Debug.Log($"✓ GrowthHpMongging 찾음: {child.name}");
            }
            else if (childName.Contains("growthjob"))
            {
                growthJobMongging = child.gameObject;
                Debug.Log($"✓ GrowthJobMongging 찾음: {child.name}");
            }
            else if (childName.Contains("growthheal"))
            {
                growthHealMongging = child.gameObject;
                Debug.Log($"✓ GrowthHealMongging 찾음: {child.name}");
            }
        }
        
        Debug.Log($"찾기 결과 - HP: {(growthHpMongging != null ? "성공" : "실패")}, Job: {(growthJobMongging != null ? "성공" : "실패")}, Heal: {(growthHealMongging != null ? "성공" : "실패")}");
        
        // 파티클 시스템들도 자동으로 찾기
        FindParticleSystems();
    }

    // 파티클 시스템들 자동으로 찾기
    void FindParticleSystems()
    {
        Debug.Log("=== 파티클 시스템 찾기 시작 ===");
        ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
        Debug.Log($"총 파티클 시스템 수: {allParticles.Length}");
        
        foreach (ParticleSystem ps in allParticles)
        {
            string particleName = ps.name.ToLower();
            Debug.Log($"파티클 시스템 확인: {ps.name} -> {particleName}");
            
            if (particleName.Contains("growstart"))
            {
                growStart = ps;
                Debug.Log($"✓ GrowStart 찾음: {ps.name}");
            }
            else if (particleName.Contains("growsuccess") && !particleName.Contains("background"))
            {
                growSuccess = ps;
                Debug.Log($"✓ GrowSuccess 찾음: {ps.name}");
            }
            else if (particleName.Contains("growsuccessbackground"))
            {
                growSuccessBackground = ps;
                Debug.Log($"✓ GrowSuccessBackground 찾음: {ps.name}");
            }
            else if (particleName.Contains("growfailed"))
            {
                growFailed = ps;
                Debug.Log($"✓ GrowFailed 찾음: {ps.name}");
            }
        }
        
        Debug.Log($"파티클 찾기 결과 - Start: {(growStart != null ? "성공" : "실패")}, Success: {(growSuccess != null ? "성공" : "실패")}, SuccessBG: {(growSuccessBackground != null ? "성공" : "실패")}, Failed: {(growFailed != null ? "성공" : "실패")}");
        
        // 찾은 파티클들의 PlayOnAwake 다시 한번 비활성화 (안전장치)
        DisablePlayOnAwake();
    }
    
    // 모든 파티클의 PlayOnAwake 설정 비활성화
    void DisablePlayOnAwake()
    {
        if (growStart != null)
        {
            var main = growStart.main;
            main.playOnAwake = false;
            growStart.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (growSuccess != null)
        {
            var main = growSuccess.main;
            main.playOnAwake = false;
            growSuccess.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (growSuccessBackground != null)
        {
            var main = growSuccessBackground.main;
            main.playOnAwake = false;
            growSuccessBackground.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (growFailed != null)
        {
            var main = growFailed.main;
            main.playOnAwake = false;
            growFailed.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        
        Debug.Log("모든 파티클의 PlayOnAwake 비활성화 및 완전 정지 완료");
    }
    #endregion

    #region 타입 변경
    // 특정 타입으로 설정
    public void SetGrowthType(GrowthType targetType)
    {
        // 모든 타입 비활성화
        HideAllGrowthTypes();

        // 선택된 타입만 활성화
        switch (targetType)
        {
            case GrowthType.HpMongging:
                if (growthHpMongging != null)
                {
                    SetGrowthVisible(growthHpMongging, true);
                }
                break;
            case GrowthType.JobMongging:
                if (growthJobMongging != null)
                {
                    SetGrowthVisible(growthJobMongging, true);
                }
                break;
            case GrowthType.HealMongging:
                if (growthHealMongging != null)
                {
                    SetGrowthVisible(growthHealMongging, true);
                }
                break;
        }

        currentGrowthType = targetType;
        Debug.Log($"성장 타입 변경: {targetType}");
    }

    // 다음 타입으로 순환 (HP -> Job -> Heal -> HP ...)
    public void ChangeToNextGrowthType()
    {
        GrowthType nextType = (GrowthType)(((int)currentGrowthType + 1) % 3);
        SetGrowthType(nextType);
    }

    // 이전 타입으로 순환 (HP -> Heal -> Job -> HP ...)
    public void ChangeToPreviousGrowthType()
    {
        GrowthType prevType = (GrowthType)(((int)currentGrowthType - 1 + 3) % 3);
        SetGrowthType(prevType);
    }

    // 앞으로 이동 (안드로이드에서 호출) - HP -> Job -> Heal -> HP
    public void MoveToNextType()
    {
        ChangeToNextGrowthType();
        Debug.Log($"안드로이드 명령으로 다음 타입으로 이동: {currentGrowthType}");
    }

    // 뒤로 이동 (안드로이드에서 호출) - HP -> Heal -> Job -> HP
    public void MoveToPreviousType()
    {
        ChangeToPreviousGrowthType();
        Debug.Log($"안드로이드 명령으로 이전 타입으로 이동: {currentGrowthType}");
    }

    // 기존 메서드 (호환성 유지) - 다음 타입으로 이동
    public void ChangeGrowthType()
    {
        ChangeToNextGrowthType();
        Debug.Log($"안드로이드 명령으로 성장 타입 변경: {currentGrowthType}");
    }
    #endregion

    #region 가시성 제어
    // 모든 성장 타입 숨기기
    void HideAllGrowthTypes()
    {
        if (growthHpMongging != null)
            SetGrowthVisible(growthHpMongging, false);
        if (growthJobMongging != null)
            SetGrowthVisible(growthJobMongging, false);
        if (growthHealMongging != null)
            SetGrowthVisible(growthHealMongging, false);
    }

    // 성장 캐릭터의 가시성 제어 (렌더러만 제어, 애니메이터는 유지)
    void SetGrowthVisible(GameObject growthCharacter, bool visible)
    {
        if (growthCharacter == null) return;

        // 렌더러 제어
        Renderer[] renderers = growthCharacter.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
        
        // 콜라이더 제어
        Collider[] colliders = growthCharacter.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = visible;
            }
        }
    }
    #endregion

    #region 파티클 시스템 제어
    // 강화 성공 파티클 실행 (GrowStart -> GrowSuccess -> GrowSuccessBackground)
    public void PlayEnhanceSuccessEffect()
    {
        Debug.Log("강화 성공 파티클 순차 실행 시작");
        StartCoroutine(PlaySuccessSequence());
    }
    
    // 강화 성공 파티클 순차 실행 코루틴
    IEnumerator PlaySuccessSequence()
    {
        StopAllParticles();
        yield return new WaitForSeconds(0.1f); // 정지 후 잠시 대기
        
        // 1단계: GrowStart 실행
        if (growStart != null)
        {
            growStart.Play();
            Debug.Log("1단계: GrowStart 파티클 실행");
            yield return new WaitForSeconds(2.5f); // 2.5초 대기
        }
        
        // 2단계: GrowSuccess 실행
        if (growSuccess != null)
        {
            growSuccess.Play();
            Debug.Log("2단계: GrowSuccess 파티클 실행");
            yield return new WaitForSeconds(1f); // 1초 대기
        }
        
        // 3단계: GrowSuccessBackground 실행
        if (growSuccessBackground != null)
        {
            growSuccessBackground.Play();
            Debug.Log("3단계: GrowSuccessBackground 파티클 실행");
            yield return new WaitForSeconds(3f); // 3초 대기
            StopAllParticles();
        }
        
        Debug.Log("강화 성공 파티클 순차 실행 완료");
    }
    
    // 강화 실패 파티클 실행 (GrowStart -> GrowFailed)
    public void PlayEnhanceFailEffect()
    {
        Debug.Log("강화 실패 파티클 순차 실행 시작");
        StartCoroutine(PlayFailSequence());
    }
    
    // 강화 실패 파티클 순차 실행 코루틴
    IEnumerator PlayFailSequence()
    {
        StopAllParticles();
        yield return new WaitForSeconds(0.1f); // 정지 후 잠시 대기
        
        // 1단계: GrowStart 실행
        if (growStart != null)
        {
            growStart.Play();
            Debug.Log("1단계: GrowStart 파티클 실행");
            yield return new WaitForSeconds(2.5f); // 2.5초 대기
        }

        // 2단계: GrowFailed 실행
        if (growFailed != null)
        {
            growFailed.Play();
            Debug.Log("2단계: GrowFailed 파티클 실행");
            yield return new WaitForSeconds(1f); // 1초 대기
            StopAllParticles();
        }
        
        Debug.Log("강화 실패 파티클 순차 실행 완료");
    }
    
    // 모든 파티클 정지
    public void StopAllParticles()
    {
        Debug.Log("모든 파티클 정지");
        
        if (growStart != null) 
        {
            growStart.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (growSuccess != null) 
        {
            growSuccess.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (growSuccessBackground != null) 
        {
            growSuccessBackground.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (growFailed != null) 
        {
            growFailed.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    #endregion

    #region 공개 메서드 (외부에서 호출 가능)
    // 현재 타입 반환
    public GrowthType GetCurrentGrowthType()
    {
        return currentGrowthType;
    }

    // 현재 타입 문자열로 반환
    public string GetCurrentGrowthTypeString()
    {
        return currentGrowthType.ToString();
    }

    // 특정 타입이 현재 활성화되어 있는지 확인
    public bool IsGrowthTypeActive(GrowthType type)
    {
        return currentGrowthType == type;
    }
    #endregion

    #region 디버그
    // 현재 상태 출력
    public void PrintCurrentState()
    {
        Debug.Log($"=== 성장 몽깅 현재 상태 ===");
        Debug.Log($"현재 타입: {currentGrowthType}");
        Debug.Log($"HP 몽깅: {(growthHpMongging != null ? "존재" : "없음")}");
        Debug.Log($"Job 몽깅: {(growthJobMongging != null ? "존재" : "없음")}");
        Debug.Log($"Heal 몽깅: {(growthHealMongging != null ? "존재" : "없음")}");
    }
    #endregion
}