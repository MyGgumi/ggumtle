using UnityEngine;

/// <summary>
/// 아이템의 기본 메타데이터
/// </summary>
[System.Serializable]
public class ItemData
{
    [Header("기본 정보")]
    public string itemName;
    public Sprite itemIcon;
    public string description;

    [Header("스택 정보")]
    public int maxStack = 99;

    [Header("아이템 타입")]
    public ItemType itemType = ItemType.Consumable;

    [Header("기타")]
    public bool isConsumable = true;
    public float weight = 1.0f;

    public ItemData()
    {
        itemName = "";
        itemIcon = null;
        description = "";
        maxStack = 99;
        itemType = ItemType.Consumable;
        isConsumable = true;
        weight = 1.0f;
    }

    public ItemData(
        string name,
        Sprite icon,
        string desc = "",
        int stack = 99,
        ItemType type = ItemType.Consumable
    )
    {
        itemName = name;
        itemIcon = icon;
        description = desc;
        maxStack = stack;
        itemType = type;
        isConsumable = (type == ItemType.Consumable);
        weight = 1.0f;
    }
}