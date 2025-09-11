using UnityEngine;
using UnityEngine.UIElements;
using StarterAssets;

/// <summary>
/// UI Toolkit 기반 플레이어 액션 관리자 - 점프 및 상호작용 버튼 처리
/// </summary>
public class PlayerActionManager : MonoBehaviour
{
    [Header("Input System Integration")]
    public StarterAssetsInputs starterAssetsInputs;
    
    [Header("Button Settings")]
    [SerializeField]
    private float buttonPressScale = 0.9f;
    
    [SerializeField]
    private float buttonAnimationDuration = 0.1f;
    
    // UI References
    private VisualElement _root;
    private VisualElement _jumpButton;
    private VisualElement _interactButton;
    
    // Button state
    private bool _isJumpPressed = false;
    private bool _isInteractPressed = false;
    
    /// <summary>
    /// UniversalHUDController에서 호출하는 초기화 메서드
    /// </summary>
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        SetupButtonEvents();
        
        Debug.Log("[PlayerActionManager] 초기화 완료");
    }
    
    private void CacheUIElements()
    {
        _jumpButton = _root.Q<VisualElement>("jumpButton");
        _interactButton = _root.Q<VisualElement>("interactButton");
        
        Debug.Log($"[PlayerActionManager] UI 요소 캐싱: " +
                 $"점프버튼={(_jumpButton != null ? "OK" : "NULL")}, " +
                 $"상호작용버튼={(_interactButton != null ? "OK" : "NULL")}");
    }
    
    private void SetupButtonEvents()
    {
        // 점프 버튼 이벤트
        if (_jumpButton != null)
        {
            _jumpButton.RegisterCallback<PointerDownEvent>(OnJumpButtonDown);
            _jumpButton.RegisterCallback<PointerUpEvent>(OnJumpButtonUp);
            _jumpButton.RegisterCallback<PointerLeaveEvent>(OnJumpButtonLeave);
            
            Debug.Log("[PlayerActionManager] 점프 버튼 이벤트 설정 완료");
        }
        
        // 상호작용 버튼 이벤트
        if (_interactButton != null)
        {
            _interactButton.RegisterCallback<PointerDownEvent>(OnInteractButtonDown);
            _interactButton.RegisterCallback<PointerUpEvent>(OnInteractButtonUp);
            _interactButton.RegisterCallback<PointerLeaveEvent>(OnInteractButtonLeave);
            
            Debug.Log("[PlayerActionManager] 상호작용 버튼 이벤트 설정 완료");
        }
    }
    
    #region 점프 버튼 처리
    
    private void OnJumpButtonDown(PointerDownEvent evt)
    {
        if (_isJumpPressed) return;
        
        _isJumpPressed = true;
        AnimateButtonPress(_jumpButton, true);
        SendJumpInput(true);
        
        Debug.Log("[PlayerActionManager] 점프 버튼 눌림");
    }
    
    private void OnJumpButtonUp(PointerUpEvent evt)
    {
        if (!_isJumpPressed) return;
        
        _isJumpPressed = false;
        AnimateButtonPress(_jumpButton, false);
        SendJumpInput(false);
        
        Debug.Log("[PlayerActionManager] 점프 버튼 뗌");
    }
    
    private void OnJumpButtonLeave(PointerLeaveEvent evt)
    {
        if (!_isJumpPressed) return;
        
        _isJumpPressed = false;
        AnimateButtonPress(_jumpButton, false);
        SendJumpInput(false);
        
        Debug.Log("[PlayerActionManager] 점프 버튼에서 벗어남");
    }
    
    #endregion
    
    #region 상호작용 버튼 처리
    
    private void OnInteractButtonDown(PointerDownEvent evt)
    {
        if (_isInteractPressed) return;
        
        _isInteractPressed = true;
        AnimateButtonPress(_interactButton, true);
        SendInteractionHoldStart();
        
        Debug.Log("[PlayerActionManager] 상호작용 버튼 홀드 시작");
    }
    
    private void OnInteractButtonUp(PointerUpEvent evt)
    {
        if (!_isInteractPressed) return;
        
        _isInteractPressed = false;
        AnimateButtonPress(_interactButton, false);
        SendInteractionHoldEnd();
        
        Debug.Log("[PlayerActionManager] 상호작용 버튼 홀드 종료");
    }
    
    private void OnInteractButtonLeave(PointerLeaveEvent evt)
    {
        if (!_isInteractPressed) return;
        
        _isInteractPressed = false;
        AnimateButtonPress(_interactButton, false);
        SendInteractionHoldEnd();
        
        Debug.Log("[PlayerActionManager] 상호작용 버튼에서 벗어남 (홀드 종료)");
    }
    
    #endregion
    
    #region 버튼 애니메이션
    
    private void AnimateButtonPress(VisualElement button, bool pressed)
    {
        if (button == null) return;
        
        float targetScale = pressed ? buttonPressScale : 1.0f;
        
        // 간단한 스케일 애니메이션 (Unity의 Transition이 없으므로 CSS transform 사용)
        button.style.scale = new Scale(Vector3.one * targetScale);
        
        // 시각적 피드백을 위한 투명도 조정
        button.style.opacity = pressed ? 0.8f : 1.0f;
    }
    
    #endregion
    
    #region 입력 전달
    
    private void SendJumpInput(bool jumpState)
    {
        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.JumpInput(jumpState);
        }
        else
        {
            Debug.LogWarning("[PlayerActionManager] StarterAssetsInputs가 연결되지 않았습니다!");
        }
    }
    
    private void SendInteractionHoldStart()
    {
        Debug.Log("[PlayerActionManager] 상호작용 홀드 시작 - InteractionManager 호출");
        
        // InteractionManager에게 홀드 시작 알림
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.OnInteractionHoldStart();
        }
        else
        {
            Debug.LogWarning("[PlayerActionManager] InteractionManager.Instance가 null입니다!");
        }
    }
    
    private void SendInteractionHoldEnd()
    {
        Debug.Log("[PlayerActionManager] 상호작용 홀드 종료 - InteractionManager 호출");
        
        // InteractionManager에게 홀드 종료 알림
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.OnInteractionHoldEnd();
        }
        else
        {
            Debug.LogWarning("[PlayerActionManager] InteractionManager.Instance가 null입니다!");
        }
    }
    
    private void SendInteractionInput(bool interactionState)
    {
        if (interactionState)
        {
            Debug.Log("[PlayerActionManager] 상호작용 버튼 눌림 - InteractionManager 호출");
            
            // InteractionManager에게 상호작용 요청 전달
            if (InteractionManager.Instance != null)
            {
                Debug.Log("[PlayerActionManager] InteractionManager 찾음, TryNearestInteraction 호출");
                InteractionManager.Instance.TryNearestInteraction();
            }
            else
            {
                Debug.LogWarning("[PlayerActionManager] InteractionManager.Instance가 null입니다!");
            }
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 상호작용 버튼 가시성 업데이트 (InteractionManager에서 호출)
    /// </summary>
    public void UpdateInteractionButtonVisibility(bool visible)
    {
        if (_interactButton != null)
        {
            _interactButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            Debug.Log($"[PlayerActionManager] 상호작용 버튼 {(visible ? "표시" : "숨김")}");
        }
    }
    
    /// <summary>
    /// 상호작용 버튼이 표시되어 있는지 확인
    /// </summary>
    public bool IsInteractionButtonVisible()
    {
        if (_interactButton == null) return false;
        return _interactButton.style.display == DisplayStyle.Flex;
    }
    
    /// <summary>
    /// 버튼 활성화/비활성화
    /// </summary>
    public void SetButtonEnabled(string buttonName, bool enabled)
    {
        VisualElement button = buttonName.ToLower() switch
        {
            "jump" => _jumpButton,
            "interact" => _interactButton,
            _ => null
        };
        
        if (button != null)
        {
            button.SetEnabled(enabled);
            button.style.opacity = enabled ? 1.0f : 0.5f;
            
            Debug.Log($"[PlayerActionManager] {buttonName} 버튼 {(enabled ? "활성화" : "비활성화")}");
        }
    }
    
    /// <summary>
    /// 버튼 표시/숨김
    /// </summary>
    public void SetButtonVisibility(string buttonName, bool visible)
    {
        VisualElement button = buttonName.ToLower() switch
        {
            "jump" => _jumpButton,
            "interact" => _interactButton,
            _ => null
        };
        
        if (button != null)
        {
            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            
            Debug.Log($"[PlayerActionManager] {buttonName} 버튼 {(visible ? "표시" : "숨김")}");
        }
    }
    
    /// <summary>
    /// 버튼 애니메이션 설정
    /// </summary>
    public void SetButtonPressScale(float scale)
    {
        buttonPressScale = Mathf.Clamp(scale, 0.1f, 1.0f);
    }
    
    /// <summary>
    /// 모든 버튼 상태 리셋
    /// </summary>
    public void ResetAllButtons()
    {
        if (_isJumpPressed)
        {
            _isJumpPressed = false;
            AnimateButtonPress(_jumpButton, false);
            SendJumpInput(false);
        }
        
        if (_isInteractPressed)
        {
            _isInteractPressed = false;
            AnimateButtonPress(_interactButton, false);
            SendInteractionInput(false);
        }
        
        Debug.Log("[PlayerActionManager] 모든 버튼 상태 리셋");
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnDisable()
    {
        // 컴포넌트가 비활성화될 때 모든 버튼 상태 리셋
        ResetAllButtons();
    }
    
    #endregion
}