using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 인벤토리 슬롯 UI 컴포넌트
/// </summary>
[System.Serializable]
public class InventorySlotUI
{
    [Header("UI 컴포넌트")]
    public Image itemIcon;
    public Text itemName;
    public Text itemQuantity;
    public Button slotButton;
    public GameObject emptySlotIndicator; // 빈 슬롯 표시용

    [Header("설정")]
    public Color emptySlotColor = Color.gray;
    public Color filledSlotColor = Color.white;

    private int slotIndex;

    /// <summary>
    /// 슬롯 초기화
    /// </summary>
    public void Initialize(int index)
    {
        slotIndex = index;

        // 버튼 클릭 이벤트 연결
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() =>
            {
                PlayerInventoryUI inventoryUI =
                    slotButton.GetComponentInParent<PlayerInventoryUI>();
                inventoryUI?.OnSlotClicked(slotIndex);
            });

            // 드래그 가능한 슬롯 컴포넌트 추가
            DraggableInventorySlot draggableSlot =
                slotButton.GetComponent<DraggableInventorySlot>();
            if (draggableSlot == null)
            {
                draggableSlot = slotButton.gameObject.AddComponent<DraggableInventorySlot>();
            }
            draggableSlot.Initialize(index);
        }

        // 초기 빈 슬롯으로 설정
        UpdateSlot(new InventoryItem());
    }

    /// <summary>
    /// 슬롯 UI 업데이트
    /// </summary>
    public void UpdateSlot(InventoryItem item)
    {
        if (item == null || item.IsEmpty())
        {
            // 빈 슬롯 표시
            ShowEmptySlot();
        }
        else
        {
            // 아이템이 있는 슬롯 표시
            ShowFilledSlot(item);
        }

        // DraggableInventorySlot도 업데이트
        if (slotButton != null)
        {
            DraggableInventorySlot draggableSlot =
                slotButton.GetComponent<DraggableInventorySlot>();
            if (draggableSlot != null)
            {
                draggableSlot.UpdateSlotData();
            }
        }
    }

    /// <summary>
    /// 빈 슬롯 표시
    /// </summary>
    private void ShowEmptySlot()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = emptySlotColor;
            itemIcon.enabled = false; // 빈 슬롯일 때 이미지 컴포넌트 비활성화
        }

        if (itemName != null)
            itemName.text = "";

        if (itemQuantity != null)
            itemQuantity.text = "";

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(true);

        Debug.Log($"[InventorySlotUI] 슬롯 {slotIndex} 빈 슬롯으로 표시");
    }

    /// <summary>
    /// 아이템이 있는 슬롯 표시
    /// </summary>
    private void ShowFilledSlot(InventoryItem item)
    {
        Debug.Log(
            $"[InventorySlotUI] ShowFilledSlot 호출됨 - 아이템: {item.itemName}, 아이콘: {(item.itemIcon != null ? "있음" : "없음")}"
        );

        if (itemIcon != null)
        {
            itemIcon.enabled = true; // 아이템이 있을 때 이미지 컴포넌트 활성화
            itemIcon.sprite = item.itemIcon;
            itemIcon.color = filledSlotColor;

            // 아이템 아이콘이 null인 경우 처리
            if (item.itemIcon == null)
            {
                Debug.LogWarning($"[InventorySlotUI] {item.itemName}의 itemIcon이 null입니다!");
                Debug.LogWarning(
                    $"[InventorySlotUI] GlobalItemManager에서 아이콘을 다시 찾아보겠습니다..."
                );

                // GlobalItemManager에서 아이콘 다시 찾기
                if (GlobalItemManager.Instance != null)
                {
                    var itemData = GlobalItemManager.Instance.GetItemData(item.itemName);
                    if (itemData != null && itemData.itemIcon != null)
                    {
                        Debug.Log(
                            $"[InventorySlotUI] GlobalItemManager에서 {item.itemName} 아이콘 찾음!"
                        );
                        itemIcon.sprite = itemData.itemIcon;
                        itemIcon.enabled = true;
                    }
                    else
                    {
                        Debug.LogError(
                            $"[InventorySlotUI] GlobalItemManager에서도 {item.itemName} 아이콘을 찾을 수 없습니다!"
                        );
                        itemIcon.enabled = false;
                    }
                }
                else
                {
                    itemIcon.enabled = false;
                }
            }
            else
            {
                Debug.Log($"[InventorySlotUI] 아이콘 설정 성공: {item.itemName}");
            }
        }
        else
        {
            Debug.LogError($"[InventorySlotUI] itemIcon이 null입니다! 슬롯 {slotIndex}");
        }

        if (itemName != null)
            itemName.text = item.itemName;

        if (itemQuantity != null)
        {
            itemQuantity.text = item.quantity > 1 ? item.quantity.ToString() : "";
        }

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(false);

        Debug.Log(
            $"[InventorySlotUI] 슬롯 {slotIndex}에 {item.itemName} x{item.quantity} 표시, 아이콘: {(item.itemIcon != null ? "있음" : "없음")}"
        );
    }
}
