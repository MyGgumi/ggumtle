using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// UI Toolkit 기반 플레이어 인벤토리 UI 관리
/// 기존 PlayerInventory 시스템과 연동하여 UI만 UI Toolkit으로 처리
/// </summary>
public class PlayerInventoryManager : MonoBehaviour
{
    [Header("UI References")]
    private VisualElement _root;
    private VisualElement[] _slots = new VisualElement[3];
    private VisualElement[] _itemImages = new VisualElement[3];
    private Label[] _countLabels = new Label[3];
    private VisualElement[] _counters = new VisualElement[3];
    
    [Header("Sprites")]
    public Sprite backgroundItemSlotSprite;
    
    // UI 이벤트 콜백 저장 변수들
    private EventCallback<ClickEvent>[] _slotClickCallbacks = new EventCallback<ClickEvent>[3];
    private EventCallback<PointerDownEvent>[] _slotPointerDownCallbacks = new EventCallback<PointerDownEvent>[3];
    private EventCallback<PointerMoveEvent>[] _slotPointerMoveCallbacks = new EventCallback<PointerMoveEvent>[3];
    
    private void Start()
    {
        // UniversalHUDController에서 초기화하므로 여기서는 이벤트 구독만
        SubscribeToInventory();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromInventory();
        UnregisterUIEvents();
    }
    
    private void UnregisterUIEvents()
    {
        // 슬롯 이벤트 해제
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                if (_slotClickCallbacks[i] != null)
                    _slots[i].UnregisterCallback(_slotClickCallbacks[i]);
                if (_slotPointerDownCallbacks[i] != null)
                    _slots[i].UnregisterCallback(_slotPointerDownCallbacks[i]);
                if (_slotPointerMoveCallbacks[i] != null)
                    _slots[i].UnregisterCallback(_slotPointerMoveCallbacks[i]);
            }
        }
        
        Debug.Log("[PlayerInventoryManager] UI 이벤트 구독 해제 완료");
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
        
        Debug.Log("[PlayerInventoryManager] UI 초기화 완료");
    }
    
    private void CacheUIElements()
    {
        Debug.Log("[PlayerInventoryManager] UI 요소 캐싱 시작...");
        
        // 인벤토리 슬롯들 캐싱 (Inventory.uxml 구조에 맞춤)
        for (int i = 0; i < 3; i++)
        {
            int slotNum = i + 1;
            
            _slots[i] = _root.Q<VisualElement>($"slot{slotNum}");
            _itemImages[i] = _root.Q<VisualElement>($"slot{slotNum}ItemImage");
            _countLabels[i] = _root.Q<Label>($"slot{slotNum}Count");
            _counters[i] = _root.Q<VisualElement>($"slot{slotNum}Counter");
            
            Debug.Log($"[PlayerInventoryManager] 슬롯 {slotNum} 캐싱: " +
                     $"슬롯={(_slots[i] != null ? "OK" : "NULL")}, " +
                     $"이미지={(_itemImages[i] != null ? "OK" : "NULL")}, " +
                     $"카운트={(_countLabels[i] != null ? "OK" : "NULL")}, " +
                     $"카운터={(_counters[i] != null ? "OK" : "NULL")}");
        }
        
        Debug.Log("[PlayerInventoryManager] UI 요소 캐싱 완료");
    }
    
    private void SetupSlotEvents()
    {
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i; // 클로저를 위한 로컬 변수
            if (_slots[i] != null)
            {
                Debug.Log($"[PlayerInventoryManager] 슬롯 {slotIndex + 1} 이벤트 등록 중...");
                
                // 콜백 인스턴스 생성 및 저장
                _slotClickCallbacks[i] = evt => 
                {
                    Debug.Log($"[PlayerInventoryManager] 슬롯 {slotIndex + 1} 클릭 이벤트 발생!");
                    OnSlotClicked(slotIndex);
                };
                
                _slotPointerDownCallbacks[i] = evt => 
                {
                    Debug.Log($"[PlayerInventoryManager] 슬롯 {slotIndex + 1} 터치 이벤트 발생!");
                    OnSlotClicked(slotIndex);
                };
                
                _slotPointerMoveCallbacks[i] = evt => 
                {
                    // 마우스나 터치가 눌린 상태에서 움직이면 드래그 시작
                    if ((evt.pressedButtons & (1 << 0)) != 0) // 왼쪽 버튼
                    {
                        OnDragStart(slotIndex, evt);
                    }
                };
                
                // 저장된 콜백으로 등록
                _slots[i].RegisterCallback(_slotClickCallbacks[i]);
                _slots[i].RegisterCallback(_slotPointerDownCallbacks[i]);
                _slots[i].RegisterCallback(_slotPointerMoveCallbacks[i]);
                
                Debug.Log($"[PlayerInventoryManager] 슬롯 {slotIndex + 1} 이벤트 등록 완료");
            }
            else
            {
                Debug.LogError($"[PlayerInventoryManager] 슬롯 {slotIndex + 1}을 찾을 수 없습니다!");
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
        
        Debug.Log($"[PlayerInventoryManager] 슬롯 {slotIndex} UI 업데이트: {(item?.IsEmpty() != false ? "빈 슬롯" : $"{item.itemName} x{item.quantity}")}");
    }
    
    /// <summary>
    /// 슬롯 클릭 처리 - PlayerInventory 로직 사용
    /// 슬롯 0: 아이템 사용, 슬롯 1,2: 슬롯 0과 교체
    /// </summary>
    private void OnSlotClicked(int slotIndex)
    {
        if (PlayerInventory.Instance == null)
            return;
            
        Debug.Log($"[PlayerInventoryManager] 슬롯 {slotIndex + 1} 클릭! (배열 인덱스: {slotIndex})");
        
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
        
        Debug.Log("[PlayerInventoryManager] 슬롯 배경 스프라이트 설정 완료");
    }
    
    /// <summary>
    /// 드래그 시작 처리
    /// </summary>
    private void OnDragStart(int slotIndex, PointerMoveEvent evt)
    {
        if (PlayerInventory.Instance == null)
            return;
            
        InventoryItem item = PlayerInventory.Instance.GetItem(slotIndex);
        if (item == null || item.IsEmpty())
            return;
            
        Debug.Log($"[PlayerInventoryManager] 드래그 시작: 슬롯 {slotIndex}, 아이템: {item.itemName}");
        
#if UNITY_EDITOR
        // DragAndDrop에 데이터 설정
        DragAndDrop.PrepareStartDrag();
        DragAndDrop.SetGenericData("inventorySlotIndex", slotIndex);
        DragAndDrop.SetGenericData("itemName", item.itemName);
        DragAndDrop.SetGenericData("quantity", item.quantity);
        
        // 드래그 시각적 효과를 위한 아이콘 설정
        if (item.itemIcon != null)
        {
            DragAndDrop.objectReferences = new UnityEngine.Object[] { item.itemIcon };
        }
        
        DragAndDrop.StartDrag($"드래그 중: {item.itemName}");
        
        Debug.Log($"[PlayerInventoryManager] Unity DragAndDrop 시작: {item.itemName} (슬롯 {slotIndex})");
#else
        // 런타임에서는 다른 드래그 시스템 필요 (향후 구현)
        Debug.Log($"[PlayerInventoryManager] 런타임 드래그는 아직 지원되지 않습니다: {item.itemName}");
#endif
    }
}