using Features.Mongdung.Models;
using UnityEngine;

namespace Features.Mongdung.Messages
{
    /// <summary>
    /// 몽둥이 액션 실행 요청 메시지
    /// 입력 컨트롤러나 UI에서 액션 실행을 요청할 때 사용
    /// </summary>
    public readonly struct MongdungActionRequestMessage
    {
        public readonly long PlayerId;
        public readonly MongdungActionType ActionType;
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly long TargetId;
        public readonly string RequestSource; // "DebugInput", "UIButton" 등 요청 소스

        public MongdungActionRequestMessage(
            long playerId,
            MongdungActionType actionType,
            Vector3 position,
            Vector3 direction,
            long targetId = -1,
            string requestSource = "Unknown")
        {
            PlayerId = playerId;
            ActionType = actionType;
            Position = position;
            Direction = direction;
            TargetId = targetId;
            RequestSource = requestSource;
        }
    }
}