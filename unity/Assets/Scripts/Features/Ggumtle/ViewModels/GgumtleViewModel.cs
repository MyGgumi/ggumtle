using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Features.Ggumtle.Messages;
using Features.Ggumtle.Models;
using Features.Ggumtle.Services;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Ggumtle.ViewModels
{
    /// <summary>
    /// 꿈틀이 도메인 전체의 상태 관리를 담당하는 ViewModel
    /// GameObject View와 UI View가 모두 이 ViewModel을 구독
    /// </summary>
    public class GgumtleViewModel : IDisposable
    {
        #region Observable Properties

        // 현재 범위 내 꿈틀이 정보
        public readonly ReactiveProperty<string> CurrentGgumtleId = new(string.Empty);
        public readonly ReactiveProperty<bool> IsInRange = new(false);
        public readonly ReactiveProperty<float> Distance = new(float.MaxValue);

        // 꿈틀이 상태
        public readonly ReactiveProperty<GgumtleState> State = new(GgumtleState.Buried);
        public readonly ReactiveProperty<string> InteractionText = new(string.Empty);
        public readonly ReactiveProperty<bool> CanInteract = new(false);

        // 홀드 관련
        public readonly ReactiveProperty<bool> IsHolding = new(false);
        public readonly ReactiveProperty<float> HoldProgress = new(0f);
        public readonly ReactiveProperty<float> HoldDuration = new(0f);

        // 먹이주기 관련
        public readonly ReactiveProperty<int> CurrentFood = new(0);
        public readonly ReactiveProperty<int> MaxFood = new(30);
        public readonly ReactiveProperty<float> FoodProgress = new(0f); // CurrentFood / MaxFood
        #endregion

        #region Dependencies

        private readonly IGgumtleService _ggumtleService;
        private readonly ISubscriber<GgumtleDetectedMessage> _detectedSubscriber;
        private readonly ISubscriber<GgumtleLeftMessage> _leftSubscriber;
        private readonly ISubscriber<GgumtleStateChangedMessage> _stateChangedSubscriber;
        private readonly ISubscriber<GgumtleHoldProgressMessage> _holdProgressSubscriber;
        private readonly ISubscriber<GgumtleFoodAddedMessage> _foodAddedSubscriber;
        private readonly IPublisher<NotificationMessage> _notificationPublisher;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _holdCts;
        private bool _isInitialized = false;

        #endregion

        #region Constructor & Initialization

        [Inject]
        public GgumtleViewModel(
            IGgumtleService ggumtleService,
            ISubscriber<GgumtleDetectedMessage> detectedSubscriber,
            ISubscriber<GgumtleLeftMessage> leftSubscriber,
            ISubscriber<GgumtleStateChangedMessage> stateChangedSubscriber,
            ISubscriber<GgumtleHoldProgressMessage> holdProgressSubscriber,
            ISubscriber<GgumtleFoodAddedMessage> foodAddedSubscriber,
            IPublisher<NotificationMessage> notificationPublisher
        )
        {
            _ggumtleService = ggumtleService;
            _detectedSubscriber = detectedSubscriber;
            _leftSubscriber = leftSubscriber;
            _stateChangedSubscriber = stateChangedSubscriber;
            _holdProgressSubscriber = holdProgressSubscriber;
            _foodAddedSubscriber = foodAddedSubscriber;
            _notificationPublisher = notificationPublisher;

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            // 꿈틀이 감지/벗어남 이벤트 구독
            _detectedSubscriber.Subscribe(OnGgumtleDetected).AddTo(_disposables);
            _leftSubscriber.Subscribe(OnGgumtleLeft).AddTo(_disposables);

            // 꿈틀이 상태 변경 이벤트 구독
            _stateChangedSubscriber.Subscribe(OnStateChangedFiltered).AddTo(_disposables);

            // 홀드 진행 이벤트 구독
            _holdProgressSubscriber.Subscribe(OnHoldProgressUpdatedFiltered).AddTo(_disposables);

            // 먹이 추가 이벤트 구독
            _foodAddedSubscriber.Subscribe(OnFoodAddedFiltered).AddTo(_disposables);

            // 상태 변경시 UI 텍스트 업데이트
            State.Subscribe(_ => UpdateInteractionText()).AddTo(_disposables);
            CurrentFood.Subscribe(_ => UpdateInteractionText()).AddTo(_disposables);

            // 먹이 진행률 계산
            CurrentFood
                .CombineLatest(MaxFood, (current, max) => max > 0 ? (float)current / max : 0f)
                .Subscribe(progress => FoodProgress.Value = progress)
                .AddTo(_disposables);

            _isInitialized = true;
            Debug.Log("[GgumtleViewModel] 초기화 완료");
        }

        #endregion

        #region Event Handlers

        private void OnGgumtleDetected(GgumtleDetectedMessage msg)
        {
            CurrentGgumtleId.Value = msg.GgumtleId;
            IsInRange.Value = true;
            Distance.Value = msg.Distance;
            State.Value = msg.CurrentState;

            // 서비스에서 현재 데이터 가져오기
            UpdateFromService();

            Debug.Log($"[GgumtleViewModel] 꿈틀이 감지: {msg.GgumtleId}, 상태: {msg.CurrentState}");
        }

        private void OnGgumtleLeft(GgumtleLeftMessage msg)
        {
            if (CurrentGgumtleId.Value == msg.GgumtleId)
            {
                IsInRange.Value = false;
                CancelHold();

                // 현재 정보 초기화
                CurrentGgumtleId.Value = string.Empty;
                Distance.Value = float.MaxValue;
                CanInteract.Value = false;
                InteractionText.Value = string.Empty;

                Debug.Log($"[GgumtleViewModel] 꿈틀이 벗어남: {msg.GgumtleId}");
            }
        }

        private void OnStateChanged(GgumtleStateChangedMessage msg)
        {
            State.Value = msg.NewState;
            UpdateFromService();

            Debug.Log($"[GgumtleViewModel] 상태 변경: {msg.PreviousState} → {msg.NewState}");
        }

        private void OnHoldProgressUpdated(GgumtleHoldProgressMessage msg)
        {
            HoldProgress.Value = msg.Progress;
        }

        private void OnFoodAdded(GgumtleFoodAddedMessage msg)
        {
            CurrentFood.Value = msg.CurrentAmount;
            MaxFood.Value = msg.MaxAmount;

            Debug.Log($"[GgumtleViewModel] 먹이 추가: {msg.CurrentAmount}/{msg.MaxAmount}");
        }

        // MessagePipe 필터링 메서드들 (Where 대신 수동 필터링)
        private void OnStateChangedFiltered(GgumtleStateChangedMessage msg)
        {
            if (msg.GgumtleId == CurrentGgumtleId.Value)
            {
                OnStateChanged(msg);
            }
        }

        private void OnHoldProgressUpdatedFiltered(GgumtleHoldProgressMessage msg)
        {
            if (msg.GgumtleId == CurrentGgumtleId.Value)
            {
                OnHoldProgressUpdated(msg);
            }
        }

        private void OnFoodAddedFiltered(GgumtleFoodAddedMessage msg)
        {
            if (msg.GgumtleId == CurrentGgumtleId.Value)
            {
                OnFoodAdded(msg);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 홀드 시작 (UI 버튼이나 키입력에서 호출)
        /// </summary>
        public async UniTask StartHold()
        {
            if (!IsInRange.Value || string.IsNullOrEmpty(CurrentGgumtleId.Value))
            {
                Debug.LogWarning("[GgumtleViewModel] 홀드 시작 실패: 범위 밖이거나 꿈틀이 없음");
                return;
            }

            if (IsHolding.Value)
            {
                Debug.LogWarning("[GgumtleViewModel] 이미 홀드 중");
                return;
            }

            _holdCts?.Cancel();
            _holdCts = new CancellationTokenSource();

            try
            {
                IsHolding.Value = true;
                HoldProgress.Value = 0f;

                // 상태에 따라 다른 홀드 시간
                var data = _ggumtleService.GetGgumtleData(CurrentGgumtleId.Value);
                if (data == null)
                {
                    Debug.LogError("[GgumtleViewModel] 꿈틀이 데이터 없음");
                    return;
                }

                float holdTime = GetHoldDuration(data);
                HoldDuration.Value = holdTime;

                // 서비스에 홀드 시작 알림
                _ggumtleService.StartHold(CurrentGgumtleId.Value);

                // 홀드 진행 (UniTask 사용)
                await PerformHold(holdTime, _holdCts.Token);

                // 홀드 완료
                if (!_holdCts.Token.IsCancellationRequested)
                {
                    _ggumtleService.CompleteHold(CurrentGgumtleId.Value);
                }
            }
            catch (OperationCanceledException)
            {
                // 홀드 취소됨
                Debug.Log("[GgumtleViewModel] 홀드 취소됨");
                _ggumtleService.CancelHold(CurrentGgumtleId.Value);
            }
            finally
            {
                IsHolding.Value = false;
                HoldProgress.Value = 0f;
            }
        }

        /// <summary>
        /// 홀드 취소
        /// </summary>
        public void CancelHold()
        {
            if (IsHolding.Value)
            {
                _holdCts?.Cancel();
                _ggumtleService.CancelHold(CurrentGgumtleId.Value);

                IsHolding.Value = false;
                HoldProgress.Value = 0f;

                Debug.Log("[GgumtleViewModel] 홀드 수동 취소");
            }
        }

        #endregion

        #region Private Methods

        private async UniTask PerformHold(float duration, CancellationToken ct)
        {
            float elapsed = 0f;

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                HoldProgress.Value = progress;

                // 서비스에 진행률 업데이트
                _ggumtleService.UpdateHoldProgress(CurrentGgumtleId.Value, progress);

                await UniTask.Yield(ct);
            }
        }

        private float GetHoldDuration(GgumtleData data)
        {
            return data.currentState switch
            {
                GgumtleState.Buried or GgumtleState.Digging => data.diggingHoldTime,
                GgumtleState.Feeding => 0.5f, // 먹이주기는 0.5초
                _ => 3f,
            };
        }

        private void UpdateFromService()
        {
            if (string.IsNullOrEmpty(CurrentGgumtleId.Value))
                return;

            var data = _ggumtleService.GetGgumtleData(CurrentGgumtleId.Value);
            if (data == null)
                return;

            CanInteract.Value = data.CanInteract();
            CurrentFood.Value = data.currentFoodAmount;
            MaxFood.Value = data.maxFoodRequired;
        }

        private void UpdateInteractionText()
        {
            if (string.IsNullOrEmpty(CurrentGgumtleId.Value))
            {
                InteractionText.Value = string.Empty;
                return;
            }

            InteractionText.Value = State.Value switch
            {
                GgumtleState.Buried => "파내기 (홀드)",
                GgumtleState.Digging => "파내는 중...",
                GgumtleState.Emerging => "나오는 중...",
                GgumtleState.Feeding => $"빛젤리 먹이기 ({CurrentFood.Value}/{MaxFood.Value})",
                GgumtleState.Purified => "정화 완료!",
                _ => string.Empty,
            };
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _holdCts?.Cancel();
            _holdCts?.Dispose();
            _disposables.Dispose();

            Debug.Log("[GgumtleViewModel] Dispose 완료");
        }

        #endregion
    }
}
