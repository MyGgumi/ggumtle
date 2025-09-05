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

        // 아이템 목록 갱신
        RefreshItemList();
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

    private void ClearFixedSlots()
    {
        // 고정 슬롯들을 빈 슬롯으로 초기화
        for (int i = 0; i < fixedItemSlots.Length; i++)
        {
            if (fixedItemSlots[i] != null)
            {
                fixedItemSlots[i].ClearSlot();
            }
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

        // 실시간 멀티플레이어: 서버에 직접 요청
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.RequestTakeItem(
                currentChest.chestId,
                item.itemName,
                item.quantity
            );

            Debug.Log($"[ChestBoxUI] 서버에 아이템 획득 요청: {item.itemName} x{item.quantity}");

            // 예측적 UI 업데이트 (서버 응답 전 즉시 반영)
            // 실제 결과는 서버 응답에서 처리되지만, UX를 위해 즉시 갱신
            RefreshItemList();
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

        // Take 또는 OpenChest 액션인지 확인
        bool isTakeOrOpenChest =
            actionValue.ToString() == "Take" || actionValue.ToString() == "OpenChest";

        if (!isTakeOrOpenChest)
            return;

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
                // 서버에서 성공적으로 처리됨 - UI 갱신
                RefreshItemList();

                if (actionValue.ToString() == "Take")
                {
                    Debug.Log($"[ChestBoxUI] 서버 확인 완료 - 아이템 획득 성공: {messageValue}");
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
}
