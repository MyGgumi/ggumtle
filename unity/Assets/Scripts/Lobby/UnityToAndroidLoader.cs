using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class UnityToAndroidLoader : MonoBehaviour
{
    [Header("로딩 설정")]
    public bool enableAndroidCommunication = true;
    public string targetSceneName = "Lobby";
    [Header("로딩 시뮬레이션")]
    public float minimumLoadingTime = 2f; // 최소 로딩 시간
    public bool smoothProgress = true; // 부드러운 진행률 표시
    
    private AndroidJavaObject activity;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    
    void Start()
    {
        InitializeAndroidCommunication();
        
        // 현재 씬과 다르면 로딩 시작
        if (SceneManager.GetActiveScene().name != targetSceneName)
        {
            StartCoroutine(LoadSceneWithProgress());
        }
        else
        {
            // 같은 씬이면 바로 완료 처리
            SendProgressToAndroid(100, "로딩 완료!");
            HideAndroidLoadingScreen();
        }
    }
    
    void InitializeAndroidCommunication()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (enableAndroidCommunication)
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("안드로이드 통신 초기화 실패: " + e.Message);
                activity = null;
            }
        }
        #endif
    }
    
    IEnumerator LoadSceneWithProgress()
    {
        SendProgressToAndroid(0, "초기화 중...");
        yield return new WaitForSeconds(0.3f);
        
        SendProgressToAndroid(10, "리소스 준비 중...");
        yield return new WaitForSeconds(0.3f);
        
        SendProgressToAndroid(20, "씬 로딩 시작...");
        
        // AsyncOperation 시작 - allowSceneActivation을 false로 설정
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false; // 90%에서 대기
        
        float startTime = Time.time;
        bool sceneLoadingComplete = false;
        
        while (!sceneLoadingComplete)
        {
            float elapsedTime = Time.time - startTime;
            
            // Unity의 실제 진행률 (0.9까지만)
            float unityProgress = asyncLoad.progress;
            
            // 시간 기반 진행률 계산
            float timeBasedProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);
            
            if (smoothProgress)
            {
                // 부드러운 진행률 업데이트
                if (unityProgress < 0.9f)
                {
                    // 로딩 중일 때는 시간 기반과 Unity 진행률을 조합
                    targetProgress = Mathf.Lerp(20f, 80f, Mathf.Max(unityProgress / 0.9f, timeBasedProgress));
                }
                else
                {
                    // 로딩이 90%에 도달했을 때
                    targetProgress = Mathf.Lerp(80f, 90f, timeBasedProgress);
                }
                
                // 현재 진행률을 부드럽게 목표치로 이동
                currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 2f);
                
                int displayProgress = Mathf.FloorToInt(currentProgress);
                SendProgressToAndroid(displayProgress, GetLoadingMessage(displayProgress));
            }
            else
            {
                // 일반적인 진행률 표시
                int progress;
                if (unityProgress < 0.9f)
                {
                    progress = Mathf.FloorToInt(20 + (unityProgress / 0.9f) * 70);
                }
                else
                {
                    progress = 90;
                }
                
                SendProgressToAndroid(progress, GetLoadingMessage(progress));
            }
            
            // 최소 로딩 시간이 지나고 Unity 로딩이 완료되면 씬 활성화
            if (unityProgress >= 0.9f && elapsedTime >= minimumLoadingTime)
            {
                SendProgressToAndroid(95, "씬 활성화 중...");
                asyncLoad.allowSceneActivation = true;
                sceneLoadingComplete = true;
            }
            
            yield return null;
        }
        
        // 씬 활성화 대기
        while (!asyncLoad.isDone)
        {
            SendProgressToAndroid(98, "마무리 중...");
            yield return null;
        }
        
        SendProgressToAndroid(100, "로딩 완료!");
        yield return new WaitForSeconds(0.5f);
        
        HideAndroidLoadingScreen();
    }
    
    string GetLoadingMessage(int progress)
    {
        if (progress < 30) return "초기화 중...";
        else if (progress < 60) return "리소스 로딩 중...";
        else if (progress < 80) return "씬 데이터 로딩 중...";
        else if (progress < 95) return "씬 준비 중...";
        else if (progress < 100) return "마무리 중...";
        else return "로딩 완료!";
    }
    
    void SendProgressToAndroid(int progress, string message)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (activity != null && enableAndroidCommunication)
        {
            try
            {
                activity.Call("updateLoadingProgress", progress, message);
            }
            catch (System.Exception e)
            {
                Debug.LogError("진행상황 전송 실패: " + e.Message);
            }
        }
        #endif
        
        Debug.Log($"Loading: {progress}% - {message}");
    }
    
    void HideAndroidLoadingScreen()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (activity != null && enableAndroidCommunication)
        {
            try
            {
                activity.Call("hideLoadingScreen");
            }
            catch (System.Exception e)
            {
                Debug.LogError("로딩 화면 숨김 실패: " + e.Message);
            }
        }
        #endif
    }
    
    void OnDestroy()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (activity != null)
        {
            activity.Dispose();
            activity = null;
        }
        #endif
    }
}