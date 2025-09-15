using System;
using System.Collections.Generic;
using UnityEngine;

namespace Models
{
    [System.Serializable]
    public class ItemSlot
    {
        public string itemId;
        public int count;
        public int maxCount;
        public bool isActive;

        public ItemSlot(string id = "", int maxCount = 99)
        {
            this.itemId = id;
            this.count = 0;
            this.maxCount = maxCount;
            this.isActive = true;
        }

        public bool CanAdd(int amount = 1) => isActive && count + amount <= maxCount;
        public bool CanRemove(int amount = 1) => isActive && count >= amount;
        public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;
        public bool IsFull => count >= maxCount;

        public bool AddItem(int amount = 1)
        {
            if (!CanAdd(amount)) return false;
            count += amount;
            return true;
        }

        public bool RemoveItem(int amount = 1)
        {
            if (!CanRemove(amount)) return false;
            count -= amount;
            if (count <= 0)
            {
                Clear();
            }
            return true;
        }

        public void Clear()
        {
            itemId = "";
            count = 0;
        }

        public void SetItem(string id, int amount)
        {
            itemId = id;
            count = Mathf.Clamp(amount, 0, maxCount);
        }
    }

    [System.Serializable]
    public class InventoryModel
    {
        [Header("Player Inventory")]
        public ItemSlot[] playerSlots = new ItemSlot[3];

        [Header("Feeding Inventory")]
        public int feedingCount = 0;
        public int maxFeedingCount = 999;

        [Header("Global Items")]
        public Dictionary<string, int> globalItems = new Dictionary<string, int>();

        public InventoryModel()
        {
            // 플레이어 슬롯 초기화
            for (int i = 0; i < playerSlots.Length; i++)
            {
                playerSlots[i] = new ItemSlot();
            }
        }

        #region Player Inventory Methods

        public bool AddToPlayerSlot(int slotIndex, string itemId, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= playerSlots.Length) return false;

            var slot = playerSlots[slotIndex];
            if (slot.IsEmpty)
            {
                slot.SetItem(itemId, amount);
                return true;
            }
            else if (slot.itemId == itemId)
            {
                return slot.AddItem(amount);
            }
            return false;
        }

        public bool RemoveFromPlayerSlot(int slotIndex, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= playerSlots.Length) return false;
            return playerSlots[slotIndex].RemoveItem(amount);
        }

        public bool UsePlayerSlot(int slotIndex, int amount = 1)
        {
            return RemoveFromPlayerSlot(slotIndex, amount);
        }

        public int GetPlayerSlotCount(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerSlots.Length) return 0;
            return playerSlots[slotIndex].count;
        }

        public string GetPlayerSlotItemId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerSlots.Length) return "";
            return playerSlots[slotIndex].itemId;
        }

        public bool CanUsePlayerSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerSlots.Length) return false;
            return !playerSlots[slotIndex].IsEmpty;
        }

        #endregion

        #region Feeding Inventory Methods

        public int AddFeeding(int amount)
        {
            if (amount <= 0) return 0;

            int canAdd = Mathf.Min(amount, maxFeedingCount - feedingCount);
            feedingCount += canAdd;
            return canAdd;
        }

        public bool RemoveFeeding(int amount)
        {
            if (amount <= 0 || feedingCount < amount) return false;

            feedingCount -= amount;
            return true;
        }

        public bool HasEnoughFeeding(int amount) => feedingCount >= amount;

        #endregion

        #region Global Items Methods

        public void SetGlobalItemCount(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            if (count <= 0)
            {
                globalItems.Remove(itemId);
            }
            else
            {
                globalItems[itemId] = count;
            }
        }

        public int GetGlobalItemCount(string itemId)
        {
            return globalItems.ContainsKey(itemId) ? globalItems[itemId] : 0;
        }

        public bool HasGlobalItem(string itemId, int amount = 1)
        {
            return GetGlobalItemCount(itemId) >= amount;
        }

        #endregion

        #region Utility Methods

        public void Reset()
        {
            foreach (var slot in playerSlots)
            {
                slot.Clear();
            }
            feedingCount = 0;
            globalItems.Clear();
        }

        public int GetTotalItemCount()
        {
            int total = feedingCount;
            foreach (var slot in playerSlots)
            {
                total += slot.count;
            }
            foreach (var globalItem in globalItems.Values)
            {
                total += globalItem;
            }
            return total;
        }

        #endregion
    }
}