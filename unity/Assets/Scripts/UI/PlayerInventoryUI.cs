using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 인벤토리 UI 관리
/// 3개 고정 슬롯을 항상 표시
/// </summary>
public class PlayerInventoryUI : MonoBehaviour
{
    [Header("UI 슬롯들")]
    public InventorySlotUI[] inventorySlots = new InventorySlotUI[3];

    [Header("메시지 표시")]
    public Text messageText;
    public float messageDuration = 2f;

    private float messageTimer = 0f;

    void Start()
    {
        InitializeUI();
        SubscribeToInventory();
    }

    void OnDestroy()
    {
        UnsubscribeFromInventory();
    }

    void Update()
    {
        // 메시지 타이머 업데이트
        UpdateMessageTimer();
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 슬롯 초기화
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null)
            {
                inventorySlots[i].Initialize(i);
            }
        }

        // 메시지 텍스트 초기화
        if (messageText != null)
        {
            messageText.text = "";
            messageText.gameObject.SetActive(false);
        }

        Debug.Log("[PlayerInventoryUI] UI 초기화 완료");
    }

    /// <summary>
    /// PlayerInventory 이벤트 구독
    /// </summary>
    private void SubscribeToInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += OnInventorySlotChanged;
            PlayerInventory.Instance.OnInventoryMessage += ShowMessage;

            // 초기 UI 업데이트
            RefreshAllSlots();
        }
    }

    /// <summary>
    /// PlayerInventory 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= OnInventorySlotChanged;
            PlayerInventory.Instance.OnInventoryMessage -= ShowMessage;
        }
    }

    /// <summary>
    /// 특정 슬롯 변경 시 UI 업데이트
    /// </summary>
    private void OnInventorySlotChanged(int slotIndex)
    {
        if (
            slotIndex >= 0
            && slotIndex < inventorySlots.Length
            && inventorySlots[slotIndex] != null
        )
        {
            InventoryItem item = PlayerInventory.Instance.GetItem(slotIndex);
            inventorySlots[slotIndex].UpdateSlot(item);
        }
    }

    /// <summary>
    /// 모든 슬롯 UI 새로고침
    /// </summary>
    public void RefreshAllSlots()
    {
        if (PlayerInventory.Instance == null)
            return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null)
            {
                InventoryItem item = PlayerInventory.Instance.GetItem(i);
                inventorySlots[i].UpdateSlot(item);
            }
        }
    }

    /// <summary>
    /// 메시지 표시
    /// </summary>
    public void ShowMessage(string message)
    {
        if (messageText != null && !string.IsNullOrEmpty(message))
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            messageTimer = messageDuration;

            Debug.Log($"[PlayerInventoryUI] 메시지 표시: {message}");
        }
    }

    /// <summary>
    /// 메시지 타이머 업데이트
    /// </summary>
    private void UpdateMessageTimer()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;

            if (messageTimer <= 0f && messageText != null)
            {
                messageText.gameObject.SetActive(false);
                messageText.text = "";
            }
        }
    }

    /// <summary>
    /// 슬롯 클릭 처리 (디버그용)
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        if (PlayerInventory.Instance == null)
            return;

        InventoryItem item = PlayerInventory.Instance.GetItem(slotIndex);

        if (!item.IsEmpty())
        {
            Debug.Log(
                $"[PlayerInventoryUI] 슬롯 {slotIndex} 클릭: {item.itemName} x{item.quantity}"
            );
            ShowMessage($"{item.itemName}: {item.description}");
        }
        else
        {
            Debug.Log($"[PlayerInventoryUI] 슬롯 {slotIndex} 클릭: 빈 슬롯");
        }
    }

    /// <summary>
    /// 디버그: 인벤토리 상태 출력 버튼
    /// </summary>
    public void OnDebugButtonPressed()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.PrintInventoryStatus();
        }
    }
}

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
        if (itemIcon != null)
        {
            itemIcon.enabled = true; // 아이템이 있을 때 이미지 컴포넌트 활성화
            itemIcon.sprite = item.itemIcon;
            itemIcon.color = filledSlotColor;

            // 아이템 아이콘이 null인 경우 처리
            if (item.itemIcon == null)
            {
                Debug.LogWarning($"[InventorySlotUI] {item.itemName}의 itemIcon이 null입니다!");
                itemIcon.enabled = false;
            }
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
