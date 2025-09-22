using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Features.Chest.Models
{
    /// <summary>
    /// 상자 슬롯 데이터
    /// </summary>
    [System.Serializable]
    public class ChestSlot
    {
        public string itemId;
        public int count;
        public bool isEmpty;

        public ChestSlot()
        {
            itemId = "";
            count = 0;
            isEmpty = true;
        }

        public ChestSlot(string id, int amount)
        {
            itemId = id;
            count = amount;
            isEmpty = string.IsNullOrEmpty(id) || amount <= 0;
        }

        public void SetItem(string id, int amount)
        {
            itemId = id;
            count = amount;
            isEmpty = string.IsNullOrEmpty(id) || amount <= 0;
        }

        public void Clear()
        {
            itemId = "";
            count = 0;
            isEmpty = true;
        }
    }

    /// <summary>
    /// 상자 데이터
    /// </summary>
    [System.Serializable]
    public class ChestData
    {
        public string chestId;
        public string chestName;
        public ChestSlot[] slots = new ChestSlot[9];
        public bool isOpen;
        public bool isLocked;
        public Vector3 position;
        public GameObject chestObject;

        public ChestData()
        {
            InitializeSlots();
        }

        public ChestData(string id, string name, Vector3 pos, GameObject obj = null)
        {
            chestId = id;
            chestName = name;
            isOpen = false;
            isLocked = false;
            position = pos;
            chestObject = obj;
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new ChestSlot();
            }
        }

        public bool HasItems => slots.Any(slot => !slot.isEmpty);
        public int TotalItemCount => slots.Where(slot => !slot.isEmpty).Sum(slot => slot.count);

        public void SetSlot(int index, string itemId, int count)
        {
            if (index >= 0 && index < slots.Length)
            {
                slots[index].SetItem(itemId, count);
            }
        }

        public void Open()
        {
            if (!isLocked)
            {
                isOpen = true;
            }
        }

        public void Close()
        {
            isOpen = false;
        }
    }

    /// <summary>
    /// 상자 모델 - 모든 상자 관리
    /// </summary>
    public class ChestModel
    {
        public ChestData currentChest;
        public Dictionary<string, ChestData> chests = new Dictionary<string, ChestData>();
        public bool isChestUIOpen = false;
        public string currentChestId = "";

        public bool HasCurrentChest => currentChest != null;
        public bool IsChestOpen => isChestUIOpen && currentChest != null;

        public void RegisterChest(string chestId, string chestName, Vector3 position, GameObject chestObject = null)
        {
            if (string.IsNullOrEmpty(chestId)) return;

            var chestData = new ChestData(chestId, chestName, position, chestObject);
            chests[chestId] = chestData;
        }

        public void UnregisterChest(string chestId)
        {
            if (chests.ContainsKey(chestId))
            {
                if (currentChestId == chestId)
                {
                    CloseCurrentChest();
                }
                chests.Remove(chestId);
            }
        }

        public ChestData GetChest(string chestId)
        {
            return chests.ContainsKey(chestId) ? chests[chestId] : null;
        }

        public bool HasChest(string chestId)
        {
            return chests.ContainsKey(chestId);
        }

        public bool OpenChest(string chestId)
        {
            if (!chests.ContainsKey(chestId)) return false;

            var chest = chests[chestId];
            if (chest.isLocked) return false;

            currentChest = chest;
            currentChestId = chestId;
            chest.Open();
            isChestUIOpen = true;
            return true;
        }

        public void CloseCurrentChest()
        {
            if (currentChest != null)
            {
                currentChest.Close();
            }

            currentChest = null;
            currentChestId = "";
            isChestUIOpen = false;
        }

        public bool TakeItemFromCurrentChest(int slotIndex, int amount = -1)
        {
            if (currentChest == null || slotIndex < 0 || slotIndex >= currentChest.slots.Length)
                return false;

            var slot = currentChest.slots[slotIndex];
            if (slot.isEmpty) return false;

            if (amount == -1 || amount >= slot.count)
            {
                slot.Clear();
            }
            else
            {
                slot.count -= amount;
                if (slot.count <= 0)
                {
                    slot.Clear();
                }
            }

            return true;
        }

        public void UpdateCurrentChestSlot(int slotIndex, string itemId, int count)
        {
            if (currentChest != null)
            {
                currentChest.SetSlot(slotIndex, itemId, count);
            }
        }

        public void SyncChestData(string chestId, ChestSlot[] serverSlots)
        {
            if (!chests.ContainsKey(chestId)) return;

            var chest = chests[chestId];
            for (int i = 0; i < System.Math.Min(chest.slots.Length, serverSlots.Length); i++)
            {
                chest.slots[i] = new ChestSlot(serverSlots[i].itemId, serverSlots[i].count);
            }
        }

        public void SyncChestSlot(string chestId, int slotIndex, string itemId, int count)
        {
            if (!chests.ContainsKey(chestId)) return;

            var chest = chests[chestId];
            if (slotIndex >= 0 && slotIndex < chest.slots.Length)
            {
                chest.SetSlot(slotIndex, itemId, count);
            }
        }

        public List<ChestData> GetAllChests()
        {
            return new List<ChestData>(chests.Values);
        }

        public List<ChestData> GetChestsWithItems()
        {
            return chests.Values.Where(chest => chest.HasItems).ToList();
        }

        public void Reset()
        {
            CloseCurrentChest();
            chests.Clear();
        }

        public int GetTotalChestCount()
        {
            return chests.Count;
        }

        public int GetTotalItemsInAllChests()
        {
            return chests.Values.Sum(chest => chest.TotalItemCount);
        }
    }
}