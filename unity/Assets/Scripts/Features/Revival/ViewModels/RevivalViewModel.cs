using System;
using Cysharp.Threading.Tasks;
using Features.Revival.Models;
using Features.Revival.Messages;
using Features.Revival.Services;
using Features.Player.Services;
using Features.MobileControls.Messages;
using MessagePipe;
using Networks.Players;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Revival.ViewModels
{
    /// <summary>
    /// 부활 시스템 ViewModel
    /// RevivalService와 연동하여 UI 상태를 관리
    /// </summary>
    public class RevivalViewModel : IDisposable
    {
        #region Observable Properties

        /// <summary>
        /// 부활 상호작용 범위 내에 있는지
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsInRange => _isInRange;

        /// <summary>
        /// 부활 가능한지
        /// </summary>
        public ReadOnlyReactiveProperty<bool> CanRevive => _canRevive;

        /// <summary>
        /// 상호작용 텍스트
        /// </summary>
        public ReadOnlyReactiveProperty<string> InteractionText => _interactionText;

        /// <summary>
        /// 홀드 중인지 (직접 부활)
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsHolding => _isHolding;

        /// <summary>
        /// 홀드 진행률 (직접 부활)
        /// </summary>
        public ReadOnlyReactiveProperty<float> HoldProgress => _holdProgress;

        /// <summary>
        /// 자가제세동기 부활 진행 중인지
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsSelfDefibReviving => _isSelfDefibReviving;

        /// <summary>
        /// 자가제세동기 부활 진행률
        /// </summary>
        public ReadOnlyReactiveProperty<float> SelfDefibProgress => _selfDefibProgress;

        /// <summary>
        /// 부활 상태 텍스트
        /// </summary>
        public ReadOnlyReactiveProperty<string> RevivalStatusText => _revivalStatusText;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<bool> _isInRange = new(false);
        private readonly ReactiveProperty<bool> _canRevive = new(false);
        private readonly ReactiveProperty<string> _interactionText = new("");
        private readonly ReactiveProperty<bool> _isHolding = new(false);
        private readonly ReactiveProperty<float> _holdProgress = new(0f);
        private readonly ReactiveProperty<bool> _isSelfDefibReviving = new(false);
        private readonly ReactiveProperty<float> _selfDefibProgress = new(0f);
        private readonly ReactiveProperty<string> _revivalStatusText = new("");

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        private long _currentTargetPlayerId = -1;

        #endregion

        #region Dependencies

        private readonly IRevivalService _revivalService;
        private readonly PlayerManagerService _playerManagerService;
        private readonly ISubscriber<InteractHoldStartMessage> _holdStartSubscriber;
        private readonly ISubscriber<InteractHoldEndMessage> _holdEndSubscriber;
        private readonly ISubscriber<RevivalCompletedMessage> _revivalCompletedSubscriber;

        #endregion

        #region Constructor

        [Inject]
        public RevivalViewModel(
            IRevivalService revivalService,
            PlayerManagerService playerManagerService,
            ISubscriber<InteractHoldStartMessage> holdStartSubscriber,
            ISubscriber<InteractHoldEndMessage> holdEndSubscriber,
            ISubscriber<RevivalCompletedMessage> revivalCompletedSubscriber)
        {
            _revivalService = revivalService;
            _playerManagerService = playerManagerService;
            _holdStartSubscriber = holdStartSubscriber;
            _holdEndSubscriber = holdEndSubscriber;
            _revivalCompletedSubscriber = revivalCompletedSubscriber;

            SubscribeToService();
            SubscribeToMessages();

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalViewModel] 초기화 완료");
            }
        }

        #endregion

        #region Service Subscriptions

        private void SubscribeToService()
        {
            // 상호작용 상태 구독
            _revivalService.InteractionState
                .Subscribe(OnInteractionStateChanged)
                .AddTo(_disposables);

            // 직접 부활 상태 구독
            _revivalService.IsDirectReviving
                .Subscribe(OnDirectRevivingChanged)
                .AddTo(_disposables);

            _revivalService.CurrentDirectRevival
                .Subscribe(OnDirectRevivalProgressChanged)
                .AddTo(_disposables);

            // 자가제세동기 부활 상태 구독
            _revivalService.IsSelfDefibReviving
                .Subscribe(OnSelfDefibRevivingChanged)
                .AddTo(_disposables);

            _revivalService.CurrentSelfDefibRevival
                .Subscribe(OnSelfDefibRevivalProgressChanged)
                .AddTo(_disposables);
        }

        private void OnInteractionStateChanged(RevivalInteractionData interactionData)
        {
            _currentTargetPlayerId = interactionData.targetPlayerId;
            _isInRange.Value = interactionData.isInRange;
            _canRevive.Value = interactionData.canRevive;
            _interactionText.Value = interactionData.interactionText;

            if (_enableDebugLogs)
            {
                Debug.Log($"[RevivalViewModel] 상호작용 상태 변경: TargetId={interactionData.targetPlayerId}, InRange={interactionData.isInRange}, CanRevive={interactionData.canRevive}");
            }
        }

        private void OnDirectRevivingChanged(bool isReviving)
        {
            _isHolding.Value = isReviving;

            if (isReviving)
            {
                _revivalStatusText.Value = "부활시키는 중...";
            }
            else
            {
                _holdProgress.Value = 0f;
                if (_revivalStatusText.Value == "부활시키는 중...")
                {
                    _revivalStatusText.Value = "";
                }
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[RevivalViewModel] 직접 부활 상태 변경: IsReviving={isReviving}");
            }
        }

        private void OnDirectRevivalProgressChanged(RevivalProgressData progressData)
        {
            if (progressData.state == RevivalState.DirectReviving)
            {
                _holdProgress.Value = progressData.progress;

                // 자가제세동기처럼 남은 시간 표시
                float remainingTime = progressData.GetRemainingTime();
                _revivalStatusText.Value = $"부활시키는 중... ({remainingTime:F1}초)";

                if (_enableDebugLogs)
                {
                    Debug.Log($"[RevivalViewModel] 직접 부활 진행률: {progressData.progress:P1}, 남은 시간: {remainingTime:F1}초");
                }
            }
        }

        private void OnSelfDefibRevivingChanged(bool isReviving)
        {
            _isSelfDefibReviving.Value = isReviving;

            if (isReviving)
            {
                _revivalStatusText.Value = "자가제세동기 작동 중...";
            }
            else
            {
                _selfDefibProgress.Value = 0f;
                if (_revivalStatusText.Value == "자가제세동기 작동 중...")
                {
                    _revivalStatusText.Value = "";
                }
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[RevivalViewModel] 자가제세동기 부활 상태 변경: IsReviving={isReviving}");
            }
        }

        private void OnSelfDefibRevivalProgressChanged(RevivalProgressData progressData)
        {
            if (progressData.state == RevivalState.SelfDefibReviving)
            {
                _selfDefibProgress.Value = progressData.progress;

                float remainingTime = progressData.GetRemainingTime();
                _revivalStatusText.Value = $"자가제세동기 작동 중... ({remainingTime:F1}초)";

                if (_enableDebugLogs)
                {
                    Debug.Log($"[RevivalViewModel] 자가제세동기 부활 진행률: {progressData.progress:P1}");
                }
            }
        }

        #endregion

        #region Message Subscriptions

        private void SubscribeToMessages()
        {
            // 홀드 시작 메시지 구독
            _holdStartSubscriber
                .Subscribe(OnMobileInteractHoldStart)
                .AddTo(_disposables);

            // 홀드 종료 메시지 구독
            _holdEndSubscriber
                .Subscribe(OnMobileInteractHoldEnd)
                .AddTo(_disposables);

            // 부활 완료 메시지 구독
            _revivalCompletedSubscriber
                .Subscribe(OnRevivalCompleted)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalViewModel] 메시지 구독 완료");
            }
        }

        private async void OnMobileInteractHoldStart(InteractHoldStartMessage message)
        {
            // Revival 상황인지 확인 (기절한 플레이어와 상호작용 중)
            if (!IsValidForRevivalHold())
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[RevivalViewModel] Revival 홀드 불가: 기절한 플레이어와 상호작용 중이 아님");
                }
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalViewModel] Revival 홀드 시작");
            }

            _isHolding.Value = true;

            try
            {
                bool success = await StartDirectRevivalAsync();
                if (!success)
                {
                    _isHolding.Value = false;
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning("[RevivalViewModel] 직접 부활 시작 실패");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RevivalViewModel] 홀드 시작 중 예외: {e.Message}");
                _isHolding.Value = false;
            }
        }

        private async void OnMobileInteractHoldEnd(InteractHoldEndMessage message)
        {
            if (!_isHolding.CurrentValue)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[RevivalViewModel] Revival IsHolding이 false라서 CancelHold() 호출하지 않음");
                }
                return;
            }

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalViewModel] Revival 홀드 종료 - CancelDirectRevival 호출");
            }

            try
            {
                await CancelDirectRevivalAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RevivalViewModel] 홀드 종료 중 예외: {e.Message}");
            }
            finally
            {
                _isHolding.Value = false;
            }
        }

        private bool IsValidForRevivalHold()
        {
            // 범위 내에 있고, 부활 가능하며, 현재 대상 플레이어가 있는지 확인
            return _isInRange.CurrentValue &&
                   _canRevive.CurrentValue &&
                   _currentTargetPlayerId > 0 &&
                   !_isSelfDefibReviving.CurrentValue;
        }

        private void OnRevivalCompleted(RevivalCompletedMessage message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[RevivalViewModel] 부활 완료 메시지 수신: RevivedId={message.revivedPlayerId}, CurrentTarget={_currentTargetPlayerId}");
            }

            // 현재 부활 대상과 일치하는 경우에만 UI 정리
            if (message.revivedPlayerId == _currentTargetPlayerId)
            {
                // 홀드 상태 즉시 정리
                _isHolding.Value = false;
                _holdProgress.Value = 0f;

                // 상태 텍스트 클리어
                _revivalStatusText.Value = "";

                // 범위 밖으로 나간 것으로 처리 (UI 숨김)
                _isInRange.Value = false;
                _canRevive.Value = false;
                _interactionText.Value = "";

                // 대상 플레이어 초기화
                _currentTargetPlayerId = -1;

                if (_enableDebugLogs)
                {
                    Debug.Log($"[RevivalViewModel] 부활 완료로 인한 UI 정리 완료: RevivedId={message.revivedPlayerId}");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 직접 부활 시작
        /// </summary>
        public async UniTask<bool> StartDirectRevivalAsync()
        {
            if (!CanRevive.CurrentValue || _currentTargetPlayerId <= 0)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning("[RevivalViewModel] 직접 부활 시작 불가");
                }
                return false;
            }

            var localPlayerId = _playerManagerService?.GetLocalPlayer()?.Id ?? -1;
            if (localPlayerId <= 0)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning("[RevivalViewModel] 로컬 플레이어 ID를 찾을 수 없음");
                }
                return false;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[RevivalViewModel] 직접 부활 시작 요청: TargetId={_currentTargetPlayerId}");
            }

            return await _revivalService.StartDirectRevivalAsync(localPlayerId, _currentTargetPlayerId);
        }

        /// <summary>
        /// 직접 부활 취소
        /// </summary>
        public async UniTask<bool> CancelDirectRevivalAsync()
        {
            var localPlayerId = _playerManagerService?.GetLocalPlayer()?.Id ?? -1;
            if (localPlayerId <= 0)
            {
                return false;
            }

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalViewModel] 직접 부활 취소 요청");
            }

            return await _revivalService.CancelDirectRevivalAsync(localPlayerId);
        }

        /// <summary>
        /// 상호작용 상태 업데이트 (외부에서 호출)
        /// </summary>
        public void UpdateInteractionState(long targetPlayerId, bool isInRange, bool canRevive, float distance, Vector3 targetPosition)
        {
            _revivalService.UpdateInteractionState(targetPlayerId, isInRange, canRevive, distance, targetPosition);
        }

        /// <summary>
        /// 상호작용 상태 초기화
        /// </summary>
        public void ClearInteractionState()
        {
            _revivalService.ClearInteractionState();
        }

        /// <summary>
        /// 현재 진행 중인 부활이 있는지 확인
        /// </summary>
        public bool IsAnyRevivalInProgress()
        {
            return _revivalService.IsAnyRevivalInProgress();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _disposables?.Dispose();
            _isInRange?.Dispose();
            _canRevive?.Dispose();
            _interactionText?.Dispose();
            _isHolding?.Dispose();
            _holdProgress?.Dispose();
            _isSelfDefibReviving?.Dispose();
            _selfDefibProgress?.Dispose();
            _revivalStatusText?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalViewModel] Dispose 완료");
            }
        }

        #endregion
    }
}