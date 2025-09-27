using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.Ggumtle;
using UnityEngine;
using VContainer;

namespace Features.Ggumtle.NetworkSources
{
    /// <summary>
    /// 꿈틀이 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신하고 결과를 Service 레이어에 전달
    /// </summary>
    public class GgumtleNetworkSource : IGgumtleNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public GgumtleNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[GgumtleNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<DiggingStartCommand> StartDiggingAsync(int ggumtleId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkSource] 꿈틀이 파기 시작 요청: GgumtleId={ggumtleId}"
                    );
                }

                var result = await _networkApi.DiggingStart(ggumtleId);

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkSource] 꿈틀이 파기 응답: Success={result.Success}, Result={result.Result}"
                    );
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkSource] 꿈틀이 파기 시작 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask<DiggingQuitCommand> QuitDiggingAsync()
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[GgumtleNetworkSource] 꿈틀이 파기 중단 요청");
                }

                var result = await _networkApi.DiggingQuit();

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkSource] 꿈틀이 파기 중단 응답: Success={result.Success}, Result={result.Result}"
                    );
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkSource] 꿈틀이 파기 중단 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask<JellyStartCommand> StartJellyFeedingAsync(int ggumtleId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkSource] 빛젤리 먹이기 시작 요청: GgumtleId={ggumtleId}"
                    );
                }

                var result = await _networkApi.JellyStart(ggumtleId);

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkSource] 빛젤리 먹이기 응답: Success={result.Success}, Result={result.Result}"
                    );
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkSource] 빛젤리 먹이기 시작 실패: {e.Message}");
                throw;
            }
        }

        public void QuitJellyFeeding()
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[GgumtleNetworkSource] 빛젤리 먹이기 중단 요청");
                }

                _networkApi.JellyQuit();

                if (_enableDebugLogs)
                {
                    Debug.Log("[GgumtleNetworkSource] 빛젤리 먹이기 중단 요청 전송 완료");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkSource] 빛젤리 먹이기 중단 실패: {e.Message}");
                throw;
            }
        }
    }
}
