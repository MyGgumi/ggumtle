using System;
using DotNetty.Transport.Channels;
using Features.Mongging.Messages;
using Features.Mongging.Models;
using Networks.Attributes;
using Networks.Game;
using Networks.Packets;
using Networks.Players;
using UnityEngine;

namespace Features.Mongging.NetworkSources
{
    /// <summary>
    /// 몽깅이 네트워크 이벤트를 처리하는 핸들러
    /// 서버로부터 몽깅이 상태 브로드캐스트를 수신하여 처리
    /// </summary>
    public static class MonggingNetworkEventHandler
    {
        private static readonly bool _enableDebugLogs = true;

        /// <summary>
        /// 몽깅이 상태 브로드캐스트 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.MonggingStateBroadcast)]
        public static void MonggingStateBroadcast(
            MonggingStateBroadcastCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                if (command == null)
                {
                    Debug.LogWarning(
                        "[MonggingNetworkEventHandler] MonggingStateBroadcastCommand가 null입니다."
                    );
                    return;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[MonggingNetworkEventHandler] 📡 서버로부터 몽깅이 상태 브로드캐스트 수신: PlayerId={command.playerId}, Type={command.type}"
                    );
                }

                // 서버 상태 코드를 MonggingPlayerState로 변환
                var playerState = ConvertServerStateToMonggingState(command.type);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingNetworkEventHandler] 상태 변환: Type={command.type} → State={playerState}");
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // 서버 상태 메시지 발행
                    var serverStateMessage = new MonggingPlayerServerStateMessage(command.playerId, playerState);
                    bridge.PublishMessage(serverStateMessage);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            $"[MonggingNetworkEventHandler] 몽깅이 상태 변경 메시지 발행 성공: PlayerId={command.playerId}, State={playerState}"
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "[MonggingNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[MonggingNetworkEventHandler] 몽깅이 상태 브로드캐스트 처리 실패: {e.Message}"
                );
            }
        }


        /// <summary>
        /// 서버 상태 코드를 MonggingPlayerState로 변환
        /// </summary>
        private static MonggingPlayerState ConvertServerStateToMonggingState(MonggingStateType serverStateType)
        {
            return serverStateType switch
            {
                MonggingStateType.Normal => MonggingPlayerState.Normal,
                MonggingStateType.Digging => MonggingPlayerState.Digging,
                MonggingStateType.Feeding => MonggingPlayerState.Feeding,
                MonggingStateType.Knockout => MonggingPlayerState.Fainted,
                MonggingStateType.Dead => MonggingPlayerState.Dead,
                MonggingStateType.Escape => MonggingPlayerState.Escaped,
                MonggingStateType.Stunned => MonggingPlayerState.Stunned,
                _ => MonggingPlayerState.Normal
            };
        }
    }
}
