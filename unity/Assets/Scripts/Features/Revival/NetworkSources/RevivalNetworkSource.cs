using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.Game;
using Networks.Players;
using UnityEngine;
using VContainer;

namespace Features.Revival.NetworkSources
{
    /// <summary>
    /// 부활 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신
    /// </summary>
    public class RevivalNetworkSource : IRevivalNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public RevivalNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[RevivalNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<MonggingRevivalStartCommand> StartDirectRevivalAsync(long targetMonggingId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[RevivalNetworkSource] 직접 부활 시작 요청: TargetId={targetMonggingId}");
                }

                var result = await _networkApi.MonggingRevivalStart(targetMonggingId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[RevivalNetworkSource] 직접 부활 시작 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RevivalNetworkSource] 직접 부활 시작 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask<MonggingRevivalStopCommand> StopDirectRevivalAsync()
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[RevivalNetworkSource] 직접 부활 중지 요청");
                }

                var result = await _networkApi.MonggingRevivalStop();

                if (_enableDebugLogs)
                {
                    Debug.Log($"[RevivalNetworkSource] 직접 부활 중지 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RevivalNetworkSource] 직접 부활 중지 실패: {e.Message}");
                throw;
            }
        }
    }
}