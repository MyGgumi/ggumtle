using System;
using System.Collections.Generic;
using UnityEngine;
using Models;
using Services;
using MVVM.Core;
using StarterAssets;
using Interaction.Handlers;

namespace ViewModels.UI
{
    public class InteractionViewModel : BaseViewModel
    {
        [Header("Model & Service")]
        [SerializeField] private InteractionModel _interactionModel;
        [SerializeField] private InteractionService _interactionService;

        // GgumtleService 참조 (MVVM 패턴을 위한 Service 연동)
        private GgumtleService _ggumtleService;

        [Header("Current Interaction State")]
        [SerializeField] private InteractionType _currentType = InteractionType.None;
        [SerializeField] private string _currentText = "";
        [SerializeField] private float _currentProgress = 0f;
        [SerializeField] private bool _isActive = false;
        [SerializeField] private bool _isInProgress = false;

        [Header("Settings")]
        [SerializeField] private float _detectionRange = 3f;
        [SerializeField] private bool _playerCanMove = true;
        [SerializeField] private bool _showUI = false;

        [Header("Player Tracking")]
        [SerializeField] private Transform _playerTransform;
        private readonly List<IInteractable> _allInteractables = new List<IInteractable>();
        private float _updateTimer = 0f;
        private const float UPDATE_INTERVAL = 0.5f; // 0.5초마다 업데이트

        public event Action<InteractionType, string, float> OnInteractionStarted;
        public event Action<float> OnInteractionProgress;
        public event Action<InteractionType> OnInteractionCompleted;
        public event Action OnInteractionCancelled;
        public event Action<InteractionType, string, GameObject> OnNearbyInteractionAdded;
        public event Action<InteractionType, GameObject> OnNearbyInteractionRemoved;
        public event Action<bool> OnUIVisibilityChanged;

        // 핸들러 기반 UI 제어 이벤트
        public event Action<bool> OnProgressBarVisibilityChanged;

        void Awake()
        {
            if (_interactionModel == null)
                _interactionModel = new InteractionModel();

            if (_interactionService == null)
                _interactionService = new InteractionService(_interactionModel);

            // GgumtleService 참조 획득 또는 생성
            EnsureGgumtleServiceExists();

            SubscribeToServiceEvents();
            InitializeFromModel();
        }

        private void EnsureGgumtleServiceExists()
        {
            _ggumtleService = GgumtleService.Instance;
            if (_ggumtleService == null)
            {
                Debug.Log("[InteractionViewModel] GgumtleService가 없어서 자동 생성합니다.");

                // GgumtleService를 가진 GameObject 생성
                var serviceObject = new GameObject("GgumtleService");
                _ggumtleService = serviceObject.AddComponent<GgumtleService>();

                Debug.Log("[InteractionViewModel] GgumtleService 자동 생성 완료");
            }
        }

        void Update()
        {
            if (_isInProgress)
            {
                _interactionService.UpdateInteraction(Time.deltaTime);
            }

            // 플레이어 위치 기반 상호작용 감지 (0.5초마다 실행)
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0f;
                UpdateNearbyInteractions();
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromServiceEvents();
        }

        #region Service Event Handling

        private void SubscribeToServiceEvents()
        {
            if (_interactionService != null)
            {
                _interactionService.OnInteractionStarted += HandleInteractionStarted;
                _interactionService.OnInteractionProgress += HandleInteractionProgress;
                _interactionService.OnInteractionCompleted += HandleInteractionCompleted;
                _interactionService.OnInteractionCancelled += HandleInteractionCancelled;
                _interactionService.OnNearbyInteractionAdded += HandleNearbyInteractionAdded;
                _interactionService.OnNearbyInteractionRemoved += HandleNearbyInteractionRemoved;
            }
        }

        private void UnsubscribeFromServiceEvents()
        {
            if (_interactionService != null)
            {
                _interactionService.OnInteractionStarted -= HandleInteractionStarted;
                _interactionService.OnInteractionProgress -= HandleInteractionProgress;
                _interactionService.OnInteractionCompleted -= HandleInteractionCompleted;
                _interactionService.OnInteractionCancelled -= HandleInteractionCancelled;
                _interactionService.OnNearbyInteractionAdded -= HandleNearbyInteractionAdded;
                _interactionService.OnNearbyInteractionRemoved -= HandleNearbyInteractionRemoved;
            }
        }

        private void HandleInteractionStarted(InteractionType type, string text, float duration)
        {
            _currentType = type;
            _currentText = text;
            _currentProgress = 0f;
            _isActive = true;
            _isInProgress = true;

            // 핸들러를 사용해서 UI 전략 결정
            var handler = InteractionHandlerFactory.GetHandler(type);
            OnProgressBarVisibilityChanged?.Invoke(handler.ShouldShowProgressBar());

            OnInteractionStarted?.Invoke(type, text, duration);
            NotifyPropertyChanged();
        }

        private void HandleInteractionProgress(float progress)
        {
            _currentProgress = progress;
            OnInteractionProgress?.Invoke(progress);
            NotifyPropertyChanged();
        }

        private void HandleInteractionCompleted(InteractionType type)
        {
            _isInProgress = false;
            _isActive = false;
            _currentProgress = 1f;

            // 핸들러의 완료 처리 실행
            var handler = InteractionHandlerFactory.GetHandler(type);
            var interactionData = _interactionModel.currentInteraction;
            handler.OnInteractionCompleted(interactionData);

            OnInteractionCompleted?.Invoke(type);
            NotifyPropertyChanged();

            ClearInteraction();
        }

        private void HandleInteractionCancelled()
        {
            _isInProgress = false;
            _currentProgress = 0f;

            OnInteractionCancelled?.Invoke();
            NotifyPropertyChanged();
        }

        private void HandleNearbyInteractionAdded(InteractionType type, string text, GameObject target)
        {
            OnNearbyInteractionAdded?.Invoke(type, text, target);
            NotifyPropertyChanged();
        }

        private void HandleNearbyInteractionRemoved(InteractionType type, GameObject target)
        {
            OnNearbyInteractionRemoved?.Invoke(type, target);
            NotifyPropertyChanged();
        }

        #endregion

        #region Initialization

        private void InitializeFromModel()
        {
            _detectionRange = _interactionModel.detectionRange;
            _playerCanMove = _interactionModel.playerCanMove;
            _showUI = _interactionModel.showUI;

            _isActive = _interactionModel.HasActiveInteraction;
            _isInProgress = _interactionModel.IsInteractionInProgress;
            _currentProgress = _interactionModel.CurrentProgress;
            _currentType = _interactionModel.currentInteraction.type;
            _currentText = _interactionModel.currentInteraction.displayText;

            // 플레이어 찾기
            FindPlayer();

            // 씬의 모든 상호작용 가능한 객체 찾기
            FindAllInteractables();
        }

        private void FindPlayer()
        {
            // 다양한 방법으로 플레이어 찾기
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                // ThirdPersonController로 찾기
                var controller = FindFirstObjectByType<ThirdPersonController>();
                if (controller != null)
                    player = controller.gameObject;
            }
            if (player == null)
            {
                // "Player" 이름으로 찾기
                player = GameObject.Find("Player");
            }

            if (player != null)
            {
                _playerTransform = player.transform;
                Debug.Log($"[InteractionViewModel] 플레이어 발견: {player.name}");
            }
            else
            {
                Debug.LogError("[InteractionViewModel] 플레이어를 찾을 수 없습니다!");
            }
        }

        private void FindAllInteractables()
        {
            _allInteractables.Clear();

            // 씬의 모든 IInteractable 찾기
            var interactableObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var obj in interactableObjects)
            {
                if (obj is IInteractable interactable)
                {
                    _allInteractables.Add(interactable);
                }
            }

            Debug.Log($"[InteractionViewModel] {_allInteractables.Count}개의 상호작용 객체 발견");
        }

        private void UpdateNearbyInteractions()
        {
            if (_playerTransform == null) return;

            // 현재 근처 상호작용을 임시 저장
            var currentNearby = new List<IInteractable>();

            // 모든 상호작용 객체와의 거리 체크
            foreach (var interactable in _allInteractables)
            {
                if (interactable == null) continue;

                var interactableTransform = (interactable as MonoBehaviour)?.transform;
                if (interactableTransform == null) continue;

                float distance = Vector3.Distance(_playerTransform.position, interactableTransform.position);

                // 개별 객체의 상호작용 범위 사용
                float range = _detectionRange;
                if (interactable != null)
                {
                    var method = interactable.GetType().GetMethod("GetInteractionRange");
                    if (method != null)
                    {
                        try
                        {
                            var result = method.Invoke(interactable, null);
                            if (result is float customRange)
                                range = customRange;
                        }
                        catch
                        {
                            range = _detectionRange; // fallback
                        }
                    }
                }

                if (distance <= range && interactable.CanInteract())
                {
                    currentNearby.Add(interactable);
                }
            }

            // 새로 범위에 들어온 상호작용들 추가
            foreach (var interactable in currentNearby)
            {
                if (!IsInteractableInNearby(interactable))
                {
                    AddInteractableToNearby(interactable);
                }
            }

            // 범위를 벗어난 상호작용들 제거
            var toRemove = new List<InteractionData>();
            foreach (var nearbyData in _interactionModel.nearbyInteractions)
            {
                if (nearbyData.type == Models.InteractionType.None) continue;

                bool stillNearby = false;
                foreach (var interactable in currentNearby)
                {
                    var obj = (interactable as MonoBehaviour)?.gameObject;
                    if (obj == nearbyData.targetObject)
                    {
                        stillNearby = true;
                        break;
                    }
                }

                if (!stillNearby)
                {
                    toRemove.Add(nearbyData);

                    // 진행 중인 상호작용이 범위를 벗어나면 취소
                    if (_isInProgress && _interactionModel.currentInteraction.targetObject == nearbyData.targetObject)
                    {
                        Debug.Log($"[InteractionViewModel] {nearbyData.targetObject?.name} 범위 이탈로 상호작용 취소");
                        CancelInteraction();
                    }
                }
            }

            foreach (var data in toRemove)
            {
                RemoveInteractableFromNearby(data);
            }

            // 상자 상태 변경 감지를 위한 텍스트 업데이트
            foreach (var nearbyData in _interactionModel.nearbyInteractions)
            {
                if (nearbyData.type == Models.InteractionType.Chest && nearbyData.targetObject != null)
                {
                    var chest = nearbyData.targetObject.GetComponent<InteractableChest>();
                    if (chest != null)
                    {
                        var newText = chest.GetInteractionText();
                        if (nearbyData.displayText != newText)
                        {
                            // 상자 상태가 변경되었으므로 텍스트 업데이트
                            nearbyData.displayText = newText;
                            NotifyPropertyChanged();
                        }
                    }
                }
            }
        }

        private bool IsInteractableInNearby(IInteractable interactable)
        {
            var obj = (interactable as MonoBehaviour)?.gameObject;
            if (obj == null) return false;

            foreach (var nearbyData in _interactionModel.nearbyInteractions)
            {
                if (nearbyData.targetObject == obj)
                    return true;
            }
            return false;
        }

        private void AddInteractableToNearby(IInteractable interactable)
        {
            var obj = (interactable as MonoBehaviour)?.gameObject;
            if (obj == null)
            {
                Debug.LogWarning("[InteractionViewModel] AddInteractableToNearby - GameObject가 null입니다!");
                return;
            }

            // 상호작용 타입 결정
            var interactionType = GetInteractionType(interactable);
            var text = GetInteractionText(interactable);

            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"[InteractionViewModel] {obj.name} - GetInteractionText()가 빈 문자열을 반환했습니다!");
                return;
            }

            _interactionService.AddNearbyInteraction(interactionType, text, obj);
            Debug.Log($"[InteractionViewModel] {obj.name} 상호작용 추가: {text}");
        }

        private void RemoveInteractableFromNearby(InteractionData data)
        {
            var interactionType = data.type;
            _interactionService.RemoveNearbyInteraction(interactionType, data.targetObject);
            Debug.Log($"[InteractionViewModel] {data.targetObject?.name} 상호작용 제거");
        }

        private Models.InteractionType GetInteractionType(IInteractable interactable)
        {
            // 타입에 따라 InteractionType 결정
            if (interactable is InteractableChest)
                return Models.InteractionType.Chest;
            else if (interactable is InteractableGgumtle)
                return Models.InteractionType.Dig;
            else
                return Models.InteractionType.Custom;
        }

        private string GetInteractionText(IInteractable interactable)
        {
            // IInteractable에 GetInteractionText 메서드가 있다면 사용
            if (interactable is InteractableChest chest)
                return chest.GetInteractionText();
            else if (interactable is InteractableGgumtle ggumtle)
                return ggumtle.GetInteractionText();
            else
                return "상호작용";
        }

        #endregion

        #region Interaction Operations

        public void StartInteraction(InteractionType type, string text, float duration = 0f, bool requiresHold = false, GameObject targetObject = null, Vector3 targetPosition = default)
        {
            _interactionService.StartInteraction(type, text, duration, requiresHold, targetObject, targetPosition);
        }

        public void CompleteInteraction()
        {
            _interactionService.CompleteInteraction();
        }

        public void CancelInteraction()
        {
            _interactionService.CancelInteraction();
        }

        public void ClearInteraction()
        {
            _currentType = InteractionType.None;
            _currentText = "";
            _currentProgress = 0f;
            _isActive = false;
            _isInProgress = false;

            _interactionService.ClearInteraction();
            NotifyPropertyChanged();
        }

        #endregion

        #region Nearby Interactions

        public void AddNearbyInteraction(InteractionType type, string text, GameObject target = null)
        {
            _interactionService.AddNearbyInteraction(type, text, target);
            Debug.Log($"[InteractionViewModel] Nearby interaction added: {type} - {text}, HasNearby: {HasNearbyInteractions}");
        }

        public void RemoveNearbyInteraction(InteractionType type, GameObject target = null)
        {
            _interactionService.RemoveNearbyInteraction(type, target);
            Debug.Log($"[InteractionViewModel] Nearby interaction removed: {type}, HasNearby: {HasNearbyInteractions}");
        }

        /// <summary>
        /// 기존 상호작용의 텍스트만 업데이트 (상호작용을 제거/추가하지 않음)
        /// </summary>
        public bool UpdateNearbyInteractionText(InteractionType type, GameObject targetObject, string newText)
        {
            if (_interactionModel?.nearbyInteractions == null) return false;

            foreach (var interaction in _interactionModel.nearbyInteractions)
            {
                if (interaction.type == type && interaction.targetObject == targetObject)
                {
                    if (interaction.displayText != newText)
                    {
                        interaction.displayText = newText;
                        NotifyPropertyChanged();
                        Debug.Log($"[InteractionViewModel] 상호작용 텍스트 업데이트: {newText}");
                        return true;
                    }
                    return false; // 텍스트가 동일하면 업데이트 불필요
                }
            }
            return false; // 해당 상호작용을 찾지 못함
        }

        public void ClearNearbyInteractions()
        {
            _interactionService.ClearNearbyInteractions();
        }

        public InteractionData GetBestNearbyInteraction()
        {
            return _interactionService.GetBestNearbyInteraction();
        }

        public bool HasNearbyInteractions => _interactionService.HasNearbyInteractions;

        #endregion

        #region Settings and State

        public void SetPlayerMovement(bool canMove)
        {
            _playerCanMove = canMove;
            _interactionService.SetPlayerMovement(canMove);
            NotifyPropertyChanged();
        }

        public void SetUIVisibility(bool visible)
        {
            _showUI = visible;
            _interactionService.SetUIVisibility(visible);
            OnUIVisibilityChanged?.Invoke(visible);
            NotifyPropertyChanged();
        }

        #endregion

        #region Legacy Compatibility (InteractionManager integration)

        public static InteractionViewModel Instance { get; private set; }

        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[InteractionViewModel] Multiple instances detected! Using first instance.");
            }
        }

        public void SetActionButtonController(System.Action buttonAction)
        {
            Debug.Log("[InteractionViewModel] Legacy SetActionButtonController called - handled by new input system");
        }

        public void ShowInteractionUI(string text, Models.InteractionType type = Models.InteractionType.Custom)
        {
            SetUIVisibility(true);
            if (!_isActive)
            {
                StartInteraction(type, text);
            }
        }

        public void HideInteractionUI()
        {
            SetUIVisibility(false);
        }

        #endregion

        #region Utility Methods

        public void Reset()
        {
            _interactionService.Reset();
            InitializeFromModel();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogState()
        {
            Debug.Log($"[InteractionViewModel] Debug State:");
            Debug.Log($"  - Player found: {_playerTransform != null}");
            Debug.Log($"  - Interactables count: {_allInteractables.Count}");
            Debug.Log($"  - Nearby interactions: {_interactionModel.nearbyInteractions.Length}");
            Debug.Log($"  - Has nearby: {HasNearbyInteractions}");
            Debug.Log($"  - Is active: {IsActive}");
            Debug.Log($"  - Show UI: {ShowUI}");
        }

        #endregion

        #region GgumtleService Integration (MVVM Pattern)

        /// <summary>
        /// 꿈틀이 홀드 시작 (View에서 호출)
        /// </summary>
        public void StartGgumtleHold(string ggumtleId)
        {
            if (_ggumtleService != null)
            {
                _ggumtleService.StartHold(ggumtleId);
            }
        }

        /// <summary>
        /// 꿈틀이 홀드 진행 업데이트 (View에서 호출)
        /// </summary>
        public void UpdateGgumtleHoldProgress(string ggumtleId, float progress)
        {
            if (_ggumtleService != null)
            {
                _ggumtleService.UpdateHoldProgress(ggumtleId, progress);
            }
        }

        /// <summary>
        /// 꿈틀이 홀드 완료 (View에서 호출)
        /// </summary>
        public void CompleteGgumtleHold(string ggumtleId)
        {
            if (_ggumtleService != null)
            {
                _ggumtleService.CompleteHold(ggumtleId);
            }
        }

        /// <summary>
        /// 꿈틀이 홀드 취소 (View에서 호출)
        /// </summary>
        public void CancelGgumtleHold(string ggumtleId)
        {
            if (_ggumtleService != null)
            {
                _ggumtleService.CancelHold(ggumtleId);
            }
        }

        /// <summary>
        /// 꿈틀이 먹이주기 중지 (View에서 호출) - 홀드 기반으로 변경되어 더 이상 사용되지 않음
        /// </summary>
        public void StopGgumtleFeeding(string ggumtleId)
        {
            // 홀드 기반으로 변경되어 별도 중단 불필요
            Debug.Log($"[InteractionViewModel] 먹이주기가 홀드 기반으로 변경되어 별도 중단 불필요: {ggumtleId}");
        }

        /// <summary>
        /// 먹이주기 후 프로그레스 바 리셋
        /// </summary>
        public void ResetFeedingProgressBar()
        {
            if (_isInProgress && _currentType == InteractionType.Feeding)
            {
                _currentProgress = 0f;
                NotifyPropertyChanged();
                Debug.Log("[InteractionViewModel] 먹이주기 프로그레스 바 리셋");
            }
        }

        #endregion

        #region Properties

        public InteractionType CurrentType => _currentType;
        public string CurrentText => _currentText;
        public float CurrentProgress => _currentProgress;
        public bool IsActive => _isActive;
        public bool IsInProgress => _isInProgress;
        public bool PlayerCanMove => _playerCanMove;
        public bool ShowUI => _showUI;
        public float DetectionRange => _detectionRange;

        #endregion
    }
}