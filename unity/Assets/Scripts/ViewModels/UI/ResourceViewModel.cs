using System;
using UnityEngine;
using MVVM.Core;

namespace MVVM.UI
{
    [System.Serializable]
    public class ResourceInventorySlot
    {
        public int itemCount;
        public int maxCount;
        public bool isActive;

        public ResourceInventorySlot(int maxCount = 99)
        {
            this.maxCount = maxCount;
            this.itemCount = 0;
            this.isActive = true;
        }

        public bool CanUse() => isActive && itemCount > 0;
        public bool CanAdd(int amount = 1) => isActive && itemCount + amount <= maxCount;

        public bool UseItem(int amount = 1)
        {
            if (!CanUse() || itemCount < amount) return false;
            itemCount -= amount;
            return true;
        }

        public bool AddItem(int amount = 1)
        {
            if (!CanAdd(amount)) return false;
            itemCount += amount;
            return true;
        }
    }

    public class ResourceViewModel : BaseViewModel
    {
        [Header("Light/Mushroom State")]
        [SerializeField] private int _lightCount = 0;

        [Header("Inventory State")]
        [SerializeField] private ResourceInventorySlot _slot1 = new ResourceInventorySlot();
        [SerializeField] private ResourceInventorySlot _slot2 = new ResourceInventorySlot();
        [SerializeField] private ResourceInventorySlot _slot3 = new ResourceInventorySlot();

        [Header("Sprites")]
        [SerializeField] private Sprite _lightJellySprite;
        [SerializeField] private Sprite _backgroundItemSlotSprite;

        public event Action<int> LightCountChanged;
        public event Action<int, int> InventorySlotChanged; // slotNumber, newCount
        public event Action<int, int> InventorySlotUsed; // slotNumber, usedAmount
        public event Action<Sprite, Sprite> SpritesChanged;

        #region Properties

        public int LightCount
        {
            get => _lightCount;
            set
            {
                if (SetProperty(ref _lightCount, Mathf.Max(0, value)))
                {
                    LightCountChanged?.Invoke(_lightCount);
                }
            }
        }

        public ResourceInventorySlot Slot1
        {
            get => _slot1;
            set => SetProperty(ref _slot1, value ?? new ResourceInventorySlot());
        }

        public ResourceInventorySlot Slot2
        {
            get => _slot2;
            set => SetProperty(ref _slot2, value ?? new ResourceInventorySlot());
        }

        public ResourceInventorySlot Slot3
        {
            get => _slot3;
            set => SetProperty(ref _slot3, value ?? new ResourceInventorySlot());
        }

        public Sprite LightJellySprite
        {
            get => _lightJellySprite;
            set
            {
                if (SetProperty(ref _lightJellySprite, value))
                {
                    SpritesChanged?.Invoke(_lightJellySprite, _backgroundItemSlotSprite);
                }
            }
        }

        public Sprite BackgroundItemSlotSprite
        {
            get => _backgroundItemSlotSprite;
            set
            {
                if (SetProperty(ref _backgroundItemSlotSprite, value))
                {
                    SpritesChanged?.Invoke(_lightJellySprite, _backgroundItemSlotSprite);
                }
            }
        }

        #endregion

        #region Light/Mushroom Methods

        public int AddLight(int amount)
        {
            if (amount <= 0) return 0;

            if (ViewModels.UI.InventoryViewModel.Instance != null)
            {
                int actualAdded = ViewModels.UI.InventoryViewModel.Instance.AddFeeding(amount);
                UpdateLightFromInventoryViewModel();
                return actualAdded;
            }
            else
            {
                LightCount += amount;
                return amount;
            }
        }

        public bool RemoveLight(int amount)
        {
            if (amount <= 0) return false;

            if (ViewModels.UI.InventoryViewModel.Instance != null)
            {
                bool success = ViewModels.UI.InventoryViewModel.Instance.RemoveFeeding(amount);
                if (success) UpdateLightFromInventoryViewModel();
                return success;
            }
            else
            {
                if (_lightCount >= amount)
                {
                    LightCount -= amount;
                    return true;
                }
                return false;
            }
        }

        public bool HasEnoughLight(int requiredAmount)
        {
            if (ViewModels.UI.InventoryViewModel.Instance != null)
            {
                return ViewModels.UI.InventoryViewModel.Instance.HasEnoughFeeding(requiredAmount);
            }
            return _lightCount >= requiredAmount;
        }

        public void UpdateLightFromInventoryViewModel()
        {
            if (ViewModels.UI.InventoryViewModel.Instance != null)
            {
                LightCount = ViewModels.UI.InventoryViewModel.Instance.GetFeedingCount();
            }
        }

        #endregion

        #region Inventory Methods

        public bool UseInventoryItem(int slotNumber, int amount = 1)
        {
            var slot = GetInventorySlot(slotNumber);
            if (slot != null && slot.UseItem(amount))
            {
                InventorySlotChanged?.Invoke(slotNumber, slot.itemCount);
                InventorySlotUsed?.Invoke(slotNumber, amount);
                HUDEvents.TriggerInventoryUse(slotNumber, amount);

                if (EnableDebugLogs)
                {
                    Debug.Log($"[ResourceViewModel] 슬롯 {slotNumber} 사용, 남은 개수: {slot.itemCount}");
                }
                return true;
            }

            if (EnableDebugLogs)
            {
                Debug.Log($"[ResourceViewModel] 슬롯 {slotNumber} 사용 불가 (개수: {slot?.itemCount ?? 0})");
            }
            return false;
        }

        public bool AddInventoryItem(int slotNumber, int amount = 1)
        {
            var slot = GetInventorySlot(slotNumber);
            if (slot != null && slot.AddItem(amount))
            {
                InventorySlotChanged?.Invoke(slotNumber, slot.itemCount);
                HUDEvents.TriggerInventoryUpdate(slotNumber, slot.itemCount);

                if (EnableDebugLogs)
                {
                    Debug.Log($"[ResourceViewModel] 슬롯 {slotNumber}에 {amount}개 추가, 총 개수: {slot.itemCount}");
                }
                return true;
            }

            if (EnableDebugLogs)
            {
                Debug.Log($"[ResourceViewModel] 슬롯 {slotNumber}에 추가 불가 (현재: {slot?.itemCount ?? 0}, 최대: {slot?.maxCount ?? 0})");
            }
            return false;
        }

        public void SetInventoryItemCount(int slotNumber, int count)
        {
            var slot = GetInventorySlot(slotNumber);
            if (slot != null)
            {
                int oldCount = slot.itemCount;
                slot.itemCount = Mathf.Clamp(count, 0, slot.maxCount);

                if (oldCount != slot.itemCount)
                {
                    InventorySlotChanged?.Invoke(slotNumber, slot.itemCount);
                    HUDEvents.TriggerInventoryUpdate(slotNumber, slot.itemCount);

                    if (EnableDebugLogs)
                    {
                        Debug.Log($"[ResourceViewModel] 슬롯 {slotNumber} 개수를 {slot.itemCount}로 설정");
                    }
                }
            }
        }

        public int GetInventoryItemCount(int slotNumber)
        {
            var slot = GetInventorySlot(slotNumber);
            return slot?.itemCount ?? 0;
        }

        public bool CanUseInventorySlot(int slotNumber)
        {
            var slot = GetInventorySlot(slotNumber);
            return slot?.CanUse() ?? false;
        }

        public void SetSlotActive(int slotNumber, bool active)
        {
            var slot = GetInventorySlot(slotNumber);
            if (slot != null)
            {
                slot.isActive = active;
                OnPropertyChanged(nameof(GetInventorySlot)); // UI 업데이트 트리거
            }
        }

        private ResourceInventorySlot GetInventorySlot(int slotNumber)
        {
            return slotNumber switch
            {
                1 => _slot1,
                2 => _slot2,
                3 => _slot3,
                _ => null,
            };
        }

        #endregion

        #region Sprite Management

        public void SetSprites(Sprite lightSprite, Sprite itemSlotSprite)
        {
            // 둘 다 한번에 설정하고 이벤트는 한 번만 발생
            bool changed = false;

            if (_lightJellySprite != lightSprite)
            {
                _lightJellySprite = lightSprite;
                changed = true;
            }

            if (_backgroundItemSlotSprite != itemSlotSprite)
            {
                _backgroundItemSlotSprite = itemSlotSprite;
                changed = true;
            }

            if (changed)
            {
                OnPropertiesChanged(nameof(LightJellySprite), nameof(BackgroundItemSlotSprite));
                SpritesChanged?.Invoke(_lightJellySprite, _backgroundItemSlotSprite);
            }
        }

        #endregion

        #region BaseViewModel Override

        protected override void InitializeViewModel()
        {
            base.InitializeViewModel();

            _lightCount = 0;
            _slot1 = new ResourceInventorySlot();
            _slot2 = new ResourceInventorySlot();
            _slot3 = new ResourceInventorySlot();

            // InventoryViewModel에서 현재 빛 개수 동기화
            UpdateLightFromInventoryViewModel();

            if (EnableDebugLogs)
            {
                Debug.Log("[ResourceViewModel] 초기화 완료");
            }
        }

        protected override void CleanupViewModel()
        {
            base.CleanupViewModel();

            LightCountChanged = null;
            InventorySlotChanged = null;
            InventorySlotUsed = null;
            SpritesChanged = null;

            if (EnableDebugLogs)
            {
                Debug.Log("[ResourceViewModel] 정리 완료");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log($"[ResourceViewModel] State:\n" +
                     $"  LightCount: {LightCount}\n" +
                     $"  Slot1: {Slot1.itemCount}/{Slot1.maxCount} (Active: {Slot1.isActive})\n" +
                     $"  Slot2: {Slot2.itemCount}/{Slot2.maxCount} (Active: {Slot2.isActive})\n" +
                     $"  Slot3: {Slot3.itemCount}/{Slot3.maxCount} (Active: {Slot3.isActive})\n" +
                     $"  InventoryViewModel: {(ViewModels.UI.InventoryViewModel.Instance != null ? "Connected" : "NULL")}");
        }

        [ContextMenu("Initialize For Testing")]
        private void InitializeForTesting()
        {
            SetInventoryItemCount(1, 2);
            SetInventoryItemCount(2, 1);
            SetInventoryItemCount(3, 3);
            LightCount = 24;
        }

        [ContextMenu("Add 10 Light")]
        private void DebugAddLight() => AddLight(10);

        [ContextMenu("Remove 5 Light")]
        private void DebugRemoveLight() => RemoveLight(5);

        [ContextMenu("Use Slot 1")]
        private void DebugUseSlot1() => UseInventoryItem(1);

        #endregion
    }
}