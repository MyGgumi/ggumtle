using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    [Header("UI References")]
    private VisualElement _root;
    private VisualElement _lightArea;
    private Label _lightCountLabel;

    [Header("Inventory Slots")]
    private VisualElement[] _slots = new VisualElement[3];
    private Label[] _countLabels = new Label[3];
    private VisualElement[] _counters = new VisualElement[3];

    [Header("Sprites")]
    public Sprite lightJellySprite;
    public Sprite backgroundItemSlotSprite;

    [Header("Current State")]
    public InventoryData inventoryData;

    [Header("먹이 시스템 통합")]
    public bool showDebugLogs = true;
    
    // UI 이벤트 콜백 저장 변수들
    private EventCallback<ClickEvent>[] _slotClickCallbacks = new EventCallback<ClickEvent>[3];

    // Static 이벤트는 HUDEvents로 이동됨

    private void Awake()
    {
        // 싱글톤 설정 (UniversalHUDController에서 생성되므로 중복 체크만)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning(
                "[ResourceManager] 이미 인스턴스가 존재합니다. UniversalHUDController에서 관리됨"
            );
        }

        inventoryData = new InventoryData();
    }

    public void Initialize(VisualElement root)
    {
        _root = root;
        CacheUIElements();
        SetupSlotEvents();
        UpdateUI();
    }

    private void CacheUIElements()
    {
        _lightArea = _root.Q<VisualElement>("LightArea");
        // LightArea 안의 Label을 직접 찾기 (name이 없으므로)
        _lightCountLabel = _lightArea?.Q<Label>();

        // UXML의 기본 텍스트 초기화
        if (_lightCountLabel != null)
        {
            _lightCountLabel.text = "";
        }

        // 인벤토리 슬롯들 캐싱
        for (int i = 0; i < 3; i++)
        {
            int slotNum = i + 1;
            _slots[i] = _root.Q<VisualElement>($"slot{slotNum}");
            _countLabels[i] = _root.Q<Label>($"slot{slotNum}Count");
            _counters[i] = _root.Q<VisualElement>($"slot{slotNum}Counter");
        }
    }

    private void SetupSlotEvents()
    {
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i; // 클로저를 위한 로컬 변수
            if (_slots[i] != null)
            {
                // 콜백 인스턴스 생성 및 저장
                _slotClickCallbacks[i] = evt => OnSlotClicked(slotIndex + 1);
                
                // 저장된 콜백으로 등록
                _slots[i].RegisterCallback(_slotClickCallbacks[i]);
            }
        }
    }

    public void SetLightCount(int count)
    {
        SetLightCountInternal(count);
    }

    public void SetLightCountInternal(int count)
    {
        if (_root == null)
        {
            Debug.LogError(
                "[ResourceManager] _root가 null입니다. Initialize가 호출되지 않았습니다."
            );
            return;
        }

        if (_lightCountLabel != null)
        {
            _lightCountLabel.text = count.ToString();
        }

        Debug.Log($"[ResourceManager] 빛 개수 UI 설정: {count}");
    }

    /// <summary>
    /// FeedingInventory에서 호출하는 UI 업데이트 메서드
    /// </summary>
    public void UpdateLightUI(int count)
    {
        SetLightCountInternal(count);
    }

    public void SetLightAreaVisibility(bool visible)
    {
        if (_lightArea != null)
        {
            _lightArea.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public bool UseInventoryItem(int slotNumber)
    {
        InventorySlot slot = GetInventorySlot(slotNumber);
        if (slot != null && slot.UseItem())
        {
            UpdateSlotUI(slotNumber, slot.itemCount);
            HUDEvents.TriggerInventoryUse(slotNumber, 1);

            Debug.Log($"[ResourceManager] 슬롯 {slotNumber} 사용, 남은 개수: {slot.itemCount}");
            return true;
        }

        Debug.Log($"[ResourceManager] 슬롯 {slotNumber} 사용 불가 (개수: {slot?.itemCount ?? 0})");
        return false;
    }

    public bool AddInventoryItem(int slotNumber, int amount = 1)
    {
        InventorySlot slot = GetInventorySlot(slotNumber);
        if (slot != null && slot.AddItem(amount))
        {
            UpdateSlotUI(slotNumber, slot.itemCount);
            HUDEvents.TriggerInventoryUpdate(slotNumber, slot.itemCount);

            Debug.Log(
                $"[ResourceManager] 슬롯 {slotNumber}에 {amount}개 추가, 총 개수: {slot.itemCount}"
            );
            return true;
        }

        Debug.Log(
            $"[ResourceManager] 슬롯 {slotNumber}에 추가 불가 (현재: {slot?.itemCount ?? 0}, 최대: {slot?.maxCount ?? 0})"
        );
        return false;
    }

    public void SetInventoryItemCount(int slotNumber, int count)
    {
        InventorySlot slot = GetInventorySlot(slotNumber);
        if (slot != null)
        {
            slot.itemCount = Mathf.Clamp(count, 0, slot.maxCount);
            UpdateSlotUI(slotNumber, slot.itemCount);
            HUDEvents.TriggerInventoryUpdate(slotNumber, slot.itemCount);

            Debug.Log($"[ResourceManager] 슬롯 {slotNumber} 개수를 {slot.itemCount}로 설정");
        }
    }

    public int GetInventoryItemCount(int slotNumber)
    {
        InventorySlot slot = GetInventorySlot(slotNumber);
        return slot?.itemCount ?? 0;
    }

    public bool CanUseInventorySlot(int slotNumber)
    {
        InventorySlot slot = GetInventorySlot(slotNumber);
        return slot != null && slot.CanUse();
    }

    public void SetSlotVisibility(int slotNumber, bool visible)
    {
        if (slotNumber >= 1 && slotNumber <= 3)
        {
            int index = slotNumber - 1;
            if (_slots[index] != null)
            {
                _slots[index].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }

    public void UpdateInventoryFromData(InventoryData data)
    {
        if (data != null)
        {
            inventoryData = data;
            UpdateSlotUI(1, data.slot1.itemCount);
            UpdateSlotUI(2, data.slot2.itemCount);
            UpdateSlotUI(3, data.slot3.itemCount);
        }
    }

    private void OnSlotClicked(int slotNumber)
    {
        Debug.Log($"[ResourceManager] 인벤토리 슬롯 {slotNumber} 클릭!");
        UseInventoryItem(slotNumber);
    }

    private InventorySlot GetInventorySlot(int slotNumber)
    {
        if (inventoryData == null)
            return null;

        return slotNumber switch
        {
            1 => inventoryData.slot1,
            2 => inventoryData.slot2,
            3 => inventoryData.slot3,
            _ => null,
        };
    }

    private void UpdateSlotUI(int slotNumber, int count)
    {
        if (slotNumber < 1 || slotNumber > 3)
            return;

        int index = slotNumber - 1;

        if (_countLabels[index] != null)
        {
            _countLabels[index].text = count.ToString();
        }

        if (_counters[index] != null)
        {
            _counters[index].style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void UpdateUI()
    {
        // FeedingInventory에서 현재 빛 개수를 가져와서 UI 업데이트
        int lightCount = FeedingInventory.Instance?.GetMushroomCount() ?? 0;
        SetLightCount(lightCount);

        // 인벤토리 UI 업데이트
        if (inventoryData != null)
        {
            UpdateSlotUI(1, inventoryData.slot1.itemCount);
            UpdateSlotUI(2, inventoryData.slot2.itemCount);
            UpdateSlotUI(3, inventoryData.slot3.itemCount);
        }
    }

    public void SetSprites(Sprite lightSprite, Sprite itemSlotSprite)
    {
        lightJellySprite = lightSprite;
        backgroundItemSlotSprite = itemSlotSprite;

        // 빛 아이콘 설정
        var lightIcon = _root.Q<VisualElement>("lightIcon");
        if (lightIcon != null && lightSprite != null)
        {
            lightIcon.style.backgroundImage = new StyleBackground(lightSprite);
        }

        // 슬롯 배경 설정
        for (int i = 0; i < 3; i++)
        {
            if (_slots[i] != null && itemSlotSprite != null)
            {
                _slots[i].style.backgroundImage = new StyleBackground(itemSlotSprite);
            }
        }
    }

    public void InitializeForTesting()
    {
        SetInventoryItemCount(1, 2);
        SetInventoryItemCount(2, 1);
        SetInventoryItemCount(3, 3);
        SetLightCount(24);
    }

    public int GetCurrentLightCount() => FeedingInventory.Instance?.GetMushroomCount() ?? 0;

    public InventoryData GetInventoryData() => inventoryData;

    #region 먹이 시스템 (Light와 통합)

    /// <summary>
    /// 빛(먹이) 추가 - FeedingInventory로 위임
    /// </summary>
    public int AddLight(int amount)
    {
        if (amount <= 0)
            return 0;

        if (FeedingInventory.Instance != null)
        {
            return FeedingInventory.Instance.AddMushrooms(amount);
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError("[ResourceManager] FeedingInventory.Instance가 null입니다!");
            return 0;
        }
    }

    /// <summary>
    /// 빛(먹이) 제거 시도 - FeedingInventory로 위임
    /// </summary>
    public bool RemoveLight(int amount)
    {
        if (amount <= 0)
            return false;

        if (FeedingInventory.Instance != null)
        {
            return FeedingInventory.Instance.RemoveMushrooms(amount);
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError("[ResourceManager] FeedingInventory.Instance가 null입니다!");
            return false;
        }
    }

    /// <summary>
    /// 빛이 충분한지 확인 - FeedingInventory로 위임
    /// </summary>
    public bool HasEnoughLight(int requiredAmount)
    {
        return FeedingInventory.Instance?.HasEnoughMushrooms(requiredAmount) ?? false;
    }

    /// <summary>
    /// 꿈틀이 먹이주기용 - FeedingInventory로 위임
    /// </summary>
    public bool CanPlayerFeed()
    {
        return FeedingInventory.Instance?.GetMushroomCount() > 0;
    }

    /// <summary>
    /// 꿈틀이 먹이 소모 시도 - FeedingInventory로 위임
    /// </summary>
    public bool TryConsumeLightForFeeding(int amount)
    {
        return RemoveLight(amount);
    }

    #endregion
    
    #region Unity Lifecycle
    
    private void OnDestroy()
    {
        UnregisterUIEvents();
    }
    
    private void UnregisterUIEvents()
    {
        // 슬롯 이벤트 해제
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && _slotClickCallbacks[i] != null)
            {
                _slots[i].UnregisterCallback(_slotClickCallbacks[i]);
            }
        }
        
        Debug.Log("[ResourceManager] UI 이벤트 구독 해제 완료");
    }
    
    #endregion
}
