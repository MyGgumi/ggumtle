using Features.Mongdung.Models;
using UnityEngine;

namespace Features.Mongdung.Messages
{
    /// <summary>
    /// 몽둥이 액션 시작 메시지
    /// </summary>
    public readonly struct MongdungActionStartedMessage
    {
        public readonly long PlayerId;
        public readonly MongdungActionType ActionType;
        public readonly Vector3 Position;
        public readonly float ExecutionDuration;

        public MongdungActionStartedMessage(
            long playerId,
            MongdungActionType actionType,
            Vector3 position,
            float executionDuration)
        {
            PlayerId = playerId;
            ActionType = actionType;
            Position = position;
            ExecutionDuration = executionDuration;
        }
    }

    /// <summary>
    /// 몽둥이 액션 완료 메시지
    /// </summary>
    public readonly struct MongdungActionCompletedMessage
    {
        public readonly long PlayerId;
        public readonly MongdungActionType ActionType;
        public readonly bool Success;
        public readonly Vector3 Position;

        public MongdungActionCompletedMessage(
            long playerId,
            MongdungActionType actionType,
            bool success,
            Vector3 position)
        {
            PlayerId = playerId;
            ActionType = actionType;
            Success = success;
            Position = position;
        }
    }

    /// <summary>
    /// 몽둥이 액션 쿨다운 메시지
    /// </summary>
    public readonly struct MongdungActionCooldownMessage
    {
        public readonly long PlayerId;
        public readonly MongdungActionType ActionType;
        public readonly float CooldownDuration;
        public readonly float RemainingTime;

        public MongdungActionCooldownMessage(
            long playerId,
            MongdungActionType actionType,
            float cooldownDuration,
            float remainingTime)
        {
            PlayerId = playerId;
            ActionType = actionType;
            CooldownDuration = cooldownDuration;
            RemainingTime = remainingTime;
        }
    }

    /// <summary>
    /// 몽둥이 상태 변경 메시지
    /// </summary>
    public readonly struct MongdungStateChangedMessage
    {
        public readonly long PlayerId;
        public readonly MongdungState PreviousState;
        public readonly MongdungState NewState;

        public MongdungStateChangedMessage(
            long playerId,
            MongdungState previousState,
            MongdungState newState)
        {
            PlayerId = playerId;
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 몽둥이 이동 제한 메시지
    /// </summary>
    public readonly struct MongdungMovementBlockedMessage
    {
        public readonly long PlayerId;
        public readonly bool IsBlocked;
        public readonly string Reason;

        public MongdungMovementBlockedMessage(
            long playerId,
            bool isBlocked,
            string reason = "")
        {
            PlayerId = playerId;
            IsBlocked = isBlocked;
            Reason = reason;
        }
    }

    // ===== 네트워크 메시지들 =====

    /// <summary>
    /// 몽둥이 액션 네트워크 요청 메시지
    /// </summary>
    public readonly struct MongdungActionNetworkRequestMessage
    {
        public readonly long PlayerId;
        public readonly MongdungActionType ActionType;
        public readonly Vector3 Position;
        public readonly Vector3 Direction;

        public MongdungActionNetworkRequestMessage(
            long playerId,
            MongdungActionType actionType,
            Vector3 position,
            Vector3 direction)
        {
            PlayerId = playerId;
            ActionType = actionType;
            Position = position;
            Direction = direction;
        }
    }

    /// <summary>
    /// 몽둥이 액션 네트워크 응답 메시지
    /// </summary>
    public readonly struct MongdungActionNetworkResponseMessage
    {
        public readonly long PlayerId;
        public readonly MongdungActionType ActionType;
        public readonly bool Success;
        public readonly string ErrorMessage;

        public MongdungActionNetworkResponseMessage(
            long playerId,
            MongdungActionType actionType,
            bool success,
            string errorMessage = "")
        {
            PlayerId = playerId;
            ActionType = actionType;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 몽둥이 액션 브로드캐스트 메시지 (서버→모든 클라이언트)
    /// </summary>
    public readonly struct MongdungActionBroadcastMessage
    {
        public readonly long PlayerId;
        public readonly MongdungActionType ActionType;
        public readonly int StatusCode; // 0=started, 1=completed, 2=failed
        public readonly Vector3 Position;
        public readonly Vector3 Direction;

        public MongdungActionBroadcastMessage(
            long playerId,
            MongdungActionType actionType,
            int statusCode,
            Vector3 position,
            Vector3 direction)
        {
            PlayerId = playerId;
            ActionType = actionType;
            StatusCode = statusCode;
            Position = position;
            Direction = direction;
        }
    }
}