namespace Features.Revival.Models
{
    /// <summary>
    /// 부활 상태 열거형
    /// </summary>
    public enum RevivalState
    {
        None = 0,           // 부활 진행 안함
        DirectReviving = 1, // 직접 부활 진행 중
        SelfDefibReviving = 2, // 자가제세동기 부활 진행 중
        Completed = 3       // 부활 완료
    }

    /// <summary>
    /// 부활 타입 열거형
    /// </summary>
    public enum RevivalType
    {
        None = 0,
        DirectRevival = 1,      // 다른 플레이어가 직접 부활
        SelfDefibrillator = 2   // 자가제세동기 부활
    }
}