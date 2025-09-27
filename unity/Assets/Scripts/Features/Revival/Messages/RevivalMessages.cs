namespace Features.Revival.Messages
{
    /// <summary>
    /// 직접 부활 시작 요청 메시지
    /// </summary>
    public readonly struct DirectRevivalStartRequestMessage
    {
        public readonly long revivingPlayerId;
        public readonly long targetPlayerId;

        public DirectRevivalStartRequestMessage(long revivingPlayerId, long targetPlayerId)
        {
            this.revivingPlayerId = revivingPlayerId;
            this.targetPlayerId = targetPlayerId;
        }
    }

    /// <summary>
    /// 직접 부활 취소 요청 메시지
    /// </summary>
    public readonly struct DirectRevivalCancelRequestMessage
    {
        public readonly long revivingPlayerId;

        public DirectRevivalCancelRequestMessage(long revivingPlayerId)
        {
            this.revivingPlayerId = revivingPlayerId;
        }
    }

    /// <summary>
    /// 부활 진행률 업데이트 메시지
    /// </summary>
    public readonly struct RevivalProgressMessage
    {
        public readonly long revivingPlayerId;
        public readonly long targetPlayerId;
        public readonly float progress;
        public readonly bool isCompleted;

        public RevivalProgressMessage(long revivingPlayerId, long targetPlayerId, float progress, bool isCompleted = false)
        {
            this.revivingPlayerId = revivingPlayerId;
            this.targetPlayerId = targetPlayerId;
            this.progress = progress;
            this.isCompleted = isCompleted;
        }
    }

    /// <summary>
    /// 부활 완료 메시지 (서버에서 받은 것을 Revival 전용으로 변환)
    /// </summary>
    public readonly struct RevivalCompletedMessage
    {
        public readonly long revivedPlayerId;
        public readonly long revivingPlayerId;
        public readonly int reviveHp;
        public readonly bool isFromSelfDefib;

        public RevivalCompletedMessage(long revivedPlayerId, long revivingPlayerId = -1, int reviveHp = 30, bool isFromSelfDefib = false)
        {
            this.revivedPlayerId = revivedPlayerId;
            this.revivingPlayerId = revivingPlayerId;
            this.reviveHp = reviveHp;
            this.isFromSelfDefib = isFromSelfDefib;
        }
    }

    /// <summary>
    /// 자가제세동기 부활 시작 메시지
    /// </summary>
    public readonly struct SelfDefibRevivalStartMessage
    {
        public readonly long playerId;
        public readonly float revivalDuration;

        public SelfDefibRevivalStartMessage(long playerId, float revivalDuration = 5f)
        {
            this.playerId = playerId;
            this.revivalDuration = revivalDuration;
        }
    }

    /// <summary>
    /// 자가제세동기 부활 진행 메시지
    /// </summary>
    public readonly struct SelfDefibRevivalProgressMessage
    {
        public readonly long playerId;
        public readonly float progress;
        public readonly float remainingTime;

        public SelfDefibRevivalProgressMessage(long playerId, float progress, float remainingTime)
        {
            this.playerId = playerId;
            this.progress = progress;
            this.remainingTime = remainingTime;
        }
    }

    /// <summary>
    /// 부활 상호작용 상태 변경 메시지
    /// </summary>
    public readonly struct RevivalInteractionStateMessage
    {
        public readonly long targetPlayerId;
        public readonly bool isInRange;
        public readonly bool canRevive;
        public readonly string interactionText;

        public RevivalInteractionStateMessage(long targetPlayerId, bool isInRange, bool canRevive, string interactionText = "")
        {
            this.targetPlayerId = targetPlayerId;
            this.isInRange = isInRange;
            this.canRevive = canRevive;
            this.interactionText = interactionText;
        }
    }
}