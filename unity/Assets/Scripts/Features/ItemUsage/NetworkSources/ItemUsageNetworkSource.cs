using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.Game;
using Networks.Players;
using UnityEngine;
using VContainer;

namespace Features.ItemUsage.NetworkSources
{
    /// <summary>
    /// 아이템 사용 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신
    /// </summary>
    public class ItemUsageNetworkSource : IItemUsageNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public ItemUsageNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[ItemUsageNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<MonggingItemUseCommand> UseItemAsync(int itemId, Vector3 direction)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageNetworkSource] 아이템 사용 요청: ItemId={itemId}, Direction={direction}");
                }

                var result = await _networkApi.MonggingItemUse(direction, itemId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageNetworkSource] 아이템 사용 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageNetworkSource] 아이템 사용 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask<MonggingFieldItemUseCommand> UseFieldItemAsync(int fieldItemId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageNetworkSource] 필드 아이템 사용 요청: FieldItemId={fieldItemId}");
                }

                var result = await _networkApi.MonggingFieldItemUse(fieldItemId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageNetworkSource] 필드 아이템 사용 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageNetworkSource] 필드 아이템 사용 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask<UseDefibrillatorCommand> UseDefibrillatorAsync()
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[ItemUsageNetworkSource] 자가제세동기 사용 요청");
                }

                var result = await _networkApi.UseDefibrillator();

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageNetworkSource] 자가제세동기 사용 응답: Success={result.Success}, Result={result.Result}, HP={result.Hp}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageNetworkSource] 자가제세동기 사용 실패: {e.Message}");
                throw;
            }
        }
    }
}