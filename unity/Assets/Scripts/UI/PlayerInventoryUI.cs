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
    /// 슬롯 클릭 처리 - 슬롯 0은 사용, 다른 슬롯은 교체
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        if (PlayerInventory.Instance == null)
            return;

        InventoryItem item = PlayerInventory.Instance.GetItem(slotIndex);

        if (slotIndex == 0)
        {
            // 슬롯 0 클릭: 아이템 사용
            if (!item.IsEmpty())
            {
                Debug.Log($"[PlayerInventoryUI] 슬롯 0 아이템 사용: {item.itemName}");
                PlayerInventory.Instance.UseSlot0Item();
            }
            else
            {
                Debug.Log("[PlayerInventoryUI] 슬롯 0이 비어있습니다.");
                ShowMessage("사용할 아이템이 없습니다.");
            }
        }
        else
        {
            // 슬롯 1,2 클릭: 슬롯 0과 위치 교체
            if (!item.IsEmpty())
            {
                Debug.Log(
                    $"[PlayerInventoryUI] 슬롯 {slotIndex} 아이템을 활성 슬롯으로 이동: {item.itemName}"
                );
                PlayerInventory.Instance.SwapWithSlot0(slotIndex);
            }
            else
            {
                Debug.Log($"[PlayerInventoryUI] 슬롯 {slotIndex}이 비어있습니다.");
                ShowMessage("빈 슬롯입니다.");
            }
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
