using System;
using System.Collections.Generic;
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

    [SerializeField]
    private InventoryItem[] items;

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
    /// 아이템 추가 시도 (각 슬롯별 다른 아이템, 슬롯당 최대 3개)
    /// </summary>
    /// <param name="chestItem">상자에서 가져온 아이템</param>
    /// <returns>성공적으로 추가된 개수</returns>
    public int TryAddItem(ChestItem chestItem)
    {
        if (chestItem == null || chestItem.quantity <= 0)
            return 0;

        // ChestItem을 InventoryItem으로 변환
        InventoryItem item = InventoryItem.FromChestItem(chestItem);
        int remainingAmount = item.quantity;

        // 1단계: 같은 아이템이 있는 슬롯에 스택 (한 슬롯에만)
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
                    Debug.Log(
                        $"[PlayerInventory] 슬롯 {i}에 {item.itemName} {added}개 스택 추가, 현재 총 {items[i].quantity}개"
                    );
                }

                // 해당 아이템이 있는 슬롯에서만 스택하고 종료 (다른 슬롯에는 같은 아이템 추가 안함)
                break;
            }
        }

        // 2단계: 같은 아이템이 없고 남은 수량이 있으면 빈 슬롯에 새로 추가
        if (remainingAmount > 0)
        {
            for (int i = 0; i < inventorySlots; i++)
            {
                if (items[i].IsEmpty())
                {
                    int quantityToAdd = Math.Min(remainingAmount, 3); // 슬롯당 최대 3개
                    items[i] = new InventoryItem(
                        item.itemName,
                        item.itemIcon,
                        quantityToAdd,
                        item.description,
                        3 // maxStack을 3으로 고정
                    );

                    OnInventoryChanged?.Invoke(i);
                    Debug.Log(
                        $"[PlayerInventory] 슬롯 {i}에 {item.itemName} {quantityToAdd}개 새로 추가"
                    );
                    remainingAmount -= quantityToAdd;
                    break; // 한 슬롯에만 추가하고 종료
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
    /// 아이템을 추가할 수 있는지 확인 (각 슬롯별 다른 아이템, 슬롯당 최대 3개)
    /// </summary>
    public bool CanAddItem(string itemName, int quantity)
    {
        if (string.IsNullOrEmpty(itemName) || quantity <= 0)
            return false;

        // 해당 아이템이 이미 있는 슬롯 찾기
        for (int i = 0; i < inventorySlots; i++)
        {
            if (!items[i].IsEmpty() && items[i].itemName == itemName)
            {
                // 이미 해당 아이템이 있는 슬롯에서 추가 가능한 수량 확인
                int canAdd = items[i].maxStack - items[i].quantity;
                return quantity <= canAdd;
            }
        }

        // 해당 아이템이 없으면 빈 슬롯이 있는지 확인
        int emptySlots = GetEmptySlotCount();
        if (emptySlots > 0)
        {
            // 빈 슬롯에 새로 들어갈 수 있는 최대 수량은 3개
            return quantity <= 3;
        }

        // 빈 슬롯도 없으면 추가 불가
        return false;
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

        string message = items[0].IsEmpty()
            ? "빈 슬롯과 교체됨"
            : $"{items[0].itemName}이(가) 활성 슬롯으로 이동";
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

        Debug.Log(
            $"[PlayerInventory] 슬롯 {slotIndex} 아이템 사용 후 수량: {items[slotIndex].quantity}"
        );

        OnInventoryChanged?.Invoke(slotIndex);
    }

    // CompactInventory 메서드 제거됨 - SyncItemsToClientSlots가 모든 슬롯 배치 처리

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

        // 메시지 필드도 확인
        var messageField = responseType.GetField("message");
        string messageValue = messageField?.GetValue(responseObj)?.ToString() ?? "";

        // Use, Take, Put 액션인지 확인
        if (
            actionValue.ToString() == "Use"
            || actionValue.ToString() == "Take"
            || actionValue.ToString() == "Put"
        )
        {
            if (successValue && dataField != null)
            {
                // 서버에서 성공적으로 처리됨 - 플레이어 인벤토리 동기화
                var dataValue = dataField.GetValue(responseObj);
                if (dataValue != null)
                {
                    SyncWithServerInventory(dataValue);

                    // SyncItemsToClientSlots()가 모든 슬롯 배치를 처리
                    Debug.Log($"[PlayerInventory] {actionValue} 액션 - 서버 동기화 완료");
                }

                // Put 액션 완료 시 드래그 플래그 즉시 해제
                if (actionValue.ToString() == "Put")
                {
                    ResetDragProcessingFlag();
                }
            }
            else
            {
                string actionName = actionValue.ToString() == "Use" ? "아이템 사용" : "아이템 이동";
                Debug.LogWarning($"[PlayerInventory] 서버에서 {actionName} 실패");
                OnInventoryMessage?.Invoke($"{actionName} 실패");
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

                // playerInventory가 null인 경우 (OpenChest 등) 동기화 건너뜀
                if (playerInventoryValue == null)
                {
                    Debug.Log(
                        "[PlayerInventory] 플레이어 인벤토리 데이터가 null - 동기화 건너뜀 (OpenChest 등)"
                    );
                    return;
                }

                // playerInventory가 있으면 동기화 수행
                var playerInventoryType = playerInventoryValue.GetType();
                var slotsField = playerInventoryType.GetField("slots");

                if (slotsField != null)
                {
                    var slotsValue = slotsField.GetValue(playerInventoryValue);
                    if (slotsValue != null && slotsValue is Array slotsArray)
                    {
                        Debug.Log(
                            "[PlayerInventory] 서버 아이템 데이터를 클라이언트 슬롯 배치에 적용 중..."
                        );

                        // 1단계: 서버에서 받은 아이템별 총 수량 계산
                        var serverItems = new Dictionary<string, int>();
                        for (int i = 0; i < slotsArray.Length; i++)
                        {
                            var slotData = slotsArray.GetValue(i);
                            if (slotData != null)
                            {
                                var slotType = slotData.GetType();
                                var isEmptyField = slotType.GetField("isEmpty");
                                var itemNameField = slotType.GetField("itemName");
                                var quantityField = slotType.GetField("quantity");

                                if (
                                    isEmptyField != null
                                    && itemNameField != null
                                    && quantityField != null
                                )
                                {
                                    bool isEmpty = (bool)isEmptyField.GetValue(slotData);
                                    if (!isEmpty)
                                    {
                                        string itemName = itemNameField
                                            .GetValue(slotData)
                                            ?.ToString();
                                        int quantity = (int)quantityField.GetValue(slotData);

                                        if (!string.IsNullOrEmpty(itemName) && quantity > 0)
                                        {
                                            if (serverItems.ContainsKey(itemName))
                                                serverItems[itemName] += quantity;
                                            else
                                                serverItems[itemName] = quantity;

                                            Debug.Log(
                                                $"[PlayerInventory] 서버 아이템 데이터: {itemName} x{quantity}"
                                            );
                                        }
                                    }
                                }
                            }
                        }

                        // 2단계: 클라이언트 슬롯 배치 유지하면서 서버 데이터 적용
                        SyncItemsToClientSlots(serverItems);

                        Debug.Log("[PlayerInventory] 클라이언트 슬롯 배치 유지 동기화 완료");

                        Debug.Log("[PlayerInventory] 서버 동기화 완료");
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
    /// 서버 아이템 데이터를 클라이언트 슬롯 배치에 적용
    /// </summary>
    private void SyncItemsToClientSlots(Dictionary<string, int> serverItems)
    {
        // 현재 클라이언트 슬롯 배치 저장
        var currentSlots = new InventoryItem[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            currentSlots[i] = items[i].Clone();
        }

        // 서버 아이템 분배 작업용 복사본
        var remainingItems = new Dictionary<string, int>(serverItems);

        // 1단계: 현재 슬롯에 있는 아이템들 먼저 처리 (위치 유지)
        for (int i = 0; i < items.Length; i++)
        {
            if (!currentSlots[i].IsEmpty())
            {
                string itemName = currentSlots[i].itemName;

                if (remainingItems.ContainsKey(itemName) && remainingItems[itemName] > 0)
                {
                    // 해당 아이템이 서버에 있으면 수량 업데이트
                    int maxCanHold = currentSlots[i].maxStack;
                    int newQuantity = Math.Min(remainingItems[itemName], maxCanHold);

                    items[i].quantity = newQuantity;
                    remainingItems[itemName] -= newQuantity;

                    Debug.Log($"[PlayerInventory] 슬롯 {i} 유지: {itemName} x{newQuantity}");
                }
                else
                {
                    // 서버에 없는 아이템은 제거
                    items[i].Clear();
                    Debug.Log($"[PlayerInventory] 슬롯 {i} 비우기: {itemName} (서버에 없음)");
                }
            }

            // UI 업데이트
            OnInventoryChanged?.Invoke(i);
        }

        // 2단계: 남은 아이템들을 빈 슬롯에 배치
        foreach (var kvp in remainingItems)
        {
            string itemName = kvp.Key;
            int remainingQuantity = kvp.Value;

            if (remainingQuantity <= 0)
                continue;

            // 빈 슬롯 찾아서 배치
            for (int i = 0; i < items.Length && remainingQuantity > 0; i++)
            {
                if (items[i].IsEmpty())
                {
                    int quantityToAdd = Math.Min(remainingQuantity, 3); // maxStack = 3

                    // 아이템 데이터 가져오기
                    if (GlobalItemManager.Instance != null)
                    {
                        var itemData = GlobalItemManager.Instance.GetItemData(itemName);
                        if (itemData != null)
                        {
                            items[i] = new InventoryItem(
                                itemName,
                                itemData.itemIcon,
                                quantityToAdd,
                                itemData.description,
                                3
                            );
                        }
                        else
                        {
                            items[i] = new InventoryItem(itemName, null, quantityToAdd, "", 3);
                        }
                    }
                    else
                    {
                        items[i] = new InventoryItem(itemName, null, quantityToAdd, "", 3);
                    }

                    remainingQuantity -= quantityToAdd;
                    Debug.Log(
                        $"[PlayerInventory] 슬롯 {i}에 새 아이템 배치: {itemName} x{quantityToAdd}"
                    );

                    // UI 업데이트
                    OnInventoryChanged?.Invoke(i);
                }
            }
        }
    }

    /// <summary>
    /// 드래그 처리 플래그 즉시 해제
    /// </summary>
    private void ResetDragProcessingFlag()
    {
        // DraggableInventorySlot의 static 변수에 접근
        var draggableSlotType = System.Type.GetType("DraggableInventorySlot");
        if (draggableSlotType != null)
        {
            var isAnySlotProcessingField = draggableSlotType.GetField(
                "isAnySlotProcessing",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );

            if (isAnySlotProcessingField != null)
            {
                isAnySlotProcessingField.SetValue(null, false);
                Debug.Log("[PlayerInventory] 드래그 처리 플래그 즉시 해제 (서버 응답)");
            }
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
