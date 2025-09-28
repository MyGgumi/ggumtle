using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.ItemUsage.Messages;
using Features.ItemUsage.NetworkSources;
using Features.Item.Services;
using Features.Inventory.Messages;
using Features.Player.Services;
using MessagePipe;
using Networks.Players;
using R3;
using UnityEngine;
using VContainer;

namespace Features.ItemUsage.Services
{
    /// <summary>
    /// 아이템 사용 서비스 구현체
    /// 테이저건, 섬광탄, 자가제세동기 사용 로직 담당
    /// </summary>
    public class ItemUsageServiceImpl : IItemUsageService, IDisposable
    {
        #region Constants

        private const int TASER_GUN_ID = 3;
        private const int FLASH_BANG_ID = 2;
        private const int SELF_DEFIBRILLATOR_ID = 4;

        #endregion

        #region Dependencies

        private readonly IItemUsageNetworkSource _networkSource;
        private readonly IPublisher<SelfDefibrillatorUsedMessage> _selfDefibUsedPublisher;
        private readonly IPublisher<TaserGunUsedMessage> _taserUsedPublisher;
        private readonly IPublisher<FlashBangUsedMessage> _flashBangUsedPublisher;
        private readonly IPublisher<ItemUsageResultMessage> _itemUsageResultPublisher;
        private readonly PlayerManagerService _playerManagerService;
        private readonly ISubscriber<ItemUsedMessage> _itemUsedSubscriber;

        #endregion

        #region Private Fields

        private readonly Dictionary<int, float> _itemCooldowns = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public ItemUsageServiceImpl(
            IItemUsageNetworkSource networkSource,
            IPublisher<SelfDefibrillatorUsedMessage> selfDefibUsedPublisher,
            IPublisher<TaserGunUsedMessage> taserUsedPublisher,
            IPublisher<FlashBangUsedMessage> flashBangUsedPublisher,
            IPublisher<ItemUsageResultMessage> itemUsageResultPublisher,
            PlayerManagerService playerManagerService,
            ISubscriber<ItemUsedMessage> itemUsedSubscriber)
        {
            _networkSource = networkSource;
            _selfDefibUsedPublisher = selfDefibUsedPublisher;
            _taserUsedPublisher = taserUsedPublisher;
            _flashBangUsedPublisher = flashBangUsedPublisher;
            _itemUsageResultPublisher = itemUsageResultPublisher;
            _playerManagerService = playerManagerService;
            _itemUsedSubscriber = itemUsedSubscriber;

            // 쿨다운 타이머 초기화
            InitializeCooldowns();

            // 메시지 구독 시작
            SubscribeToMessages();

            if (_enableDebugLogs)
            {
                Debug.Log("[ItemUsageServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public async UniTask<bool> UseTaserGunAsync(long userId)
        {
            try
            {
                if (!CanUseItem(userId, TASER_GUN_ID))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[ItemUsageServiceImpl] 테이저건 사용 불가: UserId={userId}");
                    }
                    return false;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 테이저건 사용 시작: UserId={userId}");
                }

                // 플레이어 위치에서 전방 3미터 지점에 이펙트 생성
                var playerGO = _playerManagerService?.GetLocalPlayer()?.GameObject;
                Vector3 effectPosition = playerGO != null
                    ? playerGO.transform.position + playerGO.transform.forward * 3f
                    : Vector3.zero;

                var result = await _networkSource.UseItemAsync(TASER_GUN_ID, effectPosition);
                bool success = result.Success;

                // 쿨다운 적용
                ApplyCooldown(TASER_GUN_ID);

                // 결과 메시지 발행 (서버에서 받은 좌표 사용)
                Vector3 serverPosition = result.position;
                _taserUsedPublisher.Publish(new TaserGunUsedMessage(userId, serverPosition, success));
                _itemUsageResultPublisher.Publish(new ItemUsageResultMessage(TASER_GUN_ID, success, result.Result.ToString()));

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 테이저건 사용 완료: Success={success}");
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageServiceImpl] 테이저건 사용 실패: {e.Message}");
                return false;
            }
        }

        public async UniTask<bool> UseFlashBangAsync(long userId)
        {
            try
            {
                if (!CanUseItem(userId, FLASH_BANG_ID))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[ItemUsageServiceImpl] 섬광탄 사용 불가: UserId={userId}");
                    }
                    return false;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 섬광탄 사용 시작: UserId={userId}");
                }

                // 플레이어 위치에서 전방 3미터 지점에 이펙트 생성
                var playerGO = _playerManagerService?.GetLocalPlayer()?.GameObject;
                Vector3 effectPosition = playerGO != null
                    ? playerGO.transform.position + playerGO.transform.forward * 3f
                    : Vector3.zero;

                // 이펙트 위치를 direction 파라미터로 전달 (서버에서 vx,vy,vz를 이펙트 좌표로 사용)
                var result = await _networkSource.UseItemAsync(FLASH_BANG_ID, effectPosition);
                bool success = result.Success;

                // 쿨다운 적용
                ApplyCooldown(FLASH_BANG_ID);

                // 결과 메시지 발행 (서버에서 받은 좌표 사용)
                Vector3 serverPosition = result.position;
                _flashBangUsedPublisher.Publish(new FlashBangUsedMessage(userId, serverPosition, success));
                _itemUsageResultPublisher.Publish(new ItemUsageResultMessage(FLASH_BANG_ID, success, result.Result.ToString()));

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 섬광탄 사용 완료: Success={success}");
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageServiceImpl] 섬광탄 사용 실패: {e.Message}");
                return false;
            }
        }

        public async UniTask<bool> UseSelfDefibrillatorAsync(long userId)
        {
            try
            {
                if (!CanUseItem(userId, SELF_DEFIBRILLATOR_ID))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[ItemUsageServiceImpl] 자가제세동기 사용 불가: UserId={userId}");
                    }
                    return false;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 자가제세동기 사용 시작: UserId={userId}");
                }

                // 새로운 UseDefibrillator 네트워크 함수 사용
                var result = await _networkSource.UseDefibrillatorAsync();
                bool success = result.Success;

                // 쿨다운 적용
                ApplyCooldown(SELF_DEFIBRILLATOR_ID);

                // Revival Feature에 메시지 전달 (HP 정보 포함)
                _selfDefibUsedPublisher.Publish(new SelfDefibrillatorUsedMessage(userId, success, result.Hp));
                _itemUsageResultPublisher.Publish(new ItemUsageResultMessage(SELF_DEFIBRILLATOR_ID, success, result.Result.ToString()));

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 자가제세동기 사용 완료: Success={success}, HP={result.Hp}");
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageServiceImpl] 자가제세동기 사용 실패: {e.Message}");
                return false;
            }
        }

        public bool CanUseItem(long userId, int itemId)
        {
            // 로컬 플레이어만 아이템 사용 가능
            if (_playerManagerService != null)
            {
                var localPlayerId = _playerManagerService.GetLocalPlayer().Id;
                if (userId != localPlayerId)
                {
                    return false;
                }
            }

            // 아이템 정의 확인
            var itemDef = ItemDefinitionService.GetItemById(itemId);
            if (itemDef == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[ItemUsageServiceImpl] 알 수 없는 아이템 ID: {itemId}");
                }
                return false;
            }

            // 쿨다운 확인
            if (GetItemCooldownRemaining(itemId) > 0)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[ItemUsageServiceImpl] 아이템 쿨다운 중: {itemDef.ItemName} ({GetItemCooldownRemaining(itemId):F1}초 남음)");
                }
                return false;
            }

            return true;
        }

        public float GetItemCooldownRemaining(int itemId)
        {
            if (_itemCooldowns.TryGetValue(itemId, out var cooldownEndTime))
            {
                float remaining = cooldownEndTime - Time.time;
                return Mathf.Max(0, remaining);
            }
            return 0;
        }

        #endregion

        #region Private Methods

        private void SubscribeToMessages()
        {
            if (_itemUsedSubscriber != null)
            {
                _itemUsedSubscriber
                    .Subscribe(OnItemUsed)
                    .AddTo(_disposables);

                if (_enableDebugLogs)
                {
                    Debug.Log("[ItemUsageServiceImpl] ItemUsedMessage 구독 완료");
                }
            }
        }

        private async void OnItemUsed(ItemUsedMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 아이템 사용 메시지 수신: ItemId={message.itemId}, SlotNumber={message.slotNumber}");
                }

                // 로컬 플레이어 ID 가져오기
                var localPlayerId = _playerManagerService?.GetLocalPlayer()?.Id ?? -1;
                if (localPlayerId <= 0)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning("[ItemUsageServiceImpl] 로컬 플레이어 ID를 찾을 수 없음");
                    }
                    return;
                }

                // 아이템 ID에 따른 분기 처리
                switch (message.itemId)
                {
                    case SELF_DEFIBRILLATOR_ID:
                        if (_enableDebugLogs)
                        {
                            Debug.Log("[ItemUsageServiceImpl] 자가제세동기 사용 처리");
                        }
                        await UseSelfDefibrillatorAsync(localPlayerId);
                        break;

                    case TASER_GUN_ID:
                        if (_enableDebugLogs)
                        {
                            Debug.Log("[ItemUsageServiceImpl] 테이저건 사용 처리");
                        }
                        await UseTaserGunAsync(localPlayerId);
                        break;

                    case FLASH_BANG_ID:
                        if (_enableDebugLogs)
                        {
                            Debug.Log("[ItemUsageServiceImpl] 섬광탄 사용 처리");
                        }
                        await UseFlashBangAsync(localPlayerId);
                        break;

                    default:
                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[ItemUsageServiceImpl] 알 수 없는 아이템 ID: {message.itemId}");
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageServiceImpl] 아이템 사용 메시지 처리 실패: {e.Message}");
            }
        }

        private void InitializeCooldowns()
        {
            _itemCooldowns[TASER_GUN_ID] = 0;
            _itemCooldowns[FLASH_BANG_ID] = 0;
            _itemCooldowns[SELF_DEFIBRILLATOR_ID] = 0;
        }

        private void ApplyCooldown(int itemId)
        {
            var itemDef = ItemDefinitionService.GetItemById(itemId);
            if (itemDef != null && itemDef.CooldownTime > 0)
            {
                _itemCooldowns[itemId] = Time.time + itemDef.CooldownTime;

                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 쿨다운 적용: {itemDef.ItemName} ({itemDef.CooldownTime}초)");
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _disposables?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[ItemUsageServiceImpl] Dispose 완료");
            }
        }

        #endregion
    }
}