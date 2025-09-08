using UnityEngine;

/// <summary>
/// 플레이어 인벤토리에서 사용하는 아이템 데이터
/// ChestItem과 호환 가능하도록 설계
/// </summary>
[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public Sprite itemIcon;
    public int quantity;
    public string description;
    public int maxStack = 3; // 최대 스택 개수 (플레이어 인벤토리는 3개까지)

    public InventoryItem()
    {
        itemName = "";
        itemIcon = null;
        quantity = 0;
        description = "";
        maxStack = 3;
    }

    public InventoryItem(string name, Sprite icon, int qty = 1, string desc = "", int stack = 3)
    {
        itemName = name;
        itemIcon = icon;
        quantity = qty;
        description = desc;
        maxStack = stack;
    }

    /// <summary>
    /// ChestItem을 InventoryItem으로 변환
    /// </summary>
    public static InventoryItem FromChestItem(ChestItem chestItem)
    {
        Debug.Log(
            $"[InventoryItem] FromChestItem 호출됨 - 아이템: {chestItem.itemName}, 아이콘: {(chestItem.itemIcon != null ? "있음" : "없음")}"
        );

        var inventoryItem = new InventoryItem(
            chestItem.itemName,
            chestItem.itemIcon,
            chestItem.quantity,
            chestItem.description,
            3 // 플레이어 인벤토리는 최대 3개까지 스택
        );

        Debug.Log(
            $"[InventoryItem] 변환 완료 - InventoryItem 아이콘: {(inventoryItem.itemIcon != null ? "있음" : "없음")}"
        );

        return inventoryItem;
    }

    /// <summary>
    /// 빈 슬롯인지 확인
    /// </summary>
    public bool IsEmpty()
    {
        return quantity <= 0 || string.IsNullOrEmpty(itemName);
    }

    /// <summary>
    /// 같은 아이템인지 확인 (이름으로 비교)
    /// </summary>
    public bool IsSameItem(InventoryItem other)
    {
        return !string.IsNullOrEmpty(itemName) && itemName == other.itemName;
    }

    /// <summary>
    /// 아이템을 스택에 추가 (최대 스택 고려)
    /// </summary>
    /// <returns>실제로 추가된 개수</returns>
    public int AddToStack(int amount)
    {
        int canAdd = Mathf.Min(amount, maxStack - quantity);
        quantity += canAdd;
        return canAdd;
    }

    /// <summary>
    /// 아이템 제거
    /// </summary>
    /// <returns>실제로 제거된 개수</returns>
    public int RemoveFromStack(int amount)
    {
        int canRemove = Mathf.Min(amount, quantity);
        quantity -= canRemove;

        // 수량이 0이 되면 빈 슬롯으로 만들기
        if (quantity <= 0)
        {
            Clear();
        }

        return canRemove;
    }

    /// <summary>
    /// 슬롯을 비우기
    /// </summary>
    public void Clear()
    {
        itemName = "";
        itemIcon = null;
        quantity = 0;
        description = "";
    }

    /// <summary>
    /// 아이템 복사
    /// </summary>
    public InventoryItem Clone()
    {
        return new InventoryItem(itemName, itemIcon, quantity, description, maxStack);
    }
}
