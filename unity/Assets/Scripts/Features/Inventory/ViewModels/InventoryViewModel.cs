using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.Inventory.Messages;
using Features.Inventory.Models;
using Features.Inventory.Services;
using Features.Ggumtle.Messages;
using Features.Notification.Models;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Inventory.ViewModels
{
    /// <summary>
    /// 인벤토리 도메인 전체의 상태 관리를 담당하는 ViewModel
    /// GameObject View와 UI View가 모두 이 ViewModel을 구독
    /// </summary>
    public class InventoryViewModel : IDisposable
    {
        #region Observable Properties

        // 인벤토리 데이터
        public readonly ReactiveProperty<InventoryData> CurrentInventory = new(new InventoryData());
        public readonly ReactiveProperty<InventorySlot[]> PlayerSlots = new(new InventorySlot[3]);
        public readonly ReactiveProperty<int> FeedingCount = new(0);

        // 선택된 슬롯
        public readonly ReactiveProperty<int> SelectedSlotIndex = new(-1);
        public readonly ReactiveProperty<bool> HasSelectedSlot = new(false);

        // UI 상태
        public readonly ReactiveProperty<bool> IsInventoryOpen = new(false);
        public readonly ReactiveProperty<bool> IsInteracting = new(false);
        public readonly ReactiveProperty<string> StatusText = new(string.Empty);

        // 아이템 사용 관련
        public readonly ReactiveProperty<bool> CanUseSelectedItem = new(false);
        public readonly ReactiveProperty<float> ItemCooldown = new(0f);

        // 슬롯별 쿨다운 상태 (3개 슬롯)
        public readonly ReactiveProperty<bool>[] SlotCooldownStates = new ReactiveProperty<bool>[3]
        {
            new(false), new(false), new(false)
        };

        #endregion

        #region Dependencies

        private readonly IInventoryService _inventoryService;
        private readonly IPublisher<Features.Notification.Messages.NotificationMessage> _notificationPublisher;
        private readonly Features.ItemUsage.Services.IItemUsageService _itemUsageService;
        private readonly Features.Player.Services.PlayerManagerService _playerManagerService;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private bool _isInitialized = false;
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor & Initialization

        [Inject]
        public InventoryViewModel(
            IInventoryService inventoryService,
            ISubscriber<SlotChangedMessage> slotChangedSubscriber,
            ISubscriber<ItemTransferredMessage> itemTransferredSubscriber,
            ISubscriber<InventorySlotClickedMessage> slotClickedSubscriber,
            ISubscriber<InventoryToggleMessage> toggleSubscriber,
            IPublisher<Features.Notification.Messages.NotificationMessage> notificationPublisher,
            Features.ItemUsage.Services.IItemUsageService itemUsageService,
            Features.Player.Services.PlayerManagerService playerManagerService)
        {
            _inventoryService = inventoryService;
            _notificationPublisher = notificationPublisher;
            _itemUsageService = itemUsageService;
            _playerManagerService = playerManagerService;

            Initialize(
                slotChangedSubscriber,
                itemTransferredSubscriber,
                slotClickedSubscriber,
                toggleSubscriber
            );
        }

        private void Initialize(
            ISubscriber<SlotChangedMessage> slotChangedSubscriber,
            ISubscriber<ItemTransferredMessage> itemTransferredSubscriber,
            ISubscriber<InventorySlotClickedMessage> slotClickedSubscriber,
            ISubscriber<InventoryToggleMessage> toggleSubscriber)
        {
            if (_isInitialized) return;

            // 서비스 상태 구독
            _inventoryService.CurrentInventory
                .Subscribe(inventory => OnInventoryUpdated(inventory))
                .AddTo(_disposables);

            _inventoryService.FeedingCount
                .Subscribe(count => FeedingCount.Value = count)
                .AddTo(_disposables);

            // 메시지 구독
            slotChangedSubscriber
                .Subscribe(msg => OnSlotChanged(msg))
                .AddTo(_disposables);

            itemTransferredSubscriber
                .Subscribe(msg => OnItemTransferred(msg))
                .AddTo(_disposables);

            slotClickedSubscriber
                .Subscribe(msg => OnSlotClicked(msg))
                .AddTo(_disposables);

            toggleSubscriber
                .Subscribe(msg => OnInventoryToggle(msg))
                .AddTo(_disposables);

            // 자동 업데이트 구독
            SelectedSlotIndex
                .Subscribe(index => UpdateSelectedSlotState(index))
                .AddTo(_disposables);

            // 쿨다운 상태 업데이트 타이머 시작
            StartCooldownUpdateTimer();

            _isInitialized = true;

            if (_enableDebugLogs)
                Debug.Log("[InventoryViewModel] 초기화 완료");
        }

        #endregion

        #region Item Slot Management (ResourceViewModel에서 이동)


        /// <summary>
        /// 슬롯에 아이템 추가
        /// </summary>
        public bool AddItemToSlot(int slotNumber, int itemId, int amount = 1)
        {
            if (slotNumber < 1 || slotNumber > 3 || amount <= 0) return false;

            var slots = PlayerSlots.Value;
            int slotIndex = slotNumber - 1;
            var slot = slots[slotIndex];

            // 빈 슬롯이거나 같은 아이템인 경우만 추가 가능
            if (!slot.IsEmpty && slot.ItemId != itemId) return false;

            // 아이템 정의 확인
            var itemDef = Features.Item.Services.ItemDefinitionService.GetItemById(itemId);
            if (itemDef == null) return false;

            // 최대 스택 확인
            int maxStack = itemDef.MaxStack;
            int availableSpace = maxStack - slot.Count;
            int actualAmount = Math.Min(amount, availableSpace);

            if (actualAmount <= 0) return false;

            // 아이템 추가
            slot.ItemId = itemId;
            slot.Count += actualAmount;
            PlayerSlots.OnNext(slots);

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 슬롯 {slotNumber}에 {itemDef.ItemName} {actualAmount}개 추가");

            return true;
        }

        /// <summary>
        /// 슬롯 아이템 개수 설정
        /// </summary>
        public void SetSlotItemCount(int slotNumber, int count)
        {
            if (slotNumber < 1 || slotNumber > 3) return;

            var slots = PlayerSlots.Value;
            int slotIndex = slotNumber - 1;
            var slot = slots[slotIndex];

            int oldCount = slot.Count;
            slot.Count = Math.Max(0, count);

            // 개수가 0이면 슬롯 비우기
            if (slot.Count == 0)
            {
                slot.ItemId = 0;
            }

            if (oldCount != slot.Count)
            {
                PlayerSlots.OnNext(slots);

                if (_enableDebugLogs)
                    Debug.Log($"[InventoryViewModel] 슬롯 {slotNumber} 개수 설정: {oldCount} → {slot.Count}");
            }
        }

        /// <summary>
        /// 슬롯 아이템 개수 가져오기
        /// </summary>
        public int GetSlotItemCount(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > 3) return 0;

            var slots = PlayerSlots.Value;
            int slotIndex = slotNumber - 1;
            return slots[slotIndex].Count;
        }

        /// <summary>
        /// 슬롯 사용 가능 여부
        /// </summary>
        public bool CanUseSlot(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > 3) return false;

            var slots = PlayerSlots.Value;
            int slotIndex = slotNumber - 1;
            var slot = slots[slotIndex];

            return !slot.IsEmpty && slot.Count > 0;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 슬롯 선택
        /// </summary>
        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PlayerSlots.Value.Length)
            {
                SelectedSlotIndex.Value = -1;
                return;
            }

            SelectedSlotIndex.Value = slotIndex;

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 슬롯 선택: {slotIndex}");
        }

        /// <summary>
        /// 아이템 사용 (통합된 메서드)
        /// </summary>
        public async UniTask<bool> UseItem(int slotIndex)
        {
            // 슬롯 유효성 검사
            if (slotIndex < 0 || slotIndex >= PlayerSlots.Value.Length)
                return false;

            var slot = PlayerSlots.Value[slotIndex];
            if (slot.IsEmpty || slot.Count <= 0)
                return false;

            // 쿨다운 체크
            if (ItemCooldown.Value > 0)
            {
                ShowNotification("쿨다운 중입니다!", Features.Notification.Models.NotificationType.Warning);
                return false;
            }

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 아이템 사용 시작: SlotIndex={slotIndex}, ItemId={slot.ItemId}");

            IsInteracting.Value = true;

            try
            {
                // 로컬 플레이어 ID 가져오기
                var localPlayer = _playerManagerService?.GetLocalPlayer();
                if (localPlayer == null)
                {
                    ShowNotification("플레이어 정보를 찾을 수 없습니다", Features.Notification.Models.NotificationType.Error);
                    return false;
                }
                long localPlayerId = localPlayer.Id;

                // ItemUsageService 직접 호출
                bool success = false;
                switch (slot.ItemId)
                {
                    case 3: // 테이저건
                        success = await _itemUsageService.UseTaserGunAsync(localPlayerId);
                        break;
                    case 2: // 섬광탄
                        success = await _itemUsageService.UseFlashBangAsync(localPlayerId);
                        break;
                    case 4: // 자가제세동기
                        success = await _itemUsageService.UseSelfDefibrillatorAsync(localPlayerId);
                        break;
                    default:
                        if (_enableDebugLogs)
                            Debug.LogWarning($"[InventoryViewModel] 알 수 없는 아이템 ID: {slot.ItemId}");
                        return false;
                }

                if (success)
                {
                    // 인벤토리에서 아이템 제거
                    slot.Count--;
                    if (slot.Count <= 0)
                    {
                        slot.ItemId = 0;
                        slot.Count = 0;
                    }

                    // UI 업데이트
                    PlayerSlots.ForceNotify();

                    // 쿨다운 시작
                    StartItemCooldown(2f);

                    ShowNotification("아이템 사용 성공!", Features.Notification.Models.NotificationType.Success);

                    if (_enableDebugLogs)
                        Debug.Log($"[InventoryViewModel] 아이템 사용 성공: ItemId={slot.ItemId}");
                }
                else
                {
                    ShowNotification("아이템 사용 실패", Features.Notification.Models.NotificationType.Error);
                }

                return success;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryViewModel] 아이템 사용 중 예외 발생: {e.Message}");
                ShowNotification("아이템 사용 중 오류 발생", Features.Notification.Models.NotificationType.Error);
                return false;
            }
            finally
            {
                IsInteracting.Value = false;
            }
        }

        /// <summary>
        /// 상자에서 아이템 가져오기
        /// </summary>
        public void TakeItemFromChest(int chestId, int slotIndex)
        {
            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 상자에서 아이템 가져오기: ChestId={chestId}, SlotIndex={slotIndex}");

            _inventoryService.RequestItemFromChest(chestId, slotIndex);
        }

        /// <summary>
        /// 상자에 아이템 넣기
        /// </summary>
        public bool PutItemToChest(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PlayerSlots.Value.Length)
                return false;

            var slot = PlayerSlots.Value[slotIndex];
            if (slot.IsEmpty) return false;

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 상자에 아이템 넣기: {slot.ItemId}");

            _inventoryService.RequestItemToChest(slot.ItemId);
            return true;
        }

        /// <summary>
        /// 슬롯 교환
        /// </summary>
        public void SwapSlots(int slot1, int slot2)
        {
            if (_inventoryService.SwapSlots(slot1, slot2))
            {
                if (_enableDebugLogs)
                    Debug.Log($"[InventoryViewModel] 슬롯 교환: {slot1} <-> {slot2}");
            }
        }

        // AddFeeding 메서드 제거 - 빛젤리는 서버 동기화를 통해서만 업데이트됨
        // 서버에서 JellyCount 패킷이 오면 자동으로 UI가 업데이트됨

        /// <summary>
        /// 인벤토리 UI 토글
        /// </summary>
        public void ToggleInventoryUI()
        {
            IsInventoryOpen.Value = !IsInventoryOpen.Value;

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 인벤토리 UI: {(IsInventoryOpen.Value ? "열림" : "닫힘")}");
        }

        #endregion

        #region Event Handlers

        private void OnInventoryUpdated(InventoryData inventory)
        {
            CurrentInventory.Value = inventory;

            // 새로운 배열 생성해서 ReactiveProperty 변경 감지 보장
            var newSlots = new InventorySlot[inventory.PlayerSlots.Count];
            for (int i = 0; i < inventory.PlayerSlots.Count; i++)
            {
                newSlots[i] = inventory.PlayerSlots[i].Clone();
            }
            PlayerSlots.Value = newSlots;

            FeedingCount.Value = inventory.FeedingCount;

            UpdateStatusText();

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 인벤토리 업데이트됨: {newSlots.Length}개 슬롯");
        }

        private void OnSlotChanged(SlotChangedMessage msg)
        {
            if (msg.SlotIndex >= 0 && msg.SlotIndex < PlayerSlots.Value.Length)
            {
                var slots = PlayerSlots.Value;
                slots[msg.SlotIndex] = new InventorySlot
                {
                    SlotIndex = msg.SlotIndex,
                    ItemId = msg.ItemId,
                    Count = msg.Count
                };
                PlayerSlots.Value = slots;

                if (_enableDebugLogs)
                    Debug.Log($"[InventoryViewModel] 슬롯 변경: [{msg.SlotIndex}] {msg.ItemId} x{msg.Count}");
            }
        }

        private void OnItemTransferred(ItemTransferredMessage msg)
        {
            if (!msg.Success) return;

            var direction = msg.Direction == TransferDirection.ToInventory ? "획득" : "보관";
            ShowNotification($"{msg.ItemId} x{msg.Count} {direction}!");

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 아이템 전송: {msg.ItemId} x{msg.Count} ({msg.Direction})");
        }


        private void OnSlotClicked(InventorySlotClickedMessage msg)
        {
            switch (msg.ClickType)
            {
                case ClickType.Select:
                    SelectSlot(msg.SlotIndex);
                    break;
                case ClickType.Use:
                    _ = UseItem(msg.SlotIndex);
                    break;
                case ClickType.Transfer:
                    PutItemToChest(msg.SlotIndex);
                    break;
            }

            if (_enableDebugLogs)
                Debug.Log($"[InventoryViewModel] 슬롯 클릭: [{msg.SlotIndex}] {msg.ClickType}");
        }

        private void OnInventoryToggle(InventoryToggleMessage msg)
        {
            ToggleInventoryUI();
        }

        #endregion

        #region Private Methods

        private void UpdateSelectedSlotState(int slotIndex)
        {
            HasSelectedSlot.Value = slotIndex >= 0;

            if (HasSelectedSlot.Value && slotIndex < PlayerSlots.Value.Length)
            {
                var slot = PlayerSlots.Value[slotIndex];
                CanUseSelectedItem.Value = !slot.IsEmpty && ItemCooldown.Value <= 0;
            }
            else
            {
                CanUseSelectedItem.Value = false;
            }
        }

        private void UpdateStatusText()
        {
            var emptySlots = PlayerSlots.Value.Count(s => s.IsEmpty);
            StatusText.Value = $"인벤토리: {3 - emptySlots}/3 | 빛젤리: {FeedingCount.Value}";
        }

        private async void StartItemCooldown(float duration)
        {
            ItemCooldown.Value = duration;

            while (ItemCooldown.Value > 0)
            {
                await UniTask.Yield();
                ItemCooldown.Value -= Time.deltaTime;
            }

            ItemCooldown.Value = 0;
            UpdateSelectedSlotState(SelectedSlotIndex.Value);
        }

        private void ShowNotification(string message, Features.Notification.Models.NotificationType type = Features.Notification.Models.NotificationType.Info)
        {
            _notificationPublisher?.Publish(new Features.Notification.Messages.NotificationMessage(message, 2f, type));
        }

        #endregion

        #region Cooldown Management

        /// <summary>
        /// 특정 슬롯이 쿨다운 중인지 확인
        /// </summary>
        public bool IsSlotOnCooldown(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCooldownStates.Length)
                return false;

            return SlotCooldownStates[slotIndex].Value;
        }

        /// <summary>
        /// 쿨다운 상태 업데이트 타이머 시작
        /// </summary>
        private async void StartCooldownUpdateTimer()
        {
            while (!_disposables.IsDisposed)
            {
                await UniTask.Yield();
                UpdateAllSlotCooldowns();
                await UniTask.Delay(100); // 100ms마다 업데이트
            }
        }

        /// <summary>
        /// 모든 슬롯의 쿨다운 상태 업데이트
        /// </summary>
        private void UpdateAllSlotCooldowns()
        {
            if (_itemUsageService == null) return;

            var slots = PlayerSlots.Value;
            for (int i = 0; i < slots.Length && i < SlotCooldownStates.Length; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    SlotCooldownStates[i].Value = false;
                }
                else
                {
                    float cooldown = _itemUsageService.GetItemCooldownRemaining(slot.ItemId);
                    bool isOnCooldown = cooldown > 0;

                    if (SlotCooldownStates[i].Value != isOnCooldown)
                    {
                        SlotCooldownStates[i].Value = isOnCooldown;

                        if (_enableDebugLogs && isOnCooldown)
                        {
                            Debug.Log($"[InventoryViewModel] 슬롯 {i + 1} 쿨다운 시작: {cooldown:F1}초");
                        }
                    }
                }
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables.Dispose();
            CurrentInventory?.Dispose();
            PlayerSlots?.Dispose();
            FeedingCount?.Dispose();
            SelectedSlotIndex?.Dispose();
            HasSelectedSlot?.Dispose();
            IsInventoryOpen?.Dispose();
            IsInteracting?.Dispose();
            StatusText?.Dispose();
            CanUseSelectedItem?.Dispose();
            ItemCooldown?.Dispose();

            // 슬롯 쿨다운 상태 Dispose
            for (int i = 0; i < SlotCooldownStates.Length; i++)
            {
                SlotCooldownStates[i]?.Dispose();
            }

            if (_enableDebugLogs)
                Debug.Log("[InventoryViewModel] Dispose 완료");
        }

        #endregion
    }
}