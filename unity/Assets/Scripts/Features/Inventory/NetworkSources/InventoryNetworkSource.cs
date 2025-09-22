using System;
using Cysharp.Threading.Tasks;
using Networks;
using Networks.Game;
using UnityEngine;
using VContainer;

namespace Features.Inventory.NetworkSources
{
    /// <summary>
    /// 인벤토리 관련 네트워크 통신 구현체
    /// NetworkApi를 통해 서버와 통신하고 결과를 Service 레이어에 전달
    /// </summary>
    public class InventoryNetworkSource : IInventoryNetworkSource
    {
        private readonly NetworkApi _networkApi;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public InventoryNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            if (_enableDebugLogs)
            {
                Debug.Log("[InventoryNetworkSource] 초기화 완료");
            }
        }

        public void GetItemFromChest(int chestId, int slotIndex)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[InventoryNetworkSource] 아이템 획득 요청: ChestId={chestId}, SlotIndex={slotIndex}");
                }

                _networkApi.GetItem(chestId, slotIndex);

                if (_enableDebugLogs)
                {
                    Debug.Log("[InventoryNetworkSource] 아이템 획득 요청 전송 완료");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[InventoryNetworkSource] 아이템 획득 요청 실패: {e.Message}");
            }
        }

        public void PutItemToChest(int itemId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[InventoryNetworkSource] 아이템 넣기 요청: ItemId={itemId}");
                }

                _networkApi.PutItem(itemId);

                if (_enableDebugLogs)
                {
                    Debug.Log("[InventoryNetworkSource] 아이템 넣기 요청 전송 완료");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[InventoryNetworkSource] 아이템 넣기 요청 실패: {e.Message}");
            }
        }

        public async UniTask<bool> UseItemAsync(int itemId, Vector3 direction)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[InventoryNetworkSource] 아이템 사용 요청: ItemId={itemId}, Direction={direction}");
                }

                var result = await _networkApi.MonggingItemUse(direction, itemId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[InventoryNetworkSource] 아이템 사용 응답: Success={result.Success}, Result={result.Result}");
                }

                return result.Success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[InventoryNetworkSource] 아이템 사용 실패: {e.Message}");
                return false;
            }
        }

        public async UniTask<bool> UseFieldItemAsync(int fieldItemId)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[InventoryNetworkSource] 필드 아이템 사용 요청: FieldItemId={fieldItemId}");
                }

                var result = await _networkApi.MonggingFieldItemUse(fieldItemId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[InventoryNetworkSource] 필드 아이템 사용 응답: Success={result.Success}, Result={result.Result}");
                }

                return result.Success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[InventoryNetworkSource] 필드 아이템 사용 실패: {e.Message}");
                return false;
            }
        }
    }
}