using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.Scenes;
using UnityEngine;
using VContainer;

namespace Features.MainGame.NetworkSources
{
    /// <summary>
    /// 메인 게임 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신하고 결과를 Service 레이어에 전달
    /// </summary>
    public class MainGameNetworkSource : IMainGameNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = false;

        [Inject]
        public MainGameNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[MainGameNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<SceneChangeCommand> NotifySceneReadyAsync()
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[MainGameNetworkSource] 씬 준비 완료 알림 요청");
                }

                var result = await _networkApi.SceneChange();

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MainGameNetworkSource] 씬 준비 완료 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainGameNetworkSource] 씬 준비 완료 알림 실패: {e.Message}");
                throw;
            }
        }
    }
}