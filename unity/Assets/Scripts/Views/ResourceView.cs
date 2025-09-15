using System;
using UnityEngine;
using UnityEngine.UIElements;
using MVVM.UI;

namespace Views
{
    public class ResourceView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField] private ResourceViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _lightArea;
        private Label _lightCountLabel;

        [Header("Inventory Slots")]
        private VisualElement[] _slots = new VisualElement[3];
        private Label[] _countLabels = new Label[3];
        private VisualElement[] _counters = new VisualElement[3];

        // UI 이벤트 콜백 저장 변수들
        private EventCallback<ClickEvent>[] _slotClickCallbacks = new EventCallback<ClickEvent>[3];

        public void Initialize(VisualElement root, ResourceViewModel viewModel)
        {
            _root = root;
            this.viewModel = viewModel;

            CacheUIElements();
            SetupSlotEvents();
            SubscribeToViewModel();
            UpdateAllUI();

            Debug.Log("[ResourceView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _lightArea = _root.Q<VisualElement>("LightArea");
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

            Debug.Log($"[ResourceView] UI 요소 캐싱 완료: " +
                     $"빛영역={(_lightArea != null ? "OK" : "NULL")}, " +
                     $"빛라벨={(_lightCountLabel != null ? "OK" : "NULL")}, " +
                     $"슬롯들={(_slots[0] != null && _slots[1] != null && _slots[2] != null ? "OK" : "NULL")}");
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

            Debug.Log("[ResourceView] 슬롯 이벤트 설정 완료");
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null) return;

            viewModel.LightCountChanged += OnLightCountChanged;
            viewModel.InventorySlotChanged += OnInventorySlotChanged;
            viewModel.InventorySlotUsed += OnInventorySlotUsed;
            viewModel.SpritesChanged += OnSpritesChanged;

            Debug.Log("[ResourceView] ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (viewModel == null) return;

            viewModel.LightCountChanged -= OnLightCountChanged;
            viewModel.InventorySlotChanged -= OnInventorySlotChanged;
            viewModel.InventorySlotUsed -= OnInventorySlotUsed;
            viewModel.SpritesChanged -= OnSpritesChanged;

            Debug.Log("[ResourceView] ViewModel 이벤트 구독 해제 완료");
        }

        #region ViewModel Event Handlers

        private void OnLightCountChanged(int newCount)
        {
            UpdateLightDisplay(newCount);
        }

        private void OnInventorySlotChanged(int slotNumber, int newCount)
        {
            UpdateSlotUI(slotNumber, newCount);
        }

        private void OnInventorySlotUsed(int slotNumber, int usedAmount)
        {
            // 슬롯 사용시 추가 UI 효과가 필요하면 여기서 처리
            Debug.Log($"[ResourceView] 슬롯 {slotNumber} 사용됨 (사용량: {usedAmount})");
        }

        private void OnSpritesChanged(Sprite lightSprite, Sprite itemSlotSprite)
        {
            UpdateSprites(lightSprite, itemSlotSprite);
        }

        #endregion

        #region UI Update Methods

        private void UpdateLightDisplay(int count)
        {
            if (_lightCountLabel != null)
            {
                _lightCountLabel.text = count.ToString();
            }

            Debug.Log($"[ResourceView] 빛 개수 UI 업데이트: {count}");
        }

        private void UpdateSlotUI(int slotNumber, int count)
        {
            if (slotNumber < 1 || slotNumber > 3) return;

            int index = slotNumber - 1;

            if (_countLabels[index] != null)
            {
                _countLabels[index].text = count.ToString();
            }

            if (_counters[index] != null)
            {
                _counters[index].style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            Debug.Log($"[ResourceView] 슬롯 {slotNumber} UI 업데이트: {count}개");
        }

        private void UpdateSprites(Sprite lightSprite, Sprite itemSlotSprite)
        {
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

            Debug.Log("[ResourceView] 스프라이트 업데이트 완료");
        }

        private void UpdateAllUI()
        {
            if (viewModel == null) return;

            UpdateLightDisplay(viewModel.LightCount);
            UpdateSlotUI(1, viewModel.Slot1.itemCount);
            UpdateSlotUI(2, viewModel.Slot2.itemCount);
            UpdateSlotUI(3, viewModel.Slot3.itemCount);
            UpdateSprites(viewModel.LightJellySprite, viewModel.BackgroundItemSlotSprite);
        }

        #endregion

        #region Event Handlers

        private void OnSlotClicked(int slotNumber)
        {
            Debug.Log($"[ResourceView] 인벤토리 슬롯 {slotNumber} 클릭!");

            if (viewModel != null)
            {
                viewModel.UseInventoryItem(slotNumber);
            }
        }

        #endregion

        #region Public API

        public void SetLightAreaVisibility(bool visible)
        {
            if (_lightArea != null)
            {
                _lightArea.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
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

        public void UpdateFromFeedingInventory()
        {
            if (viewModel != null)
            {
                viewModel.UpdateLightFromInventoryViewModel();
            }
        }

        public void InitializeForTesting()
        {
            if (viewModel != null)
            {
                viewModel.SetInventoryItemCount(1, 2);
                viewModel.SetInventoryItemCount(2, 1);
                viewModel.SetInventoryItemCount(3, 3);
                viewModel.LightCount = 24;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewModel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
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

            Debug.Log("[ResourceView] UI 이벤트 구독 해제 완료");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log($"[ResourceView] UI State:\n" +
                     $"  LightCount: {(_lightCountLabel?.text ?? "NULL")}\n" +
                     $"  Slot1Count: {(_countLabels[0]?.text ?? "NULL")}\n" +
                     $"  Slot2Count: {(_countLabels[1]?.text ?? "NULL")}\n" +
                     $"  Slot3Count: {(_countLabels[2]?.text ?? "NULL")}\n" +
                     $"  LightArea Visible: {(_lightArea?.style.display.value == DisplayStyle.Flex)}\n" +
                     $"  ViewModel: {(viewModel != null ? "Connected" : "NULL")}");
        }

        #endregion
    }
}