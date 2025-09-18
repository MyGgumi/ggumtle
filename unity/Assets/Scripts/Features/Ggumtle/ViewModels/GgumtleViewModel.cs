using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Ggumtle.Messages;
using Features.Ggumtle.Models;
using Features.Ggumtle.Services;
using Features.MobileControls.Messages;
using MessagePipe;
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
        private readonly IPublisher<InteractButtonVisibilityMessage> _interactButtonVisibilityPublisher;
        private readonly ISubscriber<InteractHoldStartMessage> _interactHoldStartSubscriber;
        private readonly ISubscriber<InteractHoldEndMessage> _interactHoldEndSubscriber;

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
            IPublisher<NotificationMessage> notificationPublisher,
            IPublisher<InteractButtonVisibilityMessage> interactButtonVisibilityPublisher,
            ISubscriber<InteractHoldStartMessage> interactHoldStartSubscriber,
            ISubscriber<InteractHoldEndMessage> interactHoldEndSubscriber
        )
        {
            _ggumtleService = ggumtleService;
            _detectedSubscriber = detectedSubscriber;
            _leftSubscriber = leftSubscriber;
            _stateChangedSubscriber = stateChangedSubscriber;
            _holdProgressSubscriber = holdProgressSubscriber;
            _foodAddedSubscriber = foodAddedSubscriber;
            _notificationPublisher = notificationPublisher;
            _interactButtonVisibilityPublisher = interactButtonVisibilityPublisher;
            _interactHoldStartSubscriber = interactHoldStartSubscriber;
            _interactHoldEndSubscriber = interactHoldEndSubscriber;

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

            // 모바일 상호작용 버튼 이벤트 구독
            _interactHoldStartSubscriber
                .Subscribe(_ => OnMobileInteractHoldStart())
                .AddTo(_disposables);
            _interactHoldEndSubscriber
                .Subscribe(_ => OnMobileInteractHoldEnd())
                .AddTo(_disposables);

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

            // 수동으로 InteractionText 업데이트 (혹시 모를 타이밍 이슈 해결)
            UpdateInteractionText();

            // 모바일 상호작용 버튼 표시 요청
            _interactButtonVisibilityPublisher.Publish(
                new InteractButtonVisibilityMessage(true, "Ggumtle detected")
            );

            Debug.Log($"[GgumtleViewModel] 꿈틀이 감지: {msg.GgumtleId}, 상태: {msg.CurrentState}");
        }

        private void OnGgumtleLeft(GgumtleLeftMessage msg)
        {
            if (CurrentGgumtleId.Value == msg.GgumtleId)
            {
                IsInRange.Value = false;
                CancelHold();

                // 모바일 상호작용 버튼 숨김 요청
                _interactButtonVisibilityPublisher.Publish(
                    new InteractButtonVisibilityMessage(false, "Ggumtle left range")
                );

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

        #region Mobile Interaction Handlers

        /// <summary>
        /// 모바일 상호작용 버튼 홀드 시작 처리
        /// </summary>
        private async void OnMobileInteractHoldStart()
        {
            Debug.Log(
                $"[GgumtleViewModel] OnMobileInteractHoldStart 호출됨 - IsInRange: {IsInRange.Value}, CurrentGgumtleId: '{CurrentGgumtleId.Value}'"
            );

            if (!IsInRange.Value)
            {
                Debug.LogWarning("[GgumtleViewModel] 모바일 홀드 시작 실패: 범위 밖");
                return;
            }

            if (string.IsNullOrEmpty(CurrentGgumtleId.Value))
            {
                Debug.LogWarning("[GgumtleViewModel] 모바일 홀드 시작 실패: 꿈틀이 ID 없음");
                return;
            }

            Debug.Log("[GgumtleViewModel] 모바일 상호작용 홀드 시작 - StartHold() 호출");
            await StartHold();
        }

        /// <summary>
        /// 모바일 상호작용 버튼 홀드 종료 처리
        /// </summary>
        private void OnMobileInteractHoldEnd()
        {
            if (IsHolding.Value)
            {
                Debug.Log("[GgumtleViewModel] 모바일 상호작용 홀드 종료");
                CancelHold();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 홀드 시작 (UI 버튼이나 키입력에서 호출)
        /// </summary>
        public async UniTask StartHold()
        {
            Debug.Log(
                $"[GgumtleViewModel] StartHold() 시작 - IsInRange: {IsInRange.Value}, CurrentGgumtleId: '{CurrentGgumtleId.Value}', IsHolding: {IsHolding.Value}"
            );

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

            Debug.Log("[GgumtleViewModel] 홀드 취소 토큰 생성 완료");

            _holdCts?.Cancel();
            _holdCts = new CancellationTokenSource();

            try
            {
                Debug.Log("[GgumtleViewModel] 홀드 try 블록 진입");
                IsHolding.Value = true;
                HoldProgress.Value = 0f;

                // 상태에 따라 다른 홀드 시간
                Debug.Log($"[GgumtleViewModel] 꿈틀이 데이터 조회 시작: {CurrentGgumtleId.Value}");

                GgumtleData data = null;
                try
                {
                    data = _ggumtleService.GetGgumtleData(CurrentGgumtleId.Value);
                    Debug.Log(
                        $"[GgumtleViewModel] GetGgumtleData 호출 완료, 결과: {(data != null ? "성공" : "null")}"
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GgumtleViewModel] GetGgumtleData 호출 중 예외: {e.Message}");
                    IsHolding.Value = false;
                    return;
                }

                if (data == null)
                {
                    Debug.LogError("[GgumtleViewModel] 꿈틀이 데이터 없음");
                    IsHolding.Value = false;
                    return;
                }
                Debug.Log(
                    $"[GgumtleViewModel] 꿈틀이 데이터 조회 완료: {data.ggumtleName}, 상태: {data.currentState}"
                );

                float holdTime = GetHoldDuration(data);
                HoldDuration.Value = holdTime;

                // 상태에 따라 네트워크 호출 또는 로컬 처리
                Debug.Log(
                    $"[GgumtleViewModel] 꿈틀이 현재 상태: {data.currentState}, ID: {CurrentGgumtleId.Value}"
                );

                if (data.currentState == GgumtleState.Buried)
                {
                    // 파기 시작 - 네트워크 호출
                    Debug.Log("[GgumtleViewModel] 파기 네트워크 호출 시작");
                    var success = await _ggumtleService.StartNetworkDiggingAsync(
                        CurrentGgumtleId.Value
                    );
                    Debug.Log($"[GgumtleViewModel] 파기 네트워크 호출 결과: {success}");
                    if (!success)
                    {
                        Debug.LogError("[GgumtleViewModel] 네트워크 파기 시작 실패");
                        IsHolding.Value = false;
                        return;
                    }
                }
                else if (data.currentState == GgumtleState.Feeding)
                {
                    // 먹이주기 시작 - 네트워크 호출
                    Debug.Log("[GgumtleViewModel] 먹이주기 네트워크 호출 시작");
                    var success = await _ggumtleService.StartNetworkFeedingAsync(
                        CurrentGgumtleId.Value
                    );
                    Debug.Log($"[GgumtleViewModel] 먹이주기 네트워크 호출 결과: {success}");
                    if (!success)
                    {
                        Debug.LogError("[GgumtleViewModel] 네트워크 먹이주기 시작 실패");
                        IsHolding.Value = false;
                        return;
                    }
                }
                else
                {
                    // 기타 상태는 로컬 처리
                    Debug.Log($"[GgumtleViewModel] 로컬 홀드 처리 (상태: {data.currentState})");
                    _ggumtleService.StartHold(CurrentGgumtleId.Value);
                }

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
        public async void CancelHold()
        {
            if (!IsHolding.Value)
                return;

            Debug.Log($"[GgumtleViewModel] 홀드 취소: {CurrentGgumtleId.Value}");

            // 홀드 작업 취소
            _holdCts?.Cancel();

            // 현재 상태에 따라 네트워크 종료 호출
            var data = _ggumtleService.GetGgumtleData(CurrentGgumtleId.Value);
            if (data != null)
            {
                if (data.currentState == GgumtleState.Digging)
                {
                    // 파기 중단
                    var success = await _ggumtleService.StopNetworkDiggingAsync();
                    if (!success)
                    {
                        Debug.LogError("[GgumtleViewModel] 네트워크 파기 중단 실패");
                    }
                }
                else if (data.currentState == GgumtleState.Feeding)
                {
                    // 먹이주기 중단
                    var success = await _ggumtleService.StopNetworkFeedingAsync();
                    if (!success)
                    {
                        Debug.LogError("[GgumtleViewModel] 네트워크 먹이주기 중단 실패");
                    }
                }
            }

            // 로컬 Service 취소 호출
            _ggumtleService.CancelHold(CurrentGgumtleId.Value);

            // UI 상태 리셋
            IsHolding.Value = false;
            HoldProgress.Value = 0f;
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
                Debug.Log("[GgumtleViewModel] InteractionText 비워짐 (ID 없음)");
                return;
            }

            var newText = State.Value switch
            {
                GgumtleState.Buried => "파내기 (홀드)",
                GgumtleState.Digging => "파내는 중...",
                GgumtleState.Emerging => "나오는 중...",
                GgumtleState.Feeding => $"빛젤리 먹이기 ({CurrentFood.Value}/{MaxFood.Value})",
                GgumtleState.Purified => "정화 완료!",
                _ => string.Empty,
            };

            InteractionText.Value = newText;
            Debug.Log(
                $"[GgumtleViewModel] InteractionText 업데이트: '{newText}' (상태: {State.Value})"
            );
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
