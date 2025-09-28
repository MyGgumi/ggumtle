using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.Game;
using UnityEngine;
using VContainer;

namespace Features.EscapeGate.NetworkSources
{
    /// <summary>
    /// 탈출 게이트 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신하고 결과를 Service 레이어에 전달
    /// </summary>
    public class EscapeGateNetworkSource : IEscapeGateNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public EscapeGateNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[EscapeGateNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<ExitAttemptCommand> AttemptEscapeAsync(int gateId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateNetworkSource] 탈출 시도 요청: GateId={gateId}");
                }

                var result = await _networkApi.ExitAttempt(gateId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[EscapeGateNetworkSource] 탈출 시도 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[EscapeGateNetworkSource] 탈출 시도 실패: {e.Message}");
                throw;
            }
        }
    }
}