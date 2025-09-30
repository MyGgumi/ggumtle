using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.FieldItem.Messages;
using Features.FieldItem.Models;
using Features.FieldItem.NetworkSources;
using Features.Player.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.FieldItem.Services
{
    /// <summary>
    /// 필드 아이템 서비스 구현체
    /// 필드 아이템 사용 로직과 아이템 효과 적용 담당
    /// </summary>
    public class FieldItemServiceImpl : IFieldItemService, IDisposable
    {
        #region Constants

        private const int HEAL_PACK_HP_AMOUNT = 50;
        private const float SPEED_PACK_MULTIPLIER = 2.5f;
        private const float SPEED_PACK_DURATION = 5f;

        #endregion

        #region Dependencies

        private readonly IFieldItemNetworkSource _networkSource;
        private readonly IPublisher<FieldItemGlobalUsedMessage> _globalUsedPublisher;
        private readonly IPublisher<HealthChangedMessage> _healthChangedPublisher;
        private readonly IPublisher<SpeedChangedMessage> _speedChangedPublisher;
        private readonly ISubscriber<FieldItemUseRequestMessage> _useRequestSubscriber;
        private readonly ISubscriber<FieldItemGlobalUsedMessage> _globalUsedSubscriber;
        private readonly PlayerManagerService _playerManagerService;

        #endregion

        #region Private Fields

        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        // 내가 마지막으로 요청한 필드 아이템 ID 추적
        private int _lastRequestedFieldItemId = -1;

        // 필드 아이템 ID -> 타입 매핑 (서버 스폰 정보에서 동적 등록)
        private readonly Dictionary<int, FieldItemType> _fieldItemTypeMap = new();

        #endregion

        #region Constructor

        [Inject]
        public FieldItemServiceImpl(
            IFieldItemNetworkSource networkSource,
            IPublisher<FieldItemGlobalUsedMessage> globalUsedPublisher,
            IPublisher<HealthChangedMessage> healthChangedPublisher,
            IPublisher<SpeedChangedMessage> speedChangedPublisher,
            ISubscriber<FieldItemUseRequestMessage> useRequestSubscriber,
            ISubscriber<FieldItemGlobalUsedMessage> globalUsedSubscriber,
            PlayerManagerService playerManagerService)
        {
            _networkSource = networkSource;
            _globalUsedPublisher = globalUsedPublisher;
            _healthChangedPublisher = healthChangedPublisher;
            _speedChangedPublisher = speedChangedPublisher;
            _useRequestSubscriber = useRequestSubscriber;
            _globalUsedSubscriber = globalUsedSubscriber;
            _playerManagerService = playerManagerService;

            SubscribeToMessages();

            if (_enableDebugLogs)
            {
                Debug.Log("[FieldItemServiceImpl] 초기화 완료");
            }
        }

        #endregion

        #region Public Methods

        public void UseFieldItem(int fieldItemId)
        {
            try
            {
                if (!CanUseFieldItem(fieldItemId))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[FieldItemServiceImpl] 필드 아이템 사용 불가: FieldItemId={fieldItemId}");
                    }
                    return;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[FieldItemServiceImpl] 필드 아이템 사용 요청: FieldItemId={fieldItemId}");
                }

                // 요청한 아이템 ID 기록
                _lastRequestedFieldItemId = fieldItemId;

                // 네트워크로 요청 전송 (void - 브로드캐스트로 응답 받음)
                _networkSource.UseFieldItem(fieldItemId);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[FieldItemServiceImpl] 필드 아이템 사용 요청 전송 완료: FieldItemId={fieldItemId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FieldItemServiceImpl] 필드 아이템 사용 요청 실패: {e.Message}");
            }
        }

        public void RegisterFieldItem(int fieldItemId, FieldItemType itemType)
        {
            _fieldItemTypeMap[fieldItemId] = itemType;

            if (_enableDebugLogs)
            {
                Debug.Log($"[FieldItemServiceImpl] 필드 아이템 등록: ID={fieldItemId}, Type={itemType}");
            }
        }

        public FieldItemType? GetFieldItemType(int fieldItemId)
        {
            return _fieldItemTypeMap.TryGetValue(fieldItemId, out var type) ? type : null;
        }

        public bool CanUseFieldItem(int fieldItemId)
        {
            // 로컬 플레이어 확인
            if (_playerManagerService?.GetLocalPlayer() == null)
            {
                return false;
            }

            // 필드 아이템 타입 확인
            var itemType = GetFieldItemType(fieldItemId);
            if (itemType == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[FieldItemServiceImpl] 알 수 없는 필드 아이템 ID: {fieldItemId}");
                }
                return false;
            }

            return true;
        }

        #endregion

        #region Private Methods

        private void SubscribeToMessages()
        {
            // 필드 아이템 사용 요청 메시지 구독
            _useRequestSubscriber
                .Subscribe(OnFieldItemUseRequest)
                .AddTo(_disposables);

            // 필드 아이템 전역 사용 메시지 구독
            _globalUsedSubscriber
                .Subscribe(OnFieldItemGlobalUsed)
                .AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[FieldItemServiceImpl] 메시지 구독 완료");
            }
        }

        private void OnFieldItemUseRequest(FieldItemUseRequestMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[FieldItemServiceImpl] 필드 아이템 사용 요청 수신: FieldItemId={message.fieldItemId}");
                }

                UseFieldItem(message.fieldItemId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FieldItemServiceImpl] 필드 아이템 사용 요청 처리 실패: {e.Message}");
            }
        }

        private void OnFieldItemGlobalUsed(FieldItemGlobalUsedMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[FieldItemServiceImpl] 필드 아이템 전역 사용 메시지 수신: FieldItemId={message.fieldItemId}, Success={message.success}");
                }

                // 내가 요청한 아이템인 경우 성공/실패 관계없이 아이템 효과 적용
                if (message.fieldItemId == _lastRequestedFieldItemId)
                {
                    ApplyFieldItemEffect(message.fieldItemId);
                    _lastRequestedFieldItemId = -1; // 초기화

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[FieldItemServiceImpl] 아이템 효과 적용 (성공/실패 무관): FieldItemId={message.fieldItemId}, Success={message.success}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FieldItemServiceImpl] 필드 아이템 전역 사용 메시지 처리 실패: {e.Message}");
            }
        }

        private void ApplyFieldItemEffect(int fieldItemId)
        {
            var itemType = GetFieldItemType(fieldItemId);
            if (itemType == null)
            {
                return;
            }

            var localPlayer = _playerManagerService?.GetLocalPlayer();
            if (localPlayer == null)
            {
                return;
            }

            long userId = localPlayer.Id;

            switch (itemType.Value)
            {
                case FieldItemType.HealPack:
                    ApplyHealPackEffect(userId);
                    break;

                case FieldItemType.SpeedPack:
                    ApplySpeedPackEffect(userId);
                    break;

                default:
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[FieldItemServiceImpl] 알 수 없는 필드 아이템 타입: {itemType}");
                    }
                    break;
            }
        }

        private void ApplyHealPackEffect(long userId)
        {
            // HP +50 효과 적용
            _healthChangedPublisher.Publish(new HealthChangedMessage(userId, 0, HEAL_PACK_HP_AMOUNT));

            if (_enableDebugLogs)
            {
                Debug.Log($"[FieldItemServiceImpl] 힐팩 효과 적용: UserId={userId}, HP+{HEAL_PACK_HP_AMOUNT}");
            }
        }

        private void ApplySpeedPackEffect(long userId)
        {
            // 이동속도 증가 효과 적용
            _speedChangedPublisher.Publish(new SpeedChangedMessage(userId, SPEED_PACK_MULTIPLIER, SPEED_PACK_DURATION));

            if (_enableDebugLogs)
            {
                Debug.Log($"[FieldItemServiceImpl] 스피드팩 효과 적용: UserId={userId}, SpeedMultiplier={SPEED_PACK_MULTIPLIER}, Duration={SPEED_PACK_DURATION}초");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _disposables?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[FieldItemServiceImpl] Dispose 완료");
            }
        }

        #endregion
    }
}