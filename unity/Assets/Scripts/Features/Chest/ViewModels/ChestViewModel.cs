using System;
using System.Collections.Generic;
using Features.Chest.Messages;
using Features.Chest.Models;
using Features.Chest.Services;
using Features.Ggumtle.Messages;
using Features.Inventory.Services;
using Features.MobileControls.Messages;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Chest.ViewModels
{
    /// <summary>
    /// 상자 도메인 전체의 상태 관리를 담당하는 ViewModel
    /// GameObject View와 UI View가 모두 이 ViewModel을 구독
    /// </summary>
    public class ChestViewModel : IDisposable
    {
        #region Observable Properties

        // 현재 범위 내 상자 정보 (-1: 유효하지 않은 ID, 0부터: 유효한 서버 ID)
        public readonly ReactiveProperty<int> CurrentChestId = new(-1);
        public readonly ReactiveProperty<string> CurrentChestName = new(string.Empty);
        public readonly ReactiveProperty<bool> IsInRange = new(false);
        public readonly ReactiveProperty<float> Distance = new(float.MaxValue);

        // 상자 상태
        public readonly ReactiveProperty<ChestState> State = new(ChestState.Closed);
        public readonly ReactiveProperty<bool> IsChestOpen = new(false);
        public readonly ReactiveProperty<bool> CanInteract = new(false);

        // UI 상태
        public readonly ReactiveProperty<bool> IsUIVisible = new(false);
        public readonly ReactiveProperty<ChestSlot[]> CurrentChestSlots = new(new ChestSlot[9]);

        #endregion

        #region Dependencies

        private readonly IChestService _chestService;
        private readonly IInventoryService _inventoryService;
        private readonly IPublisher<Features.Notification.Messages.NotificationMessage> _notificationPublisher;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private bool _isInitialized = false;
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor & Initialization

        [Inject]
        public ChestViewModel(
            IChestService chestService,
            IInventoryService inventoryService,
            ISubscriber<ChestDetectedMessage> detectedSubscriber,
            ISubscriber<ChestLeftMessage> leftSubscriber,
            ISubscriber<ChestOpenedMessage> openedSubscriber,
            ISubscriber<ChestClosedMessage> closedSubscriber,
            ISubscriber<MobileButtonPressedMessage> buttonPressedSubscriber,
            ISubscriber<ChestServerDataSyncMessage> serverDataSyncSubscriber,
            IPublisher<Features.Notification.Messages.NotificationMessage> notificationPublisher
        )
        {
            _chestService = chestService;
            _inventoryService = inventoryService;
            _notificationPublisher = notificationPublisher;

            Initialize(detectedSubscriber, leftSubscriber, openedSubscriber, closedSubscriber, buttonPressedSubscriber, serverDataSyncSubscriber);
        }

        private void Initialize(
            ISubscriber<ChestDetectedMessage> detectedSubscriber,
            ISubscriber<ChestLeftMessage> leftSubscriber,
            ISubscriber<ChestOpenedMessage> openedSubscriber,
            ISubscriber<ChestClosedMessage> closedSubscriber,
            ISubscriber<MobileButtonPressedMessage> buttonPressedSubscriber,
            ISubscriber<ChestServerDataSyncMessage> serverDataSyncSubscriber
        )
        {
            if (_isInitialized)
                return;

            // 초기 상자 슬롯 배열 생성
            InitializeChestSlots();

            // 메시지 구독
            detectedSubscriber.Subscribe(OnChestDetected).AddTo(_disposables);
            leftSubscriber.Subscribe(OnChestLeft).AddTo(_disposables);
            openedSubscriber.Subscribe(OnChestOpened).AddTo(_disposables);
            closedSubscriber.Subscribe(OnChestClosed).AddTo(_disposables);
            buttonPressedSubscriber.Subscribe(OnMobileButtonPressed).AddTo(_disposables);
            serverDataSyncSubscriber.Subscribe(OnChestServerDataSync).AddTo(_disposables);

            // 서비스 상태 구독
            _chestService
                .CurrentChestState.Subscribe(state => State.Value = state)
                .AddTo(_disposables);

            _chestService
                .IsUIVisible.Subscribe(visible => IsUIVisible.Value = visible)
                .AddTo(_disposables);

            // 자동 업데이트 구독
            State.Subscribe(_ => UpdateCanInteract()).AddTo(_disposables);
            IsInRange.Subscribe(_ => UpdateCanInteract()).AddTo(_disposables);

            _isInitialized = true;
            DebugLog("초기화 완료");
        }

        private void InitializeChestSlots()
        {
            var initialSlots = new ChestSlot[9];
            for (int i = 0; i < initialSlots.Length; i++)
            {
                initialSlots[i] = new ChestSlot();
            }
            CurrentChestSlots.Value = initialSlots;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 상자 열기
        /// </summary>
        public bool OpenChest(int chestId)
        {
            if (!IsInRange.Value || !CanInteract.Value)
            {
                DebugLog(
                    $"상자 열기 불가: InRange={IsInRange.Value}, CanInteract={CanInteract.Value}"
                );
                return false;
            }

            DebugLog($"상자 열기 시도: {chestId}");

            if (_chestService.OpenChest(chestId))
            {
                SetCurrentChest(chestId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 상자 닫기
        /// </summary>
        public void CloseChest()
        {
            if (!IsChestOpen.Value)
                return;

            DebugLog($"상자 닫기: {CurrentChestId.Value}");
            _chestService.CloseCurrentChest();

            // 상자를 닫지만 CurrentChestId는 유지 (근처에 있으면 다시 열 수 있도록)
            IsChestOpen.Value = false;
            ClearChestSlots();
        }

        /// <summary>
        /// 상자에서 아이템 가져오기
        /// </summary>
        public bool TakeItemFromChest(int slotIndex)
        {
            if (!IsChestOpen.Value || slotIndex < 0 || slotIndex >= CurrentChestSlots.Value.Length)
                return false;

            var slot = CurrentChestSlots.Value[slotIndex];
            if (slot.isEmpty)
                return false;

            DebugLog($"아이템 가져오기 시도: 슬롯 {slotIndex}, 아이템 {slot.itemId} x{slot.count}");

            // 클라이언트 체크 제거 - 서버에서 모든 제한사항 체크
            // if (!CanAddItemToInventory(slot.itemId, slot.count))
            // {
            //     ShowNotification("인벤토리가 가득 찼습니다.");
            //     return false;
            // }

            // 인벤토리에 추가 가능한 경우에만 서버에 직접 요청
            _inventoryService.RequestItemFromChest(CurrentChestId.Value, slotIndex);

            // 서버 응답을 기다리고, InventoryNetworkEventHandler에서 처리하도록 함
            // 성공/실패 여부는 서버 응답으로 결정되며, 상자 UI 업데이트도 서버 동기화로 처리
            DebugLog($"서버에 아이템 가져오기 요청 전송 완료: 상자ID={CurrentChestId.Value}, 슬롯={slotIndex}");
            return true;
        }

        /// <summary>
        /// 상자 등록
        /// </summary>
        public void RegisterChest(
            int chestId,
            string chestName,
            Vector3 position,
            GameObject chestObject = null
        )
        {
            _chestService.RegisterChest(chestId, chestName, position, chestObject);
        }

        /// <summary>
        /// 상자 해제
        /// </summary>
        public void UnregisterChest(int chestId)
        {
            _chestService.UnregisterChest(chestId);
        }

        #endregion

        #region Event Handlers

        private void OnChestDetected(ChestDetectedMessage msg)
        {
            DebugLog($"상자 감지: ID={msg.ChestId}, 거리={msg.Distance:F2}m");

            CurrentChestId.Value = msg.ChestId;
            IsInRange.Value = true;
            Distance.Value = msg.Distance;
            State.Value = msg.State;

            // 상자 이름 설정
            var chestData = _chestService.GetChest(msg.ChestId);
            if (chestData != null)
            {
                CurrentChestName.Value = chestData.chestName;
            }
        }

        private void OnChestLeft(ChestLeftMessage msg)
        {
            DebugLog($"[DEBUG] OnChestLeft 호출됨: 현재 CurrentChestId={CurrentChestId.Value}, 메시지 ChestId={msg.ChestId}");

            if (CurrentChestId.Value == msg.ChestId)
            {
                DebugLog($"상자 범위 벗어남: {msg.ChestId} - IsInRange={IsInRange.Value}, CanInteract={CanInteract.Value}");

                // 열린 상자라면 닫기
                if (IsChestOpen.Value)
                {
                    CloseChest();
                }

                ClearCurrentChest();
                DebugLog($"[DEBUG] ClearCurrentChest 완료: CurrentChestId={CurrentChestId.Value}, IsInRange={IsInRange.Value}, CanInteract={CanInteract.Value}");
            }
            else
            {
                DebugLog($"[DEBUG] ChestId 불일치로 무시됨: 현재={CurrentChestId.Value}, 메시지={msg.ChestId}");
            }
        }

        private void OnChestOpened(ChestOpenedMessage msg)
        {
            if (CurrentChestId.Value == msg.ChestId)
            {
                DebugLog($"상자 열림: {msg.ChestId} ({msg.ChestName})");
                IsChestOpen.Value = true;
                CurrentChestName.Value = msg.ChestName;
                UpdateChestSlotsFromService();
            }
        }

        private void OnChestClosed(ChestClosedMessage msg)
        {
            if (CurrentChestId.Value == msg.ChestId)
            {
                DebugLog($"상자 닫힘: {msg.ChestId}");
                IsChestOpen.Value = false;
                ClearChestSlots();
            }
        }

        private void OnMobileButtonPressed(MobileButtonPressedMessage msg)
        {
            // 상호작용 버튼이 눌렸고, 상자가 범위 내에 있다면 열기/닫기 토글
            if (msg.ButtonType == Features.MobileControls.Models.MobileButtonType.Interact &&
                IsInRange.Value && CanInteract.Value && CurrentChestId.Value >= 0)
            {
                if (IsChestOpen.Value)
                {
                    DebugLog($"상호작용 버튼으로 상자 닫기: {CurrentChestId.Value}");
                    CloseChest();
                }
                else
                {
                    DebugLog($"상호작용 버튼으로 상자 열기: {CurrentChestId.Value}");
                    OpenChest(CurrentChestId.Value);
                }
            }
        }

        private void OnChestServerDataSync(ChestServerDataSyncMessage msg)
        {
            // 현재 열려있는 상자의 서버 데이터 동기화인지 확인
            if (IsChestOpen.Value && CurrentChestId.Value == msg.ChestId)
            {
                DebugLog($"서버 데이터 동기화: 상자ID={msg.ChestId}");
                // ChestService에서 이미 데이터를 처리했으므로, UI만 업데이트
                UpdateChestSlotsFromService();

                // ReactiveProperty 강제 알림 (혹시 모를 경우 대비)
                CurrentChestSlots.ForceNotify();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 인벤토리에 아이템 추가 가능 여부 체크 (MaxStack 초과 시 불가)
        /// </summary>
        private bool CanAddItemToInventory(string itemId, int count)
        {
            DebugLog($"[DEBUG] CanAddItemToInventory 호출: itemId={itemId}, count={count}");

            // int로 변환
            if (!int.TryParse(itemId, out int numericItemId))
            {
                DebugLog($"[DEBUG] itemId 파싱 실패: {itemId}");
                return false;
            }

            // 아이템 정의 확인
            var itemDef = Features.Item.Services.ItemDefinitionService.GetItemById(numericItemId);
            if (itemDef == null)
            {
                DebugLog($"[DEBUG] 아이템 정의 없음: {numericItemId}");
                return false;
            }
            DebugLog($"[DEBUG] 아이템 정의: {itemDef.ItemName}, MaxStack={itemDef.MaxStack}");

            // 현재 인벤토리 상태 가져오기
            var inventoryData = _inventoryService.CurrentInventory.CurrentValue;
            if (inventoryData?.PlayerSlots == null)
            {
                DebugLog($"[DEBUG] 인벤토리 데이터 없음");
                return false;
            }

            var currentSlots = inventoryData.PlayerSlots;
            DebugLog($"[DEBUG] 현재 슬롯 개수: {currentSlots.Count}");

            // 1. 같은 아이템이 있는 슬롯 찾기
            for (int i = 0; i < currentSlots.Count; i++)
            {
                var slot = currentSlots[i];
                if (!slot.IsEmpty && slot.ItemId == numericItemId)
                {
                    DebugLog($"[DEBUG] 같은 아이템 발견 슬롯 {i}: Count={slot.Count}, 추가 후={slot.Count + count}, MaxStack={itemDef.MaxStack}");
                    bool canAdd = slot.Count + count <= itemDef.MaxStack;
                    DebugLog($"[DEBUG] MaxStack 체크 결과: {canAdd}");
                    return canAdd;
                }
            }

            // 2. 같은 아이템이 없으면 빈 슬롯 찾기
            for (int i = 0; i < currentSlots.Count; i++)
            {
                var slot = currentSlots[i];
                if (slot.IsEmpty)
                {
                    DebugLog($"[DEBUG] 빈 슬롯 발견 {i}: count={count}, MaxStack={itemDef.MaxStack}");
                    bool canAdd = count <= itemDef.MaxStack;
                    DebugLog($"[DEBUG] 빈 슬롯 MaxStack 체크 결과: {canAdd}");
                    return canAdd;
                }
            }

            DebugLog($"[DEBUG] 빈 슬롯도 없음 - 추가 불가");
            return false;
        }

        private void SetCurrentChest(int chestId)
        {
            var chestData = _chestService.GetChest(chestId);
            if (chestData != null)
            {
                CurrentChestId.Value = chestId;
                CurrentChestName.Value = chestData.chestName;
                IsChestOpen.Value = chestData.isOpen;
                UpdateChestSlotsFromService();
            }
        }

        private void ClearCurrentChest()
        {
            CurrentChestId.Value = -1;
            CurrentChestName.Value = string.Empty;
            IsInRange.Value = false;
            Distance.Value = float.MaxValue;
            IsChestOpen.Value = false;
            CanInteract.Value = false;
            ClearChestSlots();
        }

        private void UpdateCanInteract()
        {
            CanInteract.Value = IsInRange.Value && State.Value == ChestState.Closed;
        }

        private void UpdateChestSlotsFromService()
        {
            if (!IsChestOpen.Value)
                return;

            var currentChest = _chestService.CurrentChest;
            if (currentChest != null)
            {
                // 새로운 배열 인스턴스를 생성해서 ReactiveProperty 변경 감지 보장
                var newSlots = new ChestSlot[currentChest.slots.Length];
                for (int i = 0; i < currentChest.slots.Length; i++)
                {
                    newSlots[i] = currentChest.slots[i];
                }
                CurrentChestSlots.Value = newSlots;

                DebugLog($"상자 슬롯 업데이트: {newSlots.Length}개 슬롯");
            }
        }

        private void ClearChestSlots()
        {
            var emptySlots = new ChestSlot[9];
            for (int i = 0; i < emptySlots.Length; i++)
            {
                emptySlots[i] = new ChestSlot();
            }
            CurrentChestSlots.Value = emptySlots;
        }

        private void ShowNotification(string message)
        {
            _notificationPublisher.Publish(
                new Features.Notification.Messages.NotificationMessage(message, 2f, Features.Notification.Models.NotificationType.Info)
            );
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[ChestViewModel] {message}");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 열린 상자가 있는지 여부
        /// </summary>
        public bool HasCurrentChest => _chestService.HasCurrentChest;

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();
            CurrentChestId?.Dispose();
            CurrentChestName?.Dispose();
            IsInRange?.Dispose();
            Distance?.Dispose();
            State?.Dispose();
            IsChestOpen?.Dispose();
            CanInteract?.Dispose();
            IsUIVisible?.Dispose();
            CurrentChestSlots?.Dispose();

            DebugLog("Dispose 완료");
        }

        #endregion
    }
}
