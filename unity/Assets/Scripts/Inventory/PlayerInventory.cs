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
    [SerializeField]
    private int inventorySlots = 3; // 고정 3개 슬롯

    [Header("디버그 (읽기 전용)")]
    [SerializeField]
    private InventoryItem[] items; // Inspector에서 확인용

    // 인벤토리 변경 이벤트
    public event Action<int> OnInventoryChanged; // 슬롯 인덱스를 매개변수로 전달
    public event Action<string> OnInventoryMessage; // 메시지 표시용

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            InitializeInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 서버 응답 이벤트 구독
        if (ServerSimulator.Instance != null)
        {
            ServerSimulator.Instance.OnActionResponse += OnServerActionResponse;
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (ServerSimulator.Instance != null)
        {
            ServerSimulator.Instance.OnActionResponse -= OnServerActionResponse;
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
                // 아이콘 정보가 없으면 새로운 아이템의 아이콘 정보로 업데이트
                if (items[i].itemIcon == null && item.itemIcon != null)
                {
                    items[i].itemIcon = item.itemIcon;
                    Debug.Log($"[PlayerInventory] 슬롯 {i}에 아이콘 정보 추가: {item.itemName}");
                }
                
                int added = items[i].AddToStack(remainingAmount);
                remainingAmount -= added;

                if (added > 0)
                {
                    OnInventoryChanged?.Invoke(i);
                    Debug.Log($"[PlayerInventory] 슬롯 {i}에 {item.itemName} {added}개 스택 추가, 아이콘: {(items[i].itemIcon != null ? "있음" : "없음")}");
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
                    Debug.Log(
                        $"[PlayerInventory] 슬롯 {i}에 {item.itemName} {remainingAmount}개 새로 추가, 아이콘: {(items[i].itemIcon != null ? "있음" : "없음")}"
                    );
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
                OnInventoryMessage?.Invoke(
                    $"인벤토리 가득참! {item.itemName} {remainingAmount}개 버려짐"
                );
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
    /// 슬롯 0의 아이템 사용 (서버 전송용)
    /// </summary>
    public void UseSlot0Item()
    {
        if (items[0].IsEmpty())
        {
            Debug.Log("[PlayerInventory] 슬롯 0이 비어있어 사용할 수 없습니다.");
            OnInventoryMessage?.Invoke("사용할 아이템이 없습니다.");
            return;
        }

        var item = items[0];
        Debug.Log($"[PlayerInventory] 슬롯 0 아이템 사용: {item.itemName}");

        // 서버에 아이템 사용 요청
        if (ServerSimulator.Instance != null)
        {
            ServerSimulator.Instance.RequestUseItem(item.itemName, 1);
        }
        else
        {
            Debug.LogError("[PlayerInventory] ServerSimulator를 찾을 수 없습니다!");
            // 로컬에서 직접 처리 (폴백)
            UseItemLocal(0);
        }
        
        OnInventoryMessage?.Invoke($"{item.itemName} 사용");
    }

    /// <summary>
    /// 특정 슬롯의 아이템을 슬롯 0과 위치 교체
    /// </summary>
    public void SwapWithSlot0(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots || slotIndex == 0)
            return;

        if (items[slotIndex].IsEmpty())
        {
            Debug.Log($"[PlayerInventory] 슬롯 {slotIndex}이 비어있어 교체할 수 없습니다.");
            return;
        }

        Debug.Log($"[PlayerInventory] 슬롯 {slotIndex}와 슬롯 0 아이템 교체");

        // 슬롯 교체
        var temp = items[0].Clone();
        items[0] = items[slotIndex].Clone();
        items[slotIndex] = temp;

        // UI 업데이트
        OnInventoryChanged?.Invoke(0);
        OnInventoryChanged?.Invoke(slotIndex);

        string message = items[0].IsEmpty() ? 
            "빈 슬롯과 교체됨" : 
            $"{items[0].itemName}이(가) 활성 슬롯으로 이동";
        OnInventoryMessage?.Invoke(message);
    }

    /// <summary>
    /// 로컬에서 아이템 사용 처리 (수량 감소 및 정리)
    /// </summary>
    private void UseItemLocal(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots || items[slotIndex].IsEmpty())
            return;

        // 아이템 수량 감소
        items[slotIndex].RemoveFromStack(1);
        
        Debug.Log($"[PlayerInventory] 슬롯 {slotIndex} 아이템 사용 후 수량: {items[slotIndex].quantity}");

        // 슬롯 0이 비었으면 다른 아이템들을 앞으로 이동
        if (slotIndex == 0 && items[0].IsEmpty())
        {
            CompactInventory();
        }

        OnInventoryChanged?.Invoke(slotIndex);
    }

    /// <summary>
    /// 빈 슬롯을 제거하고 아이템들을 앞으로 이동
    /// </summary>
    private void CompactInventory()
    {
        Debug.Log("[PlayerInventory] 인벤토리 정리 시작");

        // 빈 슬롯이 아닌 아이템들만 앞쪽으로 이동
        var compactedItems = new InventoryItem[inventorySlots];
        for (int i = 0; i < inventorySlots; i++)
        {
            compactedItems[i] = new InventoryItem(); // 빈 아이템으로 초기화
        }

        int writeIndex = 0;
        for (int i = 0; i < inventorySlots; i++)
        {
            if (!items[i].IsEmpty())
            {
                compactedItems[writeIndex] = items[i].Clone();
                writeIndex++;
            }
        }

        // 기존 배열 교체
        items = compactedItems;

        // 모든 슬롯 UI 업데이트
        for (int i = 0; i < inventorySlots; i++)
        {
            OnInventoryChanged?.Invoke(i);
        }

        Debug.Log("[PlayerInventory] 인벤토리 정리 완료");
    }

    /// <summary>
    /// 서버 액션 응답 처리
    /// </summary>
    private void OnServerActionResponse(object responseObj)
    {
        if (responseObj == null)
            return;

        // 리플렉션으로 응답 처리
        var responseType = responseObj.GetType();
        var actionField = responseType.GetField("action");
        var successField = responseType.GetField("success");
        var dataField = responseType.GetField("data");

        if (actionField == null || successField == null)
            return;

        var actionValue = actionField.GetValue(responseObj);
        var successValue = (bool)successField.GetValue(responseObj);

        // Use 액션인지 확인
        if (actionValue.ToString() == "Use")
        {
            if (successValue && dataField != null)
            {
                // 서버에서 성공적으로 처리됨 - 플레이어 인벤토리 동기화
                var dataValue = dataField.GetValue(responseObj);
                if (dataValue != null)
                {
                    SyncWithServerInventory(dataValue);
                }
            }
            else
            {
                Debug.LogWarning("[PlayerInventory] 서버에서 아이템 사용 실패");
                OnInventoryMessage?.Invoke("아이템 사용 실패");
            }
        }
    }

    /// <summary>
    /// 서버 인벤토리 상태와 동기화
    /// </summary>
    private void SyncWithServerInventory(object dataValue)
    {
        try
        {
            var dataType = dataValue.GetType();
            var playerInventoryField = dataType.GetField("playerInventory");
            
            if (playerInventoryField != null)
            {
                var playerInventoryValue = playerInventoryField.GetValue(dataValue);
                if (playerInventoryValue != null)
                {
                    var playerInventoryType = playerInventoryValue.GetType();
                    var slotsField = playerInventoryType.GetField("slots");
                    
                    if (slotsField != null)
                    {
                        var slotsValue = slotsField.GetValue(playerInventoryValue);
                        if (slotsValue != null && slotsValue is Array slotsArray)
                        {
                            Debug.Log("[PlayerInventory] 서버 인벤토리와 동기화 중...");
                            
                            // 서버 데이터로 인벤토리 업데이트
                            for (int i = 0; i < Math.Min(slotsArray.Length, items.Length); i++)
                            {
                                var slotData = slotsArray.GetValue(i);
                                if (slotData != null)
                                {
                                    var slotType = slotData.GetType();
                                    var isEmptyField = slotType.GetField("isEmpty");
                                    var itemNameField = slotType.GetField("itemName");
                                    var quantityField = slotType.GetField("quantity");
                                    
                                    if (isEmptyField != null && itemNameField != null && quantityField != null)
                                    {
                                        bool isEmpty = (bool)isEmptyField.GetValue(slotData);
                                        
                                        if (isEmpty)
                                        {
                                            items[i].Clear();
                                        }
                                        else
                                        {
                                            string itemName = itemNameField.GetValue(slotData)?.ToString();
                                            int quantity = (int)quantityField.GetValue(slotData);
                                            
                                            if (!string.IsNullOrEmpty(itemName) && quantity > 0)
                                            {
                                                // GlobalItemManager에서 아이콘과 설명 가져오기
                                                if (GlobalItemManager.Instance != null)
                                                {
                                                    var itemData = GlobalItemManager.Instance.GetItemData(itemName);
                                                    if (itemData != null)
                                                    {
                                                        items[i] = new InventoryItem(
                                                            itemName,
                                                            itemData.itemIcon,
                                                            quantity,
                                                            itemData.description,
                                                            3
                                                        );
                                                    }
                                                    else
                                                    {
                                                        // 아이콘 정보가 없어도 기본 정보로 생성
                                                        items[i] = new InventoryItem(itemName, null, quantity, "", 3);
                                                    }
                                                }
                                                else
                                                {
                                                    items[i] = new InventoryItem(itemName, null, quantity, "", 3);
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // 슬롯 UI 업데이트
                                OnInventoryChanged?.Invoke(i);
                            }
                            
                            Debug.Log("[PlayerInventory] 서버 동기화 완료");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerInventory] 서버 동기화 중 오류: {ex.Message}");
        }
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
