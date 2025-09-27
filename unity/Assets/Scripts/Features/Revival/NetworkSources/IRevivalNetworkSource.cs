using Cysharp.Threading.Tasks;
using Networks.Game;
using Networks.Players;

namespace Features.Revival.NetworkSources
{
    /// <summary>
    /// 부활 관련 네트워크 통신 인터페이스
    /// </summary>
    public interface IRevivalNetworkSource
    {
        /// <summary>
        /// 직접 부활 시작 요청
        /// </summary>
        /// <param name="targetMonggingId">부활 대상 몽깅이 ID</param>
        /// <returns>부활 시작 응답</returns>
        UniTask<MonggingRevivalStartCommand> StartDirectRevivalAsync(long targetMonggingId);

        /// <summary>
        /// 직접 부활 중지 요청
        /// </summary>
        /// <returns>부활 중지 응답</returns>
        UniTask<MonggingRevivalStopCommand> StopDirectRevivalAsync();
    }
}