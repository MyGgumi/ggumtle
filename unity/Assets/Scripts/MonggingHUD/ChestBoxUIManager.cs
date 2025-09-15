using System;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// MonggingHUD 시스템의 상자 UI 관리 컴포넌트
/// UI Toolkit 기반으로 상자 인벤토리 표시 및 드래그 앤 드롭 처리
/// </summary>
public class ChestBoxUIManager : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    
    [Header("Debug")]
    [SerializeField]
    private bool enableVerboseLogging = false;
    
    // UI 이벤트 콜백 저장 변수들
    private EventCallback<ClickEvent> _closeButtonCallback;
    private EventCallback<DragEnterEvent> _dragEnterCallback;
    private EventCallback<DragLeaveEvent> _dragLeaveCallback;
    private EventCallback<DragUpdatedEvent> _dragUpdatedCallback;
    private EventCallback<DragPerformEvent> _dragPerformCallback;
    private EventCallback<ClickEvent>[] _takeButtonCallbacks = new EventCallback<ClickEvent>[9];
    
    private VisualElement _chestBoxUIRoot; // ChestBoxUI의 root 요소
    private VisualElement _chestBoxPanel;
    private Button _closeButton;

    [Header("Item Slots")]
    private VisualElement[] _itemSlots = new VisualElement[9];
    private VisualElement[] _itemIcons = new VisualElement[9];
    private Button[] _takeButtons = new Button[9];

    [Header("Current State")]
    private InteractableChest _currentChest;
    private bool _isInitialized = false;

    private void Start()
    {
        // 서버 응답 이벤트 연결
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.OnActionResponse += OnServerActionResponse;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 연결 해제
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.OnActionResponse -= OnServerActionResponse;
        }
        
        // UI 이벤트 해제
        UnregisterUIEvents();
    }
    
    private void UnregisterUIEvents()
    {
        // 닫기 버튼 이벤트 해제
        if (_closeButton != null && _closeButtonCallback != null)
        {
            _closeButton.UnregisterCallback(_closeButtonCallback);
        }
        
        // Take 버튼들 이벤트 해제
        for (int i = 0; i < _takeButtons.Length; i++)
        {
            if (_takeButtons[i] != null && _takeButtonCallbacks[i] != null)
            {
                _takeButtons[i].UnregisterCallback(_takeButtonCallbacks[i]);
            }
        }
        
        // 드래그 앤 드롭 이벤트 해제
        if (_chestBoxPanel != null)
        {
            if (_dragEnterCallback != null)
                _chestBoxPanel.UnregisterCallback(_dragEnterCallback);
            if (_dragLeaveCallback != null)
                _chestBoxPanel.UnregisterCallback(_dragLeaveCallback);
            if (_dragUpdatedCallback != null)
                _chestBoxPanel.UnregisterCallback(_dragUpdatedCallback);
            if (_dragPerformCallback != null)
                _chestBoxPanel.UnregisterCallback(_dragPerformCallback);
        }
        
        if (Application.isEditor)
            Debug.Log("[ChestBoxUIManager] UI 이벤트 구독 해제 완료");
    }

    /// <summary>
    /// UniversalHUDController에서 호출하는 초기화 메서드
    /// </summary>
    public void Initialize(VisualElement root)
    {
        // 이미 초기화되었으면 중복 방지
        if (_isInitialized)
        {
            Debug.LogWarning("[ChestBoxUIManager] 이미 초기화되었습니다. 중복 초기화를 방지합니다.");
            return;
        }
        
        _root = root;
        CacheUIElements();
        SetupEvents();
        SetupDropTarget(); // 드래그 앤 드롭 설정

        // 초기 상태: 패널 숨김
        HideChestBox();

        _isInitialized = true;
        if (enableVerboseLogging)
            Debug.Log("[ChestBoxUIManager] UI 초기화 완료");
    }

    /// <summary>
    /// UI 요소들 캐싱
    /// </summary>
    private void CacheUIElements()
    {
        // chestBoxUI 인스턴스 먼저 찾기
        var chestBoxUIInstance = _root.Q<VisualElement>("chestBoxUI");

        if (chestBoxUIInstance == null)
        {
            Debug.LogError("[ChestBoxUIManager] chestBoxUI 인스턴스를 찾을 수 없습니다!");
            return;
        }

        // ChestBoxUI의 root 요소 (Template의 최상위 요소)
        _chestBoxUIRoot = chestBoxUIInstance.Q<VisualElement>("root");

        // 메인 패널 및 닫기 버튼 캐싱 (chestBoxUI 인스턴스 내부에서)
        _chestBoxPanel = chestBoxUIInstance.Q<VisualElement>("chestBoxPanel");
        _closeButton = chestBoxUIInstance.Q<Button>("closeButton");

        Debug.Log(
            $"[ChestBoxUIManager] 메인 요소 캐싱: "
                + $"Root={(_chestBoxUIRoot != null ? "OK" : "NULL")}, "
                + $"패널={(_chestBoxPanel != null ? "OK" : "NULL")}, "
                + $"닫기버튼={(_closeButton != null ? "OK" : "NULL")}"
        );

        // 9개 슬롯 캐싱 (slot0 ~ slot8, chestBoxUI 인스턴스 내부에서)
        for (int i = 0; i < 9; i++)
        {
            _itemSlots[i] = chestBoxUIInstance.Q<VisualElement>($"slot{i}");
            _itemIcons[i] = chestBoxUIInstance.Q<VisualElement>($"itemIcon{i}");
            _takeButtons[i] = chestBoxUIInstance.Q<Button>($"takeButton{i}");

            Debug.Log(
                $"[ChestBoxUIManager] 슬롯 {i} 캐싱: "
                    + $"슬롯={(_itemSlots[i] != null ? "OK" : "NULL")}, "
                    + $"아이콘={(_itemIcons[i] != null ? "OK" : "NULL")}, "
                    + $"버튼={(_takeButtons[i] != null ? "OK" : "NULL")}"
            );
        }
    }

    /// <summary>
    /// UI 이벤트 설정
    /// </summary>
    private void SetupEvents()
    {
        // 기존 이벤트 먼저 해제 (중복 등록 방지)
        UnregisterUIEvents();
        
        // 닫기 버튼 이벤트
        if (_closeButton != null)
        {
            _closeButtonCallback = evt => 
            {
                evt.StopImmediatePropagation(); // 이벤트 전파 중지
                OnCloseButtonClicked();
            };
            _closeButton.RegisterCallback(_closeButtonCallback);
        }

        // 각 슬롯의 Take 버튼 이벤트 설정
        for (int i = 0; i < 9; i++)
        {
            int slotIndex = i; // 클로저를 위한 로컬 변수
            if (_takeButtons[i] != null)
            {
                _takeButtonCallbacks[i] = evt => 
                {
                    evt.StopImmediatePropagation(); // 이벤트 전파 중지
                    TakeItem(slotIndex);
                };
                _takeButtons[i].RegisterCallback(_takeButtonCallbacks[i]);
                
                if (enableVerboseLogging)
                    Debug.Log($"[ChestBoxUIManager] 슬롯 {i} Take 버튼 이벤트 등록 완료");
            }
        }
    }

    /// <summary>
    /// 상자 인벤토리 표시
    /// </summary>
    public void ShowChestInventory(InteractableChest chest)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[ChestBoxUIManager] UI가 초기화되지 않았습니다!");
            return;
        }

        if (chest == null)
            return;

        _currentChest = chest;

        // UI 전체 활성화 (root 요소 표시)
        if (_chestBoxUIRoot != null)
        {
            _chestBoxUIRoot.style.display = DisplayStyle.Flex;
            Debug.Log("[ChestBoxUIManager] ChestBoxUI Root 요소를 표시로 설정");
        }
        else
        {
            Debug.LogError("[ChestBoxUIManager] ChestBoxUI Root 요소가 null입니다!");
        }

        Debug.Log($"[ChestBoxUIManager] 상자 UI 표시 - 서버에서 최신 데이터 요청");

        // 서버에서 최신 상자 데이터 요청
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.RequestOpenChest(chest.chestId);
        }
        else
        {
            Debug.LogError(
                "[ChestBoxUIManager] ServerSyncManager를 찾을 수 없음 - 로컬 데이터로 표시"
            );
            RefreshItemList(); // 서버 없으면 로컬 데이터로 표시
        }

        // UIManager를 통해 오버레이 등록
        if (UIManager.Instance != null && _chestBoxPanel != null)
        {
            // VisualElement를 GameObject로 변환이 필요하지만, 우선 패널 표시만 처리
            Debug.Log("[ChestBoxUIManager] UI 패널 표시됨");
        }
    }

    /// <summary>
    /// 상자 UI 숨김
    /// </summary>
    public void HideChestBox()
    {
        if (_chestBoxUIRoot != null)
        {
            _chestBoxUIRoot.style.display = DisplayStyle.None;
            Debug.Log("[ChestBoxUIManager] ChestBoxUI Root 요소를 숨김으로 설정");
        }
        else
        {
            Debug.LogError("[ChestBoxUIManager] ChestBoxUI Root 요소가 null입니다!");
        }

        _currentChest = null;
        Debug.Log("[ChestBoxUIManager] 상자 UI 숨김");
    }

    /// <summary>
    /// 아이템 목록 새로고침
    /// </summary>
    private void RefreshItemList()
    {
        if (_currentChest == null)
            return;

        // ChestInventoryManager 또는 서버로부터 최신 아이템 리스트 가져오기
        var chestItems = _currentChest.GetChestItems();

        // 1. 모든 슬롯을 순회 (0부터 8번까지)
        for (int i = 0; i < 9; i++)
        {
            // 해당 슬롯이 캐싱되어 있는지 확인
            if (_itemSlots[i] == null)
                continue;

            // 2. 현재 인덱스(i)에 해당하는 아이템 데이터가 있는지 확인
            if (i < chestItems.Count)
            {
                // 아이템 데이터가 있으면: 슬롯에 아이템 정보를 설정 (UI 업데이트)
                SetupItemSlot(i, chestItems[i]);
            }
            else
            {
                // 아이템 데이터가 없으면: 슬롯을 빈 상태로 만듦
                ClearItemSlot(i);
            }
        }

        Debug.Log($"[ChestBoxUIManager] 아이템 목록 새로고침 완료 - {chestItems.Count}개 아이템");
    }

    /// <summary>
    /// 개별 슬롯에 아이템 설정
    /// </summary>
    private void SetupItemSlot(int slotIndex, ChestItem item)
    {
        if (slotIndex < 0 || slotIndex >= 9 || _itemIcons[slotIndex] == null)
            return;

        // 아이템 아이콘 설정
        if (item.itemIcon != null)
        {
            _itemIcons[slotIndex].style.backgroundImage = new StyleBackground(item.itemIcon);
            _itemIcons[slotIndex].style.display = DisplayStyle.Flex;
        }
        else
        {
            _itemIcons[slotIndex].style.display = DisplayStyle.None;
        }

        // Take 버튼 활성화
        if (_takeButtons[slotIndex] != null)
        {
            _takeButtons[slotIndex].SetEnabled(true);
        }

        Debug.Log(
            $"[ChestBoxUIManager] 슬롯 {slotIndex} 아이템 설정: {item.itemName} x{item.quantity}"
        );
    }

    /// <summary>
    /// 개별 슬롯 비우기
    /// </summary>
    private void ClearItemSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 9)
            return;

        // 아이템 아이콘 숨기기
        if (_itemIcons[slotIndex] != null)
        {
            _itemIcons[slotIndex].style.display = DisplayStyle.None;
        }

        // Take 버튼 비활성화
        if (_takeButtons[slotIndex] != null)
        {
            _takeButtons[slotIndex].SetEnabled(false);
        }

        Debug.Log($"[ChestBoxUIManager] 슬롯 {slotIndex} 비우기");
    }

    /// <summary>
    /// 아이템 가져오기
    /// </summary>
    public void TakeItem(int index)
    {
        if (_currentChest == null)
            return;

        var chestItems = _currentChest.GetChestItems();
        if (index < 0 || index >= chestItems.Count)
            return;

        ChestItem item = chestItems[index];

        // Light(빛)인 경우 ResourceManager로, 다른 아이템은 PlayerInventory로
        bool canAdd = false;
        if (item.itemName == "Light" || item.itemName == "Mushroom") // Mushroom도 Light로 처리
        {
            // Light는 ResourceManager로 (제한 없음)
            canAdd = true;
            Debug.Log(
                $"[ChestBoxUIManager] {item.itemName} {item.quantity}개 - Light로 ResourceManager에 추가될 예정"
            );
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
                        $"[ChestBoxUIManager] {item.itemName} x{item.quantity} 획득 불가 - 인벤토리 가득 참 또는 스택 제한"
                    );
                    return;
                }
            }
        }

        // 실시간 멀티플레이어: 서버에 직접 요청
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.RequestTakeItem(
                _currentChest.chestId,
                item.itemName,
                item.quantity
            );

            Debug.Log(
                $"[ChestBoxUIManager] 서버에 아이템 획득 요청: {item.itemName} x{item.quantity} - 서버 응답 대기"
            );

            // 예측적 UI 업데이트 제거 - 서버 응답에서만 UI 갱신
        }
        else
        {
            Debug.LogError("[ChestBoxUIManager] ServerSyncManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 모든 아이템 가져오기
    /// </summary>
    public void TakeAllItems()
    {
        if (_currentChest == null)
            return;

        Debug.Log("[ChestBoxUIManager] 모든 아이템 가져오기 시도 (서버 요청)");

        var chestItems = _currentChest.GetChestItems();

        // 실시간 멀티플레이어: 각 아이템을 개별적으로 서버에 요청
        if (ServerSyncManager.Instance != null)
        {
            for (int i = 0; i < chestItems.Count; i++)
            {
                var item = chestItems[i];
                ServerSyncManager.Instance.RequestTakeItem(
                    _currentChest.chestId,
                    item.itemName,
                    item.quantity
                );
            }
        }

        Debug.Log($"[ChestBoxUIManager] {chestItems.Count}개 아이템 획득 요청 전송 완료");
    }

    /// <summary>
    /// 닫기 버튼 클릭 처리
    /// </summary>
    private void OnCloseButtonClicked()
    {
        // 상자 닫기 (이것이 UI를 자동으로 닫을 것임)
        if (_currentChest != null)
        {
            _currentChest.CloseChest();
        }

        // 또는 직접 UI 닫기
        HideChestBox();

        // UIManager를 통해 오버레이 닫기는 UI Toolkit에서는 다르게 처리 필요
        Debug.Log("[ChestBoxUIManager] 닫기 버튼 클릭 - 상자 UI 닫힌");
    }

    /// <summary>
    /// 현재 열린 상자의 ID 반환
    /// </summary>
    public string GetCurrentChestId()
    {
        return _currentChest?.chestId ?? "";
    }

    /// <summary>
    /// 서버 액션 응답 처리
    /// </summary>
    private void OnServerActionResponse(object responseObj)
    {
        if (_currentChest == null || responseObj == null)
            return;

        // 리플렉션으로 ItemActionResponse 처리 (기존 로직 유지)
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
            Debug.Log($"[ChestBoxUIManager] 상자와 관련없는 액션 무시: {actionValue}");
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
                            isCurrentChestResponse = chestIdValue == _currentChest.chestId;
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
                Debug.Log(
                    $"[ChestBoxUIManager] 서버 응답 성공 - {actionType} 액션: {messageValue}"
                );

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
                    if (dataField != null)
                    {
                        var dataValue = dataField.GetValue(responseObj);
                        if (dataValue != null)
                        {
                            ProcessTakenItem(dataValue);
                        }
                    }

                    Debug.Log(
                        $"[ChestBoxUIManager] Take 액션 - 상자에서 아이템 획득: {messageValue}"
                    );
                }
                else if (actionType == "Put")
                {
                    // Put 액션 - 인벤토리→상자 이동
                    RefreshItemList();
                    Debug.Log($"[ChestBoxUIManager] Put 액션 - 상자에 아이템 추가: {messageValue}");
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
                Debug.LogWarning($"[ChestBoxUIManager] 서버 요청 실패: {messageValue}");

                // UI를 다시 원래 상태로 되돌리기
                RefreshItemList();
            }
        }
    }

    /// <summary>
    /// 드래그 앤 드롭 대상으로서 상자 패널 등록
    /// </summary>
    public void SetupDropTarget()
    {
        if (_chestBoxPanel != null)
        {
            // 콜백 인스턴스 생성 및 저장 (이벤트 전파 중지 포함)
            _dragEnterCallback = evt => 
            {
                evt.StopImmediatePropagation();
                OnDragEnter(evt);
            };
            _dragLeaveCallback = evt => 
            {
                evt.StopImmediatePropagation();
                OnDragLeave(evt);
            };
            _dragUpdatedCallback = evt => 
            {
                evt.StopImmediatePropagation();
                OnDragUpdated(evt);
            };
            _dragPerformCallback = evt => 
            {
                evt.StopImmediatePropagation();
                OnDragPerform(evt);
            };

            // 저장된 콜백으로 등록
            _chestBoxPanel.RegisterCallback(_dragEnterCallback);
            _chestBoxPanel.RegisterCallback(_dragLeaveCallback);
            _chestBoxPanel.RegisterCallback(_dragUpdatedCallback);
            _chestBoxPanel.RegisterCallback(_dragPerformCallback);

            if (enableVerboseLogging)
                Debug.Log("[ChestBoxUIManager] 드래그 앤 드롭 대상으로 등록 완료");
        }
    }

    /// <summary>
    /// 드래그 진입 시 처리
    /// </summary>
    private void OnDragEnter(DragEnterEvent evt)
    {
#if UNITY_EDITOR
        // 드래그된 데이터가 인벤토리 아이템인지 확인
        if (DragAndDrop.GetGenericData("inventorySlotIndex") != null)
        {
            Debug.Log("[ChestBoxUIManager] 인벤토리 아이템 드래그 진입");

            // 상자 패널 하이라이트 효과 (선택적)
            if (_chestBoxPanel != null)
            {
                _chestBoxPanel.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f, 0.3f);
            }
        }
#endif
    }

    /// <summary>
    /// 드래그 떠날 시 처리
    /// </summary>
    private void OnDragLeave(DragLeaveEvent evt)
    {
        // 하이라이트 해제
        if (_chestBoxPanel != null)
        {
            _chestBoxPanel.style.backgroundColor = StyleKeyword.Null;
        }

        Debug.Log("[ChestBoxUIManager] 드래그 떠남");
    }

    /// <summary>
    /// 드래그 업데이트 처리
    /// </summary>
    private void OnDragUpdated(DragUpdatedEvent evt)
    {
#if UNITY_EDITOR
        // 드래그된 데이터가 인벤토리 아이템인지 확인
        if (DragAndDrop.GetGenericData("inventorySlotIndex") != null)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
        else
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }
#endif
    }

    /// <summary>
    /// 드래그 드롭 실행 처리
    /// </summary>
    private void OnDragPerform(DragPerformEvent evt)
    {
#if UNITY_EDITOR
        var slotIndexObj = DragAndDrop.GetGenericData("inventorySlotIndex");
        var itemNameObj = DragAndDrop.GetGenericData("itemName");

        if (slotIndexObj != null && itemNameObj != null)
        {
            int slotIndex = (int)slotIndexObj;
            string itemName = (string)itemNameObj;

            Debug.Log($"[ChestBoxUIManager] 드롭 수행: {itemName} (슬롯 {slotIndex}) → 상자");

            // 서버에 아이템 이동 요청
            if (ServerSimulator.Instance != null && _currentChest != null)
            {
                ServerSimulator.Instance.RequestPutItemToChest(_currentChest.chestId, itemName, 1);
                Debug.Log(
                    $"[ChestBoxUIManager] 서버에 Put 요청: {itemName} → {_currentChest.chestId}"
                );
            }

            // 하이라이트 해제
            if (_chestBoxPanel != null)
            {
                _chestBoxPanel.style.backgroundColor = StyleKeyword.Null;
            }

            DragAndDrop.AcceptDrag();
            evt.StopPropagation();
        }
#endif
    }

    /// <summary>
    /// Take 액션에서 획득한 아이템 처리 - Light/Mushroom인 경우 ResourceManager에 추가
    /// (기존 로직 유지)
    /// </summary>
    private void ProcessTakenItem(object dataValue)
    {
        Debug.Log(
            $"[ChestBoxUIManager] ProcessTakenItem 시작 - dataValue 타입: {dataValue?.GetType().Name}"
        );

        try
        {
            // 리플렉션으로 서버 응답 데이터에서 아이템 정보 추출
            var dataType = dataValue.GetType();

            var takenItemField = dataType.GetField("takenItem");

            // takenItem이 없으면 다른 가능한 필드명들 시도
            if (takenItemField == null)
            {
                takenItemField =
                    dataType.GetField("item")
                    ?? dataType.GetField("itemData")
                    ?? dataType.GetField("actionItem");
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
                            Debug.Log(
                                $"[ChestBoxUIManager] {itemName} {quantity}개를 Light로 처리 시작 - ResourceManager.Instance: {ResourceManager.Instance != null}"
                            );

                            if (ResourceManager.Instance != null)
                            {
                                int addedAmount = ResourceManager.Instance.AddLight(quantity);
                                Debug.Log(
                                    $"[ChestBoxUIManager] ResourceManager에 Light {addedAmount}개 추가 완료 (원본 아이템: {itemName})"
                                );
                            }
                            else
                            {
                                Debug.LogError(
                                    "[ChestBoxUIManager] ResourceManager.Instance가 null입니다! UniversalHUDController가 제대로 초기화되지 않았을 가능성"
                                );
                            }
                        }
                        else
                        {
                            // 다른 아이템은 PlayerInventory가 서버 응답에서 자동으로 처리
                            Debug.Log(
                                $"[ChestBoxUIManager] 일반 아이템 {itemName} x{quantity}는 PlayerInventory에서 처리됨"
                            );
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ChestBoxUIManager] 서버 응답 데이터 파싱 실패: {ex.Message}");
        }
    }
}
