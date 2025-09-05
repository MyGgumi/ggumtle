using System;
using UnityEngine;

/// <summary>
/// 플레이어의 3개 고정 슬롯 인벤토리 시스템
/// 싱글톤 패턴으로 전역에서 접근 가능
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    [Header("인벤토리 설정")]
    [SerializeField] private int inventorySlots = 3; // 고정 3개 슬롯

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private InventoryItem[] items; // Inspector에서 확인용

    // 인벤토리 변경 이벤트
    public event Action<int> OnInventoryChanged; // 슬롯 인덱스를 매개변수로 전달
    public event Action<string> OnInventoryMessage; // 메시지 표시용

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 인벤토리 초기화
    /// </summary>
    private void InitializeInventory()
    {
        items = new InventoryItem[inventorySlots];
        
        for (int i = 0; i < inventorySlots; i++)
        {
            items[i] = new InventoryItem();
        }
        
        Debug.Log($"[PlayerInventory] {inventorySlots}개 슬롯 인벤토리 초기화 완료");
    }

    /// <summary>
    /// 아이템 추가 시도
    /// </summary>
    /// <param name="chestItem">상자에서 가져온 아이템</param>
    /// <returns>성공적으로 추가된 개수</returns>
    public int TryAddItem(ChestItem chestItem)
    {
        if (chestItem == null || chestItem.quantity <= 0)
            return 0;

        InventoryItem newItem = InventoryItem.FromChestItem(chestItem);
        return TryAddItem(newItem);
    }

    /// <summary>
    /// 아이템 추가 시도
    /// </summary>
    /// <param name="item">추가할 아이템</param>
    /// <returns>성공적으로 추가된 개수</returns>
    public int TryAddItem(InventoryItem item)
    {
        if (item == null || item.IsEmpty())
            return 0;

        int remainingAmount = item.quantity;
        
        // 1단계: 같은 아이템이 있는 슬롯에 스택
        for (int i = 0; i < inventorySlots; i++)
        {
            if (!items[i].IsEmpty() && items[i].IsSameItem(item))
            {
                int added = items[i].AddToStack(remainingAmount);
                remainingAmount -= added;
                
                if (added > 0)
                {
                    OnInventoryChanged?.Invoke(i);
                    Debug.Log($"[PlayerInventory] 슬롯 {i}에 {item.itemName} {added}개 스택 추가");
                }
                
                if (remainingAmount <= 0)
                    break;
            }
        }
        
        // 2단계: 빈 슬롯에 새로 추가
        if (remainingAmount > 0)
        {
            for (int i = 0; i < inventorySlots; i++)
            {
                if (items[i].IsEmpty())
                {
                    items[i] = new InventoryItem(
                        item.itemName,
                        item.itemIcon,
                        remainingAmount,
                        item.description,
                        item.maxStack
                    );
                    
                    OnInventoryChanged?.Invoke(i);
                    Debug.Log($"[PlayerInventory] 슬롯 {i}에 {item.itemName} {remainingAmount}개 새로 추가");
                    remainingAmount = 0;
                    break;
                }
            }
        }
        
        int totalAdded = item.quantity - remainingAmount;
        
        // 결과 메시지
        if (totalAdded > 0)
        {
            OnInventoryMessage?.Invoke($"{item.itemName} {totalAdded}개 획득!");
            
            if (remainingAmount > 0)
            {
                OnInventoryMessage?.Invoke($"인벤토리 가득참! {item.itemName} {remainingAmount}개 버려짐");
            }
        }
        else
        {
            OnInventoryMessage?.Invoke("인벤토리가 가득 차서 아이템을 가져올 수 없습니다!");
        }
        
        return totalAdded;
    }

    /// <summary>
    /// 특정 슬롯의 아이템 가져오기
    /// </summary>
    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots)
            return null;
            
        return items[slotIndex];
    }

    /// <summary>
    /// 특정 슬롯의 아이템 제거
    /// </summary>
    public bool RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots || items[slotIndex].IsEmpty())
            return false;

        int removed = items[slotIndex].RemoveFromStack(amount);
        
        if (removed > 0)
        {
            OnInventoryChanged?.Invoke(slotIndex);
            Debug.Log($"[PlayerInventory] 슬롯 {slotIndex}에서 {removed}개 제거");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 특정 슬롯 비우기
    /// </summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots)
            return;
            
        if (!items[slotIndex].IsEmpty())
        {
            items[slotIndex].Clear();
            OnInventoryChanged?.Invoke(slotIndex);
            Debug.Log($"[PlayerInventory] 슬롯 {slotIndex} 비우기");
        }
    }

    /// <summary>
    /// 전체 인벤토리 비우기
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < inventorySlots; i++)
        {
            if (!items[i].IsEmpty())
            {
                items[i].Clear();
                OnInventoryChanged?.Invoke(i);
            }
        }
        
        OnInventoryMessage?.Invoke("인벤토리를 모두 비웠습니다.");
        Debug.Log("[PlayerInventory] 전체 인벤토리 초기화");
    }

    /// <summary>
    /// 빈 슬롯 개수 확인
    /// </summary>
    public int GetEmptySlotCount()
    {
        int emptyCount = 0;
        for (int i = 0; i < inventorySlots; i++)
        {
            if (items[i].IsEmpty())
                emptyCount++;
        }
        return emptyCount;
    }

    /// <summary>
    /// 인벤토리가 가득 찼는지 확인
    /// </summary>
    public bool IsFull()
    {
        return GetEmptySlotCount() == 0;
    }

    /// <summary>
    /// 특정 아이템의 총 개수 확인
    /// </summary>
    public int GetItemCount(string itemName)
    {
        int totalCount = 0;
        for (int i = 0; i < inventorySlots; i++)
        {
            if (items[i].itemName == itemName)
                totalCount += items[i].quantity;
        }
        return totalCount;
    }

    /// <summary>
    /// 디버그: 인벤토리 상태 출력
    /// </summary>
    [ContextMenu("Print Inventory Status")]
    public void PrintInventoryStatus()
    {
        Debug.Log("=== Player Inventory Status ===");
        for (int i = 0; i < inventorySlots; i++)
        {
            if (items[i].IsEmpty())
            {
                Debug.Log($"슬롯 {i}: [빈 슬롯]");
            }
            else
            {
                Debug.Log($"슬롯 {i}: {items[i].itemName} x{items[i].quantity}");
            }
        }
        Debug.Log($"빈 슬롯: {GetEmptySlotCount()}개, 가득참: {IsFull()}");
    }
}