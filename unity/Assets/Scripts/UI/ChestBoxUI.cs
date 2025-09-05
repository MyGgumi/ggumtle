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
        ClearItemSlots();
    }

    private void RefreshItemList()
    {
        if (currentChest == null)
            return;

        // 기존 아이템 슬롯들 제거
        ClearItemSlots();

        // 새로운 아이템 슬롯들 생성
        for (int i = 0; i < currentChest.chestItems.Count; i++)
        {
            CreateItemSlot(currentChest.chestItems[i], i);
        }

        // 아이템 개수 텍스트 업데이트
        if (itemCountText != null)
        {
            itemCountText.text = $"아이템: {currentChest.chestItems.Count}개";
        }
    }

    private void CreateItemSlot(ChestItem item, int index)
    {
        if (itemSlotPrefab == null || itemsContainer == null)
            return;

        // 아이템 슬롯 생성
        GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
        itemSlots.Add(slot);

        // 아이템 슬롯 컴포넌트 설정
        ChestItemSlot slotComponent = slot.GetComponent<ChestItemSlot>();
        if (slotComponent != null)
        {
            slotComponent.SetupItem(item, index, this);
        }
        else
        {
            // ChestItemSlot 컴포넌트가 없으면 기본 UI 설정
            SetupBasicItemSlot(slot, item, index);
        }
    }

    private void SetupBasicItemSlot(GameObject slot, ChestItem item, int index)
    {
        // 기본적인 UI 설정 (ChestItemSlot 컴포넌트가 없을 경우)
        Image itemIcon = slot.transform.Find("Icon")?.GetComponent<Image>();
        Text itemName = slot.transform.Find("Name")?.GetComponent<Text>();
        Text itemQuantity = slot.transform.Find("Quantity")?.GetComponent<Text>();
        Button takeButton = slot.transform.Find("TakeButton")?.GetComponent<Button>();

        if (itemIcon != null && item.itemIcon != null)
            itemIcon.sprite = item.itemIcon;

        if (itemName != null)
            itemName.text = item.itemName;

        if (itemQuantity != null)
            itemQuantity.text = item.quantity > 1 ? item.quantity.ToString() : "";

        if (takeButton != null)
        {
            takeButton.onClick.RemoveAllListeners();
            takeButton.onClick.AddListener(() => TakeItem(index));
        }
    }

    private void ClearItemSlots()
    {
        foreach (GameObject slot in itemSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        itemSlots.Clear();
    }

    public void TakeItem(int index)
    {
        if (currentChest == null || index < 0 || index >= currentChest.chestItems.Count)
            return;

        ChestItem item = currentChest.chestItems[index];

        // 플레이어 인벤토리에 아이템 추가 시도
        if (PlayerInventory.Instance != null)
        {
            int addedAmount = PlayerInventory.Instance.TryAddItem(item);

            if (addedAmount > 0)
            {
                // 추가된 만큼 상자에서 아이템 수량 감소
                item.quantity -= addedAmount;

                // 상자의 아이템이 모두 없어지면 제거
                if (item.quantity <= 0)
                {
                    currentChest.RemoveItemAt(index);
                }

                // UI 갱신
                RefreshItemList();

                Debug.Log($"[ChestBoxUI] {item.itemName} {addedAmount}개 플레이어 인벤토리로 이동");
            }
            else
            {
                Debug.LogWarning(
                    "[ChestBoxUI] 플레이어 인벤토리가 가득 차서 아이템을 가져올 수 없습니다"
                );
            }
        }
        else
        {
            Debug.LogError("[ChestBoxUI] PlayerInventory.Instance가 null입니다!");
        }
    }

    public void TakeAllItems()
    {
        if (currentChest == null || PlayerInventory.Instance == null)
            return;

        Debug.Log("[ChestBoxUI] 모든 아이템 가져오기 시도");

        // 모든 아이템을 플레이어 인벤토리로 이동 (역순으로 처리)
        for (int i = currentChest.chestItems.Count - 1; i >= 0; i--)
        {
            TakeItem(i);

            // 인벤토리가 가득 차면 중단
            if (PlayerInventory.Instance.IsFull())
            {
                Debug.LogWarning("[ChestBoxUI] 인벤토리가 가득 차서 모든 아이템 가져오기 중단");
                break;
            }
        }

        Debug.Log("[ChestBoxUI] 모든 아이템 가져오기 완료");
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
}
