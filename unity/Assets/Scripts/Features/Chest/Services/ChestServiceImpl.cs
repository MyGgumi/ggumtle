using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.Chest.Models;
using Features.Chest.Messages;
using Features.Chest.NetworkSources;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Chest.Services
{
    /// <summary>
    /// 상자 비즈니스 로직 구현
    /// R3 Observable과 MessagePipe를 사용한 리액티브 아키텍처
    /// </summary>
    public class ChestServiceImpl : IChestService
    {
        private readonly ChestModel _chestModel;
        private readonly IChestNetworkSource _networkSource;
        private readonly IPublisher<ChestOpenedMessage> _chestOpenedPublisher;
        private readonly IPublisher<ChestClosedMessage> _chestClosedPublisher;

        // R3 Reactive Properties
        private readonly ReactiveProperty<ChestState> _currentChestState = new(ChestState.Closed);
        private readonly ReactiveProperty<bool> _isUIVisible = new(false);

        // Debug Settings
        private bool _enableDebugLogs = true;

        [Inject]
        public ChestServiceImpl(
            IChestNetworkSource networkSource,
            IPublisher<ChestOpenedMessage> chestOpenedPublisher,
            IPublisher<ChestClosedMessage> chestClosedPublisher)
        {
            _chestModel = new ChestModel();
            _networkSource = networkSource;
            _chestOpenedPublisher = chestOpenedPublisher;
            _chestClosedPublisher = chestClosedPublisher;

            DebugLog("VContainer 의존성 주입 완료");
        }

        #region 상자 등록/관리

        public void RegisterChest(int chestId, string chestName, Vector3 position, GameObject chestObject = null)
        {
            _chestModel.RegisterChest(chestId, chestName, position, chestObject);
            DebugLog($"상자 등록: {chestId} - {chestName}");
        }

        public void UnregisterChest(int chestId)
        {
            bool wasCurrentChest = _chestModel.currentChestId == chestId;
            _chestModel.UnregisterChest(chestId);

            if (wasCurrentChest)
            {
                _currentChestState.Value = ChestState.Closed;
                _isUIVisible.Value = false;
            }

            DebugLog($"상자 해제: {chestId}");
        }

        public ChestData GetChest(int chestId)
        {
            return _chestModel.GetChest(chestId);
        }

        public bool HasChest(int chestId)
        {
            return _chestModel.HasChest(chestId);
        }

        public List<ChestData> GetAllChests()
        {
            return _chestModel.GetAllChests();
        }

        #endregion

        #region 상자 상호작용

        public bool OpenChest(int chestId)
        {
            DebugLog($"OpenChest 호출: {chestId}");

            if (!_chestModel.HasChest(chestId))
            {
                DebugLogError($"상자가 등록되지 않음: {chestId}");
                return false;
            }

            var chest = _chestModel.GetChest(chestId);
            if (chest.isLocked)
            {
                DebugLogWarning($"상자가 잠겨있음: {chestId}");
                return false;
            }

            // 다른 상자가 열려있다면 먼저 닫기
            if (_chestModel.HasCurrentChest && _chestModel.currentChestId != chestId)
            {
                CloseCurrentChest();
            }

            // 로컬 상태 업데이트
            if (_chestModel.OpenChest(chestId))
            {
                _currentChestState.Value = ChestState.Open;
                _isUIVisible.Value = true;

                // 네트워크 호출 (비동기, Fire-and-forget)
                OpenChestNetworkAsync(chestId).Forget();

                // MessagePipe로 상자 열림 이벤트 발행
                _chestOpenedPublisher.Publish(new ChestOpenedMessage(chestId, chest.chestName));

                DebugLog($"상자 열기 성공: {chestId}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 상자 열기 네트워크 요청
        /// </summary>
        private async UniTaskVoid OpenChestNetworkAsync(int chestId)
        {
            try
            {
                var result = await _networkSource.OpenChestAsync(chestId);
                DebugLog($"네트워크 상자 열기 결과: {result.Success} - {result.Result}");
            }
            catch (System.Exception e)
            {
                DebugLogError($"네트워크 상자 열기 실패: {e.Message}");
            }
        }

        public void CloseCurrentChest()
        {
            if (!_chestModel.HasCurrentChest)
                return;

            int chestId = _chestModel.currentChestId;

            // 로컬 상태 업데이트
            _chestModel.CloseCurrentChest();
            _currentChestState.Value = ChestState.Closed;
            _isUIVisible.Value = false;

            // 네트워크 호출 (비동기, Fire-and-forget)
            CloseChestNetworkAsync(chestId).Forget();

            // MessagePipe로 상자 닫힘 이벤트 발행
            _chestClosedPublisher.Publish(new ChestClosedMessage(chestId));

            DebugLog($"상자 닫기 완료: {chestId}");
        }

        /// <summary>
        /// 상자 닫기 네트워크 요청
        /// </summary>
        private async UniTaskVoid CloseChestNetworkAsync(int chestId)
        {
            try
            {
                var result = await _networkSource.CloseChestAsync(chestId);
                DebugLog($"네트워크 상자 닫기 결과: {result.Success} - {result.Result}");
            }
            catch (System.Exception e)
            {
                DebugLogError($"네트워크 상자 닫기 실패: {e.Message}");
            }
        }

        public bool TakeItemFromCurrentChest(int slotIndex, int amount = -1)
        {
            if (!_chestModel.HasCurrentChest)
                return false;

            bool result = _chestModel.TakeItemFromCurrentChest(slotIndex, amount);

            if (result)
            {
                DebugLog($"아이템 제거 성공: 슬롯 {slotIndex}, 수량 {amount}");
            }

            return result;
        }

        public void UpdateCurrentChestSlot(int slotIndex, string itemId, int count)
        {
            if (_chestModel.HasCurrentChest)
            {
                _chestModel.UpdateCurrentChestSlot(slotIndex, itemId, count);
                DebugLog($"슬롯 업데이트: {slotIndex} - {itemId} x{count}");
            }
        }

        #endregion

        #region 서버 동기화

        public void SyncChestData(int chestId, ChestSlot[] serverSlots)
        {
            _chestModel.SyncChestData(chestId, serverSlots);
            DebugLog($"상자 데이터 동기화: {chestId}, {serverSlots.Length}개 슬롯");
        }

        public void SyncChestSlot(int chestId, int slotIndex, string itemId, int count)
        {
            _chestModel.SyncChestSlot(chestId, slotIndex, itemId, count);
            DebugLog($"슬롯 동기화: {chestId}[{slotIndex}] - {itemId} x{count}");
        }

        #endregion

        #region 상태 관리

        public bool HasCurrentChest => _chestModel.HasCurrentChest;
        public bool IsChestOpen => _chestModel.IsChestOpen;
        public ChestData CurrentChest => _chestModel.currentChest;
        public int CurrentChestId => _chestModel.currentChestId;

        public Observable<ChestState> CurrentChestState => _currentChestState.AsObservable();
        public Observable<bool> IsUIVisible => _isUIVisible.AsObservable();

        #endregion

        #region 유틸리티

        public void Reset()
        {
            int currentChestId = _chestModel.currentChestId;
            _chestModel.Reset();

            _currentChestState.Value = ChestState.Closed;
            _isUIVisible.Value = false;

            if (currentChestId != 0)
            {
                _chestClosedPublisher.Publish(new ChestClosedMessage(currentChestId));
            }

            DebugLog("서비스 초기화 완료");
        }

        public List<ChestData> GetChestsWithItems()
        {
            return _chestModel.GetChestsWithItems();
        }

        public int GetTotalChestCount()
        {
            return _chestModel.GetTotalChestCount();
        }

        public int GetTotalItemsInAllChests()
        {
            return _chestModel.GetTotalItemsInAllChests();
        }

        #endregion

        #region Debug Helper Methods

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[ChestServiceImpl] {message}");
        }

        private void DebugLogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[ChestServiceImpl] {message}");
        }

        private void DebugLogError(string message)
        {
            Debug.LogError($"[ChestServiceImpl] {message}");
        }

        public void SetDebugLogging(bool enabled)
        {
            _enableDebugLogs = enabled;
        }

        #endregion
    }
}