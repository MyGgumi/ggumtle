using Features.Mongging.Models;
using UnityEngine;

namespace Features.Mongging.Messages
{
    /// <summary>
    /// 몽깅이 플레이어 상태 변경 메시지
    /// </summary>
    public readonly struct MonggingPlayerStateChangedMessage
    {
        public readonly long PlayerId;
        public readonly MonggingPlayerState PreviousState;
        public readonly MonggingPlayerState NewState;
        public readonly int CurrentHp;
        public readonly int FaintCount;

        public MonggingPlayerStateChangedMessage(
            long playerId,
            MonggingPlayerState previousState,
            MonggingPlayerState newState,
            int currentHp,
            int faintCount)
        {
            PlayerId = playerId;
            PreviousState = previousState;
            NewState = newState;
            CurrentHp = currentHp;
            FaintCount = faintCount;
        }
    }

    /// <summary>
    /// 몽깅이 플레이어 피격 메시지
    /// </summary>
    public readonly struct MonggingPlayerHitMessage
    {
        public readonly long PlayerId;
        public readonly int Damage;
        public readonly int PreviousHp;
        public readonly int CurrentHp;
        public readonly MonggingPlayerState NewState;
        public readonly Vector3 HitPosition;

        public MonggingPlayerHitMessage(
            long playerId,
            int damage,
            int previousHp,
            int currentHp,
            MonggingPlayerState newState,
            Vector3 hitPosition)
        {
            PlayerId = playerId;
            Damage = damage;
            PreviousHp = previousHp;
            CurrentHp = currentHp;
            NewState = newState;
            HitPosition = hitPosition;
        }
    }

    /// <summary>
    /// 몽깅이 플레이어 회복 메시지
    /// </summary>
    public readonly struct MonggingPlayerHealMessage
    {
        public readonly long PlayerId;
        public readonly int HealAmount;
        public readonly int PreviousHp;
        public readonly int CurrentHp;

        public MonggingPlayerHealMessage(
            long playerId,
            int healAmount,
            int previousHp,
            int currentHp)
        {
            PlayerId = playerId;
            HealAmount = healAmount;
            PreviousHp = previousHp;
            CurrentHp = currentHp;
        }
    }

    /// <summary>
    /// 몽깅이 플레이어 상태이상 적용 메시지
    /// </summary>
    public readonly struct MonggingPlayerStatusEffectMessage
    {
        public readonly long PlayerId;
        public readonly MonggingPlayerState StatusEffect;
        public readonly float Duration;
        public readonly bool IsApplied; // true: 적용, false: 해제

        public MonggingPlayerStatusEffectMessage(
            long playerId,
            MonggingPlayerState statusEffect,
            float duration,
            bool isApplied)
        {
            PlayerId = playerId;
            StatusEffect = statusEffect;
            Duration = duration;
            IsApplied = isApplied;
        }
    }

    /// <summary>
    /// 몽깅이 플레이어 부활 메시지
    /// </summary>
    public readonly struct MonggingPlayerRevivedMessage
    {
        public readonly long RevivedPlayerId;
        public readonly long RevivingPlayerId; // 부활시킨 플레이어 ID
        public readonly int ReviveHp;

        public MonggingPlayerRevivedMessage(
            long revivedPlayerId,
            long revivingPlayerId,
            int reviveHp)
        {
            RevivedPlayerId = revivedPlayerId;
            RevivingPlayerId = revivingPlayerId;
            ReviveHp = reviveHp;
        }
    }

    /// <summary>
    /// 몽깅이 플레이어 탈출 메시지
    /// </summary>
    public readonly struct MonggingPlayerEscapedMessage
    {
        public readonly long PlayerId;
        public readonly Vector3 EscapePosition;

        public MonggingPlayerEscapedMessage(long playerId, Vector3 escapePosition)
        {
            PlayerId = playerId;
            EscapePosition = escapePosition;
        }
    }

    /// <summary>
    /// 몽깅이 플레이어 애니메이션 트리거 메시지
    /// </summary>
    public readonly struct MonggingPlayerAnimationMessage
    {
        public readonly long PlayerId;
        public readonly string AnimationTrigger;
        public readonly float Duration;

        public MonggingPlayerAnimationMessage(
            long playerId,
            string animationTrigger,
            float duration = 0f)
        {
            PlayerId = playerId;
            AnimationTrigger = animationTrigger;
            Duration = duration;
        }
    }

    /// <summary>
    /// 서버에서 오는 몽깅이 플레이어 상태 메시지 (상태만 포함)
    /// </summary>
    public readonly struct MonggingPlayerServerStateMessage
    {
        public readonly long PlayerId;
        public readonly MonggingPlayerState NewState;

        public MonggingPlayerServerStateMessage(long playerId, MonggingPlayerState newState)
        {
            PlayerId = playerId;
            NewState = newState;
        }
    }
}