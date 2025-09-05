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
        }
    }

    private void OnTakeButtonClicked()
    {
        if (chestBoxUI != null)
        {
            chestBoxUI.TakeItem(itemIndex);
        }
    }
}
