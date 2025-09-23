using System;
using Cysharp.Threading.Tasks;
using Features.GameInfo.Messages;
using Features.GameInfo.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.GameInfo.Services
{
    /// <summary>
    /// 게임 정보 및 꿈틀 진행도 관리 서비스 구현체
    /// </summary>
    public class GameInfoServiceImpl : IGameInfoService, IDisposable
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

        private readonly GameInfoData _gameInfoData = new();
        private readonly CompositeDisposable _disposables = new();

        private bool _isTimerRunning = false;
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Dependencies

        private readonly IPublisher<GameInfoChangedMessage> _timeChangedPublisher;
        private readonly IPublisher<GameStatusChangedMessage> _statusChangedPublisher;
        private readonly IPublisher<GgumtleProgressChangedMessage> _progressChangedPublisher;
        private readonly IPublisher<TimeWarningMessage> _timeWarningPublisher;

        #endregion

        #region Constructor

        [Inject]
        public GameInfoServiceImpl(
            IPublisher<GameInfoChangedMessage> timeChangedPublisher,
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
                    (level, progress) => level >= _gameInfoData.maxGgumtleLevel && progress >= 1.0f
                )
                .Subscribe(isComplete => _isGgumtleComplete.Value = isComplete)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[GameInfoServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void SetCurrentTime(TimeSpan time)
        {
            _gameInfoData.UpdateTime(time);
            _currentTime.Value = _gameInfoData.currentTime;
            _isTimeWarning.Value = _gameInfoData.isTimeWarning;

            _timeChangedPublisher.Publish(
                new GameInfoChangedMessage(time, _gameInfoData.isTimeWarning)
            );

            // 시간 경고 메시지 발송
            if (_gameInfoData.isTimeWarning)
            {
                int minutesRemaining = (int)Math.Ceiling(_gameInfoData.currentTime.TotalMinutes);
                bool isUrgent = minutesRemaining <= 1;
                _timeWarningPublisher.Publish(new TimeWarningMessage(minutesRemaining, isUrgent));
            }

            DebugLog($"시간 설정: {_gameInfoData.TimeString}, 경고: {_gameInfoData.isTimeWarning}");
        }

        public void SetStatusMessage(string message)
        {
            _gameInfoData.statusMessage = message;
            _statusMessage.Value = message;

            _statusChangedPublisher.Publish(new GameStatusChangedMessage(message));

            DebugLog($"상태 메시지 설정: {message}");
        }

        public void SetGgumtleProgress(int level, float progress)
        {
            _gameInfoData.SetGgumtleProgress(level, progress);
            _ggumtleLevel.Value = _gameInfoData.ggumtleLevel;
            _ggumtleProgress.Value = _gameInfoData.ggumtleProgress;

            _progressChangedPublisher.Publish(
                new GgumtleProgressChangedMessage(
                    _gameInfoData.ggumtleLevel,
                    _gameInfoData.ggumtleProgress,
                    _gameInfoData.IsGgumtleComplete
                )
            );

            DebugLog($"꿈틀 진행도 설정: {level}단계, {progress:P0}");
        }

        public bool IncrementGgumtleLevel()
        {
            if (_gameInfoData.IncrementGgumtleLevel())
            {
                _ggumtleLevel.Value = _gameInfoData.ggumtleLevel;
                _ggumtleProgress.Value = _gameInfoData.ggumtleProgress;

                _progressChangedPublisher.Publish(
                    new GgumtleProgressChangedMessage(
                        _gameInfoData.ggumtleLevel,
                        _gameInfoData.ggumtleProgress,
                        _gameInfoData.IsGgumtleComplete
                    )
                );

                DebugLog($"꿈틀 레벨 증가: {_gameInfoData.ggumtleLevel}단계");
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
            _gameInfoData.Reset();

            _currentTime.Value = _gameInfoData.currentTime;
            _statusMessage.Value = _gameInfoData.statusMessage;
            _isTimeWarning.Value = _gameInfoData.isTimeWarning;
            _ggumtleLevel.Value = _gameInfoData.ggumtleLevel;
            _ggumtleProgress.Value = _gameInfoData.ggumtleProgress;

            DebugLog("게임 정보 데이터 초기화");
        }

        public GameInfoData GetCurrentData()
        {
            return _gameInfoData;
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid StartTimerAsync()
        {
            while (_isTimerRunning && _gameInfoData.currentTime.TotalSeconds > 0)
            {
                await UniTask.Delay(1000);

                if (_isTimerRunning)
                {
                    var newTime = _gameInfoData.currentTime.Subtract(TimeSpan.FromSeconds(1));
                    SetCurrentTime(newTime);

                    if (_gameInfoData.currentTime.TotalSeconds <= 0)
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
                Debug.Log($"[GameInfoServiceImpl] {message}");
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
