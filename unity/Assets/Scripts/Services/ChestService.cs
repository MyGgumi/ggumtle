using System;
using System.Collections.Generic;
using Models;
using UnityEngine;

namespace Services
{
    public class ChestService
    {
        private ChestModel _chestModel;

        public event Action<string> OnChestOpened;
        public event Action<string> OnChestClosed;
        public event Action<string, int, string, int> OnChestSlotChanged;
        public event Action<string> OnChestRegistered;
        public event Action<string> OnChestUnregistered;

        public ChestService(ChestModel chestModel)
        {
            _chestModel = chestModel;
        }

        #region Chest Management

        public void RegisterChest(
            string chestId,
            string chestName,
            Vector3 position,
            GameObject chestObject = null
        )
        {
            _chestModel.RegisterChest(chestId, chestName, position, chestObject);
            OnChestRegistered?.Invoke(chestId);
        }

        public void UnregisterChest(string chestId)
        {
            _chestModel.UnregisterChest(chestId);
            OnChestUnregistered?.Invoke(chestId);
        }

        public ChestData GetChest(string chestId)
        {
            return _chestModel.GetChest(chestId);
        }

        public bool HasChest(string chestId)
        {
            return _chestModel.HasChest(chestId);
        }

        #endregion

        #region Current Chest Operations

        public bool OpenChest(string chestId)
        {
            Debug.Log($"[ChestService] OpenChest 호출됨: {chestId}");
            Debug.Log($"[ChestService] _chestModel null 체크: {_chestModel == null}");

            if (_chestModel == null)
            {
                Debug.LogError("[ChestService] _chestModel이 null입니다!");
                return false;
            }

            if (_chestModel.OpenChest(chestId))
            {
                Debug.Log(
                    $"[ChestService] ChestModel.OpenChest 성공, OnChestOpened 이벤트 발생: {chestId}"
                );
                OnChestOpened?.Invoke(chestId);
                return true;
            }
            else
            {
                Debug.LogWarning($"[ChestService] ChestModel.OpenChest 실패: {chestId}");
                return false;
            }
        }

        public void CloseCurrentChest()
        {
            string chestId = _chestModel.currentChestId;
            _chestModel.CloseCurrentChest();
            if (!string.IsNullOrEmpty(chestId))
            {
                OnChestClosed?.Invoke(chestId);
            }
        }

        public bool TakeItemFromCurrentChest(int slotIndex, int amount = -1)
        {
            if (_chestModel.TakeItemFromCurrentChest(slotIndex, amount))
            {
                // 슬롯 압축이 발생했으므로 모든 슬롯의 상태를 다시 알림
                var chestId = _chestModel.currentChestId;
                var slots = _chestModel.currentChest.slots;

                for (int i = 0; i < slots.Length; i++)
                {
                    OnChestSlotChanged?.Invoke(chestId, i, slots[i].itemId, slots[i].count);
                }

                Debug.Log($"[ChestService] 아이템 제거 후 모든 슬롯 상태 업데이트: {chestId}");
                return true;
            }
            return false;
        }

        public void UpdateCurrentChestSlot(int slotIndex, string itemId, int count)
        {
            _chestModel.UpdateCurrentChestSlot(slotIndex, itemId, count);
            OnChestSlotChanged?.Invoke(_chestModel.currentChestId, slotIndex, itemId, count);
        }

        public bool HasCurrentChest => _chestModel.HasCurrentChest;
        public bool IsChestOpen => _chestModel.IsChestOpen;
        public ChestData CurrentChest => _chestModel.currentChest;
        public string CurrentChestId => _chestModel.currentChestId;

        #endregion

        #region Server Sync

        public void SyncChestData(string chestId, ChestSlot[] serverSlots)
        {
            _chestModel.SyncChestData(chestId, serverSlots);

            // 서버 동기화는 이미 열려있는 상자의 데이터만 업데이트
            // OnChestOpened는 OpenChest에서만 발생시키고,
            // 여기서는 슬롯 데이터만 업데이트하도록 수정

            // 현재 열려있는 상자라면 모든 슬롯 데이터 변경 알림
            if (_chestModel.currentChestId == chestId && _chestModel.IsChestOpen)
            {
                // 각 슬롯의 변경사항을 한 번에 처리
                for (int i = 0; i < serverSlots.Length; i++)
                {
                    OnChestSlotChanged?.Invoke(
                        chestId,
                        i,
                        serverSlots[i].itemId,
                        serverSlots[i].count
                    );
                }
            }
        }

        public void SyncChestSlot(string chestId, int slotIndex, string itemId, int count)
        {
            _chestModel.SyncChestSlot(chestId, slotIndex, itemId, count);
            OnChestSlotChanged?.Invoke(chestId, slotIndex, itemId, count);
        }

        #endregion

        #region Utility Methods

        public List<ChestData> GetAllChests()
        {
            return _chestModel.GetAllChests();
        }

        public List<ChestData> GetChestsWithItems()
        {
            return _chestModel.GetChestsWithItems();
        }

        public int GetTotalChestCount()
        {
            return _chestModel.GetTotalChestCount();
        }

        public int GetTotalItemsInAllChests()
        {
            return _chestModel.GetTotalItemsInAllChests();
        }

        public void Reset()
        {
            string currentChestId = _chestModel.currentChestId;
            _chestModel.Reset();

            if (!string.IsNullOrEmpty(currentChestId))
            {
                OnChestClosed?.Invoke(currentChestId);
            }
        }

        #endregion
    }
}
