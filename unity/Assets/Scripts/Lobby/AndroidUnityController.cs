using UnityEngine;

public class AndroidUnityController : MonoBehaviour
{
    [Header("컨트롤러 참조")]
    public WaitingRoomCharacterController characterController;
    public TransitionController transitionController;
    public GrowthMonggingController growthController;

    [Header("게임 오브젝트")]
    public GameObject homePrefabs;
    public GameObject charactersObject;

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        HandleTestInputs();
    }

    // 초기화
    void InitializeGame()
    {
        // 시작 화면 설정 - 홈과 캐릭터 비활성화
        if (homePrefabs != null) homePrefabs.SetActive(false);
        if (charactersObject != null) charactersObject.SetActive(false);

        // 각 컨트롤러 초기화
        if (characterController != null)
            characterController.Initialize();
        
        if (transitionController != null)
            transitionController.Initialize();

        if (growthController != null)
            growthController.Initialize();

        Debug.Log("게임 초기화 완료 - 시작 화면 상태");
    }

    // 테스트 용도
    void HandleTestInputs()
    {
        // 테스트용 키보드 입력
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartTransition();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            StartReverse();
        }

        // 테스트용 카메라 회전 (Z키)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            RotateCameraToEnhance();
        }

        // 테스트용 카메라 회전 (U키)
        if (Input.GetKeyDown(KeyCode.X))
        {
            ResetCameraRotation();
        }

        // 테스트용 캐릭터 추가 (1~5번 키)
        if (Input.GetKeyDown(KeyCode.Alpha1)) AddCharacterByNickname("Player1,1");
        if (Input.GetKeyDown(KeyCode.Alpha2)) AddCharacterByNickname("Player2,2");
        if (Input.GetKeyDown(KeyCode.Alpha3)) AddCharacterByNickname("Player3,3");
        if (Input.GetKeyDown(KeyCode.Alpha4)) AddCharacterByNickname("Player4,4");
        if (Input.GetKeyDown(KeyCode.Alpha5)) AddCharacterByNickname("Player5,5");
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetFirstCharacter("Player1,5");

        // 테스트용 캐릭터 삭제 (Q~T 키)
        if (Input.GetKeyDown(KeyCode.Q)) RemoveCharacterByNickname("Player1");
        if (Input.GetKeyDown(KeyCode.W)) RemoveCharacterByNickname("Player2");
        if (Input.GetKeyDown(KeyCode.E)) RemoveCharacterByNickname("Player3");
        if (Input.GetKeyDown(KeyCode.R)) RemoveCharacterByNickname("Player4");
        if (Input.GetKeyDown(KeyCode.T)) RemoveCharacterByNickname("Player5");

        // 테스트용 성장 타입 변경 (G키 - 다음, F키 - 이전)
        if (Input.GetKeyDown(KeyCode.G)) MoveToNextType();
        if (Input.GetKeyDown(KeyCode.F)) MoveToPreviousType();

        // 테스트용 강화 성공 파티클 (H키)
        if (Input.GetKeyDown(KeyCode.H)) PlayEnhanceSuccessEffect();

        // 테스트용 강화 실패 파티클 (J키)
        if (Input.GetKeyDown(KeyCode.J)) PlayEnhanceFailEffect();

        // 테스트용 파티클 정지 (K키)
        if (Input.GetKeyDown(KeyCode.K)) StopAllParticles();
        
        if (Input.GetKeyDown(KeyCode.Alpha7)) ChangeCharacterType("Player5,HealMongging,4");
    }

    // === 안드로이드에서 호출할 공개 메서드들 ===

    // 로그인 -> 대기방
    public void StartTransition()
    {
        if (transitionController != null)
        {
            transitionController.StartTransition(OnTransitionComplete);
        }
    }

    // 대기방 -> 로그인
    public void StartReverse()
    {
        if (transitionController != null)
        {
            ClearAllCharacters();
            // 홈과 캐릭터 즉시 비활성화
            if (homePrefabs != null) homePrefabs.SetActive(false);
            if (charactersObject != null) charactersObject.SetActive(false);

            transitionController.StartReverse(OnReverseComplete);
        }
    }

    // 홈 -> 강화
    public void RotateCameraToEnhance()
    {
        if (transitionController != null)
        {
            transitionController.RotateCameraToEnhance();
        }
        
        // 성장 화면으로 넘어갈 때 HP 타입으로 초기화
        if (growthController != null)
        {
            growthController.StopAllParticles();
            growthController.SetGrowthType(GrowthMonggingController.GrowthType.HpMongging);
            Debug.Log("성장 화면 진입 - HP 타입으로 초기화");
        }
    }

    // 강화 -> 홈
    public void ResetCameraRotation()
    {
        if (transitionController != null)
        {
            growthController.StopAllParticles();
            transitionController.ResetCameraRotation();
        }
    }

    // 강화 성공
    public void PlayEnhanceSuccessEffect()
    {
        if (growthController != null)
        {
            growthController.PlayEnhanceSuccessEffect();
        }
    }

    // 강화 실패
    public void PlayEnhanceFailEffect()
    {
        if (growthController != null)
        {
            growthController.PlayEnhanceFailEffect();
        }
    }

    // 성정 타입 다음 타입으로 이동 : HP -> Job -> Heal -> HP
    public void MoveToNextType()
    {
        if (growthController != null)
        {
            growthController.StopAllParticles();
            growthController.MoveToNextType();
        }
    }

    // 성정 타입 이전 타입으로 이동 : HP -> Job -> Heal -> HP
    public void MoveToPreviousType()
    {
        if (growthController != null)
        {
            growthController.StopAllParticles();
            growthController.MoveToPreviousType();
        }
    }


    // 첫 번째 캐릭터 설정 (안드로이드에서 호출)
    public void SetFirstCharacter(string paramsString) 
    {
        var parts = paramsString.Split(',');
        
        if (parts.Length < 2 || !int.TryParse(parts[1], out int level))
        {
            Debug.LogError($"Invalid parameters: {paramsString}");
            return;
        }
        
        characterController.SetFirstCharacter(parts[0], level);
    }

    // 닉네임으로 캐릭터 추가 (안드로이드에서 호출)
    public void AddCharacterByNickname(string paramsString) 
    {
        var parts = paramsString.Split(',');
        
        if (parts.Length < 2 || !int.TryParse(parts[1], out int level))
        {
            Debug.LogError($"Invalid parameters: {paramsString}");
            return;
        }
        
        characterController.AddCharacterByNickname(parts[0], level);
    }

    // 해당 닉네임의 캐릭터 타입, 레벨 변경 (안드로이드에서 호출)
    public void ChangeCharacterType(string paramsString)
    {
        var parts = paramsString.Split(',');
        
        if (parts.Length < 3 || !int.TryParse(parts[2], out int level))
        {
            Debug.LogError($"Invalid parameters: {paramsString}");
            return;
        }
        
        string nickname = parts[0];
        string characterType = parts[1];
        
        characterController?.ChangeCharacterTypeByNickname(nickname, characterType, level);
    }


    // 닉네임으로 캐릭터 삭제 (안드로이드에서 호출)
    public void RemoveCharacterByNickname(string nickname)
    {
        if (characterController != null)
        {
            characterController.RemoveCharacterByNickname(nickname);
        }
    }

    // 모든 캐릭터 제거
    public void ClearAllCharacters()
    {
        if (characterController != null)
        {
            characterController.ClearAllCharacters();
        }
    }

    // === 성장 타입 제어 메서드들 ===

    // 기존 메서드 (호환성 유지) - 다음 타입으로 이동
    public void ChangeGrowthType()
    {
        if (growthController != null)
        {
            growthController.StopAllParticles();
            growthController.ChangeGrowthType();
        }
    }

    // 모든 파티클 정지 (안드로이드에서 호출)
    public void StopAllParticles()
    {
        if (growthController != null)
        {
            growthController.StopAllParticles();
        }
    }

    // === 콜백 메서드들 ===

    // Timeline 완료 시 호출
    void OnTransitionComplete()
    {
        // 홈과 캐릭터 오브젝트 활성화
        if (homePrefabs != null) homePrefabs.SetActive(true);
        if (charactersObject != null) charactersObject.SetActive(true);

        Debug.Log("전환 완료 - 메인 화면 활성화");
    }

    // 역재생 완료 시 호출
    void OnReverseComplete()
    {
        // 시작 화면으로 돌아감
        if (homePrefabs != null) homePrefabs.SetActive(false);
        if (charactersObject != null) charactersObject.SetActive(false);

        // 모든 캐릭터 비활성화 및 정리
        if (characterController != null)
        {
            characterController.DeactivateAllCharacters();
        }

        Debug.Log("역재생 완료 - 시작 화면으로 복귀");
    }

    // === 디버그/정보 함수들 ===

    // 현재 활성 캐릭터 리스트 출력
    public void PrintActiveCharacters()
    {
        if (characterController != null)
        {
            characterController.PrintActiveCharacters();
        }
    }

    // 메모리 사용량 디버그 정보
    public void PrintMemoryInfo()
    {
        if (characterController != null)
        {
            characterController.PrintMemoryInfo();
        }
    }

    // === 안드로이드 메시지 전송 ===
    void SendMessageToAndroid(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (!activity.Get<bool>("isFinishing"))
                {
                    activity.Call("onUnityMessage", message);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"안드로이드 메시지 전송 실패: {e.Message}");
        }
#endif

        Debug.Log($"안드로이드로 메시지 전송: {message}");
    }

    // === 메모리 관리 ===

    void OnDestroy()
    {
        if (characterController != null)
        {
            characterController.Cleanup();
        }

        if (transitionController != null)
        {
            transitionController.Cleanup();
        }

        Debug.Log("AndroidUnityController 리소스 정리 완료");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("앱 일시정지 - 메모리 최적화");
        }
        else
        {
            Debug.Log("앱 재개");
        }
    }
}