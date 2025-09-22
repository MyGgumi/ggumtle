using System.Collections.Generic;
using Features.Inventory.Models;
using Features.Inventory.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Features.Inventory.Views
{
    /// <summary>
    /// 인벤토리 UI를 담당하는 View (UI Toolkit 기반)
    /// GgumtleUIView 패턴을 따라 VisualElement + R3로 구현
    /// </summary>
    public class InventoryUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private InventoryViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;

        // PlayerStatus.uxml의 LightArea 참조
        private VisualElement _lightArea;
        private Label _lightCountLabel;

        private List<VisualElement> _slotElements;
        private List<Label> _slotLabels;
        private List<VisualElement> _slotIcons;
        private List<VisualElement> _slotCounters;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        private CompositeDisposable _disposables = new();

        #region Initialization

        [Inject]
        public void Construct(InventoryViewModel inventoryViewModel)
        {
            viewModel = inventoryViewModel;
            if (enableDebugLogs)
                Debug.Log($"[InventoryUIView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError(
                    "[InventoryUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요."
                );
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            InitializeUI();

            if (enableDebugLogs)
                Debug.Log("[InventoryUIView] 초기화 완료");
        }

        #endregion

        #region UI Setup

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[InventoryUIView] Root VisualElement가 null입니다.");
                return;
            }

            // LightArea (PlayerStatus.uxml의 LightArea)
            _lightArea = _root.Q<VisualElement>("LightArea");
            // LightArea 안의 Label을 직접 찾기 (name이 없으므로)
            _lightCountLabel = _lightArea?.Q<Label>();

            // 슬롯 요소들 캐싱
            CacheSlotElements();

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[InventoryUIView] UI 요소 캐싱 완룼: "
                        + $"Slots={_slotElements?.Count ?? 0}, "
                        + $"LightArea={(_lightArea != null ? "OK" : "NULL")}, "
                        + $"LightLabel={(_lightCountLabel != null ? "OK" : "NULL")}"
                );
            }
        }

        private void CacheSlotElements()
        {
            _slotElements = new List<VisualElement>();
            _slotLabels = new List<Label>();
            _slotIcons = new List<VisualElement>();
            _slotCounters = new List<VisualElement>();

            // UXML 구조에 맞게 직접 슬롯 찾기 (slot1, slot2, slot3)
            for (int i = 1; i <= 3; i++)
            {
                var slot = _root?.Q<VisualElement>($"slot{i}");
                if (slot != null)
                {
                    _slotElements.Add(slot);

                    // UXML 구조: slot1Count, slot2Count, slot3Count
                    var label = slot.Q<Label>($"slot{i}Count");
                    _slotLabels.Add(label);

                    // UXML 구조: slot1ItemImage, slot2ItemImage, slot3ItemImage
                    var icon = slot.Q<VisualElement>($"slot{i}ItemImage");
                    _slotIcons.Add(icon);

                    // UXML 구조: slot1Counter, slot2Counter, slot3Counter
                    var counter = slot.Q<VisualElement>($"slot{i}Counter");
                    _slotCounters.Add(counter);

                    // 클릭 이벤트 등록 (인덱스는 0부터 시작)
                    int slotIndex = i - 1; // UXML은 1부터, 로직은 0부터
                    slot.RegisterCallback<ClickEvent>(evt => OnSlotClicked(slotIndex));

                    if (enableDebugLogs)
                        Debug.Log(
                            $"[InventoryUIView] 슬롯 {i} 캐싱: Slot={slot != null}, Label={label != null}, Icon={icon != null}, Counter={counter != null}"
                        );
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"[InventoryUIView] 슬롯 {i}을 찾을 수 없습니다.");
                }
            }
        }

        private void InitializeUI()
        {
            // 슬롯 초기화 (항상 보이도록)
            UpdateAllSlots();

            // 빛젤리 카운트 초기화
            UpdateFeedingCount(0);

            // LightArea 초기 텍스트 수정 (UXML에서 "24"로 설정되어 있음)
            if (_lightCountLabel != null)
            {
                _lightCountLabel.text = "0";
            }

            if (enableDebugLogs)
                Debug.Log(
                    $"[InventoryUIView] UI 초기화 완료 - 슬롯 {_slotElements?.Count ?? 0}개 감지됨"
                );
        }

        #endregion

        #region ViewModel Subscription

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
            {
                Debug.LogError("[InventoryUIView] InventoryViewModel이 설정되지 않음");
                return;
            }

            // 인벤토리는 항상 표시됨 (열고 닫기 기능 제거)

            // 슬롯 데이터 변경
            viewModel
                .PlayerSlots.Subscribe(slots =>
                {
                    if (enableDebugLogs)
                        Debug.Log($"[InventoryUIView] 슬롯 업데이트: {slots?.Length ?? 0}개");
                    UpdateSlots(slots);
                })
                .AddTo(_disposables);

            // 빛젤리 카운트
            viewModel
                .FeedingCount.Subscribe(count =>
                {
                    if (enableDebugLogs)
                        Debug.Log($"[InventoryUIView] 빛젤리: {count}");
                    UpdateFeedingCount(count);
                })
                .AddTo(_disposables);

            // 상태 텍스트는 사용하지 않음

            if (enableDebugLogs)
                Debug.Log("[InventoryUIView] ViewModel R3 구독 완료");
        }

        #endregion

        #region UI Update Methods

        // 인벤토리 열고 닫기 기능 제거 - 슬롯은 항상 표시됨

        private void UpdateSlots(InventorySlot[] slots)
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length && i < _slotElements.Count; i++)
            {
                UpdateSlotUI(i, slots[i]);
            }
        }

        private void UpdateSlotUI(int index, InventorySlot slot)
        {
            if (index >= _slotElements.Count)
                return;

            var slotElement = _slotElements[index];
            var label = _slotLabels[index];
            var icon = _slotIcons[index];
            var counter = index < _slotCounters.Count ? _slotCounters[index] : null;

            // 슬롯은 항상 표시됨 (배경 스프라이트 보임)
            if (slotElement != null)
            {
                slotElement.style.display = DisplayStyle.Flex;
            }

            if (slot == null || slot.IsEmpty)
            {
                // 빈 슬롯: 배경만 보이고 아이콘/카운터는 숨김
                if (label != null)
                    label.text = "";
                if (icon != null)
                    icon.style.display = DisplayStyle.None;
                if (counter != null)
                    counter.style.display = DisplayStyle.None;
                slotElement?.RemoveFromClassList("filled");
                slotElement?.AddToClassList("empty");

                if (enableDebugLogs)
                    Debug.Log(
                        $"[InventoryUIView] 슬롯 {index + 1} UI 업데이트: 빈 슬롯 (배경만 표시)"
                    );
            }
            else
            {
                // 채워진 슬롯: 아이콘 + 카운트 표시
                if (label != null)
                    label.text = slot.Count.ToString();
                if (icon != null)
                {
                    icon.style.display = DisplayStyle.Flex;
                    // TODO: 아이템 ID에 따라 아이콘 이미지 설정
                    // icon.style.backgroundImage = GetItemIcon(slot.ItemId);
                }
                if (counter != null)
                    counter.style.display = DisplayStyle.Flex;
                slotElement?.RemoveFromClassList("empty");
                slotElement?.AddToClassList("filled");

                if (enableDebugLogs)
                    Debug.Log(
                        $"[InventoryUIView] 슬롯 {index + 1} UI 업데이트: 아이템 {slot.ItemId} x{slot.Count}"
                    );
            }
        }

        private void UpdateAllSlots()
        {
            if (viewModel?.PlayerSlots?.Value != null)
            {
                UpdateSlots(viewModel.PlayerSlots.Value);
            }
        }

        private void UpdateFeedingCount(int count)
        {
            if (_lightCountLabel != null)
            {
                _lightCountLabel.text = count.ToString();

                if (enableDebugLogs)
                    Debug.Log($"[InventoryUIView] 빛젤리 카운트 업데이트: {count}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[InventoryUIView] LightCountLabel이 null입니다.");
            }
        }

        // 상태 텍스트와 선택 슬롯, 상호작용 상태 기능 제거

        #endregion

        #region Event Handlers

        private void OnSlotClicked(int slotIndex)
        {
            if (enableDebugLogs)
                Debug.Log($"[InventoryUIView] 슬롯 {slotIndex + 1} 클릭됨");

            // 슬롯 클릭 시 아이템 사용
            UseSlotItem(slotIndex);
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables.Dispose();

            // 클릭 이벤트 해제
            if (_slotElements != null)
            {
                foreach (var slot in _slotElements)
                {
                    slot?.UnregisterCallback<ClickEvent>(evt => { });
                }
            }

            if (enableDebugLogs)
                Debug.Log("[InventoryUIView] OnDestroy");
        }

        #endregion

        #region Public API

        /// <summary>
        /// LightArea 가시성 설정
        /// </summary>
        public void SetLightAreaVisibility(bool visible)
        {
            if (_lightArea != null)
            {
                _lightArea.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

                if (enableDebugLogs)
                    Debug.Log($"[InventoryUIView] LightArea 가시성: {visible}");
            }
        }

        /// <summary>
        /// 특정 슬롯 아이템 사용
        /// </summary>
        public async void UseSlotItem(int slotIndex)
        {
            if (viewModel != null && slotIndex >= 0 && slotIndex < 3)
            {
                // 슬롯 선택 후 사용
                viewModel.SelectSlot(slotIndex);
                await viewModel.UseSelectedItem();

                if (enableDebugLogs)
                    Debug.Log($"[InventoryUIView] 슬롯 {slotIndex + 1} 아이템 사용");
            }
        }

        #endregion

        #region Public API (ResourceView에서 이동)

        /// <summary>
        /// 특정 슬롯 표시/숨김 (UniversalHUDController에서 호출)
        /// </summary>
        public void SetSlotVisibility(int slotNumber, bool visible)
        {
            if (slotNumber < 1 || slotNumber > 3)
                return;

            // 초기화되지 않은 경우 무시
            if (_slotElements == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning(
                        $"[InventoryUIView] 아직 초기화되지 않음 - 슬롯 {slotNumber} 표시 요청 무시"
                    );
                return;
            }

            int slotIndex = slotNumber - 1;
            if (slotIndex < _slotElements.Count && _slotElements[slotIndex] != null)
            {
                _slotElements[slotIndex].style.display = visible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                if (enableDebugLogs)
                    Debug.Log($"[InventoryUIView] 슬롯 {slotNumber} 표시: {visible}");
            }
        }

        /// <summary>
        /// 슬롯 배경 스프라이트 설정 (UIAssetService에서 호출)
        /// </summary>
        public void SetSlotBackgroundSprite(Sprite backgroundSprite)
        {
            if (enableDebugLogs)
                Debug.Log($"[InventoryUIView] SetSlotBackgroundSprite 호출됨. Sprite: {(backgroundSprite != null ? backgroundSprite.name : "NULL")}");

            if (backgroundSprite == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[InventoryUIView] 배경 스프라이트가 null입니다.");
                return;
            }

            // 모든 슬롯에 배경 스프라이트 적용
            for (int i = 0; i < _slotElements.Count; i++)
            {
                if (_slotElements[i] != null)
                {
                    _slotElements[i].style.backgroundImage = new StyleBackground(backgroundSprite);
                    _slotElements[i].style.backgroundSize = new BackgroundSize(
                        BackgroundSizeType.Cover
                    );

                    if (enableDebugLogs)
                        Debug.Log(
                            $"[InventoryUIView] 슬롯 {i + 1}에 배경 스프라이트 적용: {backgroundSprite.name}"
                        );
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"[InventoryUIView] 슬롯 {i + 1} 요소가 null입니다.");
                }
            }

            if (enableDebugLogs)
                Debug.Log(
                    $"[InventoryUIView] 모든 슬롯에 배경 스프라이트 설정 완료: {backgroundSprite.name}"
                );
        }

        /// <summary>
        /// 테스트용 초기화
        /// </summary>
        public void InitializeForTesting()
        {
            if (viewModel != null)
            {
                // 테스트 아이템 추가
                viewModel.AddItemToSlot(1, 1, 2); // 테이저건 2개
                viewModel.AddItemToSlot(2, 2, 1); // 섬광탄 1개
                viewModel.AddItemToSlot(3, 3, 1); // 자가제세동기 1개

                if (enableDebugLogs)
                    Debug.Log("[InventoryUIView] 테스트 초기화 완료");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current Inventory State")]
        public void LogCurrentInventoryState()
        {
            if (viewModel == null)
            {
                Debug.Log("[InventoryUIView] ViewModel is null");
                return;
            }

            Debug.Log(
                $"[InventoryUIView] Inventory State:\n"
                    + $"  Feeding Count: {viewModel.FeedingCount.Value}"
            );

            if (viewModel.PlayerSlots.Value != null)
            {
                for (int i = 0; i < viewModel.PlayerSlots.Value.Length; i++)
                {
                    var slot = viewModel.PlayerSlots.Value[i];
                    Debug.Log(
                        $"  Slot[{i + 1}]: {(slot.IsEmpty ? "Empty" : $"{slot.ItemId} x{slot.Count}")}"
                    );
                }
            }
        }

        [ContextMenu("Test Add Items")]
        private void TestAddItems() => InitializeForTesting();

        [ContextMenu("Test Clear Items")]
        private void TestClearItems()
        {
            if (viewModel != null)
            {
                // 각 슬롯의 아이템 개수를 0으로 설정하여 비우기
                for (int i = 1; i <= 3; i++)
                {
                    viewModel.SetSlotItemCount(i, 0);
                }

                if (enableDebugLogs)
                    Debug.Log("[InventoryUIView] 테스트: 인벤토리 비움");
            }
        }

        #endregion
    }
}
