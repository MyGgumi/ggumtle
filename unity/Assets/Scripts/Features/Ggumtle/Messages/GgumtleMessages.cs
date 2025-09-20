using Features.Ggumtle.Models;
using UnityEngine;

namespace Features.Ggumtle.Messages
{
    /// <summary>
    /// 꿈틀이가 플레이어 범위에 들어왔을 때 발행되는 메시지
    /// </summary>
    public readonly struct GgumtleDetectedMessage
    {
        public readonly string GgumtleId;
        public readonly Transform Transform;
        public readonly float Distance;
        public readonly GgumtleState CurrentState;

        public GgumtleDetectedMessage(
            string ggumtleId,
            Transform transform,
            float distance,
            GgumtleState currentState
        )
        {
            GgumtleId = ggumtleId;
            Transform = transform;
            Distance = distance;
            CurrentState = currentState;
        }
    }

    /// <summary>
    /// 꿈틀이가 플레이어 범위에서 벗어났을 때 발행되는 메시지
    /// </summary>
    public readonly struct GgumtleLeftMessage
    {
        public readonly string GgumtleId;

        public GgumtleLeftMessage(string ggumtleId)
        {
            GgumtleId = ggumtleId;
        }
    }

    /// <summary>
    /// 꿈틀이 상태가 변경되었을 때 발행되는 메시지
    /// </summary>
    public readonly struct GgumtleStateChangedMessage
    {
        public readonly string GgumtleId;
        public readonly GgumtleState PreviousState;
        public readonly GgumtleState NewState;

        public GgumtleStateChangedMessage(
            string ggumtleId,
            GgumtleState previousState,
            GgumtleState newState
        )
        {
            GgumtleId = ggumtleId;
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 꿈틀이 정화가 완료되었을 때 발행되는 메시지
    /// </summary>
    public readonly struct GgumtlePurifiedMessage
    {
        public readonly string GgumtleId;
        public readonly Vector3 Position;

        public GgumtlePurifiedMessage(string ggumtleId, Vector3 position)
        {
            GgumtleId = ggumtleId;
            Position = position;
        }
    }

    /// <summary>
    /// 홀드 진행률 업데이트 메시지
    /// </summary>
    public readonly struct GgumtleHoldProgressMessage
    {
        public readonly string GgumtleId;
        public readonly float Progress; // 0.0 ~ 1.0
        public readonly GgumtleState CurrentState;

        public GgumtleHoldProgressMessage(
            string ggumtleId,
            float progress,
            GgumtleState currentState
        )
        {
            GgumtleId = ggumtleId;
            Progress = progress;
            CurrentState = currentState;
        }
    }

    /// <summary>
    /// 먹이 추가 메시지
    /// </summary>
    public readonly struct GgumtleFoodAddedMessage
    {
        public readonly string GgumtleId;
        public readonly int CurrentAmount;
        public readonly int MaxAmount;

        public GgumtleFoodAddedMessage(string ggumtleId, int currentAmount, int maxAmount)
        {
            GgumtleId = ggumtleId;
            CurrentAmount = currentAmount;
            MaxAmount = maxAmount;
        }
    }

    /// <summary>
    /// 범용 알림 메시지
    /// </summary>
    public readonly struct NotificationMessage
    {
        public readonly string Text;
        public readonly float Duration;
        public readonly NotificationType Type;

        public NotificationMessage(
            string text,
            float duration = 2f,
            NotificationType type = NotificationType.Info
        )
        {
            Text = text;
            Duration = duration;
            Type = type;
        }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success,
    }

    // ===== 네트워크 이벤트 메시지들 =====

    /// <summary>
    /// 꿈틀이 파기 완료 네트워크 이벤트 메시지
    /// </summary>
    public readonly struct GgumtleDiggingDoneMessage
    {
        public readonly int GgumtleId;
        public readonly bool IsRealGgumtle;

        public GgumtleDiggingDoneMessage(int ggumtleId, bool isRealGgumtle)
        {
            GgumtleId = ggumtleId;
            IsRealGgumtle = isRealGgumtle;
        }
    }

    /// <summary>
    /// 젤리 강제 종료 네트워크 이벤트 메시지
    /// </summary>
    public readonly struct GgumtleJellyForceQuitMessage
    {
        public readonly int GgumtleId;
        public readonly int LeftJellyCount;

        public GgumtleJellyForceQuitMessage(int ggumtleId, int leftJellyCount)
        {
            GgumtleId = ggumtleId;
            LeftJellyCount = leftJellyCount;
        }
    }

    /// <summary>
    /// 꿈틀이 스폰 네트워크 이벤트 메시지
    /// </summary>
    public readonly struct GgumtleSpawnMessage
    {
        public readonly int GgumtleId;
        public readonly Vector3 Position;

        public GgumtleSpawnMessage(int ggumtleId, Vector3 position)
        {
            GgumtleId = ggumtleId;
            Position = position;
        }
    }

    /// <summary>
    /// 꿈틀이 성불 네트워크 이벤트 메시지
    /// </summary>
    public readonly struct GgumtleNirvanaMessage
    {
        public readonly int GgumtleId;

        public GgumtleNirvanaMessage(int ggumtleId)
        {
            GgumtleId = ggumtleId;
        }
    }
}
