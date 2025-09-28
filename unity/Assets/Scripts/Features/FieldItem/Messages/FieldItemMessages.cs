namespace Features.FieldItem.Messages
{
    /// <summary>
    /// 필드 아이템 사용 요청 메시지 (UI -> Service)
    /// </summary>
    public readonly struct FieldItemUseRequestMessage
    {
        public readonly int fieldItemId;

        public FieldItemUseRequestMessage(int fieldItemId)
        {
            this.fieldItemId = fieldItemId;
        }
    }

    /// <summary>
    /// 필드 아이템 전역 사용 알림 메시지 (모든 사용자에게 브로드캐스트)
    /// 모든 클라이언트에서 해당 아이템 비활성화 처리
    /// 내가 요청한 경우 + 성공 시 아이템 효과 적용
    /// </summary>
    public readonly struct FieldItemGlobalUsedMessage
    {
        public readonly int fieldItemId;
        public readonly bool success;

        public FieldItemGlobalUsedMessage(int fieldItemId, bool success)
        {
            this.fieldItemId = fieldItemId;
            this.success = success;
        }
    }

    /// <summary>
    /// 체력 변경 메시지
    /// </summary>
    public readonly struct HealthChangedMessage
    {
        public readonly long userId;
        public readonly int currentHealth;
        public readonly int healthChange;

        public HealthChangedMessage(long userId, int currentHealth, int healthChange)
        {
            this.userId = userId;
            this.currentHealth = currentHealth;
            this.healthChange = healthChange;
        }
    }

    /// <summary>
    /// 이동 속도 변경 메시지
    /// </summary>
    public readonly struct SpeedChangedMessage
    {
        public readonly long userId;
        public readonly float speedMultiplier;
        public readonly float duration;

        public SpeedChangedMessage(long userId, float speedMultiplier, float duration)
        {
            this.userId = userId;
            this.speedMultiplier = speedMultiplier;
            this.duration = duration;
        }
    }

    /// <summary>
    /// 필드 아이템 감지 메시지 (플레이어가 아이템 근처에 들어왔을 때)
    /// </summary>
    public readonly struct FieldItemDetectedMessage
    {
        public readonly int fieldItemId;
        public readonly Features.FieldItem.Models.FieldItemType itemType;

        public FieldItemDetectedMessage(int fieldItemId, Features.FieldItem.Models.FieldItemType itemType)
        {
            this.fieldItemId = fieldItemId;
            this.itemType = itemType;
        }
    }

    /// <summary>
    /// 필드 아이템 감지 해제 메시지 (플레이어가 아이템 근처에서 나갔을 때)
    /// </summary>
    public readonly struct FieldItemLeftMessage
    {
        public readonly int fieldItemId;

        public FieldItemLeftMessage(int fieldItemId)
        {
            this.fieldItemId = fieldItemId;
        }
    }
}