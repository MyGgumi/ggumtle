using System;
using UnityEngine;
using Models;
using Services;
using MVVM.Core;

namespace ViewModels.UI
{
    public class InventoryViewModel : BaseViewModel
    {
        [Header("Model & Service")]
        [SerializeField] private InventoryModel _inventoryModel;
        [SerializeField] private InventoryService _inventoryService;

        [Header("Player Inventory")]
        [SerializeField] private string[] _playerSlotItemIds = new string[3];
        [SerializeField] private int[] _playerSlotCounts = new int[3];

        [Header("Feeding Inventory")]
        [SerializeField] private int _feedingCount;
        [SerializeField] private int _maxFeedingCount = 999;

        public event Action<int, string, int> OnPlayerSlotChanged;
        public event Action<int> OnFeedingCountChanged;
        public event Action<string, int> OnGlobalItemChanged;

        void Awake()
        {
            if (_inventoryModel == null)
                _inventoryModel = new InventoryModel();

            if (_inventoryService == null)
                _inventoryService = new InventoryService(_inventoryModel);

            SubscribeToServiceEvents();
            InitializeFromModel();
        }

        void OnDestroy()
        {
            UnsubscribeFromServiceEvents();
        }

        #region Service Event Handling

        private void SubscribeToServiceEvents()
        {
            if (_inventoryService != null)
            {
                _inventoryService.OnPlayerSlotChanged += HandlePlayerSlotChanged;
                _inventoryService.OnFeedingCountChanged += HandleFeedingCountChanged;
                _inventoryService.OnGlobalItemChanged += HandleGlobalItemChanged;
            }
        }

        private void UnsubscribeFromServiceEvents()
        {
            if (_inventoryService != null)
            {
                _inventoryService.OnPlayerSlotChanged -= HandlePlayerSlotChanged;
                _inventoryService.OnFeedingCountChanged -= HandleFeedingCountChanged;
                _inventoryService.OnGlobalItemChanged -= HandleGlobalItemChanged;
            }
        }

        private void HandlePlayerSlotChanged(int slotIndex, string itemId, int count)
        {
            if (slotIndex >= 0 && slotIndex < _playerSlotItemIds.Length)
            {
                _playerSlotItemIds[slotIndex] = itemId;
                _playerSlotCounts[slotIndex] = count;
                OnPlayerSlotChanged?.Invoke(slotIndex, itemId, count);
                NotifyPropertyChanged();
            }
        }

        private void HandleFeedingCountChanged(int newCount)
        {
            _feedingCount = newCount;
            OnFeedingCountChanged?.Invoke(newCount);
            NotifyPropertyChanged();
        }

        private void HandleGlobalItemChanged(string itemId, int count)
        {
            OnGlobalItemChanged?.Invoke(itemId, count);
            NotifyPropertyChanged();
        }

        #endregion

        #region Initialization

        private void InitializeFromModel()
        {
            for (int i = 0; i < _inventoryModel.playerSlots.Length; i++)
            {
                var slot = _inventoryModel.playerSlots[i];
                _playerSlotItemIds[i] = slot.itemId;
                _playerSlotCounts[i] = slot.count;
            }

            _feedingCount = _inventoryModel.feedingCount;
            _maxFeedingCount = _inventoryModel.maxFeedingCount;
        }

        #endregion

        #region Player Inventory Operations

        public bool AddToPlayerSlot(int slotIndex, string itemId, int amount = 1)
        {
            return _inventoryService.AddToPlayerSlot(slotIndex, itemId, amount);
        }

        public bool UsePlayerSlot(int slotIndex, int amount = 1)
        {
            return _inventoryService.UsePlayerSlot(slotIndex, amount);
        }

        public bool CanUsePlayerSlot(int slotIndex)
        {
            return _inventoryService.CanUsePlayerSlot(slotIndex);
        }

        public string GetPlayerSlotItemId(int slotIndex)
        {
            return _inventoryService.GetPlayerSlotItemId(slotIndex);
        }

        public int GetPlayerSlotCount(int slotIndex)
        {
            return _inventoryService.GetPlayerSlotCount(slotIndex);
        }

        #endregion

        #region Feeding Inventory Operations

        public int AddFeeding(int amount)
        {
            return _inventoryService.AddFeeding(amount);
        }

        public bool RemoveFeeding(int amount)
        {
            return _inventoryService.RemoveFeeding(amount);
        }

        public bool HasEnoughFeeding(int amount)
        {
            return _inventoryService.HasEnoughFeeding(amount);
        }

        public int GetFeedingCount()
        {
            return _feedingCount;
        }

        public int GetMaxFeedingCount()
        {
            return _maxFeedingCount;
        }

        #endregion

        #region Global Items Operations

        public void SetGlobalItemCount(string itemId, int count)
        {
            _inventoryService.SetGlobalItemCount(itemId, count);
        }

        public int GetGlobalItemCount(string itemId)
        {
            return _inventoryService.GetGlobalItemCount(itemId);
        }

        public bool HasGlobalItem(string itemId, int amount = 1)
        {
            return _inventoryService.HasGlobalItem(itemId, amount);
        }

        #endregion

        #region Legacy Compatibility (FeedingInventory integration)

        public static InventoryViewModel Instance { get; private set; }

        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[InventoryViewModel] Multiple instances detected! Using first instance.");
            }
        }

        public int LightCount => GetFeedingCount();
        public int MaxLightCount => GetMaxFeedingCount();

        public int TakeLight(int amount)
        {
            if (amount <= 0) return 0;

            int actualTaken = Mathf.Min(amount, _feedingCount);
            if (RemoveFeeding(actualTaken))
            {
                return actualTaken;
            }
            return 0;
        }

        public void GainLight(int amount)
        {
            if (amount > 0)
            {
                AddFeeding(amount);
            }
        }

        #endregion

        #region Utility Methods

        public void Reset()
        {
            _inventoryService.Reset();
            InitializeFromModel();
        }

        public int GetTotalItemCount()
        {
            return _inventoryService.GetTotalItemCount();
        }

        #endregion

        #region Properties for Inspector

        public int FeedingCount => _feedingCount;
        public int MaxFeedingCount => _maxFeedingCount;
        public string[] PlayerSlotItemIds => _playerSlotItemIds;
        public int[] PlayerSlotCounts => _playerSlotCounts;

        #endregion
    }
}