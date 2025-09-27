using Cysharp.Threading.Tasks;
using Networks.Ggumtle;

namespace Features.Ggumtle.NetworkSources
{
    /// <summary>
    /// 꿈틀이 관련 네트워크 통신을 담당하는 인터페이스
    /// Service 레이어와 Network 레이어를 분리하여 테스트 용이성과 유연성 제공
    /// </summary>
    public interface IGgumtleNetworkSource
    {
        /// <summary>
        /// 꿈틀이 파기 시작
        /// </summary>
        /// <param name="ggumtleId">꿈틀이 ID</param>
        /// <returns>파기 시작 결과</returns>
        UniTask<DiggingStartCommand> StartDiggingAsync(int ggumtleId);

        /// <summary>
        /// 꿈틀이 파기 중단
        /// </summary>
        /// <returns>파기 중단 결과</returns>
        UniTask<DiggingQuitCommand> QuitDiggingAsync();

        /// <summary>
        /// 빛젤리 먹이기 시작
        /// </summary>
        /// <param name="ggumtleId">꿈틀이 ID</param>
        /// <returns>먹이기 시작 결과</returns>
        UniTask<JellyStartCommand> StartJellyFeedingAsync(int ggumtleId);

        /// <summary>
        /// 빛젤리 먹이기 중단 (Fire-and-forget)
        /// </summary>
        void QuitJellyFeeding();
    }
}
