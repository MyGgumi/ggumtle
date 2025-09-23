using System;
using Features.GameInfo.Messages;
using Features.GameInfo.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.GameInfo.ViewModels
{
    /// <summary>
    /// 게임 시간 및 꿈틀 진행도 ViewModel
    /// R3 + MessagePipe 기반의 반응형 ViewModel
    /// </summary>
    public class GameInfoViewModel : IDisposable
    {
        #region Observable Properties

        // 시간 관련
        public readonly ReadOnlyReactiveProperty<TimeSpan> CurrentTime;
        public readonly ReadOnlyReactiveProperty<string> StatusMessage;
        public readonly ReadOnlyReactiveProperty<bool> IsTimeWarning;
        public readonly ReadOnlyReactiveProperty<string> TimeString;
        public readonly ReadOnlyReactiveProperty<bool> IsTimeAlmostUp;

        // 꿈틀 진행도 관련
        public readonly ReadOnlyReactiveProperty<int> GgumtleLevel;
        public readonly ReadOnlyReactiveProperty<float> GgumtleProgress;
        public readonly ReadOnlyReactiveProperty<bool> IsGgumtleComplete;

        // 타이머 상태
        public readonly ReadOnlyReactiveProperty<bool> IsTimerRunning;

        #endregion

        #region Dependencies

        private readonly IGameInfoService _gameInfoService;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = false;

        #endregion

        #region Constructor

        [Inject]
        public GameInfoViewModel(
            IGameInfoService gameInfoService,
            ISubscriber<GameInfoChangedMessage> timeChangedSubscriber,
            ISubscriber<GameStatusChangedMessage> statusChangedSubscriber,
            ISubscriber<GgumtleProgressChangedMessage> progressChangedSubscriber,
            ISubscriber<TimeWarningMessage> timeWarningSubscriber
        )
        {
            _gameInfoService = gameInfoService;

            // Service의 Observable 속성들을 직접 연결
            CurrentTime = _gameInfoService.CurrentTime;
            StatusMessage = _gameInfoService.StatusMessage;
            IsTimeWarning = _gameInfoService.IsTimeWarning;
            GgumtleLevel = _gameInfoService.GgumtleLevel;
            GgumtleProgress = _gameInfoService.GgumtleProgress;
            IsGgumtleComplete = _gameInfoService.IsGgumtleComplete;

            // 계산된 속성들
            TimeString = CurrentTime
                .Select(time => $"{time.Minutes:D2}:{time.Seconds:D2}")
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            IsTimeAlmostUp = CurrentTime
                .Select(time => time.TotalMinutes <= 1)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            IsTimerRunning = Observable.Interval(TimeSpan.FromSeconds(0.5))
                .Select(_ => _gameInfoService.IsTimerRunning)
                .ToReadOnlyReactiveProperty()
                .AddTo(_disposables);

            // 메시지 구독
            timeChangedSubscriber.Subscribe(OnTimeChanged).AddTo(_disposables);
            statusChangedSubscriber.Subscribe(OnStatusChanged).AddTo(_disposables);
            progressChangedSubscriber.Subscribe(OnProgressChanged).AddTo(_disposables);
            timeWarningSubscriber.Subscribe(OnTimeWarning).AddTo(_disposables);

            DebugLog("초기화 완료");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 게임 시간 설정
        /// </summary>
        public void SetCurrentTime(TimeSpan time)
        {
            _gameInfoService.SetCurrentTime(time);
        }

        /// <summary>
        /// 상태 메시지 설정
        /// </summary>
        public void SetStatusMessage(string message)
        {
            _gameInfoService.SetStatusMessage(message);
        }

        /// <summary>
        /// 꿈틀 진행도 설정
        /// </summary>
        public void SetGgumtleProgress(int level, float progress)
        {
            _gameInfoService.SetGgumtleProgress(level, progress);
        }

        /// <summary>
        /// 꿈틀 레벨 증가
        /// </summary>
        public bool IncrementGgumtleLevel()
        {
            return _gameInfoService.IncrementGgumtleLevel();
        }

        /// <summary>
        /// 타이머 시작
        /// </summary>
        public void StartTimer()
        {
            _gameInfoService.StartTimer();
        }

        /// <summary>
        /// 타이머 정지
        /// </summary>
        public void StopTimer()
        {
            _gameInfoService.StopTimer();
        }

        /// <summary>
        /// 타이머 토글
        /// </summary>
        public void ToggleTimer()
        {
            _gameInfoService.ToggleTimer();
        }

        /// <summary>
        /// 모든 상태 초기화
        /// </summary>
        public void Reset()
        {
            _gameInfoService.Reset();
        }

        #endregion

        #region Message Handlers

        private void OnTimeChanged(GameInfoChangedMessage message)
        {
            DebugLog($"시간 변경: {message.currentTime.Minutes:D2}:{message.currentTime.Seconds:D2}, 경고: {message.isTimeWarning}");
        }

        private void OnStatusChanged(GameStatusChangedMessage message)
        {
            DebugLog($"상태 메시지 변경: {message.statusMessage}");
        }

        private void OnProgressChanged(GgumtleProgressChangedMessage message)
        {
            DebugLog($"꿈틀 진행도 변경: {message.level}단계, {message.progress:P0}, 완료: {message.isComplete}");
        }

        private void OnTimeWarning(TimeWarningMessage message)
        {
            DebugLog($"시간 경고: {message.minutesRemaining}분 남음, 긴급: {message.isUrgent}");
        }

        #endregion

        #region Private Methods

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[GameInfoViewModel] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            DebugLog("Dispose 완료");
        }

        #endregion
    }
}