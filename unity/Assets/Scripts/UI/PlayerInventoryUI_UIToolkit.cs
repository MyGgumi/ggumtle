using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 기반 플레이어 인벤토리 UI 관리
/// 기존 PlayerInventory 시스템과 연동하여 UI만 UI Toolkit으로 처리
/// </summary>
public class PlayerInventoryUI_UIToolkit : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private VisualElement[] _slots = new VisualElement[3];
    private VisualElement[] _itemImages = new VisualElement[3];
    private Label[] _countLabels = new Label[3];
    private VisualElement[] _counters = new VisualElement[3];
    
    [Header("Sprites")]
    public Sprite backgroundItemSlotSprite;
    
    private void Start()
    {
        // UniversalHUDController에서 초기화하므로 여기서는 이벤트 구독만
        SubscribeToInventory();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromInventory();
    }
    
    /// <summary>
    /// UniversalHUDController에서 호출하는 초기화 메서드
    /// </summary>
    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        SetupSlotEvents();
        
        // 초기 UI 업데이트
        RefreshAllSlots();
        
        Debug.Log("[PlayerInventoryUI_UIToolkit] UI 초기화 완료");
    }
    
    private void CacheUIElements()
    {
        Debug.Log("[PlayerInventoryUI_UIToolkit] UI 요소 캐싱 시작...");
        
        // 인벤토리 슬롯들 캐싱 (Inventory.uxml 구조에 맞춤)
        for (int i = 0; i < 3; i++)
        {
            int slotNum = i + 1;
            
            _slots[i] = _root.Q<VisualElement>($"slot{slotNum}");
            _itemImages[i] = _root.Q<VisualElement>($"slot{slotNum}ItemImage");
            _countLabels[i] = _root.Q<Label>($"slot{slotNum}Count");
            _counters[i] = _root.Q<VisualElement>($"slot{slotNum}Counter");
            
            Debug.Log($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotNum} 캐싱: " +
                     $"슬롯={(_slots[i] != null ? "OK" : "NULL")}, " +
                     $"이미지={(_itemImages[i] != null ? "OK" : "NULL")}, " +
                     $"카운트={(_countLabels[i] != null ? "OK" : "NULL")}, " +
                     $"카운터={(_counters[i] != null ? "OK" : "NULL")}");
        }
        
        Debug.Log("[PlayerInventoryUI_UIToolkit] UI 요소 캐싱 완료");
    }
    
    private void SetupSlotEvents()
    {
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i; // 클로저를 위한 로컬 변수
            if (_slots[i] != null)
            {
                Debug.Log($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotIndex + 1} 이벤트 등록 중...");
                
                // 클릭 이벤트 등록
                _slots[i].RegisterCallback<ClickEvent>(evt => 
                {
                    Debug.Log($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotIndex + 1} 클릭 이벤트 발생!");
                    OnSlotClicked(slotIndex);
                });
                
                // 터치 이벤트도 추가로 등록
                _slots[i].RegisterCallback<PointerDownEvent>(evt => 
                {
                    Debug.Log($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotIndex + 1} 터치 이벤트 발생!");
                    OnSlotClicked(slotIndex);
                });
                
                Debug.Log($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotIndex + 1} 이벤트 등록 완료");
            }
            else
            {
                Debug.LogError($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotIndex + 1}을 찾을 수 없습니다!");
            }
        }
    }
    
    /// <summary>
    /// PlayerInventory 이벤트 구독
    /// </summary>
    private void SubscribeToInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += OnInventorySlotChanged;
            
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
        }
    }
    
    /// <summary>
    /// 특정 슬롯 변경 시 UI 업데이트
    /// </summary>
    private void OnInventorySlotChanged(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _slots.Length && _slots[slotIndex] != null)
        {
            InventoryItem item = PlayerInventory.Instance.GetItem(slotIndex);
            UpdateSlotUI(slotIndex, item);
        }
    }
    
    /// <summary>
    /// 모든 슬롯 UI 새로고침
    /// </summary>
    public void RefreshAllSlots()
    {
        if (PlayerInventory.Instance == null || _root == null)
            return;
            
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                InventoryItem item = PlayerInventory.Instance.GetItem(i);
                UpdateSlotUI(i, item);
            }
        }
    }
    
    /// <summary>
    /// 개별 슬롯 UI 업데이트
    /// </summary>
    private void UpdateSlotUI(int slotIndex, InventoryItem item)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Length)
            return;
            
        // 슬롯 배경은 항상 유지 (backgroundItemSlotSprite)
        if (_slots[slotIndex] != null && backgroundItemSlotSprite != null)
        {
            _slots[slotIndex].style.backgroundImage = new StyleBackground(backgroundItemSlotSprite);
        }
            
        // 아이템 이미지 설정
        if (item != null && !item.IsEmpty())
        {
            // 아이템이 있는 경우 - 아이템 아이콘 표시
            if (_itemImages[slotIndex] != null)
            {
                if (item.itemIcon != null)
                {
                    _itemImages[slotIndex].style.backgroundImage = new StyleBackground(item.itemIcon);
                    _itemImages[slotIndex].style.display = DisplayStyle.Flex;
                }
                else
                {
                    // 아이콘이 없으면 아이템 이미지 숨기기
                    _itemImages[slotIndex].style.display = DisplayStyle.None;
                }
            }
            
            // 수량 표시
            if (_countLabels[slotIndex] != null)
            {
                _countLabels[slotIndex].text = item.quantity.ToString();
            }
            
            // 카운터 표시
            if (_counters[slotIndex] != null)
            {
                _counters[slotIndex].style.display = item.quantity > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        else
        {
            // 빈 슬롯인 경우 - 아이템 이미지 숨기기
            if (_itemImages[slotIndex] != null)
            {
                _itemImages[slotIndex].style.display = DisplayStyle.None;
            }
            
            // 수량 숨기기
            if (_countLabels[slotIndex] != null)
            {
                _countLabels[slotIndex].text = "";
            }
            
            // 카운터 숨기기
            if (_counters[slotIndex] != null)
            {
                _counters[slotIndex].style.display = DisplayStyle.None;
            }
        }
        
        Debug.Log($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotIndex} UI 업데이트: {(item?.IsEmpty() != false ? "빈 슬롯" : $"{item.itemName} x{item.quantity}")}");
    }
    
    /// <summary>
    /// 슬롯 클릭 처리 - PlayerInventory 로직 사용
    /// 슬롯 0: 아이템 사용, 슬롯 1,2: 슬롯 0과 교체
    /// </summary>
    private void OnSlotClicked(int slotIndex)
    {
        if (PlayerInventory.Instance == null)
            return;
            
        Debug.Log($"[PlayerInventoryUI_UIToolkit] 슬롯 {slotIndex + 1} 클릭! (배열 인덱스: {slotIndex})");
        
        // PlayerInventory의 기존 로직 사용 (slotIndex는 0-based이므로 그대로 사용)
        if (slotIndex == 0)
        {
            // 슬롯 0 클릭: 아이템 사용
            PlayerInventory.Instance.UseSlot0Item();
        }
        else
        {
            // 슬롯 1,2 클릭: 슬롯 0과 위치 교체
            PlayerInventory.Instance.SwapWithSlot0(slotIndex);
        }
    }
    
    /// <summary>
    /// 스프라이트 설정
    /// </summary>
    public void SetSprites(Sprite itemSlotSprite)
    {
        backgroundItemSlotSprite = itemSlotSprite;
        
        // 모든 슬롯에 기본 배경 적용
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && backgroundItemSlotSprite != null)
            {
                _slots[i].style.backgroundImage = new StyleBackground(backgroundItemSlotSprite);
            }
        }
        
        Debug.Log("[PlayerInventoryUI_UIToolkit] 슬롯 배경 스프라이트 설정 완료");
    }
}