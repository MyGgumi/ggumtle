using UnityEngine;
using UnityEngine.UIElements;
using ViewModels.UI;
using Views.Core;

namespace Views
{
    public class InventoryView : BaseView<InventoryViewModel>
    {
        [Header("Player Inventory UI Elements")]
        [SerializeField]
        private string[] _playerSlotNames = { "slot1", "slot2", "slot3" };
        private VisualElement[] _playerSlots;
        private Label[] _playerSlotCountLabels;
        private VisualElement[] _playerSlotIcons;
        private VisualElement[] _playerSlotCounters;

        [Header("Feeding Inventory UI Elements")]
        [SerializeField]
        private string _feedingCountLabelName = "FeedingCountLabel";

        [SerializeField]
        private string _feedingProgressBarName = "FeedingProgressBar";
        private Label _feedingCountLabel;
        private ProgressBar _feedingProgressBar;

        protected override void InitializeUIElements()
        {
            base.InitializeUIElements();

            InitializePlayerSlots();
            InitializeFeedingInventory();
        }

        private void InitializePlayerSlots()
        {
            _playerSlots = new VisualElement[_playerSlotNames.Length];
            _playerSlotCountLabels = new Label[_playerSlotNames.Length];
            _playerSlotIcons = new VisualElement[_playerSlotNames.Length];
            _playerSlotCounters = new VisualElement[_playerSlotNames.Length];

            for (int i = 0; i < _playerSlotNames.Length; i++)
            {
                _playerSlots[i] = GetUIElement<VisualElement>(_playerSlotNames[i]);
                if (_playerSlots[i] != null)
                {
                    _playerSlotCountLabels[i] = _playerSlots[i].Q<Label>($"slot{i + 1}Count");
                    _playerSlotIcons[i] = _playerSlots[i].Q<VisualElement>($"slot{i + 1}ItemImage");
                    _playerSlotCounters[i] = _playerSlots[i]
                        .Q<VisualElement>($"slot{i + 1}Counter");

                    int slotIndex = i;
                    _playerSlots[i]
                        .RegisterCallback<ClickEvent>(evt => OnPlayerSlotClicked(slotIndex));

                    Debug.Log(
                        $"[InventoryView] 슬롯 {i + 1} 초기화: slot={_playerSlots[i] != null}, icon={_playerSlotIcons[i] != null}, counter={_playerSlotCounters[i] != null}, label={_playerSlotCountLabels[i] != null}"
                    );
                }
            }
        }

        private void InitializeFeedingInventory()
        {
            _feedingCountLabel = GetUIElement<Label>(_feedingCountLabelName);
            _feedingProgressBar = GetUIElement<ProgressBar>(_feedingProgressBarName);
        }

        protected override void SubscribeToViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.OnPlayerSlotChanged += HandlePlayerSlotChanged;
                _viewModel.OnFeedingCountChanged += HandleFeedingCountChanged;
                _viewModel.OnGlobalItemChanged += HandleGlobalItemChanged;
                _viewModel.PropertyChanged += HandleViewModelPropertyChanged;

                RefreshAllUI();
            }
        }

        protected override void UnsubscribeFromViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.OnPlayerSlotChanged -= HandlePlayerSlotChanged;
                _viewModel.OnFeedingCountChanged -= HandleFeedingCountChanged;
                _viewModel.OnGlobalItemChanged -= HandleGlobalItemChanged;
                _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
            }
        }

        #region Event Handlers

        private void HandlePlayerSlotChanged(int slotIndex, string itemId, int count)
        {
            UpdatePlayerSlotUI(slotIndex, itemId, count);
        }

        private void HandleFeedingCountChanged(int newCount)
        {
            UpdateFeedingCountUI(newCount);
        }

        private void HandleGlobalItemChanged(string itemId, int count)
        {
            Debug.Log($"[InventoryView] Global item {itemId} changed to {count}");
        }

        private void HandleViewModelPropertyChanged(string propertyName)
        {
            RefreshAllUI();
        }

        #endregion

        #region UI Updates

        private void UpdatePlayerSlotUI(int slotIndex, string itemId, int count)
        {
            if (
                slotIndex < 0
                || slotIndex >= _playerSlots.Length
                || _playerSlots[slotIndex] == null
            )
                return;

            var slot = _playerSlots[slotIndex];
            var countLabel = _playerSlotCountLabels[slotIndex];
            var icon = _playerSlotIcons[slotIndex];
            var counter = _playerSlotCounters[slotIndex];

            bool isEmpty = string.IsNullOrEmpty(itemId) || count <= 0;

            // 아이콘 업데이트
            if (icon != null)
            {
                icon.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;

                if (!isEmpty)
                {
                    SetItemIcon(icon, itemId);
                    icon.tooltip = $"{itemId} x{count}";
                }
                else
                {
                    icon.style.backgroundImage = null;
                    icon.tooltip = "";
                }
            }

            // 카운터 컨테이너 표시/숨김
            if (counter != null)
            {
                counter.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // 카운트 라벨 업데이트
            if (countLabel != null)
            {
                countLabel.text = isEmpty ? "" : count.ToString();
                Debug.Log($"[InventoryView] 슬롯 {slotIndex + 1} 카운터 업데이트: {count}개");
            }

            slot.EnableInClassList("empty", isEmpty);
            slot.EnableInClassList("filled", !isEmpty);
        }

        private void UpdateFeedingCountUI(int count)
        {
            if (_feedingCountLabel != null)
            {
                _feedingCountLabel.text = count.ToString();
            }

            if (_feedingProgressBar != null && _viewModel != null)
            {
                float progress = (float)count / _viewModel.GetMaxFeedingCount();
                _feedingProgressBar.value = progress * 100f;
            }
        }

        private void RefreshAllUI()
        {
            if (_viewModel == null)
                return;

            for (int i = 0; i < _playerSlotNames.Length; i++)
            {
                UpdatePlayerSlotUI(
                    i,
                    _viewModel.GetPlayerSlotItemId(i),
                    _viewModel.GetPlayerSlotCount(i)
                );
            }

            UpdateFeedingCountUI(_viewModel.GetFeedingCount());
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
                }
                else
                {
                    // 아이템을 찾지 못한 경우 원래 ID로도 시도
                    itemData = GlobalItemManager.Instance.GetItemData(itemId);
                    if (itemData != null && itemData.itemIcon != null)
                    {
                        iconElement.style.backgroundImage = new StyleBackground(itemData.itemIcon);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[InventoryView] 아이템 아이콘을 찾을 수 없음: {itemId} / {actualItemId}"
                        );
                        iconElement.style.backgroundImage = null;
                    }
                }
            }
        }

        #endregion

        #region User Interactions

        private void OnPlayerSlotClicked(int slotIndex)
        {
            if (_viewModel == null)
                return;

            if (slotIndex == 0)
            {
                // 슬롯 0번: 아이템 사용
                if (_viewModel.CanUsePlayerSlot(slotIndex))
                {
                    _viewModel.UsePlayerSlot(slotIndex, 1);
                    Debug.Log($"[InventoryView] 슬롯 0번 아이템 사용");
                }
                else
                {
                    Debug.Log($"[InventoryView] 슬롯 0번에 사용할 아이템이 없습니다.");
                }
            }
            else
            {
                // 다른 슬롯: 슬롯 0과 스왑
                if (_viewModel.SwapPlayerSlots(0, slotIndex))
                {
                    Debug.Log($"[InventoryView] 슬롯 0번과 슬롯 {slotIndex}번 스왑 완료");
                }
                else
                {
                    Debug.Log($"[InventoryView] 슬롯 0번과 슬롯 {slotIndex}번 스왑 실패");
                }
            }
        }

        #endregion

        #region Public API

        public void ShowPlayerInventory()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.Flex;
            }
        }

        public void HidePlayerInventory()
        {
            if (_rootElement != null)
            {
                _rootElement.style.display = DisplayStyle.None;
            }
        }

        public void UpdateSlotItem(int slotIndex, string itemId, int count)
        {
            if (_viewModel != null)
            {
                _viewModel.AddToPlayerSlot(slotIndex, itemId, count);
            }
        }

        #endregion
    }
}
