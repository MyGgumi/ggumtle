using System.Collections.Generic;
using Models;
using UnityEngine;
using UnityEngine.UIElements;
using ViewModels.UI;
using Views.Core;

namespace Views
{
    public class ChestView : BaseView<ChestViewModel>
    {
        [Header("Chest UI Elements")]
        [SerializeField]
        private string _chestInstanceName = "chestBoxUI"; // Template Instance 이름 (변경 가능)
        [SerializeField]
        private string _chestContainerName = "chestBoxPanel";

        [SerializeField]
        private string _chestTitleName = ""; // 제목 요소 없음

        [SerializeField]
        private string _chestSlotsContainerName = "itemsGrid";

        [SerializeField]
        private string _closeButtonName = "closeButton";

        private VisualElement _chestContainer;
        private Label _chestTitle;
        private VisualElement _chestSlotsContainer;
        private Button _closeButton;

        [Header("Slot UI Elements")]
        [SerializeField]
        private string _slotTemplateClass = "chest-slot";
        private List<VisualElement> _slotElements = new List<VisualElement>();
        private List<Label> _slotCountLabels = new List<Label>();
        private List<VisualElement> _slotIcons = new List<VisualElement>();
        private List<VisualElement> _slotCounters = new List<VisualElement>();

        [Header("Animation Settings")]
        [SerializeField]
        private float _fadeInDuration = 0.3f;

        [SerializeField]
        private float _fadeOutDuration = 0.2f;

        protected override void InitializeUIElements()
        {
            base.InitializeUIElements();

            // Template Instance 찾기
            var chestInstance = GetUIElement<VisualElement>(_chestInstanceName);

            // Instance를 못 찾으면 다른 이름들도 시도
            if (chestInstance == null)
            {
                string[] alternativeNames = { "chestBox", "chest", "chestUI" };
                foreach (var name in alternativeNames)
                {
                    chestInstance = GetUIElement<VisualElement>(name);
                    if (chestInstance != null)
                    {
                        break;
                    }
                }
            }

            if (chestInstance != null)
            {
                _chestContainer = chestInstance.Q<VisualElement>(_chestContainerName);
            }
            else
            {
                // fallback: 직접 찾기
                _chestContainer = GetUIElement<VisualElement>(_chestContainerName);
            }

            // Template Instance 내에서 다른 요소들도 찾기
            if (chestInstance != null)
            {
                // 제목 요소는 UXML에 없으므로 null로 설정
                if (!string.IsNullOrEmpty(_chestTitleName))
                    _chestTitle = chestInstance.Q<Label>(_chestTitleName);

                _chestSlotsContainer = chestInstance.Q<VisualElement>(_chestSlotsContainerName);
                _closeButton = chestInstance.Q<Button>(_closeButtonName);
            }
            else
            {
                // fallback: 직접 찾기
                if (!string.IsNullOrEmpty(_chestTitleName))
                    _chestTitle = GetUIElement<Label>(_chestTitleName);

                _chestSlotsContainer = GetUIElement<VisualElement>(_chestSlotsContainerName);
                _closeButton = GetUIElement<Button>(_closeButtonName);
            }

            if (_chestContainer == null)
            {
                Debug.LogError($"[ChestView] Failed to find chest container '{_chestContainerName}'");
            }

            if (_closeButton != null)
            {
                _closeButton.clicked += OnCloseButtonClicked;
            }

            InitializeSlots();

            // 초기에는 숨김 상태로 설정
            if (_chestContainer != null)
            {
                _chestContainer.style.display = DisplayStyle.None;
            }
        }

        private void InitializeSlots()
        {
            _slotElements.Clear();
            _slotCountLabels.Clear();
            _slotIcons.Clear();
            _slotCounters.Clear();

            if (_chestSlotsContainer != null)
            {

                for (int i = 0; i < 9; i++)
                {
                    // UXML에서 미리 정의된 슬롯 요소들을 찾기
                    var slotElement = _chestSlotsContainer.Q<VisualElement>($"slot{i}");
                    var iconElement = _chestSlotsContainer.Q<VisualElement>($"itemIcon{i}");
                    var takeButton = _chestSlotsContainer.Q<Button>($"takeButton{i}");

                    if (slotElement != null)
                    {
                        int slotIndex = i;

                        // takeButton이 있으면 클릭 이벤트 등록
                        if (takeButton != null)
                        {
                            takeButton.clicked += () => OnSlotClicked(slotIndex);
                        }
                        else
                        {
                            // takeButton이 없으면 슬롯 자체에 클릭 이벤트 등록
                            slotElement.RegisterCallback<ClickEvent>(evt =>
                                OnSlotClicked(slotIndex)
                            );
                        }

                        _slotElements.Add(slotElement);
                        _slotIcons.Add(iconElement);
                        _slotCounters.Add(null);
                        _slotCountLabels.Add(null);
                    }
                    else
                    {
                        _slotElements.Add(null);
                        _slotIcons.Add(null);
                        _slotCounters.Add(null);
                        _slotCountLabels.Add(null);
                    }
                }
            }
        }

        protected override void SubscribeToViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.OnChestOpened += HandleChestOpened;
                _viewModel.OnChestClosed += HandleChestClosed;
                _viewModel.OnChestSlotChanged += HandleChestSlotChanged;
                _viewModel.OnChestUIVisibilityChanged += HandleUIVisibilityChanged;
                _viewModel.PropertyChanged += HandleViewModelPropertyChanged;

                RefreshUI();
            }
        }

        protected override void UnsubscribeFromViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.OnChestOpened -= HandleChestOpened;
                _viewModel.OnChestClosed -= HandleChestClosed;
                _viewModel.OnChestSlotChanged -= HandleChestSlotChanged;
                _viewModel.OnChestUIVisibilityChanged -= HandleUIVisibilityChanged;
                _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }
        }

        void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.clicked -= OnCloseButtonClicked;
            }
        }

        #region Event Handlers

        private void HandleChestOpened(string chestId)
        {
            UpdateChestTitle(_viewModel.CurrentChestName);
            RefreshAllSlots();
            ShowChestUI();
        }

        private void HandleChestClosed(string chestId)
        {
            HideChestUI();
        }

        private void HandleChestSlotChanged(string chestId, int slotIndex, string itemId, int count)
        {
            // 현재 열려있는 상자의 슬롯만 업데이트
            if (_viewModel != null && _viewModel.CurrentChestId == chestId)
            {
                UpdateSlotUI(slotIndex, itemId, count);
            }
            // RefreshAllSlots() 제거 - 개별 슬롯 업데이트만 수행
        }

        private void HandleUIVisibilityChanged(bool visible)
        {
            if (visible)
                ShowChestUI();
            else
                HideChestUI();
        }

        private void HandleViewModelPropertyChanged(string propertyName)
        {
            // 빈번한 RefreshUI 호출 방지
            // RefreshUI();
        }

        #endregion

        #region UI Updates

        private void UpdateChestTitle(string chestName)
        {
            if (_chestTitle != null)
            {
                _chestTitle.text = string.IsNullOrEmpty(chestName) ? "상자" : chestName;
            }
        }

        private void UpdateSlotUI(int slotIndex, string itemId, int count)
        {
            if (slotIndex < 0 || slotIndex >= _slotElements.Count)
                return;

            var slotElement = _slotElements[slotIndex];
            var iconElement = _slotIcons[slotIndex];
            var counterElement = _slotCounters[slotIndex];
            var countLabel = _slotCountLabels[slotIndex];

            if (slotElement == null)
                return;

            bool isEmpty = string.IsNullOrEmpty(itemId) || count <= 0;

            // 아이콘 업데이트
            if (iconElement != null)
            {
                iconElement.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;

                if (!isEmpty)
                {
                    SetItemIcon(iconElement, itemId);
                    iconElement.tooltip = $"{itemId} x{count}";
                }
                else
                {
                    iconElement.style.backgroundImage = null;
                    iconElement.tooltip = "";
                }
            }

            // 카운터는 ChestBox에서 제거됨 - 코드도 정리

            // CSS 클래스로 빈/찬 상태 표시
            slotElement.EnableInClassList("empty", isEmpty);
            slotElement.EnableInClassList("filled", !isEmpty);
        }

        private void RefreshAllSlots()
        {
            if (_viewModel == null)
            {
                Debug.LogWarning("[ChestView] RefreshAllSlots: _viewModel이 null입니다.");
                return;
            }

            var slots = _viewModel.CurrentChestSlots;
            if (slots == null)
            {
                Debug.LogWarning("[ChestView] RefreshAllSlots: CurrentChestSlots가 null입니다.");
                return;
            }

            for (int i = 0; i < _slotElements.Count && i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    UpdateSlotUI(i, slots[i].itemId, slots[i].count);
                }
                else
                {
                    UpdateSlotUI(i, "", 0);
                }
            }
        }

        private void ShowChestUI()
        {
            if (_chestContainer != null)
            {
                _chestContainer.style.display = DisplayStyle.Flex;
                _chestContainer.RemoveFromClassList("fade-out");
                _chestContainer.AddToClassList("fade-in");

                Debug.Log("[ChestView] Chest UI shown");
            }
        }

        private void HideChestUI()
        {
            if (_chestContainer != null)
            {
                // 즉시 숨김 - 애니메이션 딜레이 제거
                _chestContainer.style.display = DisplayStyle.None;
                _chestContainer.RemoveFromClassList("fade-in");
                _chestContainer.AddToClassList("fade-out");

                Debug.Log("[ChestView] Hiding chest UI (immediate)");
            }
        }

        private void RefreshUI()
        {
            if (_viewModel == null)
                return;

            UpdateChestTitle(_viewModel.CurrentChestName);
            RefreshAllSlots();

            if (_viewModel.IsUIVisible && _viewModel.IsChestOpen)
            {
                ShowChestUI();
            }
            else
            {
                HideChestUI();
            }
        }

        private void SetItemIcon(VisualElement iconElement, string itemId)
        {
            if (GlobalItemManager.Instance != null)
            {
                // 기존 가명을 실제 ID로 변환 (호환성)
                string actualItemId = Models.ItemTypeHelper.ConvertLegacyId(itemId);

                // ItemDatabase에서 아이템 데이터 가져오기
                var itemData = GlobalItemManager.Instance.GetItemData(actualItemId);
                if (itemData != null && itemData.itemIcon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(itemData.itemIcon);
                    Debug.Log($"[ChestView] 아이템 아이콘 설정: {actualItemId}");
                }
                else
                {
                    // 아이템을 찾지 못한 경우 원래 ID로도 시도
                    itemData = GlobalItemManager.Instance.GetItemData(itemId);
                    if (itemData != null && itemData.itemIcon != null)
                    {
                        iconElement.style.backgroundImage = new StyleBackground(itemData.itemIcon);
                        Debug.Log($"[ChestView] 아이템 아이콘 설정 (원래 ID): {itemId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ChestView] 아이템 아이콘을 찾을 수 없음: {itemId} / {actualItemId}");
                        iconElement.style.backgroundImage = null;
                    }
                }
            }
        }

        #endregion

        #region User Interactions

        private void OnSlotClicked(int slotIndex)
        {
            if (_viewModel == null || !_viewModel.IsChestOpen)
                return;

            var inventoryViewModel = InventoryViewModel.Instance;
            if (inventoryViewModel != null)
            {
                if (_viewModel.TransferToInventory(slotIndex, inventoryViewModel))
                {
                    Debug.Log(
                        $"[ChestView] Transferred item from chest slot {slotIndex} to inventory"
                    );
                }
                else
                {
                    Debug.Log(
                        $"[ChestView] Failed to transfer item from chest slot {slotIndex} - inventory full or slot empty"
                    );
                }
            }
            else
            {
                if (_viewModel.TakeItemFromSlot(slotIndex))
                {
                    Debug.Log($"[ChestView] Took item from chest slot {slotIndex}");
                }
            }
        }

        private void OnCloseButtonClicked()
        {
            if (_viewModel != null)
            {
                _viewModel.CloseChest();
            }
        }

        #endregion

        #region Public API

        public void ForceShowUI()
        {
            ShowChestUI();
        }

        public void ForceHideUI()
        {
            HideChestUI();
        }

        public bool IsUIVisible =>
            _chestContainer != null && _chestContainer.style.display == DisplayStyle.Flex;

        #endregion
    }
}
