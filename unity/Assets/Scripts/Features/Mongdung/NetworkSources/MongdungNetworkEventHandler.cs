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
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MongdungNetworkEventHandler] 몽둥이 공격 응답: Result={command.Result}, TargetId={command.targetId}, LeftHp={command.leftHp}, Success={command.Success}"
                    );
                }

                // MessagePipeBridge를 통해 이벤트 발행 (모든 몽둥이에게 브로드캐스트)
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // 몽둥이 공격 응답 메시지 발행
                    var attackResponseMessage = new MongdungAttackResponseMessage(
                        command.Result,
                        command.targetId,
                        command.leftHp
                    );
                    bridge.PublishMessage(attackResponseMessage);
                }
                else
                {
                    Debug.LogError("[MongdungNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
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
                // skillType을 MongdungActionType으로 변환
                MongdungActionType actionType = command.skillType switch
                {
                    1 => MongdungActionType.Frighten,
                    2 => MongdungActionType.TrapSetting,
                    _ => MongdungActionType.Attack,
                };

                if (_enableDebugLogs)
                {
                    string skillName = actionType == MongdungActionType.TrapSetting ? "TrapSetting(꿈틀이 심기)" : actionType.ToString();
                    Debug.Log(
                        $"[MongdungNetworkEventHandler] 몽둥이 스킬 응답: SkillType={command.skillType}, ActionType={skillName}, Result={command.Result}, Success={command.Success}"
                    );
                }

                // MessagePipeBridge를 통해 이벤트 발행 (모든 몽둥이에게 브로드캐스트)
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // 몽둥이 스킬 응답 메시지 발행
                    var skillResponseMessage = new MongdungSkillResponseMessage(
                        command.skillType,
                        actionType,
                        command.Result
                    );
                    bridge.PublishMessage(skillResponseMessage);
                }
                else
                {
                    Debug.LogError("[MongdungNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
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
