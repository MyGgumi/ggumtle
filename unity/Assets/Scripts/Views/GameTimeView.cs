using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MVVM.UI;

namespace Views
{
    /// <summary>
    /// 게임 시간 및 꿈틀 진행도 UI View
    /// GameTimeViewModel과 연결되어 UI를 업데이트
    /// </summary>
    public class GameTimeView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField] private GameTimeViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private Label _timeLabel;
        private Label _statusLabel;

        [Header("Ggumtle Progress UI")]
        private VisualElement _ggumtleArea;
        private VisualElement[] _fillMasks = new VisualElement[3];
        private VisualElement[] _fills = new VisualElement[3];
        private Label _countLabel;

        [Header("Animation Settings")]
        [SerializeField] private float progressAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve progressAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// UniversalHUDController에서 호출하는 초기화 메서드
        /// </summary>
        public void Initialize(VisualElement root, GameTimeViewModel viewModel)
        {
            _root = root;
            this.viewModel = viewModel;

            CacheUIElements();
            SubscribeToViewModel();
            UpdateAllUI();

            Debug.Log("[GameTimeView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _timeLabel = _root.Q<Label>("timeLabel");
            _statusLabel = _root.Q<Label>("statusText");
            _ggumtleArea = _root.Q<VisualElement>("ggumtleArea");
            _countLabel = _root.Q<Label>("count")?.Q<Label>();

            // UXML의 하드코딩된 텍스트들 즉시 제거
            if (_timeLabel != null)
            {
                _timeLabel.text = "";
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = "";
            }

            if (_countLabel != null)
            {
                _countLabel.text = "";
            }

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

            Debug.Log($"[GameTimeView] UI 요소 캐싱 완료: " +
                     $"시간라벨={(_timeLabel != null ? "OK" : "NULL")}, " +
                     $"상태라벨={(_statusLabel != null ? "OK" : "NULL")}, " +
                     $"꿈틀영역={(_ggumtleArea != null ? "OK" : "NULL")}");
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null) return;

            viewModel.TimeChanged += OnTimeChanged;
            viewModel.StatusMessageChanged += OnStatusMessageChanged;
            viewModel.TimeWarningChanged += OnTimeWarningChanged;
            viewModel.GgumtleProgressChanged += OnGgumtleProgressChanged;

            Debug.Log("[GameTimeView] ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (viewModel == null) return;

            viewModel.TimeChanged -= OnTimeChanged;
            viewModel.StatusMessageChanged -= OnStatusMessageChanged;
            viewModel.TimeWarningChanged -= OnTimeWarningChanged;
            viewModel.GgumtleProgressChanged -= OnGgumtleProgressChanged;

            Debug.Log("[GameTimeView] ViewModel 이벤트 구독 해제 완료");
        }

        #region ViewModel Event Handlers

        private void OnTimeChanged(TimeSpan newTime)
        {
            UpdateTimeDisplay(newTime);
        }

        private void OnStatusMessageChanged(string newMessage)
        {
            UpdateStatusDisplay(newMessage);
        }

        private void OnTimeWarningChanged(bool isWarning)
        {
            UpdateTimeWarningStyle(isWarning);
        }

        private void OnGgumtleProgressChanged(int level, float progress)
        {
            UpdateGgumtleProgress(level, progress);
        }

        #endregion

        #region UI Update Methods

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
                if (_fills[i] == null) continue;

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

            // Debug.Log($"[GameTimeView] 꿈틀 진행도 UI 업데이트: {level}단계, {progress:P0}");
        }

        private void AnimateProgressFill(VisualElement fillElement, float targetHeightPercent)
        {
            if (fillElement == null) return;

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

        private void UpdateAllUI()
        {
            if (viewModel == null) return;

            UpdateTimeDisplay(viewModel.CurrentTime);
            UpdateStatusDisplay(viewModel.StatusMessage);
            UpdateTimeWarningStyle(viewModel.IsTimeWarning);
            UpdateGgumtleProgress(viewModel.GgumtleLevel, viewModel.GgumtleProgress);
        }

        #endregion

        #region Public API (기존 GameTimeManager 호환성 유지)

        /// <summary>
        /// 남은 시간 설정 (레거시 호환성)
        /// </summary>
        public void SetTimeRemaining(TimeSpan time)
        {
            if (viewModel != null)
            {
                viewModel.CurrentTime = time;
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
                viewModel.StatusMessage = message;
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

        /// <summary>
        /// 꿈틀 진행도 내부 설정 (이벤트 발생 없음)
        /// </summary>
        public void SetGgumtleProgressInternal(int level, float progress)
        {
            // 직접 UI만 업데이트 (이벤트 시스템 우회)
            UpdateGgumtleProgress(level, progress);
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewModel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// 현재 UI 상태를 로그로 출력 (디버깅용)
        /// </summary>
        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log($"[GameTimeView] UI State:\n" +
                     $"  TimeLabel: {(_timeLabel?.text ?? "NULL")}\n" +
                     $"  StatusLabel: {(_statusLabel?.text ?? "NULL")}\n" +
                     $"  CountLabel: {(_countLabel?.text ?? "NULL")}\n" +
                     $"  ViewModel: {(viewModel != null ? "Connected" : "NULL")}");
        }

        #endregion
    }
}