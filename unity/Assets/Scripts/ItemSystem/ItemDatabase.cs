using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 모든 아이템 메타데이터를 관리하는 ScriptableObject
/// Inspector에서 아이템 정보를 설정하고 전역에서 참조
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("아이템 목록")]
    public List<ItemData> items = new List<ItemData>();

    private Dictionary<string, ItemData> itemDictionary;

    void OnEnable()
    {
        BuildDictionary();
    }

    /// <summary>
    /// 아이템 딕셔너리 구축 (빠른 조회를 위해)
    /// </summary>
    private void BuildDictionary()
    {
        itemDictionary = new Dictionary<string, ItemData>();

        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.itemName))
            {
                itemDictionary[item.itemName] = item;
            }
        }
    }

    /// <summary>
    /// 아이템명으로 아이템 데이터 조회
    /// </summary>
    public ItemData GetItemData(string itemName)
    {
        if (itemDictionary == null)
            BuildDictionary();

        return itemDictionary.TryGetValue(itemName, out ItemData data) ? data : null;
    }

    /// <summary>
    /// 모든 아이템 이름 리스트 반환
    /// </summary>
    public List<string> GetAllItemNames()
    {
        List<string> names = new List<string>();
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.itemName))
                names.Add(item.itemName);
        }
        return names;
    }

    /// <summary>
    /// 아이템이 존재하는지 확인
    /// </summary>
    public bool HasItem(string itemName)
    {
        return GetItemData(itemName) != null;
    }

    /// <summary>
    /// ChestItem 생성 헬퍼 메서드
    /// </summary>
    public ChestItem CreateChestItem(string itemName, int quantity = 1)
    {
        var itemData = GetItemData(itemName);
        if (itemData == null)
        {
            Debug.LogError($"[ItemDatabase] 아이템을 찾을 수 없습니다: {itemName}");
            return null;
        }

        return new ChestItem(itemData.itemName, itemData.itemIcon, quantity, itemData.description);
    }

    /// <summary>
    /// InventoryItem 생성 헬퍼 메서드 -> 팩토리 역할
    /// </summary>
    public InventoryItem CreateInventoryItem(string itemName, int quantity = 1)
    {
        var itemData = GetItemData(itemName);
        if (itemData == null)
        {
            Debug.LogError($"[ItemDatabase] 아이템을 찾을 수 없습니다: {itemName}");
            return null;
        }

        return new InventoryItem(
            itemData.itemName,
            itemData.itemIcon,
            quantity,
            itemData.description,
            itemData.maxStack
        );
    }

    /// <summary>
    /// 디버그: 모든 아이템 정보 출력
    /// </summary>
    [ContextMenu("Print All Items")]
    public void PrintAllItems()
    {
        Debug.Log("=== Item Database ===");
        foreach (var item in items)
        {
            Debug.Log(
                $"아이템: {item.itemName}, 최대 스택: {item.maxStack}, 설명: {item.description}"
            );
        }
    }
}
