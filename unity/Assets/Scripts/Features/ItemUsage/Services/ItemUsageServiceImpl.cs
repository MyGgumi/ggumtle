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
        private readonly Features.Mongging.Services.IMonggingTeamService _monggingTeamService;
        private readonly Features.Notification.Services.INotificationService _notificationService;

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
            ISubscriber<ItemUsedBroadcastMessage> itemUsedBroadcastSubscriber,
            PlayerManagerService playerManagerService,
            Features.Mongging.Services.IMonggingTeamService monggingTeamService,
            Features.Notification.Services.INotificationService notificationService)
        {
            _networkSource = networkSource;
            _selfDefibUsedPublisher = selfDefibUsedPublisher;
            _taserUsedPublisher = taserUsedPublisher;
            _flashBangUsedPublisher = flashBangUsedPublisher;
            _itemUsageResultPublisher = itemUsageResultPublisher;
            _playerManagerService = playerManagerService;
            _monggingTeamService = monggingTeamService;
            _notificationService = notificationService;

            // 쿨다운 타이머 초기화
            InitializeCooldowns();

            // 서버 브로드캐스트 구독
            itemUsedBroadcastSubscriber.Subscribe(OnItemUsedBroadcast).AddTo(_disposables);

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

                // 서버 응답 로그만 출력 (실제 이펙트는 브로드캐스트에서 처리)
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 테이저건 사용 요청 응답: Success={success}, Result={result.Result}");
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

                // 서버 응답 로그만 출력 (실제 이펙트는 브로드캐스트에서 처리)
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 섬광탄 사용 요청 응답: Success={success}, Result={result.Result}");
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
                // 기절 상태 체크 (Mongging 시스템에서 확인)
                if (_monggingTeamService != null)
                {
                    var localPlayer = _monggingTeamService.GetLocalPlayer();
                    if (localPlayer != null && localPlayer.currentState != Features.Mongging.Models.MonggingPlayerState.Fainted)
                    {
                        if (_enableDebugLogs)
                        {
                            Debug.LogWarning($"[ItemUsageServiceImpl] 자가제세동기 사용 불가: 기절 상태가 아님 UserId={userId}, State={localPlayer.currentState}");
                        }

                        // 알림 표시
                        _notificationService?.ShowItemCannotBeUsedNotification("자가제세동기", "기절 상태일 때만 사용 가능합니다");
                        return false;
                    }
                }

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

                // 자가제세동기는 1회용이므로 쿨다운 적용 안 함 (서버에서 처리)

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

        /// <summary>
        /// 서버 브로드캐스트 아이템 사용 이벤트 처리
        /// </summary>
        private void OnItemUsedBroadcast(ItemUsedBroadcastMessage message)
        {
            // Success 또는 Miss일 때만 이펙트 처리
            if (message.result != Networks.Players.MonggingItemUseResult.Success &&
                message.result != Networks.Players.MonggingItemUseResult.Miss)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ItemUsageServiceImpl] 아이템 사용 실패로 이펙트 생성 안 함: ItemId={message.itemId}, Result={message.result}");
                }
                return;
            }

            // 아이템 타입에 따라 처리
            switch (message.itemId)
            {
                case TASER_GUN_ID:
                    HandleTaserGunBroadcast(message);
                    break;
                case FLASH_BANG_ID:
                    HandleFlashBangBroadcast(message);
                    break;
                default:
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[ItemUsageServiceImpl] 알 수 없는 아이템 ID: {message.itemId}");
                    }
                    break;
            }
        }

        /// <summary>
        /// 테이저건 브로드캐스트 처리
        /// </summary>
        private void HandleTaserGunBroadcast(ItemUsedBroadcastMessage message)
        {
            bool success = message.result == Networks.Players.MonggingItemUseResult.Success;

            // 이펙트 생성을 위한 메시지 발행
            _taserUsedPublisher.Publish(new TaserGunUsedMessage(0, message.position, success));

            if (_enableDebugLogs)
            {
                Debug.Log($"[ItemUsageServiceImpl] 테이저건 이펙트 생성: Position={message.position}, Success={success}");
            }
        }

        /// <summary>
        /// 섬광탄 브로드캐스트 처리
        /// </summary>
        private void HandleFlashBangBroadcast(ItemUsedBroadcastMessage message)
        {
            bool success = message.result == Networks.Players.MonggingItemUseResult.Success;

            // 이펙트 생성을 위한 메시지 발행
            _flashBangUsedPublisher.Publish(new FlashBangUsedMessage(0, message.position, success));

            if (_enableDebugLogs)
            {
                Debug.Log($"[ItemUsageServiceImpl] 섬광탄 이펙트 생성: Position={message.position}, Success={success}");
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