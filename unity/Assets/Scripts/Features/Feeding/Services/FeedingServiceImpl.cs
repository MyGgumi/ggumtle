using System;
using Features.Feeding.Models;
using Features.Feeding.Messages;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Feeding.Services
{
    /// <summary>
    /// 꿈틀이 먹이 관리 서비스 구현
    /// </summary>
    public class FeedingServiceImpl : IFeedingService
    {
        private readonly ReactiveProperty<FeedingData> _currentFeeding = new(new FeedingData());
        private readonly CompositeDisposable _disposables = new();
        private Features.Inventory.Services.IInventoryService _inventoryService;

        private bool enableDebugLogs = true;

        [Inject]
        public void Construct(
            ISubscriber<JellyCountUpdateMessage> jellyCountSubscriber,
            Features.Inventory.Services.IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;

            // 젤리 개수 업데이트 메시지 구독
            jellyCountSubscriber
                .Subscribe(OnJellyCountUpdate)
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[FeedingService] JellyCountUpdateMessage 구독 완료");
        }

        #region IFeedingService Implementation

        public ReadOnlyReactiveProperty<FeedingData> CurrentFeeding => _currentFeeding.ToReadOnlyReactiveProperty();

        public int FeedingCount => _currentFeeding.Value.FeedingCount;

        public int AddFeeding(int amount)
        {
            if (amount <= 0) return 0;

            var currentData = _currentFeeding.Value;
            int actualAdded = currentData.AddFeeding(amount);

            if (actualAdded > 0)
            {
                _currentFeeding.OnNext(currentData);

                if (enableDebugLogs)
                    Debug.Log($"[FeedingService] 먹이 {actualAdded}개 추가, 총 개수: {currentData.FeedingCount}");

                // 먹이 추가 이벤트 발송
                // _publisher.Publish(new FeedingAddedMessage(actualAdded, currentData.FeedingCount));
            }

            return actualAdded;
        }

        public bool RemoveFeeding(int amount)
        {
            if (amount <= 0) return false;

            var currentData = _currentFeeding.Value;
            bool success = currentData.RemoveFeeding(amount);

            if (success)
            {
                _currentFeeding.OnNext(currentData);

                if (enableDebugLogs)
                    Debug.Log($"[FeedingService] 먹이 {amount}개 제거, 남은 개수: {currentData.FeedingCount}");

                // 먹이 제거 이벤트 발송
                // _publisher.Publish(new FeedingRemovedMessage(amount, currentData.FeedingCount));
            }

            return success;
        }

        public void SetFeedingCount(int count)
        {
            var currentData = _currentFeeding.Value;
            int oldCount = currentData.FeedingCount;
            currentData.SetFeedingCount(count);

            if (oldCount != currentData.FeedingCount)
            {
                _currentFeeding.OnNext(currentData);

                // InventoryService도 함께 동기화 (중요!)
                if (_inventoryService != null)
                {
                    _inventoryService.SyncFeedingCount(count);
                }

                if (enableDebugLogs)
                    Debug.Log($"[FeedingService] 먹이 개수 설정 및 InventoryService 동기화: {oldCount} → {currentData.FeedingCount}");

                // 먹이 개수 변경 이벤트 발송
                // _publisher.Publish(new FeedingCountChangedMessage(currentData.FeedingCount));
            }
        }

        public bool HasEnoughFeeding(int requiredAmount)
        {
            return _currentFeeding.Value.HasEnoughFeeding(requiredAmount);
        }

        public bool FeedToGgumtle(int amount)
        {
            if (!HasEnoughFeeding(amount)) return false;

            bool success = RemoveFeeding(amount);

            if (success)
            {
                if (enableDebugLogs)
                    Debug.Log($"[FeedingService] 꿈틀이에게 먹이 {amount}개 제공");

                // 꿈틀이 먹이주기 이벤트 발송
                // _publisher.Publish(new GgumtleFedMessage(amount));

                // TODO: 꿈틀이 진행도 증가 로직을 GgumtleService에 위임하거나 MessagePipe 사용
                if (enableDebugLogs)
                    Debug.Log("[FeedingService] 꿈틀이 먹이주기 완료 - 진행도 증가 로직은 GgumtleService에서 처리 예정");
            }

            return success;
        }

        public void SyncWithServer()
        {
            // TODO: 서버와 먹이 데이터 동기화
            if (enableDebugLogs)
                Debug.Log("[FeedingService] 서버와 동기화 (구현 예정)");
        }

        public void ClearFeeding()
        {
            var currentData = _currentFeeding.Value;
            currentData.Clear();
            _currentFeeding.OnNext(currentData);

            if (enableDebugLogs)
                Debug.Log("[FeedingService] 먹이 데이터 초기화");
        }

        #endregion

        #region Message Handlers

        /// <summary>
        /// 서버에서 젤리 개수 업데이트 메시지 처리
        /// </summary>
        private void OnJellyCountUpdate(JellyCountUpdateMessage message)
        {
            try
            {
                if (enableDebugLogs)
                    Debug.Log($"[FeedingService] 서버에서 젤리 개수 업데이트 수신: {message.JellyCount}");

                // 서버에서 받은 젤리 개수로 동기화
                SetFeedingCount(message.JellyCount);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FeedingService] 젤리 개수 업데이트 처리 중 오류: {e.Message}");
            }
        }

        #endregion

        #region Lifecycle

        public void Initialize()
        {
            // 서비스 초기화
            if (enableDebugLogs)
                Debug.Log("[FeedingService] 서비스 초기화 완료");
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _currentFeeding?.Dispose();

            if (enableDebugLogs)
                Debug.Log("[FeedingService] 서비스 해제 완료");
        }

        #endregion

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogCurrentState()
        {
            var data = _currentFeeding.Value;
            Debug.Log($"[FeedingService] 현재 상태: {data}");
        }

        #endregion
    }
}