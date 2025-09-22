using System;
using Features.Item.Models;
using Features.Item.Services;

namespace Features.Inventory.Models
{
    /// <summary>
    /// 인벤토리 슬롯 데이터
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }

        /// <summary>
        /// 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsEmpty => ItemId <= 0 || Count <= 0;

        /// <summary>
        /// 슬롯 초기화
        /// </summary>
        public void Clear()
        {
            ItemId = 0;
            Count = 0;
        }

        /// <summary>
        /// 슬롯 데이터 복사
        /// </summary>
        public InventorySlot Clone()
        {
            return new InventorySlot
            {
                SlotIndex = SlotIndex,
                ItemId = ItemId,
                Count = Count
            };
        }

        /// <summary>
        /// 아이템 정의 가져오기
        /// </summary>
        public ItemDefinition GetItemDefinition()
        {
            return ItemDefinitionService.GetItemById(ItemId);
        }

        public override string ToString()
        {
            if (IsEmpty) return "Empty";
            var itemDef = GetItemDefinition();
            var itemName = itemDef?.ItemName ?? $"Unknown({ItemId})";
            return $"{itemName} x{Count}";
        }
    }
}