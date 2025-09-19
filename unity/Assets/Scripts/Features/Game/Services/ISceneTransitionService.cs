using Cysharp.Threading.Tasks;
using System;

namespace Features.Game.Services
{
    /// <summary>
    /// 씬 전환을 담당하는 서비스 인터페이스
    /// </summary>
    public interface ISceneTransitionService
    {
        /// <summary>
        /// 씬 전환 시작 이벤트
        /// </summary>
        event Action<string> OnSceneTransitionStarted;

        /// <summary>
        /// 씬 전환 완료 이벤트
        /// </summary>
        event Action<string> OnSceneTransitionCompleted;

        /// <summary>
        /// 씬을 비동기적으로 로드
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름</param>
        /// <returns>로드 완료까지 대기하는 Task</returns>
        UniTask LoadSceneAsync(string sceneName);

        /// <summary>
        /// 씬을 비동기적으로 로드 (진행률 콜백 포함)
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름</param>
        /// <param name="onProgress">진행률 콜백 (0.0 ~ 1.0)</param>
        /// <returns>로드 완료까지 대기하는 Task</returns>
        UniTask LoadSceneAsync(string sceneName, Action<float> onProgress);

        /// <summary>
        /// 현재 씬 이름 가져오기
        /// </summary>
        /// <returns>현재 활성 씬 이름</returns>
        string GetCurrentSceneName();
    }
}