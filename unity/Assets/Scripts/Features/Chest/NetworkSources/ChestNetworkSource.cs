using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.chests;
using UnityEngine;
using VContainer;

namespace Features.Chest.NetworkSources
{
    /// <summary>
    /// 상자 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신하고 결과를 Service 레이어에 전달
    /// </summary>
    public class ChestNetworkSource : IChestNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public ChestNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[ChestNetworkSource] 초기화 완료");
            }
        }

        public async UniTask<ChestOpenCommand> OpenChestAsync(int chestId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkSource] 상자 열기 요청: ChestId={chestId}");
                }

                var result = await _networkApi.ChestOpen(chestId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkSource] 상자 열기 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestNetworkSource] 상자 열기 실패: {e.Message}");
                throw;
            }
        }

        public async UniTask<ChestCloseCommand> CloseChestAsync(int chestId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkSource] 상자 닫기 요청: ChestId={chestId}");
                }

                var result = await _networkApi.ChestClose(chestId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkSource] 상자 닫기 응답: Success={result.Success}, Result={result.Result}");
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestNetworkSource] 상자 닫기 실패: {e.Message}");
                throw;
            }
        }

        public void GetItemFromChest(int chestId, int slotIndex)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkSource] 아이템 가져오기 요청: ChestId={chestId}, SlotIndex={slotIndex}");
                }

                _networkApi.GetItem(chestId, slotIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestNetworkSource] 아이템 가져오기 실패: {e.Message}");
                throw;
            }
        }

        public void PutItemToChest(int itemId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkSource] 아이템 넣기 요청: ItemId={itemId}");
                }

                _networkApi.PutItem(itemId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestNetworkSource] 아이템 넣기 실패: {e.Message}");
                throw;
            }
        }
    }
}