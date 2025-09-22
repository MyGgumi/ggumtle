using System;
using System.Collections.Generic;
using Features.Item.Services;

namespace Features.Inventory.Models
{
    /// <summary>
    /// 인벤토리 데이터 모델
    /// </summary>
    [Serializable]
    public class InventoryData
    {
        public List<InventorySlot> PlayerSlots { get; set; }
        public int FeedingCount { get; set; }
        public Dictionary<string, int> GlobalItems { get; set; }

        public InventoryData()
        {
            PlayerSlots = new List<InventorySlot>();
            for (int i = 0; i < 3; i++) // 플레이어 슬롯 3개
            {
                PlayerSlots.Add(new InventorySlot { SlotIndex = i });
            }

            FeedingCount = 0;
            GlobalItems = new Dictionary<string, int>();
        }

        /// <summary>
        /// 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PlayerSlots.Count)
                return true;

            return PlayerSlots[slotIndex].IsEmpty;
        }

        /// <summary>
        /// 슬롯 아이템 추가 (최대 스택 제한 적용)
        /// </summary>
        public bool AddToSlot(int slotIndex, int itemId, int count)
        {
            if (slotIndex < 0 || slotIndex >= PlayerSlots.Count)
                return false;

            // 아이템 정의 확인
            var itemDef = ItemDefinitionService.GetItemById(itemId);
            if (itemDef == null)
                return false;

            var slot = PlayerSlots[slotIndex];
            if (slot.IsEmpty)
            {
                // 최대 스택 제한 적용
                int actualCount = System.Math.Min(count, itemDef.MaxStack);
                slot.ItemId = itemId;
                slot.Count = actualCount;
                return true;
            }
            else if (slot.ItemId == itemId)
            {
                // 기존 개수 + 추가할 개수가 최대 스택을 넘지 않도록 제한
                int availableSpace = itemDef.MaxStack - slot.Count;
                int actualCount = System.Math.Min(count, availableSpace);

                if (actualCount > 0)
                {
                    slot.Count += actualCount;
                    return true;
                }
                return false; // 더 이상 추가할 수 없음
            }

            return false; // 다른 아이템이 이미 있음
        }

        /// <summary>
        /// 슬롯 아이템 제거
        /// </summary>
        public bool RemoveFromSlot(int slotIndex, int count = -1)
        {
            if (slotIndex < 0 || slotIndex >= PlayerSlots.Count)
                return false;

            var slot = PlayerSlots[slotIndex];
            if (slot.IsEmpty)
                return false;

            if (count < 0 || count >= slot.Count)
            {
                slot.Clear();
            }
            else
            {
                slot.Count -= count;
            }

            return true;
        }
    }
}