namespace Features.Game.Models
{
    /// <summary>
    /// 게임 전체 상태를 정의하는 열거형
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// 로비 씬 - 로그인 및 방 입장
        /// </summary>
        Lobby,

        /// <summary>
        /// 로딩 씬 - Addressable 에셋 로딩 및 게임 시작 대기
        /// </summary>
        Loading,

        /// <summary>
        /// 인게임 씬 - 실제 게임 플레이
        /// </summary>
        InGame,

        /// <summary>
        /// 게임 종료 - 결과 화면 또는 로비로 돌아가기
        /// </summary>
        GameOver
    }
}