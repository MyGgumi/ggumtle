namespace Features.EscapeGate.Models
{
    /// <summary>
    /// 탈출 게이트의 상태를 나타내는 열거형
    /// </summary>
    public enum EscapeGateState
    {
        /// <summary>
        /// 비활성화 상태 (아직 열리지 않음)
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// 활성화 상태 (탈출 가능)
        /// </summary>
        Active = 1,

        /// <summary>
        /// 사용 중 상태 (누군가 탈출 시도 중)
        /// </summary>
        InUse = 2,

        /// <summary>
        /// 비활성화됨 (게임 종료 등으로 사용 불가)
        /// </summary>
        Disabled = 3
    }
}