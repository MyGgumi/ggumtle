using System;
using System.Collections.Generic;
using Features.GameInfo.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using R3DisposableBag = R3.CompositeDisposable;

namespace Features.GameInfo.Views
{
    /// <summary>
    /// 게임 시간 및 꿈틀 진행도 UI를 담당하는 View (UI Toolkit 기반)
    /// GgumtleUIView 패턴을 따라 VisualElement + R3로 구현
    /// </summary>
    public class GameInfoUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private GameInfoViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private Label _timeLabel;
        private Label _statusLabel;

        [Header("Ggumtle Progress UI")]
        private VisualElement _ggumtleArea;
        private VisualElement[] _fillMasks = new VisualElement[3];
        private VisualElement[] _fills = new VisualElement[3];
        private Label _countLabel;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        [SerializeField]
        private float progressAnimationDuration = 0.3f;

        [SerializeField]
        private AnimationCurve progressAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(GameInfoViewModel gameTimeViewModel)
        {
            viewModel = gameTimeViewModel;
            if (enableDebugLogs)
                Debug.Log($"[GameInfoUIView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError("[GameInfoUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            InitializeUI();

            if (enableDebugLogs)
                Debug.Log("[GameInfoUIView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[GameInfoUIView] Root VisualElement가 null입니다.");
                return;
            }

            // 시간 및 상태 UI 요소들 캐싱 (GameInfo.uxml 구조에 맞춤)
            _timeLabel = _root.Q<Label>("timeLabel");
            _statusLabel = _root.Q<Label>("statusText");
            _ggumtleArea = _root.Q<VisualElement>("ggumtleArea");
            _countLabel = _root.Q<Label>("count")?.Q<Label>();

            // UXML의 하드코딩된 텍스트들 즉시 제거
            if (_timeLabel != null)
                _timeLabel.text = "";

            if (_statusLabel != null)
                _statusLabel.text = "";

            if (_countLabel != null)
                _countLabel.text = "";

            // 꿈틀 진행도 마스크들 캐싱
            for (int i = 0; i < 3; i++)
            {
                _fillMasks[i] = _root.Q<VisualElement>($"fillMask_{i + 1}of3");
                _fills[i] = _root.Q<VisualElement>($"fill_{i + 1}of3");

                // 모든 fill 요소의 UXML 기본 높이를 0%로 초기화
                if (_fills[i] != null)
                {
                    _fills[i].style.height = new Length(0, LengthUnit.Percent);
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[GameInfoUIView] UI 요소 캐싱 완료: "
                        + $"시간라벨={(_timeLabel != null ? "OK" : "NULL")}, "
                        + $"상태라벨={(_statusLabel != null ? "OK" : "NULL")}, "
                        + $"꿈틀영역={(_ggumtleArea != null ? "OK" : "NULL")}"
                );
            }
        }

        private void InitializeUI()
        {
            // 초기 UI 상태 설정
            UpdateTimeDisplay(TimeSpan.Zero);
            UpdateStatusDisplay("");
            UpdateTimeWarningStyle(false);
            UpdateGgumtleProgress(1, 0f);

            if (enableDebugLogs)
                Debug.Log("[GameInfoUIView] UI 초기화 완료");
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
                return;

            // R3 Observable 구독
            viewModel.TimeString
                .Subscribe(timeString => UpdateTimeDisplay(timeString))
                .AddTo(_disposables);

            viewModel.StatusMessage
                .Subscribe(status => UpdateStatusDisplay(status))
                .AddTo(_disposables);

            viewModel.IsTimeWarning
                .Subscribe(isWarning => UpdateTimeWarningStyle(isWarning))
                .AddTo(_disposables);

            // 꿈틀 진행도 구독 (레벨과 진행도 조합)
            viewModel.GgumtleLevel.CombineLatest(viewModel.GgumtleProgress, (level, progress) => new { level, progress })
                .Subscribe(data => UpdateGgumtleProgress(data.level, data.progress))
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[GameInfoUIView] ViewModel 구독 완료");
        }

        #region UI Update Methods

        private void UpdateTimeDisplay(string timeString)
        {
            if (_timeLabel != null)
            {
                _timeLabel.text = timeString;
            }
        }

        private void UpdateTimeDisplay(TimeSpan time)
        {
            if (_timeLabel != null)
            {
                _timeLabel.text = $"{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }

        private void UpdateStatusDisplay(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = message;
            }
        }

        private void UpdateTimeWarningStyle(bool isWarning)
        {
            if (_timeLabel != null)
            {
                if (isWarning)
                {
                    _timeLabel.AddToClassList("time-warning");
                    _timeLabel.style.color = new StyleColor(Color.red);
                }
                else
                {
                    _timeLabel.RemoveFromClassList("time-warning");
                    _timeLabel.style.color = new StyleColor(Color.white);
                }
            }
        }

        private void UpdateGgumtleProgress(int level, float progress)
        {
            // 카운트 라벨 업데이트
            if (_countLabel != null)
            {
                _countLabel.text = level.ToString();
            }

            // 각 단계별 fill 요소 업데이트
            for (int i = 0; i < _fills.Length; i++)
            {
                if (_fills[i] == null)
                    continue;

                float targetHeight = 0f;

                if (i < level - 1)
                {
                    // 이미 완료된 단계들은 100%
                    targetHeight = 100f;
                }
                else if (i == level - 1)
                {
                    // 현재 진행 중인 단계
                    targetHeight = progress * 100f;
                }
                // else: 아직 시작하지 않은 단계들은 0% (기본값)

                // 애니메이션으로 높이 변경
                AnimateProgressFill(_fills[i], targetHeight);
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[GameInfoUIView] 꿈틀 진행도 UI 업데이트: {level}단계, {progress:P0}");
            }
        }

        private void AnimateProgressFill(VisualElement fillElement, float targetHeightPercent)
        {
            if (fillElement == null)
                return;

            // 현재 높이 값 가져오기
            var currentHeight = fillElement.resolvedStyle.height;
            var currentPercent = currentHeight / fillElement.parent.resolvedStyle.height * 100f;

            // 애니메이션이 필요 없으면 즉시 설정
            if (Mathf.Approximately(currentPercent, targetHeightPercent))
            {
                fillElement.style.height = new Length(targetHeightPercent, LengthUnit.Percent);
                return;
            }

            // 간단한 애니메이션 (Unity UI Toolkit의 제한된 애니메이션 기능 사용)
            fillElement.style.height = new Length(targetHeightPercent, LengthUnit.Percent);

            // 부드러운 전환을 위한 transition 설정 (CSS 스타일에서 정의 가능)
            var timeValues = new List<TimeValue> { new TimeValue(progressAnimationDuration) };
            var propertyNames = new List<StylePropertyName> { new StylePropertyName("height") };
            fillElement.style.transitionDuration = new StyleList<TimeValue>(timeValues);
            fillElement.style.transitionProperty = new StyleList<StylePropertyName>(propertyNames);
        }

        #endregion

        #region Public API (레거시 호환성)

        /// <summary>
        /// 남은 시간 설정 (레거시 호환성)
        /// </summary>
        public void SetTimeRemaining(TimeSpan time)
        {
            if (viewModel != null)
            {
                viewModel.SetCurrentTime(time);
            }
            else
            {
                // ViewModel이 없으면 직접 UI 업데이트
                UpdateTimeDisplay(time);
            }
        }

        /// <summary>
        /// 상태 메시지 설정 (레거시 호환성)
        /// </summary>
        public void SetStatusMessage(string message)
        {
            if (viewModel != null)
            {
                viewModel.SetStatusMessage(message);
            }
            else
            {
                // ViewModel이 없으면 직접 UI 업데이트
                UpdateStatusDisplay(message);
            }
        }

        /// <summary>
        /// 꿈틀 진행도 설정 (레거시 호환성)
        /// </summary>
        public void SetGgumtleProgress(int level, float progress)
        {
            if (viewModel != null)
            {
                viewModel.SetGgumtleProgress(level, progress);
            }
            else
            {
                // ViewModel이 없으면 직접 UI 업데이트
                UpdateGgumtleProgress(level, progress);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            _disposables?.Dispose();
            if (enableDebugLogs)
                Debug.Log("[GameInfoUIView] OnDestroy - Dispose 완료");
        }

        #endregion
    }
}