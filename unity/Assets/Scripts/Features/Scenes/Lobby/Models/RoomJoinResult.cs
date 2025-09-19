namespace Features.Scenes.Lobby.Models
{
    /// <summary>
    /// 방 입장 결과를 나타내는 모델
    /// </summary>
    public class RoomJoinResult
    {
        /// <summary>
        /// 성공 여부
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 방 ID (성공시)
        /// </summary>
        public int RoomId { get; }

        /// <summary>
        /// 오류 메시지 (실패시)
        /// </summary>
        public string ErrorMessage { get; }

        public RoomJoinResult(bool isSuccess, int roomId = -1, string errorMessage = "")
        {
            IsSuccess = isSuccess;
            RoomId = roomId;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 성공 결과 생성
        /// </summary>
        public static RoomJoinResult Success(int roomId)
        {
            return new RoomJoinResult(true, roomId);
        }

        /// <summary>
        /// 실패 결과 생성
        /// </summary>
        public static RoomJoinResult Failure(string errorMessage)
        {
            return new RoomJoinResult(false, -1, errorMessage);
        }
    }
}