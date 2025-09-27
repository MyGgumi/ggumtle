using UnityEngine;

namespace Features.Revival.Messages
{
    /// <summary>
    /// 기절한 몽깅이가 플레이어 범위에 들어왔을 때 발행되는 메시지
    /// </summary>
    public readonly struct FaintedMonggingDetectedMessage
    {
        public readonly long PlayerId;
        public readonly Transform Transform;
        public readonly float Distance;
        public readonly string PlayerName;

        public FaintedMonggingDetectedMessage(
            long playerId,
            Transform transform,
            float distance,
            string playerName
        )
        {
            PlayerId = playerId;
            Transform = transform;
            Distance = distance;
            PlayerName = playerName;
        }
    }

    /// <summary>
    /// 기절한 몽깅이가 플레이어 범위에서 벗어났을 때 발행되는 메시지
    /// </summary>
    public readonly struct FaintedMonggingLeftMessage
    {
        public readonly long PlayerId;

        public FaintedMonggingLeftMessage(long playerId)
        {
            PlayerId = playerId;
        }
    }
}