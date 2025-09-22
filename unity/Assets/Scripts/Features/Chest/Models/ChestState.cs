namespace Features.Chest.Models
{
    /// <summary>
    /// 상자의 상태를 나타내는 열거형
    /// </summary>
    public enum ChestState
    {
        /// <summary>
        /// 닫혀있는 상태
        /// </summary>
        Closed = 0,

        /// <summary>
        /// 열려있는 상태
        /// </summary>
        Open = 1,

        /// <summary>
        /// 잠겨있는 상태
        /// </summary>
        Locked = 2,

        /// <summary>
        /// 애니메이션 재생 중
        /// </summary>
        Animating = 3
    }
}