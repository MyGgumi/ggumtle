using System;
using UnityEngine;
using UnityEngine.UIElements;
using InputSystem.Core;

namespace InputSystem.Actions
{
    /// <summary>
    /// 액션 버튼(점프, 상호작용) 입력만 처리하는 컨트롤러
    /// </summary>
    public class ActionButtonController : MonoBehaviour, IInputLayer
    {
        [Header("Button Settings")]
        [SerializeField] private float buttonPressScale = 0.9f;
        [SerializeField] private float buttonAnimationDuration = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugVisualization = false;

        // IInputLayer 구현
        public string LayerName => "ActionButtons";
        public int Priority { get; set; } = 15;
        public bool IsEnabled { get; private set; }

        // 이벤트
        public event Action OnJumpPressed;
        public event Action OnJumpReleased;
        public event Action OnInteractPressed;
        public event Action OnInteractReleased;
        public event Action OnInteractHoldStart;
        public event Action OnInteractHoldEnd;

        // UI References
        private VisualElement _root;
        private VisualElement _jumpButton;
        private VisualElement _interactButton;

        // Button state
        private bool _isJumpPressed = false;
        private bool _isInteractPressed = false;

        // UI 이벤트 콜백 저장 변수들
        private EventCallback<PointerDownEvent> _jumpButtonDownCallback;
        private EventCallback<PointerUpEvent> _jumpButtonUpCallback;
        private EventCallback<PointerLeaveEvent> _jumpButtonLeaveCallback;
        private EventCallback<PointerDownEvent> _interactButtonDownCallback;
        private EventCallback<PointerUpEvent> _interactButtonUpCallback;
        private EventCallback<PointerLeaveEvent> _interactButtonLeaveCallback;

        // InputCoordinator 참조
        private InputCoordinator _coordinator;

        public bool IsJumpPressed => _isJumpPressed;
        public bool IsInteractPressed => _isInteractPressed;

        public void Initialize(VisualElement root)
        {
            _root = root;
            _coordinator = GetComponentInParent<InputCoordinator>();

            CacheUIElements();
            SetupButtonEvents();

            // 상호작용 버튼 초기 상태: 숨김
            UpdateInteractionButtonVisibility(false);

            UnityEngine.Debug.Log($"[{LayerName}] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _jumpButton = _root.Q<VisualElement>("jumpButton");
            _interactButton = _root.Q<VisualElement>("interactButton");

            UnityEngine.Debug.Log($"[{LayerName}] UI 요소 캐싱: " +
                $"점프버튼={(_jumpButton != null ? "OK" : "NULL")}, " +
                $"상호작용버튼={(_interactButton != null ? "OK" : "NULL")}");
        }

        private void SetupButtonEvents()
        {
            // 기존 이벤트 먼저 해제 (중복 등록 방지)
            UnregisterUIEvents();

            // 점프 버튼 이벤트
            if (_jumpButton != null)
            {
                // 콜백 인스턴스 생성 및 저장
                _jumpButtonDownCallback = evt =>
                {
                    evt.StopImmediatePropagation();
                    OnJumpButtonDown(evt);
                };
                _jumpButtonUpCallback = evt =>
                {
                    evt.StopImmediatePropagation();
                    OnJumpButtonUp(evt);
                };
                _jumpButtonLeaveCallback = evt =>
                {
                    evt.StopImmediatePropagation();
                    OnJumpButtonLeave(evt);
                };

                // 저장된 콜백으로 등록
                _jumpButton.RegisterCallback(_jumpButtonDownCallback);
                _jumpButton.RegisterCallback(_jumpButtonUpCallback);
                _jumpButton.RegisterCallback(_jumpButtonLeaveCallback);

                if (enableDebugVisualization)
                {
                    _jumpButton.AddToClassList("debug-button");
                }

                UnityEngine.Debug.Log($"[{LayerName}] 점프 버튼 이벤트 설정 완료");
            }

            // 상호작용 버튼 이벤트는 InteractionView에서 처리하므로 여기서는 가시성만 관리
            if (_interactButton != null)
            {
                if (enableDebugVisualization)
                {
                    _interactButton.AddToClassList("debug-button");
                }

                UnityEngine.Debug.Log($"[{LayerName}] 상호작용 버튼 UI 요소 설정 완료 (이벤트는 InteractionView에서 처리)");
            }
        }

        #region 점프 버튼 처리

        private void OnJumpButtonDown(PointerDownEvent evt)
        {
            if (!IsEnabled || _isJumpPressed) return;

            _isJumpPressed = true;
            AnimateButtonPress(_jumpButton, true);
            OnJumpPressed?.Invoke();

            UnityEngine.Debug.Log($"[{LayerName}] 점프 버튼 눌림");
        }

        private void OnJumpButtonUp(PointerUpEvent evt)
        {
            if (!_isJumpPressed) return;

            _isJumpPressed = false;
            AnimateButtonPress(_jumpButton, false);
            OnJumpReleased?.Invoke();

            UnityEngine.Debug.Log($"[{LayerName}] 점프 버튼 뗌");
        }

        private void OnJumpButtonLeave(PointerLeaveEvent evt)
        {
            if (!_isJumpPressed) return;

            _isJumpPressed = false;
            AnimateButtonPress(_jumpButton, false);
            OnJumpReleased?.Invoke();

            UnityEngine.Debug.Log($"[{LayerName}] 점프 버튼에서 벗어남");
        }

        #endregion

        #region 상호작용 버튼 처리 (가시성만 관리, 이벤트는 InteractionView에서 처리)

        // 상호작용 버튼 이벤트는 InteractionView에서 처리하므로 여기서는 제거됨

        #endregion

        #region 버튼 애니메이션

        private void AnimateButtonPress(VisualElement button, bool pressed)
        {
            if (button == null) return;

            float targetScale = pressed ? buttonPressScale : 1.0f;

            // 스케일 애니메이션
            button.style.scale = new Scale(Vector3.one * targetScale);

            // 시각적 피드백을 위한 투명도 조정
            button.style.opacity = pressed ? 0.8f : 1.0f;
        }

        #endregion

        #region IInputLayer 구현

        public void Enable()
        {
            IsEnabled = true;
            // 버튼들은 항상 보이지만 입력만 활성화/비활성화
        }

        public void Disable()
        {
            IsEnabled = false;
            ResetInput();
        }

        public void ResetInput()
        {
            if (_isJumpPressed)
            {
                _isJumpPressed = false;
                AnimateButtonPress(_jumpButton, false);
                OnJumpReleased?.Invoke();
            }

            // 상호작용 버튼은 InteractionView에서 관리하므로 여기서는 처리하지 않음

            UnityEngine.Debug.Log($"[{LayerName}] 점프 버튼 상태 리셋");
        }

        public void Cleanup()
        {
            UnregisterUIEvents();
            ResetInput();
        }

        private void UnregisterUIEvents()
        {
            // 점프 버튼 이벤트 해제
            if (_jumpButton != null)
            {
                if (_jumpButtonDownCallback != null)
                    _jumpButton.UnregisterCallback(_jumpButtonDownCallback);
                if (_jumpButtonUpCallback != null)
                    _jumpButton.UnregisterCallback(_jumpButtonUpCallback);
                if (_jumpButtonLeaveCallback != null)
                    _jumpButton.UnregisterCallback(_jumpButtonLeaveCallback);
            }

            // 상호작용 버튼 이벤트는 InteractionView에서 관리하므로 여기서는 해제하지 않음

            UnityEngine.Debug.Log($"[{LayerName}] 점프 버튼 이벤트 구독 해제 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 상호작용 버튼 가시성 업데이트
        /// </summary>
        public void UpdateInteractionButtonVisibility(bool visible)
        {
            if (_interactButton != null)
            {
                _interactButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                UnityEngine.Debug.Log($"[{LayerName}] 상호작용 버튼 {(visible ? "표시" : "숨김")}");
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
                _ => null,
            };

            if (button != null)
            {
                button.SetEnabled(enabled);
                button.style.opacity = enabled ? 1.0f : 0.5f;

                UnityEngine.Debug.Log($"[{LayerName}] {buttonName} 버튼 {(enabled ? "활성화" : "비활성화")}");
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
                _ => null,
            };

            if (button != null)
            {
                button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                UnityEngine.Debug.Log($"[{LayerName}] {buttonName} 버튼 {(visible ? "표시" : "숨김")}");
            }
        }

        /// <summary>
        /// 버튼 애니메이션 설정
        /// </summary>
        public void SetButtonPressScale(float scale)
        {
            buttonPressScale = Mathf.Clamp(scale, 0.1f, 1.0f);
        }

        #endregion

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnDisable()
        {
            // 컴포넌트가 비활성화될 때 모든 버튼 상태 리셋
            ResetInput();
        }
    }
}