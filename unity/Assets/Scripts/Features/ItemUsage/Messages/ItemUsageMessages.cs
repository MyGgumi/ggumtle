using UnityEngine;

namespace Features.ItemUsage.Messages
{
    /// <summary>
    /// 아이템 사용 요청 메시지
    /// </summary>
    public readonly struct ItemUsageRequestMessage
    {
        public readonly int itemId;
        public readonly Vector3 direction;
        public readonly long targetId; // 테이저건용, 다른 아이템은 -1

        public ItemUsageRequestMessage(int itemId, Vector3 direction, long targetId = -1)
        {
            this.itemId = itemId;
            this.direction = direction;
            this.targetId = targetId;
        }
    }

    /// <summary>
    /// 아이템 사용 결과 메시지
    /// </summary>
    public readonly struct ItemUsageResultMessage
    {
        public readonly int itemId;
        public readonly bool success;
        public readonly string resultMessage;

        public ItemUsageResultMessage(int itemId, bool success, string resultMessage = "")
        {
            this.itemId = itemId;
            this.success = success;
            this.resultMessage = resultMessage;
        }
    }

    /// <summary>
    /// 자가제세동기 사용 메시지 (Revival Feature로 전달용)
    /// </summary>
    public readonly struct SelfDefibrillatorUsedMessage
    {
        public readonly long playerId;
        public readonly bool success;
        public readonly int reviveHp; // 부활 시 체력 값

        public SelfDefibrillatorUsedMessage(long playerId, bool success, int reviveHp = -1)
        {
            this.playerId = playerId;
            this.success = success;
            this.reviveHp = reviveHp;
        }
    }

    /// <summary>
    /// 테이저건 사용 메시지
    /// </summary>
    public readonly struct TaserGunUsedMessage
    {
        public readonly long userId;
        public readonly Vector3 effectPosition; // 이펙트 생성 위치
        public readonly bool success;

        public TaserGunUsedMessage(long userId, Vector3 effectPosition, bool success)
        {
            this.userId = userId;
            this.effectPosition = effectPosition;
            this.success = success;
        }
    }

    /// <summary>
    /// 섬광탄 사용 메시지
    /// </summary>
    public readonly struct FlashBangUsedMessage
    {
        public readonly long userId;
        public readonly Vector3 effectPosition; // 이펙트 생성 위치
        public readonly bool success;

        public FlashBangUsedMessage(long userId, Vector3 effectPosition, bool success)
        {
            this.userId = userId;
            this.effectPosition = effectPosition;
            this.success = success;
        }
    }
}