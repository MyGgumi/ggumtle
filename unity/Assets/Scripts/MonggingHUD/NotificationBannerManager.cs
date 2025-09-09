using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class NotificationBannerManager : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private VisualElement _banner;
    private Label _bannerLabel;
    
    [Header("Settings")]
    public float defaultDisplayDuration = 2.2f;
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;
    
    [Header("Current State")]
    public bool isShowing = false;
    public string currentMessage = "";
    private bool isInitialized = false;
    
    private Coroutine _currentBannerCoroutine;
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        HideBanner(immediate: true);
        isInitialized = true;
    }
    
    private void CacheUIElements()
    {
        _banner = _root.Q<VisualElement>("topBanner");
        _bannerLabel = _banner?.Q<Label>();
        
        // UXML의 하드코딩된 배너 텍스트 즉시 제거
        if (_bannerLabel != null)
        {
            _bannerLabel.text = "";
        }
    }
    
    public void ShowBanner(string message, float duration = -1f) //알림 메시지, 뜨는 시간
    {
        if (string.IsNullOrEmpty(message)) return;
        if (!isInitialized || _root == null)
        {
            Debug.LogWarning($"[NotificationBannerManager] 아직 초기화되지 않았습니다. 메시지를 표시할 수 없습니다: {message}");
            return;
        }
        
        currentMessage = message;
        float displayDuration = duration > 0 ? duration : defaultDisplayDuration;
        
        // 배너 레이블을 다시 찾아서 텍스트 설정
        _banner = _root.Q<VisualElement>("topBanner");
        _bannerLabel = _banner?.Q<Label>();
        
        if (_bannerLabel != null)
        {
            _bannerLabel.text = message;
        }
        else
        {
            Debug.LogError("[NotificationBannerManager] 배너 레이블을 찾을 수 없습니다.");
        }
        
        // 이전 코루틴 중단
        if (_currentBannerCoroutine != null)
        {
            StopCoroutine(_currentBannerCoroutine);
        }
        
        _currentBannerCoroutine = StartCoroutine(ShowBannerCoroutine(displayDuration));
        Debug.Log($"[NotificationBannerManager] 배너 텍스트 설정: {message} ({displayDuration}초)");
    }
    
    public void HideBanner(bool immediate = false)
    {
        if (_currentBannerCoroutine != null)
        {
            StopCoroutine(_currentBannerCoroutine);
            _currentBannerCoroutine = null;
        }
        
        if (immediate)
        {
            SetBannerVisibility(false);
            isShowing = false;
        }
        else
        {
            _currentBannerCoroutine = StartCoroutine(HideBannerCoroutine());
        }
        
        Debug.Log($"[NotificationBannerManager] 배너 숨김 {(immediate ? "(즉시)" : "(애니메이션)")}");
    }
    
    
    private IEnumerator ShowBannerCoroutine(float duration)
    {
        // 페이드 인
        yield return StartCoroutine(FadeInBanner());
        
        // 표시 대기
        yield return new WaitForSeconds(duration);
        
        // 페이드 아웃
        yield return StartCoroutine(FadeOutBanner());
        
        isShowing = false;
        _currentBannerCoroutine = null;
    }
    
    private IEnumerator HideBannerCoroutine()
    {
        yield return StartCoroutine(FadeOutBanner());
        isShowing = false;
        _currentBannerCoroutine = null;
    }
    
    private IEnumerator FadeInBanner()
    {
        if (_banner == null) yield break;
        
        SetBannerVisibility(true);
        isShowing = true;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            _banner.style.opacity = alpha;
            yield return null;
        }
        
        _banner.style.opacity = 1f;
    }
    
    private IEnumerator FadeOutBanner()
    {
        if (_banner == null) yield break;
        
        float elapsed = 0f;
        float startOpacity = _banner.resolvedStyle.opacity;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startOpacity, 0f, elapsed / fadeOutDuration);
            _banner.style.opacity = alpha;
            yield return null;
        }
        
        SetBannerVisibility(false);
    }
    
    private void SetBannerVisibility(bool visible)
    {
        if (_banner != null)
        {
            if (visible)
            {
                _banner.RemoveFromClassList("hide");
                _banner.AddToClassList("show");
                _banner.style.opacity = 1f;
            }
            else
            {
                _banner.RemoveFromClassList("show");
                _banner.AddToClassList("hide");
                _banner.style.opacity = 0f;
            }
        }
    }
    
    public bool IsBannerShowing() => isShowing;
    public string GetCurrentMessage() => currentMessage;
    
    // 배너 큐 시스템 (여러 메시지를 순차적으로 표시)
    private System.Collections.Generic.Queue<(string message, float duration)> _bannerQueue 
        = new System.Collections.Generic.Queue<(string, float)>();
        
    public void QueueBanner(string message, float duration = -1f)
    {
        float displayDuration = duration > 0 ? duration : defaultDisplayDuration;
        _bannerQueue.Enqueue((message, displayDuration));
        
        if (!isShowing)
        {
            ProcessBannerQueue();
        }
    }
    
    private void ProcessBannerQueue()
    {
        if (_bannerQueue.Count > 0 && !isShowing)
        {
            var (message, duration) = _bannerQueue.Dequeue();
            ShowBanner(message, duration);
            
            // 다음 배너를 위한 지연 처리
            StartCoroutine(ProcessNextBannerAfterDelay(duration + 0.5f));
        }
    }
    
    private IEnumerator ProcessNextBannerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessBannerQueue();
    }
    
    public void ClearBannerQueue()
    {
        _bannerQueue.Clear();
        Debug.Log("[NotificationBannerManager] 배너 큐 초기화");
    }
    
    public int GetQueueCount() => _bannerQueue.Count;
}