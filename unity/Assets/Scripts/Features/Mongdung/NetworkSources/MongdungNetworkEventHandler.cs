using DotNetty.Transport.Channels;
using Features.Mongdung.Messages;
using Features.Mongdung.Models;
using MessagePipe;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;
using UnityEngine;
using VContainer;

namespace Features.Mongdung.NetworkSources
{
    /// <summary>
    /// 몽둥이 네트워크 이벤트 핸들러
    /// 서버에서 오는 몽둥이 관련 네트워크 이벤트를 처리하고 로컬 메시지로 발행
    /// Static 메서드를 사용하여 CommandDispatcher에서 직접 호출 가능
    /// </summary>
    public class MongdungNetworkEventHandler
    {
        private static bool _enableDebugLogs = true;

        /// <summary>
        /// 디버그 로그 활성화/비활성화
        /// </summary>
        public static bool EnableDebugLogs
        {
            get => _enableDebugLogs;
            set => _enableDebugLogs = value;
        }

        /// <summary>
        /// 서버에서 오는 몽둥이 액션 브로드캐스트 처리
        /// </summary>
        public static void HandleActionBroadcast(
            long playerId,
            int actionTypeCode,
            int statusCode,
            Vector3 position,
            Vector3 direction
        )
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

                // MessagePipeBridge를 통해 메시지 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            $"[MongdungNetworkEventHandler] 액션 브로드캐스트 처리: PlayerId={playerId}, ActionType={actionType}, Status={statusCode}"
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "[MongdungNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"[MongdungNetworkEventHandler] 액션 브로드캐스트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 서버에서 오는 몽둥이 액션 완료 알림 처리
        /// </summary>
        public static void HandleActionCompleted(
            long playerId,
            int actionTypeCode,
            bool success,
            Vector3 position
        )
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

                // MessagePipeBridge를 통해 메시지 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            $"[MongdungNetworkEventHandler] 액션 완료 처리: PlayerId={playerId}, ActionType={actionType}, Success={success}"
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "[MongdungNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
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
        public static void HandleStateChanged(
            long playerId,
            int previousStateCode,
            int newStateCode
        )
        {
            try
            {
                var previousState = (MongdungState)previousStateCode;
                var newState = (MongdungState)newStateCode;

                var message = new MongdungStateChangedMessage(playerId, previousState, newState);

                // MessagePipeBridge를 통해 메시지 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            $"[MongdungNetworkEventHandler] 상태 변경 처리: PlayerId={playerId}, {previousState} → {newState}"
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "[MongdungNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungNetworkEventHandler] 상태 변경 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 몽둥이 공격 응답 처리
        /// </summary>
        [CommandHandler(PacketType.MongdungAttackResponse)]
        public static void HandleMongdungAttackResponse(
            MongdungAttackCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                Debug.Log("🎯 [MongdungNetworkEventHandler] HandleMongdungAttackResponse 호출됨!");

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MongdungNetworkEventHandler] 몽둥이 공격 응답: Result={command.Result}, TargetId={command.targetId}, LeftHp={command.leftHp}, Success={command.Success}"
                    );
                }

                // NetworkApi의 pending request는 CommandDispatcher에서 이미 처리됨
                // 중복 호출 방지를 위해 제거
                Debug.Log($"📡 NetworkApi.Instance 존재 여부: {Networks.NetworkApi.Instance != null}");

                // 성공한 경우 Remote 플레이어들에게 애니메이션 트리거 메시지 발행
                if (command.Success)
                {
                    var bridge = Networks.MessagePipeBridge.Instance;
                    if (bridge != null)
                    {
                        var message = new MongdungActionCompletedMessage(
                            0, // Local 플레이어의 ID는 별도로 구해야 하지만 일단 0으로
                            MongdungActionType.Attack,
                            true,
                            Vector3.zero
                        );
                        bridge.PublishMessage(message);

                        if (_enableDebugLogs)
                        {
                            Debug.Log("[MongdungNetworkEventHandler] Attack 성공 - Remote 애니메이션 트리거 메시지 발행");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"[MongdungNetworkEventHandler] 몽둥이 공격 응답 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 몽둥이 스킬 응답 처리 (TrapSetting, Frighten)
        /// </summary>
        [CommandHandler(PacketType.MongdungSkillResponse)]
        public static void HandleMongdungSkillResponse(
            MongdungSkillCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                Debug.Log("🎯 [MongdungNetworkEventHandler] HandleMongdungSkillResponse 호출됨!");

                // skillType을 MongdungActionType으로 변환
                MongdungActionType actionType = command.skillType switch
                {
                    1 => MongdungActionType.Frighten,
                    2 => MongdungActionType.TrapSetting,
                    _ => MongdungActionType.Attack, // 기본값
                };

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MongdungNetworkEventHandler] 몽둥이 스킬 응답: SkillType={command.skillType}, ActionType={actionType}, Result={command.Result}, Success={command.Success}"
                    );
                }

                // NetworkApi의 pending request는 CommandDispatcher에서 이미 처리됨
                // 중복 호출 방지를 위해 제거
                Debug.Log($"📡 NetworkApi.Instance 존재 여부: {Networks.NetworkApi.Instance != null}");

                // 성공한 경우 Remote 플레이어들에게 애니메이션 트리거 메시지 발행
                if (command.Success)
                {
                    var bridge = Networks.MessagePipeBridge.Instance;
                    if (bridge != null)
                    {
                        var message = new MongdungActionCompletedMessage(
                            0, // Local 플레이어의 ID는 별도로 구해야 하지만 일단 0으로
                            actionType,
                            true,
                            Vector3.zero
                        );
                        bridge.PublishMessage(message);

                        if (_enableDebugLogs)
                        {
                            Debug.Log($"[MongdungNetworkEventHandler] {actionType} 성공 - Remote 애니메이션 트리거 메시지 발행");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(
                    $"[MongdungNetworkEventHandler] 몽둥이 스킬 응답 처리 실패: {e.Message}"
                );
            }
        }
    }
}
