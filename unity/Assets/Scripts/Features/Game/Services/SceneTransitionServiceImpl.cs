using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.Game.Services
{
    /// <summary>
    /// 씬 전환 서비스 구현체
    /// </summary>
    public class SceneTransitionServiceImpl : ISceneTransitionService
    {
        private readonly bool _enableDebugLogs = true;

        public event Action<string> OnSceneTransitionStarted;
        public event Action<string> OnSceneTransitionCompleted;

        public async UniTask LoadSceneAsync(string sceneName)
        {
            await LoadSceneAsync(sceneName, null);
        }

        public async UniTask LoadSceneAsync(string sceneName, Action<float> onProgress)
        {
            if (_enableDebugLogs)
                Debug.Log($"[SceneTransitionService] 씬 로드 시작: {sceneName}");

            OnSceneTransitionStarted?.Invoke(sceneName);

            try
            {
                var asyncOperation = SceneManager.LoadSceneAsync(sceneName);

                if (asyncOperation == null)
                {
                    Debug.LogError($"[SceneTransitionService] 씬 로드 실패: {sceneName}");
                    return;
                }

                // 진행률 추적
                while (!asyncOperation.isDone)
                {
                    float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                    onProgress?.Invoke(progress);
                    await UniTask.Yield();
                }

                onProgress?.Invoke(1.0f);

                if (_enableDebugLogs)
                    Debug.Log($"[SceneTransitionService] 씬 로드 완료: {sceneName}");

                OnSceneTransitionCompleted?.Invoke(sceneName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneTransitionService] 씬 로드 중 오류: {sceneName}, {e.Message}");
                throw;
            }
        }

        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}