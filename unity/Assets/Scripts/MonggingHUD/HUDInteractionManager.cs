using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDInteractionManager  : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private VisualElement _interactionUI;
    private Label _interactionLabel;
    private VisualElement _progressBar;
    private VisualElement _progressMask;
    private VisualElement _progressFill;
    
    [Header("Settings")]
    public bool enableInteractionSystem = true;
    public float defaultInteractionDuration = 3f;
    public bool autoHideOnComplete = true;
    
    [Header("Current State")]
    public bool isInteracting = false;
    public InteractionType currentInteractionType = InteractionType.Dig;
    public float currentProgress = 0f;
    public string currentInteractionText = "";
    public bool isCountdown = false;
    
    [Header("Colors")]
    public Color normalProgressColor = new Color(0.2f, 0.6f, 1f, 1f); // 파란색
    public Color countdownProgressColor = new Color(1f, 0.2f, 0.2f, 1f); // 빨간색
    public Color completeProgressColor = new Color(0.2f, 1f, 0.2f, 1f); // 초록색
    
    // Static 이벤트는 HUDEvents로 이동됨
    
    private Coroutine _interactionCoroutine;
    private float _interactionDuration;
    private UniversalHUDController _hudController;
    
    void Awake()
    {
        // HUD Controller 참조
        _hudController = GetComponent<UniversalHUDController>();
        
        // 이벤트 구독
        HUDEvents.OnInteractionStarted += OnInteractionStarted;
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        HUDEvents.OnInteractionStarted -= OnInteractionStarted;
    }
    
    private void OnInteractionStarted(InteractionType type, float duration, bool isCountdown)
    {
        // 몽둥이는 상호작용 불가
        if (_hudController != null && !_hudController.CanInteract())
        {
            Debug.Log("[InteractionManager] 몽둥이는 상호작용할 수 없습니다.");
            return;
        }
        
        if (!enableInteractionSystem) return;
        
        // 이미 동일한 상호작용이 진행 중이면 무시
        if (isInteracting && currentInteractionType == type)
        {
            Debug.Log($"[InteractionManager] 동일한 상호작용이 이미 진행 중: {type}");
            return;
        }
        
        // 다른 상호작용이 진행 중이면 취소하고 새로 시작
        if (isInteracting)
        {
            Debug.Log($"[InteractionManager] 다른 상호작용 취소 후 새로 시작: {currentInteractionType} → {type}");
            if (_interactionCoroutine != null)
            {
                StopCoroutine(_interactionCoroutine);
                _interactionCoroutine = null;
            }
            // 간단한 초기화만
            isInteracting = false;
        }
        
        currentInteractionType = type;
        _interactionDuration = duration;
        currentInteractionText = GetInteractionText(type);
        
        this.isCountdown = isCountdown;
        // 초기 진행도 설정
        float initialProgress = isCountdown ? 1f : 0f;
        
        Debug.Log($"[InteractionManager] 상호작용 초기 설정 - Type: {type}, Countdown: {isCountdown}, Initial: {initialProgress}");
        
        SetInteractionUI(true, currentInteractionText, initialProgress, isCountdown);
        
        isInteracting = true;
        _interactionCoroutine = StartCoroutine(InteractionProgressCoroutine());
        
        Debug.Log($"[InteractionManager] 상호작용 시작: {type} ({duration}초, 카운트다운: {isCountdown})");
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        SetInteractionVisibility(false);
    }
    
    private void CacheUIElements()
    {
        _interactionUI = _root.Q<VisualElement>("interactionUI");
        _interactionLabel = _root.Q<Label>("interactionLabel");
        _progressBar = _root.Q<VisualElement>("progressBar");
        _progressMask = _root.Q<VisualElement>("progressMask");
        _progressFill = _root.Q<VisualElement>("progressFill");
    }
    
    
    public void StopInteraction(bool completed = false)
    {
        if (!isInteracting) return;
        
        if (_interactionCoroutine != null)
        {
            StopCoroutine(_interactionCoroutine);
            _interactionCoroutine = null;
        }
        
        if (completed)
        {
            HUDEvents.TriggerInteractionComplete(currentInteractionType, true);
            
            if (autoHideOnComplete)
            {
                StartCoroutine(HideAfterDelay(0.5f));
            }
            
            Debug.Log($"[InteractionManager] 상호작용 완료: {currentInteractionType}");
        }
        else
        {
            SetInteractionVisibility(false);
            HUDEvents.TriggerInteractionCancel();
            Debug.Log($"[InteractionManager] 상호작용 취소: {currentInteractionType}");
        }
        
        // 상태 초기화
        isInteracting = false;
        isCountdown = false;
        currentProgress = 0f;
        currentInteractionType = InteractionType.Dig;
        _interactionDuration = 0f;
    }
    
    public void CancelInteraction()
    {
        StopInteraction(completed: false);
    }
    
   private IEnumerator InteractionProgressCoroutine()
{
    float elapsed = 0f;
    float lastTextUpdateTime = 0f;
    const float textUpdateInterval = 0.1f;
    
    // 로컬 변수로 고정 - 코루틴 실행 중 절대 안 바뀜
    bool localIsCountdown = this.isCountdown;
    
    Debug.Log($"[ProgressCoroutine] 시작 - Type: {currentInteractionType}, Duration: {_interactionDuration}, IsCountdown: {localIsCountdown}");
    
    while (elapsed < _interactionDuration)
    {
        float normalizedTime = elapsed / _interactionDuration;
        
        // this.isCountdown 대신 localIsCountdown 사용
        if (localIsCountdown)
        {
            currentProgress = 1f - normalizedTime;
            
            if (elapsed - lastTextUpdateTime >= textUpdateInterval)
            {
                float remainingTime = _interactionDuration - elapsed;
                currentInteractionText = $"기절 상태... {remainingTime:F1}초";
                if (_interactionLabel != null)
                {
                    _interactionLabel.text = currentInteractionText;
                }
                lastTextUpdateTime = elapsed;
            }
        }
        else
        {
            currentProgress = normalizedTime;
        }
        
        SetProgress(currentProgress);
        HUDEvents.TriggerInteractionProgress(currentInteractionType, currentProgress);
        
        yield return null;
        elapsed += Time.deltaTime;
    }
    
    // 최종 값도 localIsCountdown 기준
    currentProgress = localIsCountdown ? 0f : 1f;
    SetProgress(currentProgress);
    
    StopInteraction(completed: true);
}
    
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetInteractionVisibility(false);
    }
    
    public void SetInteractionUI(bool show, string text = "", float progress = 0f, bool isCountdown = false)
    {
        if (_interactionUI != null)
        {
            SetInteractionVisibility(show);
            
            if (show)
            {
                if (_interactionLabel != null && !string.IsNullOrEmpty(text))
                {
                    _interactionLabel.text = text;
                    currentInteractionText = text;
                }
                    // 수동 호출일 때만 isCountdown 설정 (코루틴 진행 중이 아닐 때)
                if (!this.isInteracting)
                {
                    this.isCountdown = isCountdown; // 진행 중이 아닐 때만 방향 변경 허용
                }
                // isCountdown는 OnInteractionStarted에서 이미 설정됨 - 여기서는 UI만 업데이트
                SetProgress(progress);
                // 수정: this.isCountdown 사용
                SetProgressColor(this.isCountdown ? countdownProgressColor : normalProgressColor);
                
                // 수정: this.isCountdown 사용
                Debug.Log($"[SetInteractionUI] Progress: {progress}, IsCountdown: {this.isCountdown}");
            }
        }
    }
    
    private void SetInteractionVisibility(bool visible)
    {
        if (_interactionUI != null)
        {
            if (visible)
            {
                _interactionUI.RemoveFromClassList("ui-hidden");
                _interactionUI.AddToClassList("ui-visible");
            }
            else
            {
                _interactionUI.AddToClassList("ui-hidden");
                _interactionUI.RemoveFromClassList("ui-visible");
            }
        }
    }
    
    private void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        
        if (_progressFill != null)
        {
            // 단순히 퍼센트만 사용하여 레이아웃 충돌 방지
            float progressPercent = currentProgress * 100f;
            _progressFill.style.width = new Length(progressPercent, LengthUnit.Percent);
        }
    }
    
    private void SetProgressColor(Color color)
    {
        if (_progressFill != null)
        {
            // 투명도 조절하여 색상 설정
            Color adjustedColor = new Color(color.r, color.g, color.b, 0.8f);
            _progressFill.style.backgroundColor = adjustedColor;
        }
    }
    
    public void SetCustomInteraction(string text, float progress = 0f, bool isCountdown = false)
    {
        SetInteractionUI(true, text, progress, isCountdown);
    }
    
    // 외부에서 직접 진행도 업데이트 (수동 상호작용용)
    public void UpdateProgress(float progress)
    {
        if (isInteracting)
        {
            SetProgress(progress);
            HUDEvents.TriggerInteractionProgress(currentInteractionType, progress);
            
            if (progress >= 1f)
            {
                StopInteraction(completed: true);
            }
        }
    }
    
    private string GetInteractionText(InteractionType type)
    {
        return type switch
        {
            InteractionType.Dig => "⊙ 땅 파기",
            InteractionType.Revive => "⊙ 동료 구조하기",
            InteractionType.Feeding => "⊙ 먹이 주기",
            InteractionType.Faint => "⊙ 구조 대기 중...",
            _ => "⊙ 상호작용"
        };
    }
    
    public void SetInteractionSystemEnabled(bool enabled)
    {
        enableInteractionSystem = enabled;
        
        if (!enabled && isInteracting)
        {
            CancelInteraction();
        }
    }
    
    public void SetProgressBarStyle(string styleClass)
    {
        if (_progressBar != null)
        {
            _progressBar.ClearClassList();
            _progressBar.AddToClassList("scifi-progress-bar");
            if (!string.IsNullOrEmpty(styleClass))
            {
                _progressBar.AddToClassList(styleClass);
            }
        }
    }
    
    // Getter 메서드들
    public bool IsInteracting() => isInteracting;
    public InteractionType GetCurrentInteractionType() => currentInteractionType;
    public float GetCurrentProgress() => currentProgress;
    public string GetCurrentInteractionText() => currentInteractionText;
    public float GetRemainingTime() => isInteracting ? (_interactionDuration * (1f - currentProgress)) : 0f;
    
    
    
    
}