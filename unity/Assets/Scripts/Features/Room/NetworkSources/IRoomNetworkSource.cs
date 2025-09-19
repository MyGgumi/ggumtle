using Cysharp.Threading.Tasks;
using Networks.Rooms;

namespace Features.Room.NetworkSources
{
    /// <summary>
    /// 방 관련 네트워크 통신을 담당하는 인터페이스
    /// </summary>
    public interface IRoomNetworkSource
    {
        /// <summary>
        /// 맵 초기화 요청
        /// </summary>
        UniTask<InitializeMapCommand> InitializeMapAsync();

        /// <summary>
        /// 플레이어 초기화 요청
        /// </summary>
        UniTask<InitializePlayerCommand> InitializePlayerAsync();

        /// <summary>
        /// 게임 시작 요청
        /// </summary>
        UniTask<bool> StartGameAsync();
    }
}