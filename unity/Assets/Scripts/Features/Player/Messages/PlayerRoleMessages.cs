using Features.PlayerList.Models;
using UnityEngine;

namespace Features.Player.Messages
{
    public readonly struct PlayerRoleChangedMessage
    {
        public readonly PlayerRole previousRole;
        public readonly PlayerRole newRole;
        public readonly bool canInteract;

        public PlayerRoleChangedMessage(PlayerRole previousRole, PlayerRole newRole, bool canInteract)
        {
            this.previousRole = previousRole;
            this.newRole = newRole;
            this.canInteract = canInteract;
        }
    }

    /// <summary>
    /// 플레이어 이동 응답 메시지
    /// </summary>
    public readonly struct PlayerMoveResponseMessage
    {
        public readonly long playerId;
        public readonly Vector3 position;
        public readonly Vector3 direction; // 이동 방향 (rotation에서 direction으로 변경)
        public readonly bool isMoving;
        public readonly float speed;

        public PlayerMoveResponseMessage(long playerId, Vector3 position, Vector3 direction, bool isMoving, float speed)
        {
            this.playerId = playerId;
            this.position = position;
            this.direction = direction;
            this.isMoving = isMoving;
            this.speed = speed;
        }
    }

    /// <summary>
    /// 플레이어 점프 메시지
    /// </summary>
    public readonly struct PlayerJumpMessage
    {
        public readonly long playerId;
        public readonly bool isJumping;

        public PlayerJumpMessage(long playerId, bool isJumping)
        {
            this.playerId = playerId;
            this.isJumping = isJumping;
        }
    }

    /// <summary>
    /// 플레이어 애니메이션 상태 메시지
    /// </summary>
    public readonly struct PlayerAnimationStateMessage
    {
        public readonly long playerId;
        public readonly string animationState;

        public PlayerAnimationStateMessage(long playerId, string animationState)
        {
            this.playerId = playerId;
            this.animationState = animationState;
        }
    }
}