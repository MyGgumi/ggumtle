using System;
using Cysharp.Threading.Tasks;
using Features.GameTime.Messages;
using Features.GameTime.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.GameTime.Services
{
    /// <summary>
    /// 게임 시간 및 꿈틀 진행도 관리 서비스 구현체
    /// </summary>
    public class GameTimeServiceImpl : IGameTimeService, IDisposable
    {
        #region Observable Properties

        public ReadOnlyReactiveProperty<TimeSpan> CurrentTime => _currentTime;
        public ReadOnlyReactiveProperty<string> StatusMessage => _statusMessage;
        public ReadOnlyReactiveProperty<bool> IsTimeWarning => _isTimeWarning;
        public ReadOnlyReactiveProperty<int> GgumtleLevel => _ggumtleLevel;
        public ReadOnlyReactiveProperty<float> GgumtleProgress => _ggumtleProgress;
        public ReadOnlyReactiveProperty<bool> IsGgumtleComplete => _isGgumtleComplete;

        #endregion

        #region Private Fields

        private readonly ReactiveProperty<TimeSpan> _currentTime = new(TimeSpan.Zero);
        private readonly ReactiveProperty<string> _statusMessage = new("");
        private readonly ReactiveProperty<bool> _isTimeWarning = new(false);
        private readonly ReactiveProperty<int> _ggumtleLevel = new(1);
        private readonly ReactiveProperty<float> _ggumtleProgress = new(0f);
        private readonly ReactiveProperty<bool> _isGgumtleComplete = new(false);

        private readonly GameTimeData _gameTimeData = new();
        private readonly CompositeDisposable _disposables = new();

        private bool _isTimerRunning = false;
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Dependencies

        private readonly IPublisher<GameTimeChangedMessage> _timeChangedPublisher;
        private readonly IPublisher<GameStatusChangedMessage> _statusChangedPublisher;
        private readonly IPublisher<GgumtleProgressChangedMessage> _progressChangedPublisher;
        private readonly IPublisher<TimeWarningMessage> _timeWarningPublisher;

        #endregion

        #region Constructor

        [Inject]
        public GameTimeServiceImpl(
            IPublisher<GameTimeChangedMessage> timeChangedPublisher,
            IPublisher<GameStatusChangedMessage> statusChangedPublisher,
            IPublisher<GgumtleProgressChangedMessage> progressChangedPublisher,
            IPublisher<TimeWarningMessage> timeWarningPublisher
        )
        {
            _timeChangedPublisher = timeChangedPublisher;
            _statusChangedPublisher = statusChangedPublisher;
            _progressChangedPublisher = progressChangedPublisher;
            _timeWarningPublisher = timeWarningPublisher;

            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // 꿈틀 완료 상태 자동 계산
            _ggumtleLevel
                .CombineLatest(
                    _ggumtleProgress,
                    (level, progress) => level >= _gameTimeData.maxGgumtleLevel && progress >= 1.0f
                )
                .Subscribe(isComplete => _isGgumtleComplete.Value = isComplete)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[GameTimeServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void SetCurrentTime(TimeSpan time)
        {
            _gameTimeData.UpdateTime(time);
            _currentTime.Value = _gameTimeData.currentTime;
            _isTimeWarning.Value = _gameTimeData.isTimeWarning;

            _timeChangedPublisher.Publish(
                new GameTimeChangedMessage(time, _gameTimeData.isTimeWarning)
            );

            // 시간 경고 메시지 발송
            if (_gameTimeData.isTimeWarning)
            {
                int minutesRemaining = (int)Math.Ceiling(_gameTimeData.currentTime.TotalMinutes);
                bool isUrgent = minutesRemaining <= 1;
                _timeWarningPublisher.Publish(new TimeWarningMessage(minutesRemaining, isUrgent));
            }

            DebugLog($"시간 설정: {_gameTimeData.TimeString}, 경고: {_gameTimeData.isTimeWarning}");
        }

        public void SetStatusMessage(string message)
        {
            _gameTimeData.statusMessage = message;
            _statusMessage.Value = message;

            _statusChangedPublisher.Publish(new GameStatusChangedMessage(message));

            DebugLog($"상태 메시지 설정: {message}");
        }

        public void SetGgumtleProgress(int level, float progress)
        {
            _gameTimeData.SetGgumtleProgress(level, progress);
            _ggumtleLevel.Value = _gameTimeData.ggumtleLevel;
            _ggumtleProgress.Value = _gameTimeData.ggumtleProgress;

            _progressChangedPublisher.Publish(
                new GgumtleProgressChangedMessage(
                    _gameTimeData.ggumtleLevel,
                    _gameTimeData.ggumtleProgress,
                    _gameTimeData.IsGgumtleComplete
                )
            );

            DebugLog($"꿈틀 진행도 설정: {level}단계, {progress:P0}");
        }

        public bool IncrementGgumtleLevel()
        {
            if (_gameTimeData.IncrementGgumtleLevel())
            {
                _ggumtleLevel.Value = _gameTimeData.ggumtleLevel;
                _ggumtleProgress.Value = _gameTimeData.ggumtleProgress;

                _progressChangedPublisher.Publish(
                    new GgumtleProgressChangedMessage(
                        _gameTimeData.ggumtleLevel,
                        _gameTimeData.ggumtleProgress,
                        _gameTimeData.IsGgumtleComplete
                    )
                );

                DebugLog($"꿈틀 레벨 증가: {_gameTimeData.ggumtleLevel}단계");
                return true;
            }
            return false;
        }

        public void StartTimer()
        {
            if (!_isTimerRunning)
            {
                _isTimerRunning = true;
                StartTimerAsync().Forget();
                DebugLog("타이머 시작");
            }
        }

        public void StopTimer()
        {
            _isTimerRunning = false;
            DebugLog("타이머 정지");
        }

        public void ToggleTimer()
        {
            if (_isTimerRunning)
                StopTimer();
            else
                StartTimer();
        }

        public bool IsTimerRunning => _isTimerRunning;

        public void Reset()
        {
            StopTimer();
            _gameTimeData.Reset();

            _currentTime.Value = _gameTimeData.currentTime;
            _statusMessage.Value = _gameTimeData.statusMessage;
            _isTimeWarning.Value = _gameTimeData.isTimeWarning;
            _ggumtleLevel.Value = _gameTimeData.ggumtleLevel;
            _ggumtleProgress.Value = _gameTimeData.ggumtleProgress;

            DebugLog("게임 시간 데이터 초기화");
        }

        public GameTimeData GetCurrentData()
        {
            return _gameTimeData;
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid StartTimerAsync()
        {
            while (_isTimerRunning && _gameTimeData.currentTime.TotalSeconds > 0)
            {
                await UniTask.Delay(1000);

                if (_isTimerRunning)
                {
                    var newTime = _gameTimeData.currentTime.Subtract(TimeSpan.FromSeconds(1));
                    SetCurrentTime(newTime);

                    if (_gameTimeData.currentTime.TotalSeconds <= 0)
                    {
                        _isTimerRunning = false;
                        DebugLog("타이머 종료 - 시간 만료");
                    }
                }
            }
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[GameTimeServiceImpl] {message}");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            StopTimer();
            _disposables?.Dispose();
            _currentTime?.Dispose();
            _statusMessage?.Dispose();
            _isTimeWarning?.Dispose();
            _ggumtleLevel?.Dispose();
            _ggumtleProgress?.Dispose();
            _isGgumtleComplete?.Dispose();

            DebugLog("Dispose 완료");
        }

        #endregion
    }
}
