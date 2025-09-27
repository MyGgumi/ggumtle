using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.Inventory.Messages;
using Features.Inventory.Models;
using Features.Inventory.NetworkSources;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Inventory.Services
{
    /// <summary>
    /// 인벤토리 서비스 구현체
    /// NetworkSource를 통해 서버와 통신하고, 로컬 인벤토리 상태를 관리
    /// </summary>
    public class InventoryServiceImpl : IInventoryService, IDisposable
    {
        #region Dependencies

        private readonly IInventoryNetworkSource _networkSource;
        private readonly IPublisher<SlotChangedMessage> _slotChangedPublisher;
        private readonly IPublisher<ItemTransferredMessage> _itemTransferredPublisher;

        #endregion

        #region Observable Properties

        private readonly ReactiveProperty<InventoryData> _currentInventory;
        private readonly ReactiveProperty<int> _feedingCount;

        public ReadOnlyReactiveProperty<InventoryData> CurrentInventory => _currentInventory.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<int> FeedingCount => _feedingCount.ToReadOnlyReactiveProperty();

        #endregion

        #region Private Fields

        private readonly bool _enableDebugLogs = true;
        private readonly CompositeDisposable _disposables = new();

        #endregion

        [Inject]
        public InventoryServiceImpl(
            IInventoryNetworkSource networkSource,
            ISubscriber<ItemAddedMessage> itemAddedSubscriber,
            ISubscriber<ItemRemovedMessage> itemRemovedSubscriber,
            ISubscriber<InventorySyncMessage> inventorySyncSubscriber,
            IPublisher<SlotChangedMessage> slotChangedPublisher,
            IPublisher<ItemTransferredMessage> itemTransferredPublisher)
        {
            _networkSource = networkSource;
            _slotChangedPublisher = slotChangedPublisher;
            _itemTransferredPublisher = itemTransferredPublisher;

            _currentInventory = new ReactiveProperty<InventoryData>(new InventoryData());
            _feedingCount = new ReactiveProperty<int>(0);

            // 메시지 구독
            SubscribeToMessages(itemAddedSubscriber, itemRemovedSubscriber, inventorySyncSubscriber);

            if (_enableDebugLogs)
                Debug.Log("[InventoryService] 초기화 완료");
        }

        #region Message Subscriptions

        private void SubscribeToMessages(
            ISubscriber<ItemAddedMessage> itemAddedSubscriber,
            ISubscriber<ItemRemovedMessage> itemRemovedSubscriber,
            ISubscriber<InventorySyncMessage> inventorySyncSubscriber)
        {
            // 아이템 추가 메시지 구독
            itemAddedSubscriber
                .Subscribe(msg => OnItemAdded(msg))
                .AddTo(_disposables);

            // 아이템 제거 메시지 구독
            itemRemovedSubscriber
                .Subscribe(msg => OnItemRemoved(msg))
                .AddTo(_disposables);

            // 인벤토리 동기화 메시지 구독
            inventorySyncSubscriber
                .Subscribe(msg => OnInventorySync(msg))
                .AddTo(_disposables);

            if (_enableDebugLogs)
                Debug.Log("[InventoryService] 메시지 구독 완료");
        }

        private void OnItemAdded(ItemAddedMessage msg)
        {
            try
            {
                if (_enableDebugLogs)
                    Debug.Log($"[디버그] ItemAddedMessage 수신: 아이템ID={msg.ItemId}, 성공={msg.Success}, 개수={msg.Count}");

                if (!msg.Success)
                {
                    if (_enableDebugLogs)
                        Debug.Log($"[InventoryService] 아이템 추가 실패 무시: {msg.ItemId}");
                    return;
                }

                if (_enableDebugLogs)
                    Debug.Log($"[InventoryService] 아이템 추가 시도: {msg.ItemId} x{msg.Count}");

                bool result = AddItem(msg.ItemId, msg.Count, msg.SlotIndex);

                if (_enableDebugLogs)
                    Debug.Log($"[InventoryService] 아이템 추가 결과: {result}");

                if (!result)
                {
                    if (_enableDebugLogs)
                        Debug.LogWarning($"[InventoryService] 아이템 추가 실패: {msg.ItemId} - 인벤토리가 가득참");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryService] OnItemAdded 처리 중 예외 발생: {e.Message}\n{e.StackTrace}");
            }

            // 전송 완료 메시지 발행
            _itemTransferredPublisher.Publish(new ItemTransferredMessage
            {
                ItemId = msg.ItemId,
                Count = msg.Count,
                Direction = TransferDirection.ToInventory,
                Success = true
            });
        }

        private void OnItemRemoved(ItemRemovedMessage msg)
        {
            if (!msg.Success) return;

            if (_enableDebugLogs)
                Debug.Log($"[InventoryService] 아이템 제거: {msg.ItemId} x{msg.Count}");

            RemoveItem(msg.ItemId, msg.Count);

            // 전송 완료 메시지 발행
            _itemTransferredPublisher.Publish(new ItemTransferredMessage
            {
                ItemId = msg.ItemId,
                Count = msg.Count,
                Direction = TransferDirection.ToChest,
                Success = true
            });
        }

        private void OnInventorySync(InventorySyncMessage msg)
        {
            if (_enableDebugLogs)
                Debug.Log($"[InventoryService] 인벤토리 동기화: 슬롯={msg.Slots?.Length ?? 0}개");

            SyncInventory(msg.Slots);
            SyncFeedingCount(msg.FeedingCount);
        }

        #endregion

        #region Item Management

        public bool AddItem(string itemId, int count, int slotIndex = -1)
        {
            var inventory = _currentInventory.Value;

            // string itemId를 int로 변환
            if (!int.TryParse(itemId, out int numericItemId))
            {
                if (_enableDebugLogs)
                    Debug.LogError($"[InventoryServiceImpl] Invalid itemId format: {itemId}");
                return false;
            }

            // 빛젤리(ID: 1)는 Feeding으로 처리
            if (numericItemId == 1)
            {
                int addedAmount = AddFeeding(count);
                if (_enableDebugLogs)
                    Debug.Log($"[InventoryService] 빛젤리 추가: {addedAmount}/{count}");
                return addedAmount > 0;
            }

            // MaxStack 제한 확인
            var itemDef = Features.Item.Services.ItemDefinitionService.GetItemById(numericItemId);
            if (itemDef == null)
            {
                if (_enableDebugLogs)
                    Debug.LogError($"[InventoryServiceImpl] 아이템 정의를 찾을 수 없음: {numericItemId}");
                return false;
            }

            // 특정 슬롯 지정
            if (slotIndex >= 0)
            {
                var slot = inventory.PlayerSlots[slotIndex];
                if (!slot.IsEmpty && slot.ItemId == numericItemId)
                {
                    // 현재 개수 + 추가할 개수가 MaxStack을 초과하는지 확인
                    if (slot.Count + count > itemDef.MaxStack)
                    {
                        if (_enableDebugLogs)
                            Debug.Log($"[InventoryService] MaxStack 초과로 획득 실패: {slot.Count + count}/{itemDef.MaxStack}");
                        return false;
                    }
                }

                if (inventory.AddToSlot(slotIndex, numericItemId, count))
                {
                    _currentInventory.ForceNotify();
                    PublishSlotChanged(slotIndex);
                    return true;
                }
                return false;
            }

            // 같은 아이템이 있는 슬롯에 먼저 추가 시도
            for (int i = 0; i < inventory.PlayerSlots.Count; i++)
            {
                var slot = inventory.PlayerSlots[i];
                if (slot.ItemId == numericItemId)
                {
                    // MaxStack 확인
                    if (slot.Count + count > itemDef.MaxStack)
                    {
                        if (_enableDebugLogs)
                            Debug.Log($"[InventoryService] MaxStack 초과로 획득 실패: {slot.Count + count}/{itemDef.MaxStack}");
                        return false;
                    }

                    if (inventory.AddToSlot(i, numericItemId, count))
                    {
                        _currentInventory.ForceNotify();
                        PublishSlotChanged(i);
                        return true;
                    }
                }
            }

            // 같은 아이템이 없거나 스택이 가득 찬 경우, 빈 슬롯 찾기
            var emptySlot = GetEmptySlotIndex();
            if (emptySlot >= 0)
            {
                // 새로운 슬롯에서도 MaxStack 확인
                if (count > itemDef.MaxStack)
                {
                    if (_enableDebugLogs)
                        Debug.Log($"[InventoryService] MaxStack 초과로 획득 실패: {count}/{itemDef.MaxStack}");
                    return false;
                }

                if (inventory.AddToSlot(emptySlot, numericItemId, count))
                {
                    _currentInventory.ForceNotify();
                    PublishSlotChanged(emptySlot);
                    return true;
                }
            }

            return false;
        }

        public bool AddToPlayerSlot(int slotIndex, string itemId, int count)
        {
            return AddItem(itemId, count, slotIndex);
        }

        public bool RemoveItem(string itemId, int count)
        {
            var inventory = _currentInventory.Value;
            int remaining = count;

            // string itemId를 int로 변환
            if (!int.TryParse(itemId, out int numericItemId))
            {
                if (_enableDebugLogs)
                    Debug.LogError($"[InventoryServiceImpl] Invalid itemId format: {itemId}");
                return false;
            }

            for (int i = 0; i < inventory.PlayerSlots.Count; i++)
            {
                var slot = inventory.PlayerSlots[i];
                if (slot.ItemId == numericItemId)
                {
                    int removeCount = Math.Min(remaining, slot.Count);
                    if (inventory.RemoveFromSlot(i, removeCount))
                    {
                        remaining -= removeCount;
                        PublishSlotChanged(i);

                        if (remaining <= 0)
                        {
                            _currentInventory.ForceNotify();
                            return true;
                        }
                    }
                }
            }

            _currentInventory.ForceNotify();
            return remaining <= 0;
        }

        public bool UseItem(int slotIndex)
        {
            var slot = GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
                return false;

            // TODO: 아이템 사용 로직 구현
            RemoveFromSlot(slotIndex, 1);
            return true;
        }

        public bool TransferToChest(int slotIndex, int chestId)
        {
            var slot = GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
                return false;

            // 서버 요청
            _networkSource.PutItemToChest(slot.ItemId);
            return true;
        }

        #endregion

        #region Slot Management

        public bool SwapSlots(int slotIndex1, int slotIndex2)
        {
            var inventory = _currentInventory.Value;
            if (slotIndex1 < 0 || slotIndex1 >= inventory.PlayerSlots.Count ||
                slotIndex2 < 0 || slotIndex2 >= inventory.PlayerSlots.Count)
                return false;

            var temp = inventory.PlayerSlots[slotIndex1].Clone();
            inventory.PlayerSlots[slotIndex1] = inventory.PlayerSlots[slotIndex2].Clone();
            inventory.PlayerSlots[slotIndex2] = temp;

            _currentInventory.ForceNotify();
            PublishSlotChanged(slotIndex1);
            PublishSlotChanged(slotIndex2);

            return true;
        }

        public void ClearSlot(int slotIndex)
        {
            var inventory = _currentInventory.Value;
            if (slotIndex >= 0 && slotIndex < inventory.PlayerSlots.Count)
            {
                inventory.PlayerSlots[slotIndex].Clear();
                _currentInventory.ForceNotify();
                PublishSlotChanged(slotIndex);
            }
        }

        public InventorySlot GetSlot(int slotIndex)
        {
            var inventory = _currentInventory.Value;
            if (slotIndex >= 0 && slotIndex < inventory.PlayerSlots.Count)
            {
                return inventory.PlayerSlots[slotIndex];
            }
            return null;
        }

        private bool RemoveFromSlot(int slotIndex, int count)
        {
            var inventory = _currentInventory.Value;
            if (inventory.RemoveFromSlot(slotIndex, count))
            {
                _currentInventory.ForceNotify();
                PublishSlotChanged(slotIndex);
                return true;
            }
            return false;
        }

        #endregion

        #region Feeding Management

        public int AddFeeding(int amount)
        {
            var inventory = _currentInventory.Value;
            var oldCount = inventory.FeedingCount;

            // 서버에서 받은 최신 값(_feedingCount.Value)을 기준으로 더함
            // 이렇게 하면 로컬 값에 누적되지 않고 서버 기준값에서 더함
            _feedingCount.Value += amount;
            inventory.FeedingCount = _feedingCount.Value;
            _currentInventory.ForceNotify();

            if (_enableDebugLogs)
                Debug.Log($"[InventoryServiceImpl] Feeding 추가 (서버 기준값 사용): {oldCount} → {inventory.FeedingCount} (+{amount}, 서버 기준값: {_feedingCount.Value})");

            return amount;
        }

        #endregion

        #region Network Operations

        public void RequestItemFromChest(int chestId, int slotIndex)
        {
            _networkSource.GetItemFromChest(chestId, slotIndex);
        }

        public void RequestItemToChest(int itemId)
        {
            _networkSource.PutItemToChest(itemId);
        }

        public async UniTask<bool> RequestUseItemAsync(int itemId, Vector3 direction)
        {
            return await _networkSource.UseItemAsync(itemId, direction);
        }

        public async UniTask<bool> RequestUseFieldItemAsync(int fieldItemId)
        {
            return await _networkSource.UseFieldItemAsync(fieldItemId);
        }

        #endregion

        #region Synchronization

        public void SyncInventory(object[] serverSlots)
        {
            if (serverSlots == null) return;

            var inventory = _currentInventory.Value;

            // 서버 슬롯 데이터를 로컬 슬롯으로 변환
            for (int i = 0; i < serverSlots.Length && i < inventory.PlayerSlots.Count; i++)
            {
                // TODO: 서버 슬롯 데이터 형식에 따라 파싱 구현
                // 예: var serverSlot = JsonUtility.FromJson<ServerSlotData>(serverSlots[i].ToString());
                // inventory.PlayerSlots[i].ItemId = serverSlot.ItemId;
                // inventory.PlayerSlots[i].Count = serverSlot.Count;
            }

            _currentInventory.ForceNotify();
        }

        public void SyncFeedingCount(int count)
        {
            _feedingCount.Value = count;
            _currentInventory.Value.FeedingCount = count;
        }

        #endregion

        #region Utilities

        public int GetEmptySlotIndex()
        {
            var inventory = _currentInventory.Value;
            for (int i = 0; i < inventory.PlayerSlots.Count; i++)
            {
                if (inventory.PlayerSlots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        public bool HasItem(string itemId)
        {
            if (!int.TryParse(itemId, out int numericItemId))
                return false;

            return _currentInventory.Value.PlayerSlots.Any(slot => slot.ItemId == numericItemId);
        }

        public int GetItemCount(string itemId)
        {
            if (!int.TryParse(itemId, out int numericItemId))
                return 0;

            return _currentInventory.Value.PlayerSlots
                .Where(slot => slot.ItemId == numericItemId)
                .Sum(slot => slot.Count);
        }

        public void Reset()
        {
            _currentInventory.Value = new InventoryData();
            _feedingCount.Value = 0;

            if (_enableDebugLogs)
                Debug.Log("[InventoryService] 인벤토리 초기화");
        }

        private void PublishSlotChanged(int slotIndex)
        {
            var slot = GetSlot(slotIndex);
            if (slot != null)
            {
                _slotChangedPublisher.Publish(new SlotChangedMessage
                {
                    SlotIndex = slotIndex,
                    ItemId = slot.ItemId,
                    Count = slot.Count
                });
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _disposables.Dispose();
            _currentInventory?.Dispose();
            _feedingCount?.Dispose();

            if (_enableDebugLogs)
                Debug.Log("[InventoryService] Dispose 완료");
        }

        #endregion
    }
}