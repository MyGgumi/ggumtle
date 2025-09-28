using System;
using System.Collections.Generic;
using System.Linq;
using Features.EscapeGate.Messages;
using Features.EscapeGate.Models;
using Features.EscapeGate.NetworkSources;
using Features.Notification.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.EscapeGate.Services
{
    /// <summary>
    /// 탈출 게이트 관리 서비스 구현체
    /// </summary>
    public class EscapeGateServiceImpl : IEscapeGateService, IDisposable
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

        #endregion

        #region Dependencies

        private readonly IEscapeGateNetworkSource _networkSource;
        private readonly INotificationService _notificationService;
        private readonly ISubscriber<EscapeGateOpenedMessage> _gateOpenedSubscriber;

        #endregion

        #region Constructor

        [Inject]
        public EscapeGateServiceImpl(
            IEscapeGateNetworkSource networkSource,
            INotificationService notificationService,
            ISubscriber<EscapeGateOpenedMessage> gateOpenedSubscriber
        )
        {
            _networkSource = networkSource;
            _notificationService = notificationService;
            _gateOpenedSubscriber = gateOpenedSubscriber;

            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // 탈출구 오픈 메시지 구독
            _gateOpenedSubscriber.Subscribe(OnGateOpenedReceived).AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[EscapeGateServiceImpl] 초기화 완료");
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
                _notificationService.ShowNotification($"탈출구가 열렸습니다! ({activatedCount}개)", 3f);
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

            if (gateData.IsActive)
            {
                _notificationService.ShowNotification("얼른 탈출하세요!", 2f);
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
                gateData.SetInUse();
                if (_currentGateId.Value == gateId)
                {
                    _currentGateState.Value = gateData.State;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateServiceImpl] 탈출 시도 시작: {gateData.GateName}");
                }

                var response = await _networkSource.AttemptEscapeAsync(gateId);

                if (response.Success)
                {
                    _notificationService.ShowSuccessNotification("탈출 성공!");
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[EscapeGateServiceImpl] 탈출 성공: {gateData.GateName}");
                    }
                }
                else
                {
                    _notificationService.ShowWarningNotification("탈출에 실패했습니다.");
                    gateData.Activate(); // 다시 활성화 상태로 복원
                    if (_currentGateId.Value == gateId)
                    {
                        _currentGateState.Value = gateData.State;
                    }
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

                gateData.Activate(); // 다시 활성화 상태로 복원
                if (_currentGateId.Value == gateId)
                {
                    _currentGateState.Value = gateData.State;
                }
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

        private void OnGateOpenedReceived(EscapeGateOpenedMessage message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[EscapeGateServiceImpl] 탈출구 오픈 메시지 수신: {message.Count}개");
            }

            ActivateGates(message.GateIds);
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