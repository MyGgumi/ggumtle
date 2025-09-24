using Cysharp.Threading.Tasks;
using Networks.Scenes;

namespace Features.MainGame.NetworkSources
{
    /// <summary>
    /// 메인 게임 관련 네트워크 통신을 담당하는 인터페이스
    /// Service 레이어와 Network 레이어를 분리하여 테스트 용이성과 유연성 제공
    /// </summary>
    public interface IMainGameNetworkSource
    {
        /// <summary>
        /// 씬 전환 완료 알림 (서버에 준비 완료 상태 전송)
        /// </summary>
        /// <returns>씬 전환 응답</returns>
        UniTask<SceneChangeCommand> NotifySceneReadyAsync();
    }
}