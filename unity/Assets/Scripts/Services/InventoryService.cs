using System;
using System.Collections.Generic;
using UnityEngine;
using Models;

namespace Services
{
    public class InventoryService
    {
        private InventoryModel _inventoryModel;

        public event Action<int, string, int> OnPlayerSlotChanged;
        public event Action<int> OnFeedingCountChanged;
        public event Action<string, int> OnGlobalItemChanged;

        public InventoryService(InventoryModel inventoryModel)
        {
            _inventoryModel = inventoryModel;
        }

        #region Player Inventory Operations

        public bool AddToPlayerSlot(int slotIndex, string itemId, int amount = 1)
        {
            if (_inventoryModel.AddToPlayerSlot(slotIndex, itemId, amount))
            {
                OnPlayerSlotChanged?.Invoke(slotIndex, itemId, _inventoryModel.GetPlayerSlotCount(slotIndex));
                return true;
            }
            return false;
        }

        public bool UsePlayerSlot(int slotIndex, int amount = 1)
        {
            if (_inventoryModel.UsePlayerSlot(slotIndex, amount))
            {
                var slot = _inventoryModel.playerSlots[slotIndex];
                OnPlayerSlotChanged?.Invoke(slotIndex, slot.itemId, slot.count);
                return true;
            }
            return false;
        }

        public bool CanUsePlayerSlot(int slotIndex)
        {
            return _inventoryModel.CanUsePlayerSlot(slotIndex);
        }

        public string GetPlayerSlotItemId(int slotIndex)
        {
            return _inventoryModel.GetPlayerSlotItemId(slotIndex);
        }

        public int GetPlayerSlotCount(int slotIndex)
        {
            return _inventoryModel.GetPlayerSlotCount(slotIndex);
        }

        public bool SwapPlayerSlots(int slotA, int slotB)
        {
            if (_inventoryModel.SwapPlayerSlots(slotA, slotB))
            {
                // 두 슬롯 모두 변경 이벤트 발생
                var slotAData = _inventoryModel.playerSlots[slotA];
                var slotBData = _inventoryModel.playerSlots[slotB];

                OnPlayerSlotChanged?.Invoke(slotA, slotAData.itemId, slotAData.count);
                OnPlayerSlotChanged?.Invoke(slotB, slotBData.itemId, slotBData.count);

                return true;
            }
            return false;
        }

        #endregion

        #region Feeding Inventory Operations

        public int AddFeeding(int amount)
        {
            int added = _inventoryModel.AddFeeding(amount);
            if (added > 0)
            {
                OnFeedingCountChanged?.Invoke(_inventoryModel.feedingCount);
            }
            return added;
        }

        public bool RemoveFeeding(int amount)
        {
            if (_inventoryModel.RemoveFeeding(amount))
            {
                OnFeedingCountChanged?.Invoke(_inventoryModel.feedingCount);
                return true;
            }
            return false;
        }

        public bool HasEnoughFeeding(int amount)
        {
            return _inventoryModel.HasEnoughFeeding(amount);
        }

        public int GetFeedingCount()
        {
            return _inventoryModel.feedingCount;
        }

        #endregion

        #region Global Items Operations

        public void SetGlobalItemCount(string itemId, int count)
        {
            _inventoryModel.SetGlobalItemCount(itemId, count);
            OnGlobalItemChanged?.Invoke(itemId, count);
        }

        public int GetGlobalItemCount(string itemId)
        {
            return _inventoryModel.GetGlobalItemCount(itemId);
        }

        public bool HasGlobalItem(string itemId, int amount = 1)
        {
            return _inventoryModel.HasGlobalItem(itemId, amount);
        }

        #endregion

        #region Utility Methods

        public void Reset()
        {
            _inventoryModel.Reset();

            for (int i = 0; i < _inventoryModel.playerSlots.Length; i++)
            {
                OnPlayerSlotChanged?.Invoke(i, "", 0);
            }
            OnFeedingCountChanged?.Invoke(0);
        }

        public int GetTotalItemCount()
        {
            return _inventoryModel.GetTotalItemCount();
        }

        #endregion
    }
}