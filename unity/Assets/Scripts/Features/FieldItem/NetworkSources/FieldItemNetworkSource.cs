using System;
using Networks;
using UnityEngine;
using VContainer;

namespace Features.FieldItem.NetworkSources
{
    /// <summary>
    /// 필드 아이템 사용 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신 (void 전송, 브로드캐스트로 응답 수신)
    /// </summary>
    public class FieldItemNetworkSource : IFieldItemNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public FieldItemNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[FieldItemNetworkSource] 초기화 완료");
            }
        }

        public void UseFieldItem(int fieldItemId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[FieldItemNetworkSource] 필드 아이템 사용 요청 전송: FieldItemId={fieldItemId}");
                }

                _networkApi.MonggingFieldItemUse(fieldItemId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[FieldItemNetworkSource] 필드 아이템 사용 요청 전송 완료: FieldItemId={fieldItemId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FieldItemNetworkSource] 필드 아이템 사용 요청 전송 실패: {e.Message}");
            }
        }
    }
}