namespace InputSystem.Core
{
    /// <summary>
    /// 입력 모드 - 상황에 따라 특정 입력을 활성화/비활성화
    /// </summary>
    public enum InputMode
    {
        /// <summary>
        /// 일반 게임플레이 - 모든 입력 활성화
        /// </summary>
        Normal,

        /// <summary>
        /// 채팅 중 - 이동/액션 비활성화, UI만 활성
        /// </summary>
        Chatting,

        /// <summary>
        /// UI 상호작용 중 - 카메라 회전 비활성화
        /// </summary>
        UIInteraction,

        /// <summary>
        /// 인벤토리 열림 - 이동 비활성화, 카메라/UI 활성
        /// </summary>
        Inventory,

        /// <summary>
        /// 컷신 - 모든 입력 비활성화
        /// </summary>
        Cutscene,

        /// <summary>
        /// 일시정지 - 게임플레이 입력 비활성화
        /// </summary>
        Paused
    }
}