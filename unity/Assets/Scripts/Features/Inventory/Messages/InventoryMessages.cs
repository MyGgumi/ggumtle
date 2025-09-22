using Features.Inventory.Models;

namespace Features.Inventory.Messages
{
    /// <summary>
    /// 아이템 추가 메시지
    /// </summary>
    public class ItemAddedMessage
    {
        public string ItemId { get; set; }
        public int Count { get; set; }
        public int ChestId { get; set; }
        public int SlotIndex { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// 아이템 제거 메시지
    /// </summary>
    public class ItemRemovedMessage
    {
        public string ItemId { get; set; }
        public int Count { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// 아이템 사용 메시지
    /// </summary>
    public readonly struct ItemUsedMessage
    {
        public readonly int slotNumber;
        public readonly int itemId;
        public readonly int amount;
        public readonly int remainingCount;

        public ItemUsedMessage(int slotNumber, int itemId, int amount, int remainingCount)
        {
            this.slotNumber = slotNumber;
            this.itemId = itemId;
            this.amount = amount;
            this.remainingCount = remainingCount;
        }
    }

    /// <summary>
    /// 필드 아이템 사용 메시지
    /// </summary>
    public class FieldItemUsedMessage
    {
        public int FieldItemId { get; set; }
        public bool Success { get; set; }
        public string Result { get; set; }
    }

    /// <summary>
    /// 인벤토리 동기화 메시지
    /// </summary>
    public class InventorySyncMessage
    {
        public object[] Slots { get; set; } // 서버에서 오는 슬롯 데이터
        public int FeedingCount { get; set; }
    }

    /// <summary>
    /// 슬롯 변경 메시지
    /// </summary>
    public class SlotChangedMessage
    {
        public int SlotIndex { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 아이템 이동 메시지 (인벤토리 ↔ 상자)
    /// </summary>
    public class ItemTransferredMessage
    {
        public string ItemId { get; set; }
        public int Count { get; set; }
        public TransferDirection Direction { get; set; }
        public bool Success { get; set; }
    }

    public enum TransferDirection
    {
        ToInventory, // 상자 → 인벤토리
        ToChest, // 인벤토리 → 상자
    }

    /// <summary>
    /// 인벤토리 슬롯 클릭 메시지
    /// </summary>
    public class InventorySlotClickedMessage
    {
        public int SlotIndex { get; set; }
        public ClickType ClickType { get; set; }

        public InventorySlotClickedMessage(int slotIndex, ClickType clickType)
        {
            SlotIndex = slotIndex;
            ClickType = clickType;
        }
    }

    /// <summary>
    /// 인벤토리 UI 토글 메시지
    /// </summary>
    public class InventoryToggleMessage
    {
        public static readonly InventoryToggleMessage Instance = new();
        private InventoryToggleMessage() { }
    }

    public enum ClickType
    {
        Select, // 슬롯 선택
        Use, // 아이템 사용
        Transfer, // 아이템 이동 (상자와의 교환)
    }
}
