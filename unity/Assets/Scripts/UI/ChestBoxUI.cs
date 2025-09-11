using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChestBoxUI : MonoBehaviour
{
    [Header("UI 패널")]
    public GameObject chestBoxPanel;
    public Transform itemsContainer;
    public GameObject itemSlotPrefab;
    public Button closeButton;

    [Header("고정 슬롯들 (9개)")]
    public ChestItemSlot[] fixedItemSlots = new ChestItemSlot[9];

    [Header("상자 정보")]
    public Text chestTitleText;
    public Text itemCountText;

    private InteractableChest currentChest;
    private List<GameObject> itemSlots = new List<GameObject>();

    void Start()
    {
        // 초기화 - 기본적으로 비활성화
        HideChestBox();

        // 닫기 버튼 이벤트 연결
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        // 서버 응답 이벤트 연결
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.OnActionResponse += OnServerActionResponse;
        }
    }

    void OnDestroy()
    {
        // 이벤트 연결 해제
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.OnActionResponse -= OnServerActionResponse;
        }
    }

    public void ShowChestInventory(InteractableChest chest)
    {
        if (chest == null)
            return;

        currentChest = chest;

        // UI 패널 활성화
        if (chestBoxPanel != null)
            chestBoxPanel.SetActive(true);

        // 상자 제목 설정
        if (chestTitleText != null)
            chestTitleText.text = "상자";

        Debug.Log($"[ChestBoxUI] 상자 UI 표시 - 서버에서 최신 데이터 요청");

        // 서버에서 최신 상자 데이터 요청
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.RequestOpenChest(chest.chestId);
        }
        else
        {
            Debug.LogError("[ChestBoxUI] ServerSyncManager를 찾을 수 없음 - 로컬 데이터로 표시");
            RefreshItemList(); // 서버 없으면 로컬 데이터로 표시
        }
    }

    public void HideChestBox()
    {
        if (chestBoxPanel != null)
            chestBoxPanel.SetActive(false);

        currentChest = null;
    }

    private void RefreshItemList()
    {
        if (currentChest == null)
            return;

        // ChestInventoryManager 또는 서버로부터 최신 아이템 리스트 가져오기
        var chestItems = currentChest.GetChestItems();

        // 1. 모든 고정 슬롯을 순회 (0부터 8번까지)
        for (int i = 0; i < fixedItemSlots.Length; i++)
        {
            // 해당 슬롯이 Inspector에 할당되어 있는지 확인
            if (fixedItemSlots[i] == null)
                continue;

            // 2. 현재 인덱스(i)에 해당하는 아이템 데이터가 있는지 확인
            if (i < chestItems.Count)
            {
                // 아이템 데이터가 있으면: 슬롯에 아이템 정보를 설정 (UI 업데이트)
                fixedItemSlots[i].SetupItem(chestItems[i], i, this);
            }
            else
            {
                // 아이템 데이터가 없으면: 슬롯을 빈 상태로 만듦
                fixedItemSlots[i].ClearSlot();
            }
        }

        // 아이템 개수 텍스트 업데이트 (이 부분은 그대로 유지)
        if (itemCountText != null)
        {
            itemCountText.text = $"아이템: {chestItems.Count}개";
        }
    }

    public void TakeItem(int index)
    {
        if (currentChest == null)
            return;

        var chestItems = currentChest.GetChestItems();
        if (index < 0 || index >= chestItems.Count)
            return;

        ChestItem item = chestItems[index];

        // Light(빛)인 경우 ResourceManager로, 다른 아이템은 PlayerInventory로  
        bool canAdd = false;
        if (item.itemName == "Light" || item.itemName == "Mushroom") // Mushroom도 Light로 처리
        {
            // Light는 ResourceManager로 (제한 없음)
            canAdd = true;
            Debug.Log($"[ChestBoxUI] {item.itemName} {item.quantity}개 - Light로 ResourceManager에 추가될 예정");
        }
        else
        {
            // 다른 아이템은 기존 PlayerInventory로
            if (PlayerInventory.Instance != null)
            {
                canAdd = PlayerInventory.Instance.CanAddItem(item.itemName, item.quantity);
                if (!canAdd)
                {
                    Debug.LogWarning(
                        $"[ChestBoxUI] {item.itemName} x{item.quantity} 획득 불가 - 인벤토리 가득 참 또는 스택 제한"
                    );
                    return;
                }
            }
        }

        // 실시간 멀티플레이어: 서버에 직접 요청
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.RequestTakeItem(
                currentChest.chestId,
                item.itemName,
                item.quantity
            );

            Debug.Log(
                $"[ChestBoxUI] 서버에 아이템 획득 요청: {item.itemName} x{item.quantity} - 서버 응답 대기"
            );

            // 예측적 UI 업데이트 제거 - 서버 응답에서만 UI 갱신
        }
        else
        {
            Debug.LogError("[ChestBoxUI] ServerSyncManager를 찾을 수 없습니다!");
        }
    }

    public void TakeAllItems()
    {
        if (currentChest == null)
            return;

        Debug.Log("[ChestBoxUI] 모든 아이템 가져오기 시도 (서버 요청)");

        var chestItems = currentChest.GetChestItems();

        // 실시간 멀티플레이어: 각 아이템을 개별적으로 서버에 요청
        if (ServerSyncManager.Instance != null)
        {
            for (int i = 0; i < chestItems.Count; i++)
            {
                var item = chestItems[i];
                ServerSyncManager.Instance.RequestTakeItem(
                    currentChest.chestId,
                    item.itemName,
                    item.quantity
                );
            }
        }

        Debug.Log($"[ChestBoxUI] {chestItems.Count}개 아이템 획득 요청 전송 완료");
    }

    private void OnCloseButtonClicked()
    {
        // 상자 닫기 (이것이 UI를 자동으로 닫을 것임)
        if (currentChest != null)
        {
            currentChest.CloseChest();
        }

        // 또는 직접 UI 닫기
        HideChestBox();

        // UIManager를 통해 오버레이 닫기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseOverlay(chestBoxPanel);
        }
    }

    public void OnTakeAllButtonPressed()
    {
        TakeAllItems();
    }

    /// <summary>
    /// 서버 액션 응답 처리
    /// </summary>
    private void OnServerActionResponse(object responseObj)
    {
        if (currentChest == null || responseObj == null)
            return;

        // 리플렉션으로 ItemActionResponse 처리
        var responseType = responseObj.GetType();

        // action 필드 가져오기
        var actionField = responseType.GetField("action");
        var successField = responseType.GetField("success");
        var messageField = responseType.GetField("message");
        var dataField = responseType.GetField("data");

        if (actionField == null || successField == null)
            return;

        var actionValue = actionField.GetValue(responseObj);
        var successValue = (bool)successField.GetValue(responseObj);
        var messageValue = messageField?.GetValue(responseObj)?.ToString() ?? "";

        // Take, Put, OpenChest 액션인지 확인
        bool isChestRelatedAction =
            actionValue.ToString() == "Take"
            || actionValue.ToString() == "Put"
            || actionValue.ToString() == "OpenChest";

        if (!isChestRelatedAction)
        {
            Debug.Log($"[ChestBoxUI] 상자와 관련없는 액션 무시: {actionValue}");
            return;
        }

        // 현재 상자와 관련된 응답인지 확인
        bool isCurrentChestResponse = false;
        if (dataField != null)
        {
            var dataValue = dataField.GetValue(responseObj);
            if (dataValue != null)
            {
                var dataType = dataValue.GetType();
                var chestInventoryField = dataType.GetField("chestInventory");
                if (chestInventoryField != null)
                {
                    var chestInventoryValue = chestInventoryField.GetValue(dataValue);
                    if (chestInventoryValue != null)
                    {
                        var chestInventoryType = chestInventoryValue.GetType();
                        var chestIdField = chestInventoryType.GetField("chestId");
                        if (chestIdField != null)
                        {
                            var chestIdValue = chestIdField
                                .GetValue(chestInventoryValue)
                                ?.ToString();
                            isCurrentChestResponse = chestIdValue == currentChest.chestId;
                        }
                    }
                }
            }
        }

        if (isCurrentChestResponse)
        {
            if (successValue)
            {
                string actionType = actionValue.ToString();
                Debug.Log($"[ChestBoxUI] 서버 응답 성공 - {actionType} 액션: {messageValue}");

                // 액션 타입에 따른 처리
                if (actionType == "OpenChest")
                {
                    // 상자 열기 성공 - UI 갱신만
                    RefreshItemList();
                }
                else if (actionType == "Take")
                {
                    // Take 액션 - 상자→인벤토리 이동
                    RefreshItemList();

                    // 서버 응답에서 아이템 정보 추출 후 ResourceManager에 Light 추가
                    Debug.Log($"[ChestBoxUI] Take 액션 처리 시작 - dataField: {dataField != null}");
                    
                    if (dataField != null)
                    {
                        var dataValue = dataField.GetValue(responseObj);
                        Debug.Log($"[ChestBoxUI] dataValue 추출됨: {dataValue != null}");
                        
                        if (dataValue != null)
                        {
                            ProcessTakenItem(dataValue);
                        }
                        else
                        {
                            Debug.LogWarning("[ChestBoxUI] dataValue가 null입니다!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[ChestBoxUI] dataField가 null입니다!");
                    }
                    
                    Debug.Log($"[ChestBoxUI] Take 액션 - 상자에서 아이템 획득: {messageValue}");
                }
                else if (actionType == "Put")
                {
                    // Put 액션 - 인벤토리→상자 이동
                    RefreshItemList();
                    Debug.Log($"[ChestBoxUI] Put 액션 - 상자에 아이템 추가: {messageValue}");
                }
                else
                {
                    // 기타 액션 - 기본 UI 갱신
                    RefreshItemList();
                }
            }
            else
            {
                // 서버에서 실패 - 에러 메시지 표시
                Debug.LogWarning($"[ChestBoxUI] 서버 요청 실패: {messageValue}");

                // UI를 다시 원래 상태로 되돌리기
                RefreshItemList();
            }
        }
    }

    // /// <summary>
    // /// 서버 응답에서 받은 아이템을 플레이어 인벤토리에 추가
    // /// </summary>
    // private void AddItemToPlayerInventory(object dataValue)
    // {
    //     if (dataValue == null || PlayerInventory.Instance == null)
    //         return;

    //     try
    //     {
    //         // 리플렉션으로 takenItem 정보 추출
    //         var dataType = dataValue.GetType();
    //         var takenItemField = dataType.GetField("takenItem");

    //         if (takenItemField != null)
    //         {
    //             var takenItemValue = takenItemField.GetValue(dataValue);
    //             if (takenItemValue != null)
    //             {
    //                 var takenItemType = takenItemValue.GetType();
    //                 var itemNameField = takenItemType.GetField("itemName");
    //                 var quantityField = takenItemType.GetField("quantity");

    //                 if (itemNameField != null && quantityField != null)
    //                 {
    //                     string itemName = itemNameField.GetValue(takenItemValue)?.ToString();
    //                     int quantity = (int)quantityField.GetValue(takenItemValue);

    //                     if (!string.IsNullOrEmpty(itemName) && quantity > 0)
    //                     {
    //                         // GlobalItemManager에서 아이템 데이터 찾기
    //                         if (GlobalItemManager.Instance != null)
    //                         {
    //                             var itemData = GlobalItemManager.Instance.GetItemData(itemName);
    //                             if (itemData != null)
    //                             {
    //                                 // ChestItem 생성
    //                                 ChestItem chestItem = new ChestItem
    //                                 {
    //                                     itemName = itemName,
    //                                     itemIcon = itemData.itemIcon,
    //                                     quantity = quantity,
    //                                     description = itemData.description,
    //                                 };

    //                                 // 플레이어 인벤토리에 추가
    //                                 int addedCount = PlayerInventory.Instance.TryAddItem(chestItem);

    //                                 Debug.Log(
    //                                     $"[ChestBoxUI] 플레이어 인벤토리에 {itemName} {addedCount}개 추가됨"
    //                                 );

    //                                 if (addedCount < quantity)
    //                                 {
    //                                     Debug.LogWarning(
    //                                         $"[ChestBoxUI] {itemName} {quantity - addedCount}개는 인벤토리가 가득 찼습니다!"
    //                                     );
    //                                 }
    //                             }
    //                             else
    //                             {
    //                                 Debug.LogError(
    //                                     $"[ChestBoxUI] GlobalItemManager에서 {itemName} 아이템 데이터를 찾을 수 없습니다!"
    //                                 );
    //                             }
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogError($"[ChestBoxUI] 플레이어 인벤토리 아이템 추가 중 오류: {ex.Message}");
    //     }
    // }

    /// <summary>
    /// Take 액션에서 획득한 아이템 처리 - Light/Mushroom인 경우 ResourceManager에 추가
    /// </summary>
    private void ProcessTakenItem(object dataValue)
    {
        Debug.Log($"[ChestBoxUI] ProcessTakenItem 시작 - dataValue 타입: {dataValue?.GetType().Name}");
        
        try
        {
            // 리플렉션으로 서버 응답 데이터에서 아이템 정보 추출
            var dataType = dataValue.GetType();
            Debug.Log($"[ChestBoxUI] dataType: {dataType.Name}");
            
            // 모든 필드 출력해서 구조 확인
            var allFields = dataType.GetFields();
            Debug.Log($"[ChestBoxUI] ItemActionData 필드들: {string.Join(", ", System.Array.ConvertAll(allFields, f => f.Name))}");
            
            var takenItemField = dataType.GetField("takenItem");
            Debug.Log($"[ChestBoxUI] takenItemField 찾음: {takenItemField != null}");
            
            // takenItem이 없으면 다른 가능한 필드명들 시도
            if (takenItemField == null)
            {
                // 가능한 다른 필드명들 시도
                takenItemField = dataType.GetField("item") ?? 
                                dataType.GetField("itemData") ?? 
                                dataType.GetField("actionItem");
                Debug.Log($"[ChestBoxUI] 대체 필드 찾음: {takenItemField?.Name ?? "없음"}");
            }
            
            if (takenItemField != null)
            {
                var takenItemValue = takenItemField.GetValue(dataValue);
                if (takenItemValue != null)
                {
                    var takenItemType = takenItemValue.GetType();
                    var itemNameField = takenItemType.GetField("itemName");
                    var quantityField = takenItemType.GetField("quantity");
                    
                    if (itemNameField != null && quantityField != null)
                    {
                        string itemName = itemNameField.GetValue(takenItemValue)?.ToString();
                        int quantity = (int)quantityField.GetValue(takenItemValue);
                        
                        // Light 또는 Mushroom인 경우 Light로 처리
                        if (itemName == "Light" || itemName == "Mushroom")
                        {
                            Debug.Log($"[ChestBoxUI] {itemName} {quantity}개를 Light로 처리 시작 - ResourceManager.Instance: {ResourceManager.Instance != null}");
                            
                            if (ResourceManager.Instance != null)
                            {
                                int addedAmount = ResourceManager.Instance.AddLight(quantity);
                                Debug.Log($"[ChestBoxUI] ResourceManager에 Light {addedAmount}개 추가 완료 (원본 아이템: {itemName})");
                            }
                            else
                            {
                                Debug.LogError("[ChestBoxUI] ResourceManager.Instance가 null입니다! UniversalHUDController가 제대로 초기화되지 않았을 가능성");
                            }
                        }
                        else
                        {
                            // 다른 아이템은 PlayerInventory가 서버 응답에서 자동으로 처리
                            Debug.Log($"[ChestBoxUI] 일반 아이템 {itemName} x{quantity}는 PlayerInventory에서 처리됨");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ChestBoxUI] 서버 응답 데이터 파싱 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 현재 열린 상자의 ID 반환
    /// </summary>
    public string GetCurrentChestId()
    {
        return currentChest?.chestId ?? "";
    }
}
