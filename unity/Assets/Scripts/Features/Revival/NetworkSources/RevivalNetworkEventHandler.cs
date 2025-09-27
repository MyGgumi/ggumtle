using System;
using DotNetty.Transport.Channels;
using Features.Revival.Messages;
using Networks.Attributes;
using Networks.Game;
using Networks.Packets;
using Networks.Players;
using UnityEngine;

namespace Features.Revival.NetworkSources
{
    /// <summary>
    /// 부활 관련 네트워크 이벤트 처리 핸들러
    /// 서버로부터 부활 완료 브로드캐스트를 수신하여 처리
    /// </summary>
    public static class RevivalNetworkEventHandler
    {
        private static readonly bool _enableDebugLogs = true;

        /// <summary>
        /// 몽깅이 부활 완료 이벤트 처리 (MonggingNetworkEventHandler에서 이동)
        /// </summary>
        [CommandHandler(PacketType.MonggingRevivalComplete)]
        public static void MonggingRevivalComplete(
            MonggingRevivalCompleteCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                if (command == null)
                {
                    Debug.LogWarning(
                        "[RevivalNetworkEventHandler] MonggingRevivalCompleteCommand가 null입니다."
                    );
                    return;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[RevivalNetworkEventHandler] 📡 서버로부터 몽깅이 부활 완료 수신: RevivedPlayerId={command.revivedMonggingId}"
                    );
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // Revival 전용 완료 메시지로 변환
                    var revivalCompletedMessage = new RevivalCompletedMessage(
                        command.revivedMonggingId,
                        -1, // 부활시킨 플레이어 ID는 서버에서 제공되지 않음
                        50, // 기본 부활 체력
                        false // 직접 부활로 처리 (서버에서 오는 것이므로)
                    );
                    bridge.PublishMessage(revivalCompletedMessage);

                    // 기존 MonggingPlayerRevivedMessage도 호환성을 위해 발행
                    var legacyMessage = new Features.Mongging.Messages.MonggingPlayerRevivedMessage(
                        command.revivedMonggingId,
                        -1, // 부활시킨 플레이어 ID는 서버에서 제공되지 않음
                        50 // 기본 부활 체력
                    );
                    bridge.PublishMessage(legacyMessage);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            "[RevivalNetworkEventHandler] 몽깅이 부활 완료 메시지 발행 성공 → RevivalService & MonggingTeamService"
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "[RevivalNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[RevivalNetworkEventHandler] 몽깅이 부활 완료 처리 실패: {e.Message}"
                );
                Debug.LogError($"[RevivalNetworkEventHandler] Stack trace: {e.StackTrace}");
            }
        }
    }
}
