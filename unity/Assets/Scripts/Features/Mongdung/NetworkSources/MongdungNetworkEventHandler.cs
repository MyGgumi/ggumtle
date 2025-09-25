using Features.Mongdung.Messages;
using Features.Mongdung.Models;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Features.Mongdung.NetworkSources
{
    /// <summary>
    /// 몽둥이 네트워크 이벤트 핸들러
    /// 서버에서 오는 몽둥이 관련 네트워크 이벤트를 처리하고 로컬 메시지로 발행
    /// </summary>
    public class MongdungNetworkEventHandler
    {
        private readonly IPublisher<MongdungActionBroadcastMessage> _actionBroadcastPublisher;
        private readonly IPublisher<MongdungActionCompletedMessage> _actionCompletedPublisher;
        private readonly IPublisher<MongdungStateChangedMessage> _stateChangedPublisher;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public MongdungNetworkEventHandler(
            IPublisher<MongdungActionBroadcastMessage> actionBroadcastPublisher,
            IPublisher<MongdungActionCompletedMessage> actionCompletedPublisher,
            IPublisher<MongdungStateChangedMessage> stateChangedPublisher)
        {
            _actionBroadcastPublisher = actionBroadcastPublisher;
            _actionCompletedPublisher = actionCompletedPublisher;
            _stateChangedPublisher = stateChangedPublisher;

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungNetworkEventHandler] 초기화 완료");
            }
        }

        /// <summary>
        /// 서버에서 오는 몽둥이 액션 브로드캐스트 처리
        /// </summary>
        public void HandleActionBroadcast(long playerId, int actionTypeCode, int statusCode, Vector3 position, Vector3 direction)
        {
            try
            {
                var actionType = (MongdungActionType)actionTypeCode;

                var message = new MongdungActionBroadcastMessage(
                    playerId,
                    actionType,
                    statusCode,
                    position,
                    direction
                );

                _actionBroadcastPublisher.Publish(message);

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MongdungNetworkEventHandler] 액션 브로드캐스트 처리: PlayerId={playerId}, ActionType={actionType}, Status={statusCode}"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungNetworkEventHandler] 액션 브로드캐스트 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 서버에서 오는 몽둥이 액션 완료 알림 처리
        /// </summary>
        public void HandleActionCompleted(long playerId, int actionTypeCode, bool success, Vector3 position)
        {
            try
            {
                var actionType = (MongdungActionType)actionTypeCode;

                var message = new MongdungActionCompletedMessage(
                    playerId,
                    actionType,
                    success,
                    position
                );

                _actionCompletedPublisher.Publish(message);

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MongdungNetworkEventHandler] 액션 완료 처리: PlayerId={playerId}, ActionType={actionType}, Success={success}"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungNetworkEventHandler] 액션 완료 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 서버에서 오는 몽둥이 상태 변경 알림 처리
        /// </summary>
        public void HandleStateChanged(long playerId, int previousStateCode, int newStateCode)
        {
            try
            {
                var previousState = (MongdungState)previousStateCode;
                var newState = (MongdungState)newStateCode;

                var message = new MongdungStateChangedMessage(
                    playerId,
                    previousState,
                    newState
                );

                _stateChangedPublisher.Publish(message);

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MongdungNetworkEventHandler] 상태 변경 처리: PlayerId={playerId}, {previousState} → {newState}"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungNetworkEventHandler] 상태 변경 처리 실패: {e.Message}");
            }
        }
    }
}