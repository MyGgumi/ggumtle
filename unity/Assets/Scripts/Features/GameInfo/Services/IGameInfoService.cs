using System;
using Features.GameInfo.Models;
using R3;

namespace Features.GameInfo.Services
{
    /// <summary>
    /// 게임 정보 및 꿈틀 진행도 관리 서비스 인터페이스
    /// </summary>
    public interface IGameInfoService
    {
        /// <summary>
        /// 현재 게임 시간 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<TimeSpan> CurrentTime { get; }

        /// <summary>
        /// 상태 메시지 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<string> StatusMessage { get; }

        /// <summary>
        /// 시간 경고 상태 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsTimeWarning { get; }

        /// <summary>
        /// 꿈틀 레벨 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<int> GgumtleLevel { get; }

        /// <summary>
        /// 꿈틀 진행도 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<float> GgumtleProgress { get; }

        /// <summary>
        /// 꿈틀 완료 상태 (Observable)
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsGgumtleComplete { get; }

        /// <summary>
        /// 게임 시간 설정
        /// </summary>
        void SetCurrentTime(TimeSpan time);

        /// <summary>
        /// 상태 메시지 설정
        /// </summary>
        void SetStatusMessage(string message);

        /// <summary>
        /// 꿈틀 진행도 설정
        /// </summary>
        void SetGgumtleProgress(int level, float progress);

        /// <summary>
        /// 꿈틀 레벨 증가
        /// </summary>
        bool IncrementGgumtleLevel();

        /// <summary>
        /// 게임 타이머 시작
        /// </summary>
        void StartTimer();

        /// <summary>
        /// 게임 타이머 정지
        /// </summary>
        void StopTimer();

        /// <summary>
        /// 게임 타이머 일시정지/재개
        /// </summary>
        void ToggleTimer();

        /// <summary>
        /// 타이머 실행 상태
        /// </summary>
        bool IsTimerRunning { get; }

        /// <summary>
        /// 모든 상태 초기화
        /// </summary>
        void Reset();

        /// <summary>
        /// 현재 게임 데이터 가져오기
        /// </summary>
        GameInfoData GetCurrentData();
    }
}