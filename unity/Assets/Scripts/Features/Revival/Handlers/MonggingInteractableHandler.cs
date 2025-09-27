using System;
using Features.Revival.Messages;
using Features.Revival.Views;
using Features.Player.Services;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Revival.Handlers
{
    /// <summary>
    /// 몽깅이 상호작용 상태 변경 핸들러
    /// MonggingInteractableStateMessage를 구독하여 FaintedMonggingInteractable 컴포넌트를 토글
    /// </summary>
    public class MonggingInteractableHandler : IStartable, IDisposable
    {
        #region Dependencies

        private readonly PlayerManagerService _playerManagerService;
        private readonly ISubscriber<MonggingInteractableStateMessage> _subscriber;

        #endregion

        #region Private Fields

        private readonly bool _enableDebugLogs = true;
        private IDisposable _subscription;

        #endregion

        #region Constructor

        [Inject]
        public MonggingInteractableHandler(
            PlayerManagerService playerManagerService,
            ISubscriber<MonggingInteractableStateMessage> subscriber)
        {
            _playerManagerService = playerManagerService ?? throw new ArgumentNullException(nameof(playerManagerService));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        #endregion

        #region IStartable

        public void Start()
        {
            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // 메시지 구독
            _subscription = _subscriber.Subscribe(OnMonggingInteractableStateChanged);

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingInteractableHandler] 초기화 완료 - MonggingInteractableStateMessage 구독 시작");
            }
        }

        #endregion

        #region Message Handlers

        private void OnMonggingInteractableStateChanged(MonggingInteractableStateMessage message)
        {
            try
            {
                if (message == null)
                {
                    Debug.LogWarning("[MonggingInteractableHandler] 메시지가 null입니다");
                    return;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingInteractableHandler] 상호작용 상태 변경: PlayerId={message.PlayerId}, IsInteractable={message.IsInteractable}, Name={message.PlayerName}");
                }

                // PlayerManagerService를 통해 플레이어 GameObject 찾기
                var playerObject = _playerManagerService.GetPlayerObject(message.PlayerId);
                if (playerObject == null)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[MonggingInteractableHandler] 플레이어 GameObject를 찾을 수 없음: PlayerId={message.PlayerId}");
                    }
                    return;
                }

                // FaintedMonggingInteractable 컴포넌트 찾기 (자식 GameObject 포함)
                var interactable = playerObject.GetComponentInChildren<FaintedMonggingInteractable>();
                if (interactable == null)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.LogWarning($"[MonggingInteractableHandler] FaintedMonggingInteractable 컴포넌트를 찾을 수 없음: PlayerId={message.PlayerId}, GameObject={playerObject.name}");
                    }
                    return;
                }

                // 상호작용 상태 토글
                interactable.enabled = message.IsInteractable;

                // 기절 상태일 때 플레이어 정보 설정
                if (message.IsInteractable)
                {
                    interactable.SetFaintedPlayer(message.PlayerId, message.PlayerName);
                }

                if (_enableDebugLogs)
                {
                    string action = message.IsInteractable ? "활성화" : "비활성화";
                    Debug.Log($"[MonggingInteractableHandler] FaintedMonggingInteractable {action} 완료: PlayerId={message.PlayerId}, Name={message.PlayerName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MonggingInteractableHandler] 상호작용 상태 변경 처리 중 예외 발생: {e.Message}");
                Debug.LogError($"[MonggingInteractableHandler] Stack trace: {e.StackTrace}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _subscription?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[MonggingInteractableHandler] Dispose 완료");
            }
        }

        #endregion
    }
}