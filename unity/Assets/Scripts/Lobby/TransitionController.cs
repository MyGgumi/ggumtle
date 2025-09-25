using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using System;

public class TransitionController : MonoBehaviour
{
    [Header("Timeline 설정")]
    public PlayableDirector timeline;

    [Header("카메라 회전 설정")]
    public Transform cameraTransform; // 회전할 카메라 또는 카메라 부모 오브젝트
    public float rotationSpeed = 1f; // 회전에 걸리는 시간 (초)

    private bool isPlayingReverse = false;
    private bool isRotating = false; // 회전 중인지 확인
    private double timelineDuration;
    
    // 콜백 이벤트
    private Action onTransitionComplete;
    private Action onReverseComplete;

    public void Initialize()
    {
        // 카메라 Transform이 설정되지 않았으면 메인 카메라 찾기
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
                Debug.Log("메인 카메라를 카메라 Transform으로 자동 설정");
            }
            else
            {
                Debug.LogWarning("카메라 Transform이 설정되지 않았습니다. Inspector에서 설정해주세요.");
            }
        }

        // Timeline 설정
        if (timeline != null)
        {
            timeline.Stop();
            timeline.time = 0;
            timelineDuration = timeline.duration;
        }

        Debug.Log("TransitionController 초기화 완료");
    }

    void Update()
    {
        // 역방향 재생 업데이트
        if (isPlayingReverse && timeline != null)
        {
            timeline.time -= Time.deltaTime;
            timeline.Evaluate();

            if (timeline.time <= 0)
            {
                timeline.time = 0;
                timeline.Evaluate();
                isPlayingReverse = false;
                OnReverseCompleteInternal();
            }
        }
    }

    // === Timeline 관련 메서드 ===

    // 전환 애니메이션 시작
    public void StartTransition(Action onComplete = null)
    {
        if (timeline != null)
        {
            // 기존 콜백 제거 (중복 등록 방지)
            timeline.stopped -= OnTransitionCompleteInternal;

            onTransitionComplete = onComplete;
            isPlayingReverse = false;
            timeline.time = 0;
            timeline.Play();

            // Timeline 완료 시 콜백 등록
            timeline.stopped += OnTransitionCompleteInternal;

            Debug.Log("전환 애니메이션 시작");
        }
    }

    // 역재생 시작
    public void StartReverse(Action onComplete = null)
    {
        if (timeline != null)
        {
            onReverseComplete = onComplete;
            isPlayingReverse = true;

            // 현재 위치에서 역재생 시작
            if (timeline.time <= 0)
            {
                timeline.time = timelineDuration;
            }

            timeline.Pause(); // Pause 상태에서 수동 조작
            Debug.Log("역재생 시작");
        }
    }

    // Timeline 완료 시 호출
    void OnTransitionCompleteInternal(PlayableDirector director)
    {
        // Timeline 이벤트 해제
        timeline.stopped -= OnTransitionCompleteInternal;

        // 콜백 실행
        onTransitionComplete?.Invoke();
        onTransitionComplete = null;

        Debug.Log("Timeline 전환 완료");
    }

    // 역재생 완료 시 호출
    void OnReverseCompleteInternal()
    {
        // 콜백 실행
        onReverseComplete?.Invoke();
        onReverseComplete = null;

        Debug.Log("Timeline 역재생 완료");
    }

    // === 카메라 회전 기능 ===

    // 강화 화면으로 카메라 회전
    public void RotateCameraToEnhance()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("카메라 Transform이 설정되지 않았습니다!");
            return;
        }

        if (isRotating)
        {
            Debug.Log("이미 카메라가 회전 중입니다.");
            return;
        }

        StartCoroutine(RotateCameraCoroutine(-150f));
    }

    // 카메라를 원래 위치로 되돌리기
    public void ResetCameraRotation()
    {
        if (cameraTransform == null || isRotating) return;

        StartCoroutine(RotateCameraCoroutine(150f));
    }

    // 카메라를 특정 각도만큼 회전시키는 코루틴
    IEnumerator RotateCameraCoroutine(float rotationAngle)
    {
        isRotating = true;

        Vector3 startRotation = cameraTransform.eulerAngles;
        Vector3 targetRotation = startRotation + new Vector3(0, rotationAngle, 0);

        float elapsedTime = 0f;

        Debug.Log($"카메라 회전 시작: {startRotation.y}° → {targetRotation.y}°");

        while (elapsedTime < rotationSpeed)
        {
            // 부드러운 회전을 위한 이징 (시작과 끝에서 천천히)
            float t = elapsedTime / rotationSpeed;
            t = t * t * (3.0f - 2.0f * t); // Smoothstep 함수

            // Y축 회전값 보간
            float currentY = Mathf.LerpAngle(startRotation.y, targetRotation.y, t);
            cameraTransform.eulerAngles = new Vector3(startRotation.x, currentY, startRotation.z);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 최종 각도로 정확히 설정
        cameraTransform.eulerAngles = targetRotation;
        isRotating = false;

        Debug.Log($"카메라 회전 완료: {targetRotation.y}°");

        // 회전 완료 후 안드로이드로 메시지 전송 (선택사항)
        SendMessageToAndroid("CameraRotationComplete");
    }

    // === 상태 확인 메서드 ===

    public bool IsTransitionPlaying()
    {
        return timeline != null && timeline.state == PlayState.Playing;
    }

    public bool IsReversePlayingActive()
    {
        return isPlayingReverse;
    }

    public bool IsCameraRotating()
    {
        return isRotating;
    }

    public double GetTimelineProgress()
    {
        if (timeline != null && timelineDuration > 0)
        {
            return timeline.time / timelineDuration;
        }
        return 0;
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

    public void Cleanup()
    {
        // Timeline 이벤트 해제
        if (timeline != null)
        {
            timeline.stopped -= OnTransitionCompleteInternal;
        }

        // 진행 중인 코루틴 정리
        StopAllCoroutines();

        // 콜백 정리
        onTransitionComplete = null;
        onReverseComplete = null;

        Debug.Log("TransitionController 리소스 정리 완료");
    }

    void OnDestroy()
    {
        Cleanup();
    }
}