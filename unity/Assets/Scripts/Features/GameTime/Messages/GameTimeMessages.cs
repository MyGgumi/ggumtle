using System;

namespace Features.GameTime.Messages
{
    /// <summary>
    /// 게임 시간 변경 메시지
    /// </summary>
    public readonly struct GameTimeChangedMessage
    {
        public readonly TimeSpan currentTime;
        public readonly bool isTimeWarning;

        public GameTimeChangedMessage(TimeSpan currentTime, bool isTimeWarning)
        {
            this.currentTime = currentTime;
            this.isTimeWarning = isTimeWarning;
        }
    }

    /// <summary>
    /// 상태 메시지 변경 메시지
    /// </summary>
    public readonly struct GameStatusChangedMessage
    {
        public readonly string statusMessage;

        public GameStatusChangedMessage(string statusMessage)
        {
            this.statusMessage = statusMessage;
        }
    }

    /// <summary>
    /// 꿈틀 진행도 변경 메시지
    /// </summary>
    public readonly struct GgumtleProgressChangedMessage
    {
        public readonly int level;
        public readonly float progress;
        public readonly bool isComplete;

        public GgumtleProgressChangedMessage(int level, float progress, bool isComplete)
        {
            this.level = level;
            this.progress = progress;
            this.isComplete = isComplete;
        }
    }

    /// <summary>
    /// 시간 경고 메시지
    /// </summary>
    public readonly struct TimeWarningMessage
    {
        public readonly int minutesRemaining;
        public readonly bool isUrgent;

        public TimeWarningMessage(int minutesRemaining, bool isUrgent)
        {
            this.minutesRemaining = minutesRemaining;
            this.isUrgent = isUrgent;
        }
    }

    /// <summary>
    /// 게임 시간 초기화 메시지
    /// </summary>
    public readonly struct GameTimeResetMessage
    {
        public readonly TimeSpan initialTime;

        public GameTimeResetMessage(TimeSpan initialTime)
        {
            this.initialTime = initialTime;
        }
    }

    /// <summary>
    /// 게임 시간 시작 메시지
    /// </summary>
    public readonly struct GameTimeStartedMessage
    {
        public readonly TimeSpan startTime;
        public readonly string startReason;

        public GameTimeStartedMessage(TimeSpan startTime, string startReason = "")
        {
            this.startTime = startTime;
            this.startReason = startReason;
        }
    }

    /// <summary>
    /// 게임 시간 정지 메시지
    /// </summary>
    public readonly struct GameTimeStoppedMessage
    {
        public readonly TimeSpan stoppedTime;
        public readonly string stopReason;

        public GameTimeStoppedMessage(TimeSpan stoppedTime, string stopReason = "")
        {
            this.stoppedTime = stoppedTime;
            this.stopReason = stopReason;
        }
    }

    /// <summary>
    /// 게임 시간 경고 메시지 (MainLifetimeScope 호환)
    /// </summary>
    public readonly struct GameTimeWarningMessage
    {
        public readonly TimeSpan remainingTime;
        public readonly int warningLevel;
        public readonly string warningMessage;

        public GameTimeWarningMessage(TimeSpan remainingTime, int warningLevel, string warningMessage)
        {
            this.remainingTime = remainingTime;
            this.warningLevel = warningLevel;
            this.warningMessage = warningMessage;
        }
    }

    /// <summary>
    /// 게임 시간 만료 메시지
    /// </summary>
    public readonly struct GameTimeExpiredMessage
    {
        public readonly TimeSpan expiredTime;
        public readonly string expiredReason;

        public GameTimeExpiredMessage(TimeSpan expiredTime, string expiredReason = "시간 종료")
        {
            this.expiredTime = expiredTime;
            this.expiredReason = expiredReason;
        }
    }

    /// <summary>
    /// 꿈틀 레벨 변경 메시지
    /// </summary>
    public readonly struct GgumtleLevelChangedMessage
    {
        public readonly int newLevel;
        public readonly int previousLevel;
        public readonly bool isCompleted;

        public GgumtleLevelChangedMessage(int newLevel, int previousLevel, bool isCompleted = false)
        {
            this.newLevel = newLevel;
            this.previousLevel = previousLevel;
            this.isCompleted = isCompleted;
        }
    }

    /// <summary>
    /// 상태 메시지 변경 메시지 (MainLifetimeScope 호환)
    /// </summary>
    public readonly struct StatusMessageChangedMessage
    {
        public readonly string newMessage;
        public readonly string previousMessage;

        public StatusMessageChangedMessage(string newMessage, string previousMessage)
        {
            this.newMessage = newMessage;
            this.previousMessage = previousMessage;
        }
    }
}