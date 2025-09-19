using Cysharp.Threading.Tasks;
using Networks.Rooms;
using Networks.Sessions;

namespace Features.Scenes.Lobby.NetworkSources
{
    /// <summary>
    /// 로비 관련 네트워크 통신을 담당하는 인터페이스
    /// </summary>
    public interface ILobbyNetworkSource
    {
        /// <summary>
        /// 액세스 토큰 검증
        /// </summary>
        /// <param name="accessToken">검증할 액세스 토큰</param>
        /// <returns>검증 결과</returns>
        UniTask<VerifyTokenCommand> VerifyTokenAsync(string accessToken);

        /// <summary>
        /// 방 입장
        /// </summary>
        /// <param name="roomId">입장할 방 ID (-1은 자동 매칭)</param>
        /// <returns>방 입장 결과</returns>
        UniTask<RoomJoinCommand> JoinRoomAsync(int roomId);

        /// <summary>
        /// 네트워크 연결 초기화
        /// </summary>
        /// <param name="host">서버 호스트</param>
        /// <param name="port">서버 포트</param>
        /// <returns>초기화 완료까지 대기</returns>
        UniTask InitializeNetworkAsync(string host, int port);
    }
}
