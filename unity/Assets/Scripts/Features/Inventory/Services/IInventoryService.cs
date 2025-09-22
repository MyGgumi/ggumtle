using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.Inventory.Models;
using R3;
using UnityEngine;

namespace Features.Inventory.Services
{
    /// <summary>
    /// 인벤토리 서비스 인터페이스
    /// </summary>
    public interface IInventoryService
    {
        // Observable Properties
        ReadOnlyReactiveProperty<InventoryData> CurrentInventory { get; }
        ReadOnlyReactiveProperty<int> FeedingCount { get; }

        // 아이템 관리
        bool AddItem(string itemId, int count, int slotIndex = -1);
        bool AddToPlayerSlot(int slotIndex, string itemId, int count);
        bool RemoveItem(string itemId, int count);
        bool UseItem(int slotIndex);
        bool TransferToChest(int slotIndex, int chestId);

        // 슬롯 관리
        bool SwapSlots(int slotIndex1, int slotIndex2);
        void ClearSlot(int slotIndex);
        InventorySlot GetSlot(int slotIndex);

        // 네트워크 작업
        void RequestItemFromChest(int chestId, int slotIndex);
        void RequestItemToChest(int itemId);
        UniTask<bool> RequestUseItemAsync(int itemId, Vector3 direction);
        UniTask<bool> RequestUseFieldItemAsync(int fieldItemId);

        // 동기화
        void SyncInventory(object[] serverSlots);
        void SyncFeedingCount(int count);

        // Feeding 관리
        int AddFeeding(int amount);

        // 유틸리티
        int GetEmptySlotIndex();
        bool HasItem(string itemId);
        int GetItemCount(string itemId);
        void Reset();
    }
}
