using System;
using System.Collections.Generic;
using System.Linq;
using Features.EscapeGate.Messages;
using Features.EscapeGate.Models;
using Features.EscapeGate.NetworkSources;
using Features.Notification.Services;
using Features.MobileControls.Messages;
using Features.Mongging.Messages;
using Features.Player.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Cysharp.Threading.Tasks;

namespace Features.EscapeGate.Services
{
    /// <summary>
    /// 탈출 게이트 관리 서비스 구현체
    /// </summary>
    public class EscapeGateServiceImpl : IEscapeGateService, IStartable, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<int> CurrentGateId => _currentGateId;
        public ReadOnlyReactiveProperty<bool> IsInRange => _isInRange;
        public ReadOnlyReactiveProperty<EscapeGateState> CurrentGateState => _currentGateState;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<int> _currentGateId = new(-1);
        private readonly ReactiveProperty<bool> _isInRange = new(false);
        private readonly ReactiveProperty<EscapeGateState> _currentGateState = new(EscapeGateState.Inactive);

        private readonly Dictionary<int, EscapeGateData> _registeredGates = new();
        private readonly Dictionary<int, GameObject> _gateObjects = new();
        private readonly CompositeDisposable _disposables = new();

        private readonly bool _enableDebugLogs = true;

        // Addressable 키 상수
        private const string ESCAPEGATE_PREFAB_KEY = "EscapeGateGameObject";

        // 게이트 ID별 위치 매핑
        private readonly Dictionary<int, Vector3> _gatePositions = new()
        {
            { 0, new Vector3(60f, 5.5f, -10f) },
            { 1, new Vector3(42f, 5f, 20f) }
        };

        #endregion

        #region Dependencies

        private readonly IEscapeGateNetworkSource _networkSource;
        private readonly INotificationService _notificationService;
        private readonly ISubscriber<EscapeGateOpenedMessage> _gateOpenedSubscriber;
        private readonly Features.Map.Services.IAddressableLoadService _addressableLoadService;
        private readonly ISubscriber<Features.EscapeGate.Messages.EscapeGateDetectedMessage> _gateDetectedSubscriber;
        private readonly ISubscriber<Features.EscapeGate.Messages.EscapeGateLeftMessage> _gateLeftSubscriber;
        private readonly ISubscriber<InteractHoldStartMessage> _holdStartSubscriber;
        private readonly ISubscriber<InteractHoldEndMessage> _holdEndSubscriber;
        private readonly PlayerManagerService _playerManagerService;
        private readonly IPublisher<MonggingPlayerEscapedMessage> _escapedPublisher;

        #endregion

        #region Constructor

        [Inject]
        public EscapeGateServiceImpl(
            IEscapeGateNetworkSource networkSource,
            INotificationService notificationService,
            ISubscriber<EscapeGateOpenedMessage> gateOpenedSubscriber,
            Features.Map.Services.IAddressableLoadService addressableLoadService,
            ISubscriber<Features.EscapeGate.Messages.EscapeGateDetectedMessage> gateDetectedSubscriber,
            ISubscriber<Features.EscapeGate.Messages.EscapeGateLeftMessage> gateLeftSubscriber,
            ISubscriber<InteractHoldStartMessage> holdStartSubscriber,
            ISubscriber<InteractHoldEndMessage> holdEndSubscriber,
            PlayerManagerService playerManagerService,
            IPublisher<MonggingPlayerEscapedMessage> escapedPublisher
        )
        {
            _networkSource = networkSource;
            _notificationService = notificationService;
            _gateOpenedSubscriber = gateOpenedSubscriber;
            _addressableLoadService = addressableLoadService;
            _gateDetectedSubscriber = gateDetectedSubscriber;
            _gateLeftSubscriber = gateLeftSubscriber;
            _holdStartSubscriber = holdStartSubscriber;
            _holdEndSubscriber = holdEndSubscriber;
            _playerManagerService = playerManagerService;
            _escapedPublisher = escapedPublisher;

            // Initialize()는 Start()에서 호출됨 (EntryPoint 방식)
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // 탈출구 오픈 메시지 구독
            _gateOpenedSubscriber.Subscribe(OnGateOpenedReceived).AddTo(_disposables);

            // 탈출구 감지/벗어남 메시지 구독 (InteractionTriggerDetector에서 발행)
            _gateDetectedSubscriber.Subscribe(OnGateDetectedMessage).AddTo(_disposables);
            _gateLeftSubscriber.Subscribe(OnGateLeftMessage).AddTo(_disposables);

            // 상호작용 홀드 메시지 구독
            _holdStartSubscriber.Subscribe(OnHoldStartMessage).AddTo(_disposables);
            _holdEndSubscriber.Subscribe(OnHoldEndMessage).AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[EscapeGateServiceImpl] 초기화 완료 - 모든 메시지 구독 활성화");
            }
        }

        public void Start()
        {
            Initialize();

            if (_enableDebugLogs)
            {
                Debug.Log("[EscapeGateServiceImpl] EntryPoint Start() 호출 - 서비스 시작됨");
            }
        }

        #endregion

        #region Public Methods

        public void RegisterGate(int gateId, string gateName, Vector3 position, GameObject gateObject)
        {
            if (_registeredGates.ContainsKey(gateId))
            {
                Debug.LogWarning($"[EscapeGateServiceImpl] 이미 등록된 탈출 게이트: {gateId}");
                return;
            }

            var gateData = new EscapeGateData(gateId, gateName, position);
            _registeredGates[gateId] = gateData;
            _gateObjects[gateId] = gateObject;

            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] 탈출 게이트 등록: {gateName} (ID: {gateId})");
            }
        }

        public void UnregisterGate(int gateId)
        {
            if (_registeredGates.Remove(gateId))
            {
                _gateObjects.Remove(gateId);

                if (_currentGateId.Value == gateId)
                {
                    _currentGateId.Value = -1;
                    _isInRange.Value = false;
                    _currentGateState.Value = EscapeGateState.Inactive;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 탈출 게이트 해제: {gateId}");
                }
            }
        }

        public void ActivateGates(int[] gateIds)
        {
            int activatedCount = 0;

            foreach (var gateId in gateIds)
            {
                if (_registeredGates.TryGetValue(gateId, out var gateData))
                {
                    gateData.Activate();
                    activatedCount++;

                    // GameObject 활성화
                    if (_gateObjects.TryGetValue(gateId, out var gateObject) && gateObject != null)
                    {
                        gateObject.SetActive(true);
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[EscapeGateServiceImpl] 탈출구 GameObject 활성화: {gateData.GateName} (ID: {gateId})");
                        }
                    }

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[EscapeGateServiceImpl] 탈출구 활성화: {gateData.GateName} (ID: {gateId})");
                    }

                    // 현재 감지된 게이트라면 상태 업데이트
                    if (_currentGateId.Value == gateId)
                    {
                        _currentGateState.Value = gateData.State;
                    }
                }
                else
                {
                    Debug.LogWarning($"[EscapeGateServiceImpl] 등록되지 않은 탈출구 ID: {gateId}");
                }
            }

            if (activatedCount > 0)
            {
                if (_notificationService != null)
                {
                    _notificationService.ShowNotification($"탈출구가 열렸습니다! ({activatedCount}개)", 3f);
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[EscapeGateServiceImpl] 탈출구 오픈 알림 표시: {activatedCount}개");
                    }
                }
                else
                {
                    Debug.LogError("[EscapeGateServiceImpl] NotificationService가 null입니다! 알림을 표시할 수 없습니다.");
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] {activatedCount}개의 탈출구 활성화 완료");
                }
            }
        }

        public void OnGateDetected(int gateId, float distance)
        {
            if (!_registeredGates.TryGetValue(gateId, out var gateData))
            {
                Debug.LogWarning($"[EscapeGateServiceImpl] 등록되지 않은 탈출구 감지: {gateId}");
                return;
            }

            _currentGateId.Value = gateId;
            _isInRange.Value = true;
            _currentGateState.Value = gateData.State;

            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] Observable 값 변경 - CurrentGateId: {gateId}, IsInRange: true, CurrentGateState: {gateData.State}");
            }

            if (gateData.IsActive)
            {
                if (_notificationService != null)
                {
                    _notificationService.ShowNotification("얼른 탈출하세요!", 2f);
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[EscapeGateServiceImpl] 탈출 독려 알림 표시");
                    }
                }
                else
                {
                    Debug.LogError("[EscapeGateServiceImpl] NotificationService가 null입니다!");
                }
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] 탈출구 감지: {gateData.GateName} (거리: {distance:F2}m, 상태: {gateData.State})");
            }
        }

        public void OnGateLeft(int gateId)
        {
            if (_currentGateId.Value == gateId)
            {
                _currentGateId.Value = -1;
                _isInRange.Value = false;
                _currentGateState.Value = EscapeGateState.Inactive;

                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 탈출구 벗어남: {gateId}");
                }
            }
        }

        public async void AttemptEscape(int gateId)
        {
            if (!_registeredGates.TryGetValue(gateId, out var gateData))
            {
                Debug.LogError($"[EscapeGateServiceImpl] 등록되지 않은 탈출구에 탈출 시도: {gateId}");
                return;
            }

            if (!gateData.CanInteract)
            {
                _notificationService.ShowWarningNotification("아직 탈출할 수 없습니다.");
                return;
            }

            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 탈출 시도 시작: {gateData.GateName}");
                }

                var response = await _networkSource.AttemptEscapeAsync(gateId);

                if (response.Success)
                {
                    _notificationService.ShowSuccessNotification("탈출 성공!");

                    // 로컬 플레이어 탈출 시 몽깅이 팀 시스템에 알림
                    var localPlayer = _playerManagerService.GetLocalPlayer();
                    if (localPlayer != null && localPlayer.IsMine)
                    {
                        // 탈출 위치는 현재 게이트 위치로 설정
                        Vector3 escapePosition = Vector3.zero;
                        if (_gatePositions.TryGetValue(gateId, out var gatePosition))
                        {
                            escapePosition = gatePosition;
                        }

                        var escapeMessage = new MonggingPlayerEscapedMessage(localPlayer.Id, escapePosition);
                        _escapedPublisher.Publish(escapeMessage);

                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[EscapeGateServiceImpl] 로컬 플레이어 탈출 메시지 발행: PlayerId={localPlayer.Id}, Position={escapePosition}");
                        }
                    }

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[EscapeGateServiceImpl] 탈출 성공: {gateData.GateName}");
                    }
                }
                else
                {
                    _notificationService.ShowWarningNotification("탈출에 실패했습니다.");
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[EscapeGateServiceImpl] 탈출 실패: {gateData.GateName}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EscapeGateServiceImpl] 탈출 시도 중 오류 발생: {e.Message}");
                _notificationService.ShowWarningNotification("탈출 시도 중 오류가 발생했습니다.");
            }
        }

        public IReadOnlyDictionary<int, EscapeGateData> GetAllGates()
        {
            return _registeredGates;
        }

        public EscapeGateData GetGate(int gateId)
        {
            return _registeredGates.TryGetValue(gateId, out var gateData) ? gateData : null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 특정 게이트 ID에 맞는 위치에 EscapeGate를 동적으로 생성
        /// </summary>
        private async UniTask SpawnEscapeGateAsync(int gateId)
        {
            if (!_gatePositions.TryGetValue(gateId, out var position))
            {
                Debug.LogError($"[EscapeGateServiceImpl] 알 수 없는 게이트 ID: {gateId}");
                return;
            }

            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 탈출구 동적 생성 시작: ID={gateId}, Position={position}");
                }

                // Addressable을 통해 EscapeGate 프리팹 로드 및 생성
                var gateObject = await _addressableLoadService.SpawnWithInjectionAsync<Features.EscapeGate.Views.EscapeGateGameObject>(
                    ESCAPEGATE_PREFAB_KEY, position, Quaternion.identity);

                // 게이트 ID 설정
                gateObject.SetGateId(gateId);

                // 한 프레임 대기 (EscapeGateGameObject.Start()에서 서비스 등록 완료 대기)
                await UniTask.Yield();

                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 탈출구 동적 생성 완료: ID={gateId}, Position={position}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EscapeGateServiceImpl] 탈출구 동적 생성 실패: ID={gateId}, 오류: {e.Message}");
            }
        }


        private void OnGateDetectedMessage(Features.EscapeGate.Messages.EscapeGateDetectedMessage message)
        {
            OnGateDetected(message.GateId, message.Distance);
        }

        private void OnGateLeftMessage(Features.EscapeGate.Messages.EscapeGateLeftMessage message)
        {
            OnGateLeft(message.GateId);
        }

        private async void OnGateOpenedReceived(EscapeGateOpenedMessage message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] 탈출구 오픈 메시지 수신: {message.Count}개");
            }

            // 1단계: 등록되지 않은 게이트들 먼저 동적 생성
            foreach (var gateId in message.GateIds)
            {
                if (!_registeredGates.ContainsKey(gateId))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[EscapeGateServiceImpl] 등록되지 않은 게이트 발견, 동적 생성 시작: ID={gateId}");
                    }
                    await SpawnEscapeGateAsync(gateId);
                }
            }

            // 2단계: 모든 게이트들 활성화 (기존 + 새로 생성된 게이트)
            ActivateGates(message.GateIds);

            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] 탈출구 처리 완료 - 총 {message.GateIds.Length}개 게이트 활성화");
            }
        }

        private void OnHoldStartMessage(InteractHoldStartMessage message)
        {
            // 현재 감지된 게이트가 있고, 활성화된 상태인지 확인
            if (_currentGateId.Value == -1 || !_isInRange.Value)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[EscapeGateServiceImpl] 홀드 시작 - 감지된 탈출구가 없음");
                }
                return;
            }

            if (!_registeredGates.TryGetValue(_currentGateId.Value, out var gateData) || !gateData.CanInteract)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 홀드 시작 - 탈출구 상호작용 불가: ID={_currentGateId.Value}");
                }
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] 탈출 홀드 시작: {gateData.GateName}");
            }

            // 홀드 시작 시에는 로그만 남기고 실제 탈출은 홀드 완료 시에 처리
        }

        private void OnHoldEndMessage(InteractHoldEndMessage message)
        {
            // 홀드 완료 시 탈출 시도
            if (_currentGateId.Value == -1 || !_isInRange.Value)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[EscapeGateServiceImpl] 홀드 완료 - 감지된 탈출구가 없음");
                }
                return;
            }

            if (!_registeredGates.TryGetValue(_currentGateId.Value, out var gateData) || !gateData.CanInteract)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 홀드 완료 - 탈출구 상호작용 불가: ID={_currentGateId.Value}");
                }
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] 탈출 홀드 완료 - 탈출 시도: {gateData.GateName}");
            }

            // 홀드 완료 시 탈출 시도
            AttemptEscape(_currentGateId.Value);
        }


        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            _currentGateId?.Dispose();
            _isInRange?.Dispose();
            _currentGateState?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[EscapeGateServiceImpl] Dispose 완료");
            }
        }

        #endregion
    }
}