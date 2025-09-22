using System;
using System.Collections.Generic;
using Features.Chest.Messages;
using Features.Chest.Models;
using Features.Chest.Services;
using Features.Ggumtle.Messages;
using Features.Inventory.Services;
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

        // 현재 범위 내 상자 정보
        public readonly ReactiveProperty<string> CurrentChestId = new(string.Empty);
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
            IPublisher<Features.Notification.Messages.NotificationMessage> notificationPublisher
        )
        {
            _chestService = chestService;
            _inventoryService = inventoryService;
            _notificationPublisher = notificationPublisher;

            Initialize(detectedSubscriber, leftSubscriber, openedSubscriber, closedSubscriber);
        }

        private void Initialize(
            ISubscriber<ChestDetectedMessage> detectedSubscriber,
            ISubscriber<ChestLeftMessage> leftSubscriber,
            ISubscriber<ChestOpenedMessage> openedSubscriber,
            ISubscriber<ChestClosedMessage> closedSubscriber
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
        public bool OpenChest(string chestId)
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
            ClearCurrentChest();
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

            // 아이템 타입에 따른 처리
            if (slot.itemId == "4") // 빛젤리
            {
                var addedAmount = _inventoryService.AddFeeding(slot.count);
                if (addedAmount > 0)
                {
                    _chestService.TakeItemFromCurrentChest(slotIndex);
                    ShowNotification($"빛젤리 {addedAmount}개 획득!");
                    UpdateChestSlotsFromService();
                    return true;
                }
                else
                {
                    ShowNotification("더 이상 빛젤리를 가져올 수 없습니다.");
                    return false;
                }
            }
            else
            {
                // 일반 아이템을 인벤토리에 추가
                if (_inventoryService.AddItem(slot.itemId, slot.count))
                {
                    _chestService.TakeItemFromCurrentChest(slotIndex);
                    ShowNotification($"아이템 획득!");
                    UpdateChestSlotsFromService();
                    return true;
                }
                else
                {
                    ShowNotification("인벤토리가 가득 찼습니다.");
                    return false;
                }
            }
        }

        /// <summary>
        /// 상자 등록
        /// </summary>
        public void RegisterChest(
            string chestId,
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
        public void UnregisterChest(string chestId)
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
            if (CurrentChestId.Value == msg.ChestId)
            {
                DebugLog($"상자 범위 벗어남: {msg.ChestId}");

                // 열린 상자라면 닫기
                if (IsChestOpen.Value)
                {
                    CloseChest();
                }

                ClearCurrentChest();
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

        #endregion

        #region Private Methods

        private void SetCurrentChest(string chestId)
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
            CurrentChestId.Value = string.Empty;
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
                CurrentChestSlots.Value = currentChest.slots;
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
