using System;
using MVVM.Core;
using UnityEngine;

namespace MVVM.UI
{
    /// <summary>
    /// 게임 시간 및 꿈틀 진행도 관련 ViewModel
    /// </summary>
    public class GameTimeViewModel : BaseViewModel
    {
        [Header("Time State")]
        [SerializeField]
        private TimeSpan _currentTime = TimeSpan.Zero;

        [SerializeField]
        private string _statusMessage = "";

        [SerializeField]
        private bool _isTimeWarning = false;

        [Header("Ggumtle Progress State")]
        [SerializeField]
        private int _ggumtleLevel = 1;

        [SerializeField]
        private float _ggumtleProgress = 0f;

        [SerializeField]
        private int _maxGgumtleLevel = 3;

        // 이벤트들
        public event Action<TimeSpan> TimeChanged;
        public event Action<string> StatusMessageChanged;
        public event Action<bool> TimeWarningChanged;
        public event Action<int, float> GgumtleProgressChanged;

        #region Properties

        /// <summary>
        /// 현재 남은 시간
        /// </summary>
        public TimeSpan CurrentTime
        {
            get => _currentTime;
            set
            {
                if (SetProperty(ref _currentTime, value))
                {
                    TimeChanged?.Invoke(value);
                    CheckTimeWarning();
                }
            }
        }

        /// <summary>
        /// 상태 메시지 (역할별 안내 텍스트)
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    StatusMessageChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// 시간 경고 상태 (3분, 1분 남았을 때 등)
        /// </summary>
        public bool IsTimeWarning
        {
            get => _isTimeWarning;
            private set
            {
                if (SetProperty(ref _isTimeWarning, value))
                {
                    TimeWarningChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// 꿈틀 현재 단계 (1~3)
        /// </summary>
        public int GgumtleLevel
        {
            get => _ggumtleLevel;
            set
            {
                var clampedValue = Mathf.Clamp(value, 1, _maxGgumtleLevel);
                if (SetProperty(ref _ggumtleLevel, clampedValue))
                {
                    GgumtleProgressChanged?.Invoke(_ggumtleLevel, _ggumtleProgress);
                }
            }
        }

        /// <summary>
        /// 꿈틀 현재 단계 진행도 (0.0 ~ 1.0)
        /// </summary>
        public float GgumtleProgress
        {
            get => _ggumtleProgress;
            set
            {
                var clampedValue = Mathf.Clamp01(value);
                if (SetProperty(ref _ggumtleProgress, clampedValue))
                {
                    GgumtleProgressChanged?.Invoke(_ggumtleLevel, _ggumtleProgress);
                }
            }
        }

        /// <summary>
        /// 꿈틀 최대 단계
        /// </summary>
        public int MaxGgumtleLevel
        {
            get => _maxGgumtleLevel;
            set => SetProperty(ref _maxGgumtleLevel, value);
        }

        /// <summary>
        /// 꿈틀이 완전히 완료되었는지 여부
        /// </summary>
        public bool IsGgumtleComplete =>
            _ggumtleLevel >= _maxGgumtleLevel && _ggumtleProgress >= 1.0f;

        /// <summary>
        /// 시간이 거의 다 되었는지 여부 (1분 이하)
        /// </summary>
        public bool IsTimeAlmostUp => _currentTime.TotalMinutes <= 1;

        /// <summary>
        /// 시간 문자열 (MM:SS 형식)
        /// </summary>
        public string TimeString => $"{_currentTime.Minutes:D2}:{_currentTime.Seconds:D2}";

        #endregion

        #region Public Methods

        /// <summary>
        /// 꿈틀 진행도를 단계와 함께 설정
        /// </summary>
        public void SetGgumtleProgress(int level, float progress)
        {
            var clampedLevel = Mathf.Clamp(level, 1, _maxGgumtleLevel);
            var clampedProgress = Mathf.Clamp01(progress);

            bool levelChanged = _ggumtleLevel != clampedLevel;
            bool progressChanged = !Mathf.Approximately(_ggumtleProgress, clampedProgress);

            if (levelChanged || progressChanged)
            {
                _ggumtleLevel = clampedLevel;
                _ggumtleProgress = clampedProgress;

                OnPropertiesChanged(nameof(GgumtleLevel), nameof(GgumtleProgress));
                GgumtleProgressChanged?.Invoke(_ggumtleLevel, _ggumtleProgress);

                if (EnableDebugLogs)
                {
                    Debug.Log(
                        $"[GameTimeViewModel] 꿈틀 진행도 업데이트: {_ggumtleLevel}단계, {_ggumtleProgress:P0}"
                    );
                }
            }
        }

        /// <summary>
        /// 꿈틀 레벨을 1 증가
        /// </summary>
        public void IncrementGgumtleLevel()
        {
            if (_ggumtleLevel < _maxGgumtleLevel)
            {
                GgumtleLevel = _ggumtleLevel + 1;
                GgumtleProgress = 1.0f; // 레벨 업 시 해당 단계 완료
            }
        }

        /// <summary>
        /// 모든 상태 리셋
        /// </summary>
        public void ResetAll()
        {
            CurrentTime = TimeSpan.Zero;
            StatusMessage = "";
            GgumtleLevel = 1;
            GgumtleProgress = 0f;
            IsTimeWarning = false;

            if (EnableDebugLogs)
            {
                Debug.Log("[GameTimeViewModel] 모든 상태 리셋");
            }
        }

        #endregion

        #region Time Warning Logic

        private void CheckTimeWarning()
        {
            bool newWarningState = _currentTime.TotalMinutes <= 3 && _currentTime.TotalMinutes > 0;
            IsTimeWarning = newWarningState;
        }

        #endregion

        #region Unity Lifecycle

        protected override void InitializeViewModel()
        {
            base.InitializeViewModel();

            // 초기 상태 설정
            _currentTime = TimeSpan.Zero;
            _statusMessage = "";
            _ggumtleLevel = 1;
            _ggumtleProgress = 0f;
            _maxGgumtleLevel = 3;
            _isTimeWarning = false;

            if (EnableDebugLogs)
            {
                Debug.Log("[GameTimeViewModel] 초기화 완료");
            }
        }

        protected override void CleanupViewModel()
        {
            base.CleanupViewModel();

            // 이벤트 정리
            TimeChanged = null;
            StatusMessageChanged = null;
            TimeWarningChanged = null;
            GgumtleProgressChanged = null;

            if (EnableDebugLogs)
            {
                Debug.Log("[GameTimeViewModel] 정리 완료");
            }
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// 현재 상태를 로그로 출력 (디버깅용)
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log(
                $"[GameTimeViewModel] State:\n"
                    + $"  CurrentTime: {TimeString}\n"
                    + $"  StatusMessage: {StatusMessage}\n"
                    + $"  IsTimeWarning: {IsTimeWarning}\n"
                    + $"  GgumtleLevel: {GgumtleLevel}/{MaxGgumtleLevel}\n"
                    + $"  GgumtleProgress: {GgumtleProgress:P1}\n"
                    + $"  IsGgumtleComplete: {IsGgumtleComplete}\n"
                    + $"  IsTimeAlmostUp: {IsTimeAlmostUp}"
            );
        }

        /// <summary>
        /// 테스트용 시간 설정
        /// </summary>
        [ContextMenu("Set Test Time (3 minutes)")]
        private void SetTestTime()
        {
            CurrentTime = TimeSpan.FromMinutes(3);
        }

        /// <summary>
        /// 테스트용 꿈틀 진행도 설정
        /// </summary>
        [ContextMenu("Set Test Ggumtle (Level 2, 50%)")]
        private void SetTestGgumtle()
        {
            SetGgumtleProgress(2, 0.5f);
        }

        #endregion
    }
}
