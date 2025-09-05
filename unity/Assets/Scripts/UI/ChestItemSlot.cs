using UnityEngine;
using UnityEngine.UI;

// 개별 아이템 슬롯 컴포넌트
public class ChestItemSlot : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Image itemIcon;
    public Text itemName;
    public Text itemQuantity;
    public Button takeButton;
    public Text itemDescription;

    private ChestItem item;
    private int itemIndex;
    private ChestBoxUI chestBoxUI;

    public void SetupItem(ChestItem chestItem, int index, ChestBoxUI ui)
    {
        item = chestItem;
        itemIndex = index;
        chestBoxUI = ui;

        // UI 업데이트
        if (itemIcon != null && item.itemIcon != null)
            itemIcon.sprite = item.itemIcon;

        if (itemName != null)
            itemName.text = item.itemName;

        if (itemQuantity != null)
            itemQuantity.text = item.quantity > 1 ? item.quantity.ToString() : "";

        if (itemDescription != null)
            itemDescription.text = item.description;

        if (takeButton != null)
        {
            takeButton.onClick.RemoveAllListeners();
            takeButton.onClick.AddListener(OnTakeButtonClicked);
            takeButton.gameObject.SetActive(true); // 아이템이 있을 때는 버튼 표시
        }
    }

    private void OnTakeButtonClicked()
    {
        if (chestBoxUI != null)
        {
            chestBoxUI.TakeItem(itemIndex);
        }
    }

    public void ClearSlot()
    {
        item = null;
        itemIndex = -1;

        // UI를 빈 슬롯으로 초기화
        if (itemIcon != null)
            itemIcon.sprite = null;

        if (itemName != null)
            itemName.text = "";

        if (itemQuantity != null)
            itemQuantity.text = "";

        if (itemDescription != null)
            itemDescription.text = "";

        if (takeButton != null)
        {
            takeButton.onClick.RemoveAllListeners();
            takeButton.gameObject.SetActive(false); // 빈 슬롯에서는 버튼 숨김
        }
    }

    public bool IsEmpty()
    {
        return item == null;
    }
}
