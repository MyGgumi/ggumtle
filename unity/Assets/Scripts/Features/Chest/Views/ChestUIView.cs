using System.Collections.Generic;
using Features.Chest.ViewModels;
using Features.Inventory.Services;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Features.Chest.Views
{
    /// <summary>
    /// 상자 UI를 담당하는 View (UI Toolkit 기반)
    /// GgumtleUIView 패턴을 따라 VisualElement + R3로 구현
    /// </summary>
    public class ChestUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private ChestViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _chestContainer;
        private VisualElement _chestSlotsContainer;
        private Button _closeButton;
        private List<VisualElement> _slotElements;
        private List<VisualElement> _slotIcons;
        private List<Button> _takeButtons;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;
        [SerializeField]
        private string chestContainerName = "chestBoxPanel";
        [SerializeField]
        private string slotsContainerName = "itemsGrid";
        [SerializeField]
        private string closeButtonName = "closeButton";

        private CompositeDisposable _disposables = new();

        private IInventoryService _inventoryService;

        #region Initialization

        [Inject]
        public void Construct(ChestViewModel chestViewModel, IInventoryService inventoryService)
        {
            viewModel = chestViewModel;
            _inventoryService = inventoryService;
            if (enableDebugLogs)
                Debug.Log($"[ChestUIView] VContainer 의존성 주입 완료: VM={viewModel != null}, Service={_inventoryService != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError("[ChestUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            InitializeUI();

            if (enableDebugLogs)
                Debug.Log("[ChestUIView] 초기화 완료");
        }

        #endregion

        #region UI Setup

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[ChestUIView] Root VisualElement가 null입니다.");
                return;
            }

            // 상자 컨테이너
            _chestContainer = _root.Q<VisualElement>(chestContainerName);
            _chestSlotsContainer = _root.Q<VisualElement>(slotsContainerName);
            _closeButton = _root.Q<Button>(closeButtonName);

            // 슬롯 요소들 캐싱
            CacheSlotElements();

            if (_closeButton != null)
            {
                _closeButton.clicked += OnCloseButtonClicked;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[ChestUIView] UI 요소 캐싱 완료: " +
                    $"Container={(_chestContainer != null ? "OK" : "NULL")}, " +
                    $"Slots={_slotElements?.Count ?? 0}, " +
                    $"CloseButton={(_closeButton != null ? "OK" : "NULL")}");
            }
        }

        private void CacheSlotElements()
        {
            _slotElements = new List<VisualElement>();
            _slotIcons = new List<VisualElement>();
            _takeButtons = new List<Button>();

            if (_chestSlotsContainer != null)
            {
                // 슬롯 0-8 찾기 (9개 슬롯)
                for (int i = 0; i < 9; i++)
                {
                    var slot = _chestSlotsContainer.Q<VisualElement>($"slot{i}");
                    if (slot != null)
                    {
                        _slotElements.Add(slot);

                        var icon = slot.Q<VisualElement>($"itemIcon{i}");
                        _slotIcons.Add(icon);

                        var takeButton = slot.Q<Button>($"takeButton{i}");
                        if (takeButton != null)
                        {
                            int slotIndex = i; // 클로저를 위한 로컬 복사
                            takeButton.clicked += () => OnSlotClicked(slotIndex);
                            _takeButtons.Add(takeButton);
                        }
                        else
                        {
                            // takeButton이 없으면 슬롯 자체에 클릭 이벤트 등록
                            int slotIndex = i;
                            slot.RegisterCallback<ClickEvent>(evt => OnSlotClicked(slotIndex));
                            _takeButtons.Add(null);
                        }
                    }
                }
            }
        }

        private void InitializeUI()
        {
            // 초기에는 상자 UI 숨김
            HideChestUI();

            // 슬롯 초기화
            UpdateAllSlots();

            if (enableDebugLogs)
                Debug.Log("[ChestUIView] UI 초기화 완료");
        }

        #endregion

        #region ViewModel Subscription

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
            {
                Debug.LogError("[ChestUIView] ChestViewModel이 설정되지 않음");
                return;
            }

            // 상자 열림/닫힘
            viewModel.IsChestOpen
                .Subscribe(isOpen =>
                {
                    if (enableDebugLogs)
                        Debug.Log($"[ChestUIView] 상자 {(isOpen ? "열림" : "닫힘")}");
                    if (isOpen)
                        ShowChestUI();
                    else
                        HideChestUI();
                })
                .AddTo(_disposables);

            // UI 가시성
            viewModel.IsUIVisible
                .Subscribe(visible =>
                {
                    if (visible && viewModel.IsChestOpen.Value)
                        ShowChestUI();
                    else
                        HideChestUI();
                })
                .AddTo(_disposables);

            // 슬롯 데이터 변경
            viewModel.CurrentChestSlots
                .Subscribe(slots =>
                {
                    if (enableDebugLogs)
                        Debug.Log($"[ChestUIView] 슬롯 업데이트: {slots?.Length ?? 0}개");
                    UpdateSlots(slots);
                })
                .AddTo(_disposables);

            // 상자 이름
            viewModel.CurrentChestName
                .Subscribe(name =>
                {
                    UpdateChestTitle(name);
                })
                .AddTo(_disposables);

            // 상호작용 상태
            viewModel.IsUIVisible
                .Subscribe(isInteracting =>
                {
                    SetInteractionState(isInteracting);
                })
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[ChestUIView] ViewModel R3 구독 완료");
        }

        #endregion

        #region UI Update Methods

        private void ShowChestUI()
        {
            if (_chestContainer != null)
            {
                _chestContainer.style.display = DisplayStyle.Flex;
                _chestContainer.RemoveFromClassList("fade-out");
                _chestContainer.AddToClassList("fade-in");

                if (enableDebugLogs)
                    Debug.Log("[ChestUIView] 상자 UI 표시");
            }
        }

        private void HideChestUI()
        {
            if (_chestContainer != null)
            {
                _chestContainer.style.display = DisplayStyle.None;
                _chestContainer.RemoveFromClassList("fade-in");
                _chestContainer.AddToClassList("fade-out");

                if (enableDebugLogs)
                    Debug.Log("[ChestUIView] 상자 UI 숨김");
            }
        }

        private void UpdateSlots(object[] slots)
        {
            if (slots == null) return;

            for (int i = 0; i < slots.Length && i < _slotElements.Count; i++)
            {
                // TODO: 서버 슬롯 데이터 파싱
                // 임시로 빈 슬롯으로 처리
                UpdateSlotUI(i, "", 0);
            }
        }

        private void UpdateSlotUI(int index, string itemId, int count)
        {
            if (index >= _slotElements.Count) return;

            var slotElement = _slotElements[index];
            var icon = _slotIcons[index];

            if (slotElement == null) return;

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

            // CSS 클래스로 빈/찬 상태 표시
            slotElement?.EnableInClassList("empty", isEmpty);
            slotElement?.EnableInClassList("filled", !isEmpty);
        }

        private void UpdateAllSlots()
        {
            if (viewModel?.CurrentChestSlots?.Value != null)
            {
                UpdateSlots(viewModel.CurrentChestSlots.Value);
            }
        }

        private void UpdateChestTitle(string name)
        {
            // Title Label이 있다면 업데이트 (현재 UXML에는 없음)
            var titleLabel = _root?.Q<Label>("chestTitle");
            if (titleLabel != null)
            {
                titleLabel.text = string.IsNullOrEmpty(name) ? "상자" : name;
            }
        }

        private void SetInteractionState(bool isInteracting)
        {
            if (_chestContainer != null)
            {
                if (isInteracting)
                {
                    _chestContainer.AddToClassList("interacting");
                }
                else
                {
                    _chestContainer.RemoveFromClassList("interacting");
                }
            }
        }

        private void SetItemIcon(VisualElement iconElement, string itemId)
        {
            if (int.TryParse(itemId, out int id))
            {
                var itemDef = Features.Item.Services.ItemDefinitionService.GetItemById(id);
                if (itemDef?.ItemIcon != null)
                {
                    iconElement.style.backgroundImage = new StyleBackground(itemDef.ItemIcon);
                }
                else
                {
                    iconElement.style.backgroundImage = null;
                }
            }
            else
            {
                iconElement.style.backgroundImage = null;
            }
        }

        #endregion

        #region Event Handlers

        private void OnSlotClicked(int slotIndex)
        {
            if (enableDebugLogs)
                Debug.Log($"[ChestUIView] 슬롯 {slotIndex} 클릭됨");

            // 인벤토리 서비스를 통해 아이템 전송
            if (_inventoryService != null && viewModel != null)
            {
                var chestId = viewModel.CurrentChestId.Value;
                _inventoryService.RequestItemFromChest(chestId, slotIndex);
            }
        }

        private void OnCloseButtonClicked()
        {
            if (enableDebugLogs)
                Debug.Log("[ChestUIView] 닫기 버튼 클릭됨");

            viewModel?.CloseChest();
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables.Dispose();

            // 클릭 이벤트 해제
            if (_closeButton != null)
            {
                _closeButton.clicked -= OnCloseButtonClicked;
            }

            if (_takeButtons != null)
            {
                foreach (var button in _takeButtons)
                {
                    if (button != null)
                    {
                        button.clicked -= () => { }; // 개별 해제는 불가능하므로 생략
                    }
                }
            }

            if (enableDebugLogs)
                Debug.Log("[ChestUIView] OnDestroy");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 상자 UI 표시/숨김
        /// </summary>
        public void SetChestVisibility(bool visible)
        {
            if (visible)
                ShowChestUI();
            else
                HideChestUI();
        }

        /// <summary>
        /// UI 가시성 확인
        /// </summary>
        public bool IsUIVisible =>
            _chestContainer != null && _chestContainer.style.display == DisplayStyle.Flex;

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current Chest State")]
        public void LogCurrentChestState()
        {
            if (viewModel == null)
            {
                Debug.Log("[ChestUIView] ViewModel is null");
                return;
            }

            Debug.Log($"[ChestUIView] Chest State:\n" +
                $"  UI Visible: {IsUIVisible}\n" +
                $"  Is Open: {viewModel.IsChestOpen.Value}\n" +
                $"  Chest ID: {viewModel.CurrentChestId.Value}\n" +
                $"  Chest Name: {viewModel.CurrentChestName.Value}");
        }

        [ContextMenu("Test Show Chest")]
        private void TestShowChest() => SetChestVisibility(true);

        [ContextMenu("Test Hide Chest")]
        private void TestHideChest() => SetChestVisibility(false);

        #endregion
    }
}