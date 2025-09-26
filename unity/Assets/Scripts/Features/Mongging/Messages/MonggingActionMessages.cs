using Features.Mongging.Models;
using UnityEngine;

namespace Features.Mongging.Messages
{
    /// <summary>
    /// 몽깅이 아이템 사용 메시지 (구조만 - Inventory 연동 대기)
    /// </summary>
    public readonly struct MonggingItemUseMessage
    {
        public readonly long PlayerId;
        public readonly string ItemType; // "TaserGun", "FlashBang", "SelfDefib"
        public readonly long TargetId; // 대상이 있는 경우
        public readonly Vector3 UsePosition;

        public MonggingItemUseMessage(
            long playerId,
            string itemType,
            long targetId = -1,
            Vector3 usePosition = default)
        {
            PlayerId = playerId;
            ItemType = itemType;
            TargetId = targetId;
            UsePosition = usePosition;
        }
    }

    /// <summary>
    /// 몽깅이 부활 액션 메시지 (구조만 - 추후 구현)
    /// </summary>
    public readonly struct MonggingRevivalActionMessage
    {
        public readonly long RevivingPlayerId; // 부활을 시도하는 플레이어
        public readonly long TargetPlayerId;   // 부활 대상 플레이어
        public readonly RevivalActionType ActionType;
        public readonly float Progress; // 부활 진행률 (0.0 ~ 1.0)

        public MonggingRevivalActionMessage(
            long revivingPlayerId,
            long targetPlayerId,
            RevivalActionType actionType,
            float progress = 0f)
        {
            RevivingPlayerId = revivingPlayerId;
            TargetPlayerId = targetPlayerId;
            ActionType = actionType;
            Progress = progress;
        }
    }

    /// <summary>
    /// 부활 액션 타입
    /// </summary>
    public enum RevivalActionType
    {
        Start,    // 부활 시작
        Progress, // 부활 진행 중
        Complete, // 부활 완료
        Cancel    // 부활 취소
    }

    /// <summary>
    /// 몽깅이 상호작용 메시지 (구조만)
    /// </summary>
    public readonly struct MonggingInteractionMessage
    {
        public readonly long PlayerId;
        public readonly string InteractionType; // "Revival", "ItemUse", etc.
        public readonly long TargetId;
        public readonly Vector3 InteractionPosition;
        public readonly bool IsStarted;

        public MonggingInteractionMessage(
            long playerId,
            string interactionType,
            long targetId,
            Vector3 interactionPosition,
            bool isStarted)
        {
            PlayerId = playerId;
            InteractionType = interactionType;
            TargetId = targetId;
            InteractionPosition = interactionPosition;
            IsStarted = isStarted;
        }
    }
}