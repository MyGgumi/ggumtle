using System;
using System.Collections.Generic;
using UnityEngine;
using Interfaces;

namespace Managers
{
    /// <summary>
    /// 상호작용 UI 관리자 - 범위 체크, 버튼 표시, 홀드 처리만 담당
    /// 실제 상호작용 로직은 각 객체가 독립적으로 처리
    /// </summary>
    public class UIInteractionManager : MonoBehaviour
    {
        public static UIInteractionManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject interactionUI;
        [SerializeField] private UnityEngine.UI.Button interactionButton;
        [SerializeField] private TMPro.TextMeshProUGUI interactionText;
        [SerializeField] private UnityEngine.UI.Slider progressBar;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.1f; // 범위 체크 주기
        [SerializeField] private bool enableDebugLogs = true;

        // 상태 관리
        private readonly List<IUIInteractable> _nearbyInteractables = new List<IUIInteractable>();
        private IUIInteractable _currentInteractable;
        private Transform _playerTransform;

        // 홀드 관련
        private bool _isHolding = false;
        private float _holdProgress = 0f;
        private float _holdDuration = 0f;
        private Coroutine _holdCoroutine;

        // 이벤트
        public event Action<IUIInteractable> OnInteractionStarted;
        public event Action<IUIInteractable> OnInteractionCompleted;
        public event Action OnInteractionCancelled;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // 플레이어 찾기
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;

                // 정기적인 범위 체크 시작
                InvokeRepeating(nameof(UpdateNearbyInteractables), 0f, updateInterval);
            }
            else
            {
                Debug.LogError("[UIInteractionManager] Player not found!");
            }
        }

        void OnDestroy()
        {
            CancelCurrentHold();
        }

        #region UI 초기화

        private void InitializeUI()
        {
            if (interactionButton != null)
            {
                interactionButton.onClick.AddListener(OnInteractionButtonClicked);
            }

            HideUI();
        }

        #endregion

        #region 상호작용 객체 등록/해제

        /// <summary>
        /// 상호작용 객체 등록
        /// </summary>
        public void RegisterInteractable(IUIInteractable interactable)
        {
            if (interactable == null) return;

            if (!_nearbyInteractables.Contains(interactable))
            {
                _nearbyInteractables.Add(interactable);
                DebugLog($"[UIInteractionManager] 상호작용 객체 등록: {interactable.GetTransform().name}");
            }
        }

        /// <summary>
        /// 상호작용 객체 해제
        /// </summary>
        public void UnregisterInteractable(IUIInteractable interactable)
        {
            if (interactable == null) return;

            if (_nearbyInteractables.Remove(interactable))
            {
                DebugLog($"[UIInteractionManager] 상호작용 객체 해제: {interactable.GetTransform().name}");

                // 현재 상호작용 중인 객체였다면 취소
                if (_currentInteractable == interactable)
                {
                    CancelCurrentInteraction();
                }
            }
        }

        #endregion

        #region 범위 체크 및 UI 업데이트

        private void UpdateNearbyInteractables()
        {
            if (_playerTransform == null) return;

            IUIInteractable bestInteractable = null;
            float closestDistance = float.MaxValue;

            // 등록된 모든 객체 중 범위 내에서 가장 가까운 것 찾기
            foreach (var interactable in _nearbyInteractables)
            {
                if (interactable?.GetTransform() == null) continue;

                if (!interactable.CanInteract()) continue;

                float distance = Vector3.Distance(_playerTransform.position,
                                                interactable.GetTransform().position);

                if (distance <= interactable.GetInteractionRange() && distance < closestDistance)
                {
                    closestDistance = distance;
                    bestInteractable = interactable;
                }
            }

            // 현재 상호작용 객체 업데이트
            if (bestInteractable != _currentInteractable)
            {
                _currentInteractable = bestInteractable;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (_currentInteractable != null)
            {
                ShowUI();
                UpdateInteractionText();
            }
            else
            {
                HideUI();
            }
        }

        private void UpdateInteractionText()
        {
            if (_currentInteractable != null && interactionText != null)
            {
                interactionText.text = _currentInteractable.GetInteractionText();
            }
        }

        private void ShowUI()
        {
            if (interactionUI != null)
            {
                interactionUI.SetActive(true);
            }
        }

        private void HideUI()
        {
            if (interactionUI != null)
            {
                interactionUI.SetActive(false);
            }

            CancelCurrentHold();
        }

        #endregion

        #region 상호작용 처리

        private void OnInteractionButtonClicked()
        {
            if (_currentInteractable == null) return;

            if (_isHolding) return; // 이미 홀드 중

            DebugLog($"[UIInteractionManager] 상호작용 버튼 클릭: {_currentInteractable.GetTransform().name}");

            if (_currentInteractable.RequiresHold())
            {
                StartHold();
            }
            else
            {
                // 즉시 실행
                ExecuteInteraction();
            }
        }

        private void ExecuteInteraction()
        {
            if (_currentInteractable == null) return;

            DebugLog($"[UIInteractionManager] 상호작용 실행: {_currentInteractable.GetTransform().name}");

            OnInteractionStarted?.Invoke(_currentInteractable);
            _currentInteractable.OnInteract();
            OnInteractionCompleted?.Invoke(_currentInteractable);
        }

        #endregion

        #region 홀드 처리

        private void StartHold()
        {
            if (_currentInteractable == null || _isHolding) return;

            _isHolding = true;
            _holdProgress = 0f;
            _holdDuration = _currentInteractable.GetHoldDuration();

            DebugLog($"[UIInteractionManager] 홀드 시작: {_currentInteractable.GetTransform().name}, 시간: {_holdDuration}초");

            _currentInteractable.OnHoldStart();
            OnInteractionStarted?.Invoke(_currentInteractable);

            ShowProgressBar();
            _holdCoroutine = StartCoroutine(HoldCoroutine());
        }

        private System.Collections.IEnumerator HoldCoroutine()
        {
            while (_isHolding && _holdProgress < 1f)
            {
                _holdProgress += Time.deltaTime / _holdDuration;
                _holdProgress = Mathf.Clamp01(_holdProgress);

                UpdateProgressBar(_holdProgress);
                _currentInteractable?.OnHoldProgress(_holdProgress);

                yield return null;
            }

            if (_isHolding && _holdProgress >= 1f)
            {
                CompleteHold();
            }
        }

        private void CompleteHold()
        {
            if (!_isHolding || _currentInteractable == null) return;

            DebugLog($"[UIInteractionManager] 홀드 완료: {_currentInteractable.GetTransform().name}");

            var completedInteractable = _currentInteractable;
            _isHolding = false;
            _holdProgress = 0f;

            HideProgressBar();

            completedInteractable.OnHoldComplete();
            OnInteractionCompleted?.Invoke(completedInteractable);
        }

        private void CancelCurrentHold()
        {
            if (!_isHolding) return;

            DebugLog($"[UIInteractionManager] 홀드 취소");

            var cancelledInteractable = _currentInteractable;
            _isHolding = false;
            _holdProgress = 0f;

            if (_holdCoroutine != null)
            {
                StopCoroutine(_holdCoroutine);
                _holdCoroutine = null;
            }

            HideProgressBar();

            cancelledInteractable?.OnHoldCancelled();
            OnInteractionCancelled?.Invoke();
        }

        private void CancelCurrentInteraction()
        {
            CancelCurrentHold();
            _currentInteractable = null;
            UpdateUI();
        }

        #endregion

        #region 프로그레스 바

        private void ShowProgressBar()
        {
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.value = 0f;
            }
        }

        private void UpdateProgressBar(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
        }

        private void HideProgressBar()
        {
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(false);
            }
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 현재 상호작용 중인 객체
        /// </summary>
        public IUIInteractable CurrentInteractable => _currentInteractable;

        /// <summary>
        /// 홀드 진행 중 여부
        /// </summary>
        public bool IsHolding => _isHolding;

        /// <summary>
        /// 홀드 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float HoldProgress => _holdProgress;

        /// <summary>
        /// 수동으로 홀드 취소
        /// </summary>
        public void CancelHold()
        {
            CancelCurrentHold();
        }

        #endregion

        #region 디버그

        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        #endregion
    }
}