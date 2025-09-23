using Features.Chest.Models;
using UnityEngine;

namespace Features.Chest.Messages
{
    /// <summary>
    /// 상자가 감지되었을 때 발행되는 메시지
    /// </summary>
    public readonly struct ChestDetectedMessage
    {
        public readonly int ChestId;
        public readonly Transform ChestTransform;
        public readonly float Distance;
        public readonly ChestState State;

        public ChestDetectedMessage(int chestId, Transform chestTransform, float distance, ChestState state)
        {
            ChestId = chestId;
            ChestTransform = chestTransform;
            Distance = distance;
            State = state;
        }
    }
}