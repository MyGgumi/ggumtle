using System.Collections.Generic;
using System.Linq;
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
    public class ChestServiceImpl : IChestService, System.IDisposable
    {
        private readonly ChestModel _chestModel;
        private readonly IChestNetworkSource _networkSource;
        private readonly IPublisher<ChestOpenedMessage> _chestOpenedPublisher;
        private readonly IPublisher<ChestClosedMessage> _chestClosedPublisher;
        private readonly CompositeDisposable _messageDisposables = new();

        // R3 Reactive Properties
        private readonly ReactiveProperty<ChestState> _currentChestState = new(ChestState.Closed);
        private readonly ReactiveProperty<bool> _isUIVisible = new(false);

        // Debug Settings
        private bool _enableDebugLogs = true;

        [Inject]
        public ChestServiceImpl(
            IChestNetworkSource networkSource,
            IPublisher<ChestOpenedMessage> chestOpenedPublisher,
            IPublisher<ChestClosedMessage> chestClosedPublisher,
            ISubscriber<ChestServerDataSyncMessage> serverDataSyncSubscriber)
        {
            _chestModel = new ChestModel();
            _networkSource = networkSource;
            _chestOpenedPublisher = chestOpenedPublisher;
            _chestClosedPublisher = chestClosedPublisher;

            // 서버 데이터 동기화 메시지 구독
            serverDataSyncSubscriber.Subscribe(OnServerDataSync).AddTo(_messageDisposables);
        }

        #region 상자 등록/관리

        /// <summary>
        /// 서버에서 받은 상자 정보를 일괄 등록
        /// </summary>
        public void RegisterChestsFromServer(Networks.Rooms.Domains.ChestPacket[] serverChests)
        {
            if (serverChests == null || serverChests.Length == 0)
            {
                return;
            }

            foreach (var serverChest in serverChests)
            {
                var unityPosition = new UnityEngine.Vector3(serverChest.Position.X, serverChest.Position.Y, serverChest.Position.Z);
                var chestName = $"ServerChest_{serverChest.Id}";

                _chestModel.RegisterChest(serverChest.Id, chestName, unityPosition, null);
            }

            Debug.Log($"서버 상자 등록 완료: {serverChests.Length}개");
        }

        public void RegisterChest(int chestId, string chestName, Vector3 position, GameObject chestObject = null)
        {
            _chestModel.RegisterChest(chestId, chestName, position, chestObject);
        }

        public void UpdateChestGameObject(int chestId, GameObject chestObject)
        {
            _chestModel.UpdateChestGameObject(chestId, chestObject);
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
            if (!_chestModel.HasChest(chestId))
            {
                DebugLogError($"상자 열기 실패: 상자 ID {chestId}가 등록되지 않음");
                return false;
            }

            var chest = _chestModel.GetChest(chestId);
            if (chest.isLocked)
            {
                DebugLogWarning($"상자 열기 실패: 상자가 잠겨있음 (ID: {chestId})");
                return false;
            }

            // 현재 열린 상자가 있다면 먼저 닫기 (같은 상자도 포함)
            if (_chestModel.HasCurrentChest)
            {
                CloseCurrentChest();
            }

            // 로컬 상태 업데이트
            if (_chestModel.OpenChest(chestId))
            {
                _currentChestState.Value = ChestState.Open;
                _isUIVisible.Value = true;

                Debug.Log($"[상자열기요청] ID:{chestId}");

                // 네트워크 호출 (비동기, Fire-and-forget)
                OpenChestNetworkAsync(chestId).Forget();

                // MessagePipe로 상자 열림 이벤트 발행
                _chestOpenedPublisher.Publish(new ChestOpenedMessage(chestId, chest.chestName));

                return true;
            }

            DebugLogError($"로컬 상자 열기 실패: {chestId}");
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

            Debug.Log($"[상자닫기요청] ID:{chestId}");

            // 로컬 상태 업데이트
            _chestModel.CloseCurrentChest();
            _currentChestState.Value = ChestState.Closed;
            _isUIVisible.Value = false;

            // 네트워크 호출 (비동기, Fire-and-forget)
            CloseChestNetworkAsync(chestId).Forget();

            // MessagePipe로 상자 닫힘 이벤트 발행
            _chestClosedPublisher.Publish(new ChestClosedMessage(chestId));
        }

        /// <summary>
        /// 상자 닫기 네트워크 요청
        /// </summary>
        private async UniTaskVoid CloseChestNetworkAsync(int chestId)
        {
            try
            {
                var result = await _networkSource.CloseChestAsync(chestId);
            }
            catch (System.Exception e)
            {
                // 상자 닫기는 중요하지 않은 작업이므로 Warning 레벨로 처리
                if (e.Message.Contains("canceled"))
                {
                    Debug.LogWarning($"[ChestService] 상자 닫기 요청 취소됨 (타임아웃): ID={chestId}");
                }
                else
                {
                    DebugLogError($"네트워크 상자 닫기 실패: {e.Message}");
                }
            }
        }

        public bool TakeItemFromCurrentChest(int slotIndex, int amount = -1)
        {
            if (!_chestModel.HasCurrentChest)
                return false;

            return _chestModel.TakeItemFromCurrentChest(slotIndex, amount);
        }

        public void UpdateCurrentChestSlot(int slotIndex, string itemId, int count)
        {
            if (_chestModel.HasCurrentChest)
            {
                _chestModel.UpdateCurrentChestSlot(slotIndex, itemId, count);
            }
        }

        #endregion

        #region 서버 동기화

        public void SyncChestData(int chestId, ChestSlot[] serverSlots)
        {
            _chestModel.SyncChestData(chestId, serverSlots);
        }

        public void SyncChestSlot(int chestId, int slotIndex, string itemId, int count)
        {
            _chestModel.SyncChestSlot(chestId, slotIndex, itemId, count);
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

            if (currentChestId >= 0)
            {
                _chestClosedPublisher.Publish(new ChestClosedMessage(currentChestId));
            }
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

        #region 서버 메시지 처리

        /// <summary>
        /// 서버로부터 받은 상자 데이터 동기화 처리
        /// </summary>
        private void OnServerDataSync(ChestServerDataSyncMessage message)
        {
            if (message.Items == null) return;

            var chestSlots = new ChestSlot[9]; // 9개 슬롯 고정

            for (int i = 0; i < chestSlots.Length; i++)
            {
                if (i < message.Items.Count)
                {
                    int itemId = message.Items[i];
                    if (itemId > 0) // 유효한 아이템
                    {
                        chestSlots[i] = new ChestSlot(itemId.ToString(), 1);
                    }
                    else // -1 또는 0이면 빈 슬롯
                    {
                        chestSlots[i] = new ChestSlot();
                    }
                }
                else
                {
                    chestSlots[i] = new ChestSlot(); // 빈 슬롯
                }
            }

            SyncChestData(message.ChestId, chestSlots);

            // 3x3 그리드로 아이템 표시
            string grid = "\n┌─────┬─────┬─────┐\n";
            for (int row = 0; row < 3; row++)
            {
                grid += "│";
                for (int col = 0; col < 3; col++)
                {
                    int index = row * 3 + col;
                    int itemId = index < message.Items.Count ? message.Items[index] : -1;
                    string cell = itemId > 0 ? $"  {itemId}  " : "  -  ";
                    grid += cell + "│";
                }
                grid += "\n" + (row < 2 ? "├─────┼─────┼─────┤\n" : "└─────┴─────┴─────┘");
            }

            Debug.Log($"[ChestService] 상자 ID {message.ChestId} 동기화 완료:{grid}");

            // 서버 데이터 동기화 후 UI 강제 업데이트를 위한 이벤트 발행
            _chestOpenedPublisher.Publish(new ChestOpenedMessage(message.ChestId, $"Chest_{message.ChestId}"));
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

        #region Dispose

        public void Dispose()
        {
            _messageDisposables?.Dispose();
            _currentChestState?.Dispose();
            _isUIVisible?.Dispose();
        }

        #endregion
    }
}