using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Ggumtle.Messages;
using Features.Ggumtle.Models;
using Features.Ggumtle.Services;
using Features.MobileControls.Messages;
using Features.Notification.Messages;
using Features.Notification.Models;
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

        // 상태 제어 플래그
        public readonly ReactiveProperty<bool> IsDiggingInProgress = new(false);
        public readonly ReactiveProperty<bool> IsCancelRequested = new(false);
        public readonly ReactiveProperty<bool> IsQuitRequested = new(false);

        // 먹이주기 관련
        public readonly ReactiveProperty<int> CurrentFood = new(0);
        public readonly ReactiveProperty<int> MaxFood = new(30);
        public readonly ReactiveProperty<float> FoodProgress = new(0f);

        #endregion

        #region Dependencies

        private readonly IGgumtleService _ggumtleService;
        private readonly IPublisher<Features.Notification.Messages.NotificationMessage> _notificationPublisher;
        private readonly IPublisher<InteractButtonVisibilityMessage> _interactButtonVisibilityPublisher;

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
            ISubscriber<GgumtleDiggingDoneMessage> diggingDoneSubscriber,
            ISubscriber<GgumtleJellyEatenMessage> jellyEatenSubscriber,
            IPublisher<Features.Notification.Messages.NotificationMessage> notificationPublisher,
            IPublisher<InteractButtonVisibilityMessage> interactButtonVisibilityPublisher,
            ISubscriber<InteractHoldStartMessage> interactHoldStartSubscriber,
            ISubscriber<InteractHoldEndMessage> interactHoldEndSubscriber
        )
        {
            _ggumtleService = ggumtleService;
            _notificationPublisher = notificationPublisher;
            _interactButtonVisibilityPublisher = interactButtonVisibilityPublisher;

            Initialize(
                detectedSubscriber,
                leftSubscriber,
                stateChangedSubscriber,
                holdProgressSubscriber,
                foodAddedSubscriber,
                diggingDoneSubscriber,
                jellyEatenSubscriber,
                interactHoldStartSubscriber,
                interactHoldEndSubscriber
            );
        }

        private void Initialize(
            ISubscriber<GgumtleDetectedMessage> detectedSubscriber,
            ISubscriber<GgumtleLeftMessage> leftSubscriber,
            ISubscriber<GgumtleStateChangedMessage> stateChangedSubscriber,
            ISubscriber<GgumtleHoldProgressMessage> holdProgressSubscriber,
            ISubscriber<GgumtleFoodAddedMessage> foodAddedSubscriber,
            ISubscriber<GgumtleDiggingDoneMessage> diggingDoneSubscriber,
            ISubscriber<GgumtleJellyEatenMessage> jellyEatenSubscriber,
            ISubscriber<InteractHoldStartMessage> interactHoldStartSubscriber,
            ISubscriber<InteractHoldEndMessage> interactHoldEndSubscriber
        )
        {
            if (_isInitialized)
                return;

            // 메시지 구독
            detectedSubscriber.Subscribe(OnGgumtleDetected).AddTo(_disposables);
            leftSubscriber.Subscribe(OnGgumtleLeft).AddTo(_disposables);
            stateChangedSubscriber
                .Subscribe(msg =>
                {
                    if (IsCurrentGgumtle(msg.GgumtleId))
                        OnStateChanged(msg);
                })
                .AddTo(_disposables);
            holdProgressSubscriber
                .Subscribe(msg =>
                {
                    if (IsCurrentGgumtle(msg.GgumtleId))
                        OnHoldProgressUpdated(msg);
                })
                .AddTo(_disposables);
            foodAddedSubscriber
                .Subscribe(msg =>
                {
                    if (IsCurrentGgumtle(msg.GgumtleId))
                        OnFoodAdded(msg);
                })
                .AddTo(_disposables);
            diggingDoneSubscriber.Subscribe(OnDiggingDoneReceived).AddTo(_disposables);

            // 꿈틀이별 먹은 젤리 개수 업데이트 구독
            jellyEatenSubscriber
                .Subscribe(msg =>
                {
                    if (IsCurrentGgumtleById(msg.GgumtleId))
                        OnJellyEatenUpdated(msg);
                })
                .AddTo(_disposables);

            // 모바일 상호작용 이벤트
            interactHoldStartSubscriber
                .Subscribe(_ => OnMobileInteractHoldStart())
                .AddTo(_disposables);
            interactHoldEndSubscriber.Subscribe(_ => OnMobileInteractHoldEnd()).AddTo(_disposables);

            // 자동 업데이트 구독
            State.Subscribe(_ => UpdateInteractionText()).AddTo(_disposables);
            CurrentFood.Subscribe(_ => UpdateInteractionText()).AddTo(_disposables);
            CurrentFood
                .CombineLatest(MaxFood, (current, max) => max > 0 ? (float)current / max : 0f)
                .Subscribe(progress => FoodProgress.Value = progress)
                .AddTo(_disposables);

            _isInitialized = true;
            Debug.Log("[GgumtleViewModel] 초기화 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 홀드 시작
        /// </summary>
        public async UniTask StartHold()
        {
            if (!IsValidForHold())
                return;

            Debug.Log($"[GgumtleViewModel] 홀드 시작: {CurrentGgumtleId.Value}");

            ResetFlags();
            InitializeHold();

            try
            {
                var data = GetCurrentGgumtleData();
                if (data == null)
                    return;

                var holdTime = GetHoldDuration(data);
                HoldDuration.Value = holdTime;

                // 네트워크 작업 처리
                if (!await HandleNetworkActions(data))
                    return;
                if (IsCancelRequested.Value)
                    return;

                // 홀드 실행
                await PerformHold(holdTime, _holdCts.Token);

                // 완료 처리
                await HandleHoldCompletion(data);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[GgumtleViewModel] 홀드 취소됨");
                await HandleHoldCancellation();
            }
            finally
            {
                FinalizeHold();
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
            IsCancelRequested.Value = true;
            _holdCts?.Cancel();
            await HandleHoldCancellation();
        }

        #endregion

        #region Event Handlers

        private void OnGgumtleDetected(GgumtleDetectedMessage msg)
        {
            Debug.Log(
                $"[GgumtleViewModel] 꿈틀이 감지: ID={msg.GgumtleId}, State={msg.CurrentState}"
            );

            SetCurrentGgumtle(msg.GgumtleId, msg.CurrentState, msg.Distance);
            ShowInteractButton("Ggumtle detected");
        }

        private void OnGgumtleLeft(GgumtleLeftMessage msg)
        {
            if (CurrentGgumtleId.Value == msg.GgumtleId)
            {
                Debug.Log($"[GgumtleViewModel] 꿈틀이 벗어남: {msg.GgumtleId}");
                ClearCurrentGgumtle();
                HideInteractButton("Ggumtle left range");
            }
        }

        private void OnStateChanged(GgumtleStateChangedMessage msg)
        {
            State.Value = msg.NewState;
            UpdateFromService();
            UpdateCurrentFood(CurrentGgumtleId.Value); // 서버 동기화된 젤리 개수로 업데이트
            Debug.Log($"[GgumtleViewModel] 상태 변경: {msg.PreviousState} → {msg.NewState}");

            // Fake 또는 Emerging 상태일 때 홀드 프로그레스바만 숨기기
            if (msg.NewState == GgumtleState.Fake || msg.NewState == GgumtleState.Emerging)
            {
                HoldProgress.Value = 0f;
                // FoodProgress는 UI에서 상태에 따라 표시/숨김 처리하도록 함
            }
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

        private void OnDiggingDoneReceived(GgumtleDiggingDoneMessage msg)
        {
            if (!IsCurrentGgumtleById(msg.GgumtleId))
                return;

            Debug.Log(
                $"[GgumtleViewModel] 서버 파기 완료: GgumtleId={msg.GgumtleId}, IsReal={msg.IsRealGgumtle}"
            );

            // 중단 요청 중이면 무시
            if (IsCancelRequested.Value)
            {
                Debug.Log("[GgumtleViewModel] 중단 요청 중 - 서버 응답 무시");
                return;
            }

            // 파기 진행 중이었다면 완료 처리
            if (IsDiggingInProgress.Value)
            {
                CompleteDigging(msg.IsRealGgumtle);
            }
        }

        private void OnJellyEatenUpdated(GgumtleJellyEatenMessage msg)
        {
            // 현재 접근 중인 꿈틀이의 먹은 젤리 개수가 업데이트되면 즉시 UI 반영
            UpdateCurrentFood(CurrentGgumtleId.Value);

            Debug.Log(
                $"[GgumtleViewModel] 꿈틀이 {msg.GgumtleId} 젤리 개수 실시간 업데이트: {msg.EatenCount}"
            );
        }

        private async void OnMobileInteractHoldStart()
        {
            if (IsValidForHold())
            {
                Debug.Log("[GgumtleViewModel] 모바일 홀드 시작");
                await StartHold();
            }
        }

        private void OnMobileInteractHoldEnd()
        {
            Debug.Log(
                $"[GgumtleViewModel] OnMobileInteractHoldEnd 호출됨 - IsHolding: {IsHolding.Value}, CurrentGgumtleId: {CurrentGgumtleId.Value}"
            );

            if (IsHolding.Value)
            {
                Debug.Log("[GgumtleViewModel] 모바일 홀드 종료 - CancelHold() 호출");
                CancelHold();
            }
            else
            {
                Debug.Log("[GgumtleViewModel] IsHolding이 false라서 CancelHold() 호출하지 않음");
            }
        }

        #endregion

        #region Private Methods - Hold Processing

        private bool IsValidForHold()
        {
            if (!IsInRange.Value || string.IsNullOrEmpty(CurrentGgumtleId.Value))
            {
                Debug.LogWarning("[GgumtleViewModel] 홀드 불가: 범위 밖이거나 꿈틀이 없음");
                return false;
            }

            if (IsHolding.Value)
            {
                Debug.LogWarning("[GgumtleViewModel] 이미 홀드 중");
                return false;
            }

            return true;
        }

        private void ResetFlags()
        {
            IsCancelRequested.Value = false;
            IsDiggingInProgress.Value = false;
            IsQuitRequested.Value = false;
        }

        private void InitializeHold()
        {
            _holdCts?.Cancel();
            _holdCts = new CancellationTokenSource();
            IsHolding.Value = true;
            HoldProgress.Value = 0f;
        }

        private GgumtleData GetCurrentGgumtleData()
        {
            try
            {
                var data = _ggumtleService.GetGgumtleData(CurrentGgumtleId.Value);
                if (data == null)
                {
                    Debug.LogError("[GgumtleViewModel] 꿈틀이 데이터 없음");
                    ResetHoldState();
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleViewModel] 데이터 조회 실패: {e.Message}");
                ResetHoldState();
                return null;
            }
        }

        private async UniTask<bool> HandleNetworkActions(GgumtleData data)
        {
            switch (data.currentState)
            {
                case GgumtleState.Buried:
                    return await StartDigging();

                case GgumtleState.Feeding:
                    return await StartFeeding();

                default:
                    // _ggumtleService.StartHold(CurrentGgumtleId.Value);
                    return true;
            }
        }

        private async UniTask<bool> StartDigging()
        {
            Debug.Log("[GgumtleViewModel] 파기 시작");
            IsDiggingInProgress.Value = true;

            var success = await _ggumtleService.StartNetworkDiggingAsync(CurrentGgumtleId.Value);
            Debug.Log($"[GgumtleViewModel] 파기 네트워크 결과: {success}");

            if (!success)
            {
                Debug.LogError("[GgumtleViewModel] 파기 시작 실패");
                ResetHoldState();
                return false;
            }

            return true;
        }

        private async UniTask<bool> StartFeeding()
        {
            Debug.Log("[GgumtleViewModel] 먹이주기 시작");
            var success = await _ggumtleService.StartNetworkFeedingAsync(CurrentGgumtleId.Value);
            Debug.Log($"[GgumtleViewModel] 먹이주기 네트워크 결과: {success}");

            if (!success)
            {
                Debug.LogError("[GgumtleViewModel] 먹이주기 시작 실패");
                ResetHoldState();
                return false;
            }

            return true;
        }

        private async UniTask PerformHold(float duration, CancellationToken ct)
        {
            float elapsed = 0f;
            Debug.Log($"[GgumtleViewModel] 홀드 진행 시작: {duration:F2}s");

            while (elapsed < duration && !ct.IsCancellationRequested && !IsCancelRequested.Value)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);

                HoldProgress.Value = progress;
                _ggumtleService.UpdateHoldProgress(CurrentGgumtleId.Value, progress);

                await UniTask.Yield(ct);
            }

            Debug.Log($"[GgumtleViewModel] 홀드 진행 완료: {elapsed:F2}s");
        }

        private async UniTask HandleHoldCompletion(GgumtleData data)
        {
            if (_holdCts.Token.IsCancellationRequested || IsCancelRequested.Value)
                return;

            if (data.currentState == GgumtleState.Buried && IsDiggingInProgress.Value)
            {
                // 파기 완료 - Semi-optimistic 패턴
                Debug.Log("[GgumtleViewModel] 파기 홀드 완료 - Emerging 상태로 변경");
                await StartEmerging();
                // 홀드 상태 리셋
                IsHolding.Value = false;
                HoldProgress.Value = 0f;
            }
            else if (
                data.currentState == GgumtleState.Feeding
                || data.currentState == GgumtleState.Emerged
            )
            {
                // 먹이주기는 홀드 완료되어도 중단 가능하도록 IsHolding 유지
                Debug.Log("[GgumtleViewModel] 먹이주기 홀드 완료 - 중단 가능하도록 IsHolding 유지");
                // IsHolding.Value는 true로 유지하여 사용자가 버튼을 뗄 때 중단 가능
                // HoldProgress는 리셋하지 않음
                return;
            }
            else
            {
                // 일반 홀드 완료
                _ggumtleService.CompleteHold(CurrentGgumtleId.Value);
                // 홀드 상태 리셋
                IsHolding.Value = false;
                HoldProgress.Value = 0f;
            }
        }

        private async UniTask HandleHoldCancellation()
        {
            Debug.Log(
                $"[GgumtleViewModel] 홀드 취소 처리 - CurrentGgumtleId: {CurrentGgumtleId.Value}"
            );

            var data = _ggumtleService.GetGgumtleData(CurrentGgumtleId.Value);
            if (data != null)
            {
                Debug.Log($"[GgumtleViewModel] GgumtleData 찾음 - 상태: {data.currentState}");
                await HandleNetworkCancellation(data);
            }
            else
            {
                Debug.LogError(
                    $"[GgumtleViewModel] GgumtleData를 찾을 수 없음: {CurrentGgumtleId.Value}"
                );
            }

            _ggumtleService.CancelHold(CurrentGgumtleId.Value);

            // 홀드 상태 리셋
            IsHolding.Value = false;
            HoldProgress.Value = 0f;

            Debug.Log("[GgumtleViewModel] 홀드 취소 완료");
        }

        private async UniTask HandleNetworkCancellation(GgumtleData data)
        {
            Debug.Log(
                $"[GgumtleViewModel] HandleNetworkCancellation 호출됨 - 상태: {data.currentState}"
            );

            // 파기 중단
            if (
                IsDiggingInProgress.Value
                && data.currentState == GgumtleState.Digging
                && !IsQuitRequested.Value
            )
            {
                IsQuitRequested.Value = true;
                try
                {
                    var success = await _ggumtleService.StopNetworkDiggingAsync();
                    Debug.Log($"[GgumtleViewModel] 파기 중단 결과: {success}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GgumtleViewModel] 파기 중단 예외: {ex.Message}");
                }
                finally
                {
                    IsQuitRequested.Value = false;
                }
            }
            // 먹이주기 중단 (Emerging, Emerged, Feeding 상태)
            else if (
                data.currentState == GgumtleState.Emerging
                || data.currentState == GgumtleState.Emerged
                || data.currentState == GgumtleState.Feeding
            )
            {
                Debug.Log(
                    $"[GgumtleViewModel] 먹이주기 중단 시도, 현재 상태: {data.currentState}, GgumtleId: {CurrentGgumtleId.Value}"
                );
                Debug.Log(
                    $"[GgumtleViewModel] 먹이주기 중단 전 - IsHolding: {IsHolding.Value}, IsCancelRequested: {IsCancelRequested.Value}"
                );
                try
                {
                    var success = _ggumtleService.StopNetworkFeeding();
                    Debug.Log(
                        $"[GgumtleViewModel] 먹이주기 중단 결과: {success}, 상태: {data.currentState}"
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GgumtleViewModel] 먹이주기 중단 예외: {ex.Message}");
                }
            }
        }

        private void FinalizeHold()
        {
            if (!IsDiggingInProgress.Value || IsCancelRequested.Value)
            {
                ResetHoldState();
            }
        }

        private async UniTask StartEmerging()
        {
            Debug.Log("[GgumtleViewModel] Emerging 상태 시작");

            _holdCts?.Cancel();
            HoldProgress.Value = 1f;
            _ggumtleService.CompleteHold(CurrentGgumtleId.Value);

            IsHolding.Value = false;
            HoldProgress.Value = 0f;
        }

        private async void CompleteDigging(bool isRealGgumtle)
        {
            Debug.Log($"[GgumtleViewModel] 최종 파기 완료: IsReal={isRealGgumtle}");

            if (isRealGgumtle)
            {
                HandleRealGgumtle();
            }
            else
            {
                await HandleFakeGgumtle();
            }

            ResetDiggingFlags();
        }

        private void HandleRealGgumtle()
        {
            Debug.Log("[GgumtleViewModel] 진짜 꿈틀이 발견");

            if (int.TryParse(CurrentGgumtleId.Value, out int ggumtleId))
            {
                _ggumtleService.HandleDiggingDone(ggumtleId, true);
            }

            _notificationPublisher.Publish(
                new Features.Notification.Messages.NotificationMessage(
                    "진짜 꿈틀이를 발견했습니다!",
                    3f,
                    Features.Notification.Models.NotificationType.Success
                )
            );
        }

        private async UniTask HandleFakeGgumtle()
        {
            Debug.Log("[GgumtleViewModel] 가짜 꿈틀이 처리");

            _notificationPublisher.Publish(
                new Features.Notification.Messages.NotificationMessage(
                    "가짜 꿈틀이였습니다... 2초 후 사라집니다.",
                    3f,
                    Features.Notification.Models.NotificationType.Warning
                )
            );

            if (int.TryParse(CurrentGgumtleId.Value, out int ggumtleId))
            {
                _ggumtleService.HandleDiggingDone(ggumtleId, false);
            }

            await UniTask.Delay(2000);
            // UnregisterGgumtle 호출 제거 - 가짜 꿈틀이 처리 시 다른 꿈틀이들에게 영향을 주지 않도록 함
            // _ggumtleService.UnregisterGgumtle(CurrentGgumtleId.Value);
            Debug.Log(
                $"[GgumtleViewModel] 가짜 꿈틀이 처리 완료 - UnregisterGgumtle 호출하지 않음: {CurrentGgumtleId.Value}"
            );
        }

        private void ResetHoldState()
        {
            IsHolding.Value = false;
            HoldProgress.Value = 0f;
            ResetDiggingFlags();
            Debug.Log("[GgumtleViewModel] 홀드 상태 리셋");
        }

        private void ResetDiggingFlags()
        {
            IsDiggingInProgress.Value = false;
            IsCancelRequested.Value = false;
            IsQuitRequested.Value = false;
        }

        #endregion

        #region Private Methods - Utilities

        private bool IsCurrentGgumtle(string ggumtleId) => CurrentGgumtleId.Value == ggumtleId;

        private bool IsCurrentGgumtleById(int ggumtleId) =>
            int.TryParse(CurrentGgumtleId.Value, out int currentId) && currentId == ggumtleId;

        private void SetCurrentGgumtle(string id, GgumtleState state, float distance)
        {
            CurrentGgumtleId.Value = id;
            IsInRange.Value = true;
            Distance.Value = distance;
            State.Value = state;
            UpdateFromService();
            UpdateCurrentFood(id);
            UpdateInteractionText();
        }

        private void ClearCurrentGgumtle()
        {
            IsInRange.Value = false;
            CancelHold();
            CurrentGgumtleId.Value = string.Empty;
            Distance.Value = float.MaxValue;
            CanInteract.Value = false;
            InteractionText.Value = string.Empty;
        }

        private void ShowInteractButton(string reason)
        {
            _interactButtonVisibilityPublisher.Publish(
                new InteractButtonVisibilityMessage(true, reason)
            );
        }

        private void HideInteractButton(string reason)
        {
            _interactButtonVisibilityPublisher.Publish(
                new InteractButtonVisibilityMessage(false, reason)
            );
        }

        private float GetHoldDuration(GgumtleData data)
        {
            return data.currentState switch
            {
                GgumtleState.Buried or GgumtleState.Digging => data.diggingHoldTime,
                GgumtleState.Feeding => float.MaxValue, // 먹이주기는 사용자가 뗄 때까지 무한히 유지
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
                GgumtleState.Emerged => $"빛젤리 먹이기 ({CurrentFood.Value}/{MaxFood.Value})",
                GgumtleState.Feeding => $"빛젤리 먹이기 ({CurrentFood.Value}/{MaxFood.Value})",
                GgumtleState.Purified => "정화 완료!",
                GgumtleState.Fake => string.Empty, // 짭꿈틀이는 텍스트 표시 안함
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 현재 꿈틀이의 먹은 젤리 개수 업데이트
        /// </summary>
        private void UpdateCurrentFood(string ggumtleId)
        {
            if (int.TryParse(ggumtleId, out int id))
            {
                int eatenCount = _ggumtleService.GetGgumtleJellyEaten(id);
                CurrentFood.Value = eatenCount;
            }
            else
            {
                CurrentFood.Value = 0;
            }

            // 텍스트도 함께 업데이트
            UpdateInteractionText();
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
