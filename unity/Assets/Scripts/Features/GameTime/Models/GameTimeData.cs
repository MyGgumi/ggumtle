using System;
using UnityEngine;

namespace Features.GameTime.Models
{
    /// <summary>
    /// 게임 시간 및 꿈틀 진행도 데이터 모델
    /// </summary>
    [Serializable]
    public class GameTimeData
    {
        [Header("Time Settings")]
        public TimeSpan currentTime = TimeSpan.Zero;
        public string statusMessage = "";
        public bool isTimeWarning = false;

        [Header("Ggumtle Progress")]
        public int ggumtleLevel = 1;
        public float ggumtleProgress = 0f;
        public int maxGgumtleLevel = 3;

        /// <summary>
        /// 꿈틀이 완전히 완료되었는지 여부
        /// </summary>
        public bool IsGgumtleComplete => ggumtleLevel >= maxGgumtleLevel && ggumtleProgress >= 1.0f;

        /// <summary>
        /// 시간이 거의 다 되었는지 여부 (1분 이하)
        /// </summary>
        public bool IsTimeAlmostUp => currentTime.TotalMinutes <= 1;

        /// <summary>
        /// 시간 문자열 (MM:SS 형식)
        /// </summary>
        public string TimeString => $"{currentTime.Minutes:D2}:{currentTime.Seconds:D2}";

        /// <summary>
        /// 시간 경고 상태 확인 (3분 이하)
        /// </summary>
        public bool ShouldShowTimeWarning => currentTime.TotalMinutes <= 3 && currentTime.TotalMinutes > 0;

        /// <summary>
        /// 꿈틀 진행도 설정
        /// </summary>
        public void SetGgumtleProgress(int level, float progress)
        {
            ggumtleLevel = Mathf.Clamp(level, 1, maxGgumtleLevel);
            ggumtleProgress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// 꿈틀 레벨 증가
        /// </summary>
        public bool IncrementGgumtleLevel()
        {
            if (ggumtleLevel < maxGgumtleLevel)
            {
                ggumtleLevel++;
                ggumtleProgress = 1.0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 모든 상태 리셋
        /// </summary>
        public void Reset()
        {
            currentTime = TimeSpan.Zero;
            statusMessage = "";
            isTimeWarning = false;
            ggumtleLevel = 1;
            ggumtleProgress = 0f;
        }

        /// <summary>
        /// 시간 업데이트 및 경고 상태 확인
        /// </summary>
        public void UpdateTime(TimeSpan newTime)
        {
            currentTime = newTime;
            isTimeWarning = ShouldShowTimeWarning;
        }
    }
}