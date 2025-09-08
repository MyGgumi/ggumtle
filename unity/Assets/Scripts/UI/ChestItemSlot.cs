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

        Debug.Log(
            $"[ChestItemSlot] SetupItem 호출됨 - 아이템: {chestItem?.itemName}, 인덱스: {index}"
        );

        // UI 업데이트
        if (itemIcon != null)
        {
            if (item.itemIcon != null)
            {
                itemIcon.sprite = item.itemIcon;
                itemIcon.color = new Color(1, 1, 1, 1); // 불투명하게
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.color = new Color(1, 1, 1, 0); // 투명하게
            }
        }
        else
        {
            Debug.LogError($"[ChestItemSlot] itemIcon이 null입니다! GameObject: {gameObject.name}");
        }

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
            takeButton.interactable = true; // 아이템이 있을 때는 버튼 활성화

            // Button 상태 확인
            Debug.Log(
                $"[ChestItemSlot] Take 버튼 설정 완료 - {gameObject.name}, Interactable: {takeButton.interactable}"
            );

            // 테스트용 직접 호출 추가
            takeButton.onClick.AddListener(() =>
            {
                Debug.Log($"[ChestItemSlot] 람다 버튼 클릭 감지됨!");
            });
        }
        else
        {
            Debug.LogError(
                $"[ChestItemSlot] takeButton이 null입니다! GameObject: {gameObject.name}, Inspector에서 연결하세요."
            );
        }
    }

    private void OnTakeButtonClicked()
    {
        Debug.Log(
            $"[ChestItemSlot] Take 버튼 클릭됨! 아이템: {item?.itemName}, 인덱스: {itemIndex}"
        );

        if (chestBoxUI != null)
        {
            Debug.Log($"[ChestItemSlot] ChestBoxUI.TakeItem() 호출");
            chestBoxUI.TakeItem(itemIndex);
        }
        else
        {
            Debug.LogError("[ChestItemSlot] chestBoxUI가 null입니다!");
        }
    }

    public void ClearSlot()
    {
        item = null;
        itemIndex = -1;

        // UI를 빈 슬롯으로 초기화
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(1, 1, 1, 0); // 완전 투명하게
        }

        if (itemName != null)
            itemName.text = "";

        if (itemQuantity != null)
            itemQuantity.text = "";

        if (itemDescription != null)
            itemDescription.text = "";

        if (takeButton != null)
        {
            takeButton.onClick.RemoveAllListeners();
            takeButton.interactable = false; // 버튼 비활성화 (하지만 보이기는 함)
        }
    }

    public bool IsEmpty()
    {
        return item == null;
    }
}
