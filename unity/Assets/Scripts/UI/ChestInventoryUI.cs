using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChestInventoryUI : MonoBehaviour
{
    [Header("UI 패널")]
    public GameObject chestInventoryPanel;
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
        // 초기화
        if (chestInventoryPanel != null)
            chestInventoryPanel.SetActive(false);

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
        if (chestInventoryPanel != null)
            chestInventoryPanel.SetActive(true);

        // 상자 제목 설정
        if (chestTitleText != null)
            chestTitleText.text = "상자 인벤토리";

        // 아이템 목록 갱신
        RefreshItemList();
    }

    public void HideChestInventory()
    {
        if (chestInventoryPanel != null)
            chestInventoryPanel.SetActive(false);

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

        // TODO: 플레이어 인벤토리에 아이템 추가하는 로직
        // PlayerInventory.Instance.AddItem(item);

        // 상자에서 아이템 제거
        currentChest.RemoveItemAt(index);

        // UI 갱신
        RefreshItemList();

        Debug.Log($"아이템 획득: {item.itemName} x{item.quantity}");
    }

    public void TakeAllItems()
    {
        if (currentChest == null)
            return;

        // 모든 아이템을 플레이어 인벤토리로 이동
        for (int i = currentChest.chestItems.Count - 1; i >= 0; i--)
        {
            TakeItem(i);
        }
    }

    private void OnCloseButtonClicked()
    {
        // UIManager를 통해 오버레이 닫기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseOverlay(chestInventoryPanel);
        }

        // 상자 닫기
        if (currentChest != null)
        {
            currentChest.CloseChest();
        }
    }

    // 모바일용 버튼들
    public void OnTakeAllButtonPressed()
    {
        TakeAllItems();
    }
}

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
    private ChestInventoryUI inventoryUI;

    public void SetupItem(ChestItem chestItem, int index, ChestInventoryUI ui)
    {
        item = chestItem;
        itemIndex = index;
        inventoryUI = ui;

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
        if (inventoryUI != null)
        {
            inventoryUI.TakeItem(itemIndex);
        }
    }
}
