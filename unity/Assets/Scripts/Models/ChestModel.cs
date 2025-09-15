using System;
using System.Collections.Generic;
using UnityEngine;

namespace Models
{
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

        public bool TakeAll()
        {
            if (isEmpty) return false;
            Clear();
            return true;
        }

        public bool TakeAmount(int amount)
        {
            if (isEmpty || count < amount) return false;

            count -= amount;
            if (count <= 0)
            {
                Clear();
            }
            return true;
        }
    }

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
            chestId = "";
            chestName = "";
            isOpen = false;
            isLocked = false;
            position = Vector3.zero;
            chestObject = null;

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new ChestSlot();
            }
        }

        public ChestData(string id, string name, Vector3 pos, GameObject obj = null)
        {
            chestId = id;
            chestName = name;
            isOpen = false;
            isLocked = false;
            position = pos;
            chestObject = obj;

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new ChestSlot();
            }
        }

        public bool HasItems
        {
            get
            {
                foreach (var slot in slots)
                {
                    if (!slot.isEmpty) return true;
                }
                return false;
            }
        }

        public int TotalItemCount
        {
            get
            {
                int total = 0;
                foreach (var slot in slots)
                {
                    if (!slot.isEmpty) total += slot.count;
                }
                return total;
            }
        }

        public void SetSlot(int index, string itemId, int count)
        {
            if (index >= 0 && index < slots.Length)
            {
                slots[index].SetItem(itemId, count);
            }
        }

        public bool TakeSlot(int index, int amount = -1)
        {
            if (index < 0 || index >= slots.Length) return false;

            if (amount == -1)
            {
                return slots[index].TakeAll();
            }
            else
            {
                return slots[index].TakeAmount(amount);
            }
        }

        public void ClearAllSlots()
        {
            foreach (var slot in slots)
            {
                slot.Clear();
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

        public void Lock()
        {
            isLocked = true;
            isOpen = false;
        }

        public void Unlock()
        {
            isLocked = false;
        }
    }

    [System.Serializable]
    public class ChestModel
    {
        [Header("Current Chest")]
        public ChestData currentChest;

        [Header("All Chests")]
        public Dictionary<string, ChestData> chests = new Dictionary<string, ChestData>();

        [Header("UI State")]
        public bool isChestUIOpen = false;
        public string currentChestId = "";

        public ChestModel()
        {
            currentChest = null;
        }

        #region Chest Management

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

        #endregion

        #region Current Chest Operations

        public bool OpenChest(string chestId)
        {
            Debug.Log($"[ChestModel] OpenChest 호출됨: {chestId}");
            Debug.Log($"[ChestModel] 등록된 상자 목록: {string.Join(", ", chests.Keys)}");

            if (!chests.ContainsKey(chestId))
            {
                Debug.LogError($"[ChestModel] 상자가 등록되지 않음: {chestId}");
                return false;
            }

            var chest = chests[chestId];
            if (chest.isLocked)
            {
                Debug.LogWarning($"[ChestModel] 상자가 잠겨있음: {chestId}");
                return false;
            }

            Debug.Log($"[ChestModel] 상자 열기 성공: {chestId}");
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
            if (currentChest == null) return false;
            bool result = currentChest.TakeSlot(slotIndex, amount);

            // 아이템 제거 후 자동 인덱스 조정 (빈 슬롯을 뒤로 보내기)
            if (result)
            {
                CompactChestSlots(currentChest);
            }

            return result;
        }

        /// <summary>
        /// 상자 슬롯을 압축하여 빈 슬롯을 뒤로 보냄
        /// </summary>
        private void CompactChestSlots(ChestData chest)
        {
            var slots = chest.slots;
            var compactedSlots = new ChestSlot[slots.Length];

            // 모든 슬롯을 빈 슬롯으로 초기화
            for (int i = 0; i < compactedSlots.Length; i++)
            {
                compactedSlots[i] = new ChestSlot();
            }

            // 비어있지 않은 슬롯들을 앞쪽부터 배치
            int targetIndex = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].isEmpty)
                {
                    compactedSlots[targetIndex] = new ChestSlot(slots[i].itemId, slots[i].count);
                    targetIndex++;
                }
            }

            // 압축된 슬롯 배열로 교체
            chest.slots = compactedSlots;

            Debug.Log($"[ChestModel] 슬롯 압축 완료: {targetIndex}개 아이템이 앞쪽으로 정렬됨");
        }

        public void UpdateCurrentChestSlot(int slotIndex, string itemId, int count)
        {
            if (currentChest != null)
            {
                currentChest.SetSlot(slotIndex, itemId, count);
            }
        }

        #endregion

        #region Server Sync

        public void SyncChestData(string chestId, ChestSlot[] serverSlots)
        {
            if (!chests.ContainsKey(chestId)) return;

            var chest = chests[chestId];
            for (int i = 0; i < Mathf.Min(chest.slots.Length, serverSlots.Length); i++)
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

        #endregion

        #region Utility Methods

        public bool HasCurrentChest => currentChest != null;
        public bool IsChestOpen => isChestUIOpen && currentChest != null;

        public List<ChestData> GetAllChests()
        {
            return new List<ChestData>(chests.Values);
        }

        public List<ChestData> GetChestsWithItems()
        {
            var result = new List<ChestData>();
            foreach (var chest in chests.Values)
            {
                if (chest.HasItems)
                {
                    result.Add(chest);
                }
            }
            return result;
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
            int total = 0;
            foreach (var chest in chests.Values)
            {
                total += chest.TotalItemCount;
            }
            return total;
        }

        #endregion
    }
}