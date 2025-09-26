namespace Features.Mongging.Models
{
    /// <summary>
    /// 몽깅이 플레이어 상태
    /// </summary>
    public enum MonggingPlayerState
    {
        Normal,     // 정상 상태
        Stunned,    // 감전 상태 (2초간 이동 불가)
        Frightened, // 공포 상태 (시야 어둡게)
        Fainted,    // 기절 상태 (체력 0)
        Dead,       // 사망 상태 (3번 기절 후)
        Escaped     // 탈출 상태
    }
}