using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Models;
using Services;
using MVVM.Core;

namespace ViewModels.UI
{
    public class ChestViewModel : BaseViewModel
    {
        [Header("Model & Service")]
        [SerializeField] private ChestModel _chestModel;
        [SerializeField] private ChestService _chestService;

        [Header("Current Chest State")]
        [SerializeField] private string _currentChestId = "";
        [SerializeField] private string _currentChestName = "";
        [SerializeField] private bool _isChestOpen = false;
        [SerializeField] private ChestSlot[] _currentChestSlots = new ChestSlot[9];

        [Header("UI State")]
        [SerializeField] private bool _isUIVisible = false;

        [Header("Distance Check")]
        [SerializeField] private float _maxInteractionDistance = 3f;
        [SerializeField] private Transform _playerTransform;
        private InteractableChest _currentChestObject;

        public event Action<string> OnChestOpened;
        public event Action<string> OnChestClosed;
        public event Action<string, int, string, int> OnChestSlotChanged;
        public event Action<string> OnChestRegistered;
        public event Action<string> OnChestUnregistered;
        public event Action<bool> OnChestUIVisibilityChanged;

        void Awake()
        {
            if (_chestModel == null)
                _chestModel = new ChestModel();

            if (_chestService == null)
                _chestService = new ChestService(_chestModel);

            SubscribeToServiceEvents();
            InitializeFromModel();
        }

        void OnDestroy()
        {
            UnsubscribeFromServiceEvents();
        }

        void Update()
        {
            // 상자가 열려있고 플레이어가 설정되어 있을 때만 거리 체크
            if (_isChestOpen && _playerTransform != null && _currentChestObject != null)
            {
                float distance = Vector3.Distance(_playerTransform.position, _currentChestObject.transform.position);

                // 해당 상자의 상호작용 범위 사용 (fallback: _maxInteractionDistance)
                float maxDistance = _currentChestObject.interactionRange > 0
                    ? _currentChestObject.interactionRange
                    : _maxInteractionDistance;

                // 상호작용 범위를 벗어났을 때
                if (distance > maxDistance)
                {
                    Debug.Log($"[ChestViewModel] 플레이어가 상자 범위를 벗어남 (거리: {distance:F2}m, 최대: {maxDistance}m)");
                    CloseChest();
                }
            }
        }

        #region Service Event Handling

        private void SubscribeToServiceEvents()
        {
            if (_chestService != null)
            {
                _chestService.OnChestOpened += HandleChestOpened;
                _chestService.OnChestClosed += HandleChestClosed;
                _chestService.OnChestSlotChanged += HandleChestSlotChanged;
                _chestService.OnChestRegistered += HandleChestRegistered;
                _chestService.OnChestUnregistered += HandleChestUnregistered;
            }
        }

        private void UnsubscribeFromServiceEvents()
        {
            if (_chestService != null)
            {
                _chestService.OnChestOpened -= HandleChestOpened;
                _chestService.OnChestClosed -= HandleChestClosed;
                _chestService.OnChestSlotChanged -= HandleChestSlotChanged;
                _chestService.OnChestRegistered -= HandleChestRegistered;
                _chestService.OnChestUnregistered -= HandleChestUnregistered;
            }
        }

        private void HandleChestOpened(string chestId)
        {
            var chest = _chestService.GetChest(chestId);
            if (chest != null)
            {
                _currentChestId = chestId;
                _currentChestName = chest.chestName;
                _isChestOpen = true;

                // 현재 상자 오브젝트 찾기
                _currentChestObject = FindObjectsByType<InteractableChest>(FindObjectsSortMode.None)
                    .FirstOrDefault(c => c.ChestId == chestId);

                // 플레이어 찾기 (한 번만)
                if (_playerTransform == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                        _playerTransform = player.transform;
                }

                for (int i = 0; i < _currentChestSlots.Length && i < chest.slots.Length; i++)
                {
                    _currentChestSlots[i] = new ChestSlot(chest.slots[i].itemId, chest.slots[i].count);
                }

                _isUIVisible = true;
                OnChestUIVisibilityChanged?.Invoke(true);
                OnChestOpened?.Invoke(chestId);
                NotifyPropertyChanged();
            }
        }

        private void HandleChestClosed(string chestId)
        {
            // InteractableChest 상태 업데이트
            var interactableChest = FindObjectsByType<InteractableChest>(FindObjectsSortMode.None)
                .FirstOrDefault(chest => chest.ChestId == chestId);
            if (interactableChest != null)
            {
                interactableChest.isOpen = false;
            }

            _currentChestId = "";
            _currentChestName = "";
            _isChestOpen = false;
            _currentChestObject = null; // 현재 상자 오브젝트 정리

            // 슬롯 데이터 정리
            for (int i = 0; i < _currentChestSlots.Length; i++)
            {
                _currentChestSlots[i] = new ChestSlot();
            }

            // UI 즉시 숨김
            _isUIVisible = false;
            OnChestUIVisibilityChanged?.Invoke(false);
            OnChestClosed?.Invoke(chestId);
            NotifyPropertyChanged();
        }

        private void HandleChestSlotChanged(string chestId, int slotIndex, string itemId, int count)
        {
            if (chestId == _currentChestId && slotIndex >= 0 && slotIndex < _currentChestSlots.Length)
            {
                _currentChestSlots[slotIndex] = new ChestSlot(itemId, count);
                OnChestSlotChanged?.Invoke(chestId, slotIndex, itemId, count);
                NotifyPropertyChanged();
            }
        }

        private void HandleChestRegistered(string chestId)
        {
            OnChestRegistered?.Invoke(chestId);
            NotifyPropertyChanged();
        }

        private void HandleChestUnregistered(string chestId)
        {
            OnChestUnregistered?.Invoke(chestId);
            NotifyPropertyChanged();
        }

        #endregion

        #region Initialization

        private void InitializeFromModel()
        {
            _isChestOpen = _chestModel.IsChestOpen;
            _currentChestId = _chestModel.currentChestId;

            if (_chestModel.currentChest != null)
            {
                _currentChestName = _chestModel.currentChest.chestName;
                for (int i = 0; i < _currentChestSlots.Length && i < _chestModel.currentChest.slots.Length; i++)
                {
                    _currentChestSlots[i] = new ChestSlot(_chestModel.currentChest.slots[i].itemId, _chestModel.currentChest.slots[i].count);
                }
            }

            _isUIVisible = _chestModel.isChestUIOpen;
        }

        #endregion

        #region Chest Management

        public void RegisterChest(string chestId, string chestName, Vector3 position, GameObject chestObject = null)
        {
            _chestService.RegisterChest(chestId, chestName, position, chestObject);
        }

        public void UnregisterChest(string chestId)
        {
            _chestService.UnregisterChest(chestId);
        }

        public ChestData GetChest(string chestId)
        {
            return _chestService.GetChest(chestId);
        }

        public bool HasChest(string chestId)
        {
            return _chestService.HasChest(chestId);
        }

        #endregion

        #region Chest Operations

        public bool OpenChest(string chestId)
        {
            Debug.Log($"[ChestViewModel] OpenChest 호출됨: {chestId}");
            bool result = _chestService.OpenChest(chestId);
            Debug.Log($"[ChestViewModel] ChestService.OpenChest 결과: {result}");
            return result;
        }

        public void CloseChest()
        {
            _chestService.CloseCurrentChest();
        }

        public bool TakeItemFromSlot(int slotIndex, int amount = -1)
        {
            return _chestService.TakeItemFromCurrentChest(slotIndex, amount);
        }

        public void UpdateSlot(int slotIndex, string itemId, int count)
        {
            _chestService.UpdateCurrentChestSlot(slotIndex, itemId, count);
        }

        public bool TransferToInventory(int slotIndex, InventoryViewModel inventoryViewModel, int targetSlot = -1)
        {
            if (!_isChestOpen || slotIndex < 0 || slotIndex >= _currentChestSlots.Length)
                return false;

            var chestSlot = _currentChestSlots[slotIndex];
            if (chestSlot.isEmpty) return false;

            // 기존 가명 아이템을 실제 ID로 변환 (호환성)
            string actualItemId = Models.ItemTypeHelper.ConvertLegacyId(chestSlot.itemId);

            // 아이템 타입에 따른 처리
            var itemType = Models.ItemTypeHelper.GetItemType(actualItemId);
            var displayName = Models.ItemTypeHelper.GetDisplayName(actualItemId);

            Debug.Log($"[ChestViewModel] 아이템 전송 시도: {chestSlot.itemId} ({displayName}) x{chestSlot.count}, 타입: {itemType}");

            switch (itemType)
            {
                case Models.ItemType.Feeding:
                    // 빛젤리는 먹이 시스템으로
                    int addedAmount = inventoryViewModel.AddFeeding(chestSlot.count);
                    if (addedAmount > 0)
                    {
                        TakeItemFromSlot(slotIndex);
                        Debug.Log($"[ChestViewModel] {displayName} {addedAmount}개 획득!");
                        return true;
                    }
                    Debug.Log($"[ChestViewModel] {displayName} 저장 공간이 부족합니다.");
                    return false;

                case Models.ItemType.Equipment:
                    // 장비 아이템은 플레이어 슬롯으로 (최대 3개 제한)
                    if (targetSlot >= 0)
                    {
                        if (inventoryViewModel.AddToPlayerSlot(targetSlot, actualItemId, chestSlot.count))
                        {
                            TakeItemFromSlot(slotIndex);
                            Debug.Log($"[ChestViewModel] {displayName} {chestSlot.count}개 획득! (슬롯 {targetSlot})");
                            return true;
                        }
                        else
                        {
                            // 해당 슬롯에 추가 실패 (최대 3개 제한 또는 다른 아이템)
                            ShowEquipmentLimitNotification(displayName);
                        }
                    }
                    else
                    {
                        // 빈 슬롯 자동 찾기
                        for (int i = 0; i < 3; i++)
                        {
                            if (inventoryViewModel.AddToPlayerSlot(i, actualItemId, chestSlot.count))
                            {
                                TakeItemFromSlot(slotIndex);
                                Debug.Log($"[ChestViewModel] {displayName} {chestSlot.count}개 획득! (슬롯 {i})");
                                return true;
                            }
                        }
                        // 모든 슬롯에서 추가 실패
                        ShowEquipmentLimitNotification(displayName);
                    }
                    Debug.Log($"[ChestViewModel] 인벤토리가 가득 찼거나 {displayName}이(가) 최대 개수에 도달했습니다.");
                    return false;

                default:
                    // 기타 아이템 처리 (기존 방식 유지)
                    Debug.LogWarning($"[ChestViewModel] 알 수 없는 아이템 타입: {chestSlot.itemId} -> {actualItemId}");
                    if (targetSlot >= 0)
                    {
                        if (inventoryViewModel.AddToPlayerSlot(targetSlot, actualItemId, chestSlot.count))
                        {
                            TakeItemFromSlot(slotIndex);
                            Debug.Log($"[ChestViewModel] {displayName} {chestSlot.count}개 획득! (기타 아이템, 슬롯 {targetSlot})");
                            return true;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (inventoryViewModel.AddToPlayerSlot(i, actualItemId, chestSlot.count))
                            {
                                TakeItemFromSlot(slotIndex);
                                Debug.Log($"[ChestViewModel] {displayName} {chestSlot.count}개 획득! (기타 아이템, 슬롯 {i})");
                                return true;
                            }
                        }
                    }
                    Debug.Log($"[ChestViewModel] 인벤토리가 가득 찼습니다. {displayName}을(를) 가져올 수 없습니다.");
                    return false;
            }
        }

        /// <summary>
        /// 장비 아이템 최대 개수 제한 알림 표시
        /// </summary>
        private void ShowEquipmentLimitNotification(string itemName)
        {
            var universalHUD = UnityEngine.GameObject.FindFirstObjectByType<UniversalHUDController>();
            if (universalHUD != null && universalHUD.notificationViewModel != null)
            {
                universalHUD.notificationViewModel.ShowNotification($"{itemName}은(는) 최대 3개까지만 보유할 수 있습니다!", 2.5f);
                Debug.Log($"[ChestViewModel] 장비 아이템 제한 알림 표시: {itemName}");
            }
        }

        #endregion

        #region Server Sync

        public void SyncChestData(string chestId, ChestSlot[] serverSlots)
        {
            _chestService.SyncChestData(chestId, serverSlots);
        }

        public void SyncChestSlot(string chestId, int slotIndex, string itemId, int count)
        {
            _chestService.SyncChestSlot(chestId, slotIndex, itemId, count);
        }

        #endregion

        #region Legacy Compatibility (ChestBoxUIManager integration)

        public static ChestViewModel Instance { get; private set; }

        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[ChestViewModel] Multiple instances detected! Using first instance.");
            }
        }

        public void ShowChestUI(string chestId)
        {
            if (OpenChest(chestId))
            {
                _isUIVisible = true;
                OnChestUIVisibilityChanged?.Invoke(true);
            }
        }

        public void HideChestUI()
        {
            CloseChest();
        }

        public void SetChestUIVisibility(bool visible)
        {
            _isUIVisible = visible;
            OnChestUIVisibilityChanged?.Invoke(visible);

            if (!visible && _isChestOpen)
            {
                CloseChest();
            }
        }

        #endregion

        #region Utility Methods

        public List<ChestData> GetAllChests()
        {
            return _chestService.GetAllChests();
        }

        public List<ChestData> GetChestsWithItems()
        {
            return _chestService.GetChestsWithItems();
        }

        public int GetTotalChestCount()
        {
            return _chestService.GetTotalChestCount();
        }

        public int GetTotalItemsInAllChests()
        {
            return _chestService.GetTotalItemsInAllChests();
        }

        public void Reset()
        {
            _chestService.Reset();
            InitializeFromModel();
        }

        #endregion

        #region Properties

        public string CurrentChestId => _currentChestId;
        public string CurrentChestName => _currentChestName;
        public bool IsChestOpen => _isChestOpen;
        public bool IsUIVisible => _isUIVisible;
        public ChestSlot[] CurrentChestSlots => _currentChestSlots;
        public bool HasCurrentChest => _chestService.HasCurrentChest;

        #endregion
    }
}