using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class UnityToAndroidLoader : MonoBehaviour
{
    [Header("로딩 설정")]
    public bool enableAndroidCommunication = true;
    public string targetSceneName = "Lobby";
    [Header("로딩 시뮬레이션")]
    public float minimumLoadingTime = 2f;
    public bool smoothProgress = true;
    
    private AndroidJavaObject activity;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    
    void Start()
    {
        setSceenPortrait();
        InitializeAndroidCommunication();
        
        // 현재 씬과 다르면 로딩 시작
        if (SceneManager.GetActiveScene().name != targetSceneName)
        {
            // 씬 전환 시 오브젝트가 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadSceneWithProgress());
        }
        else
        {
            // 같은 씬이면 바로 완료 처리
            SendProgressToAndroid(100, "로딩 완료!");
            HideAndroidLoadingScreen();
        }
    }

    void setSceenPortrait()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
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
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;
        
        float startTime = Time.time;
        bool sceneLoadingComplete = false;
        
        while (!sceneLoadingComplete)
        {
            float elapsedTime = Time.time - startTime;
            float unityProgress = asyncLoad.progress;
            float timeBasedProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);
            
            if (smoothProgress)
            {
                if (unityProgress < 0.9f)
                {
                    targetProgress = Mathf.Lerp(20f, 80f, Mathf.Max(unityProgress / 0.9f, timeBasedProgress));
                }
                else
                {
                    targetProgress = Mathf.Lerp(80f, 90f, timeBasedProgress);
                }
                
                currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 2f);
                int displayProgress = Mathf.FloorToInt(currentProgress);
                SendProgressToAndroid(displayProgress, GetLoadingMessage(displayProgress));
            }
            else
            {
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
            
            if (unityProgress >= 0.9f && elapsedTime >= minimumLoadingTime)
            {
                SendProgressToAndroid(95, "씬 활성화 중...");
                asyncLoad.allowSceneActivation = true;
                sceneLoadingComplete = true;
            }
            
            yield return null;
        }
        
        // 씬 전환 완료까지 대기
        while (!asyncLoad.isDone)
        {
            SendProgressToAndroid(98, "마무리 중...");
            yield return null;
        }
        
        // 씬이 완전히 로드된 후 완료 처리
        SendProgressToAndroid(100, "로딩 완료!");
        yield return new WaitForSeconds(0.5f);
        
        HideAndroidLoadingScreen();
        
        // 작업 완료 후 이 오브젝트 파괴
        Destroy(gameObject);
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