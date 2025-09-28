using System;
using System.Linq;
using DotNetty.Transport.Channels;
using Features.EscapeGate.Messages;
using MessagePipe;
using Networks;
using Networks.Attributes;
using Networks.Game;
using Networks.Packets;
using UnityEngine;
using VContainer;

namespace Features.EscapeGate.NetworkSources
{
    /// <summary>
    /// 탈출 게이트 관련 네트워크 이벤트를 처리하는 핸들러
    /// static CommandHandler들만 가지고 있는 순수 핸들러 클래스
    /// </summary>
    public static class EscapeGateNetworkEventHandler
    {
        // static CommandHandler를 위한 static Publisher 저장소
        private static IPublisher<EscapeGateOpenedMessage> _escapeGateOpenedPublisher;

        /// <summary>
        /// Publisher 설정 (DI Container에서 호출)
        /// </summary>
        public static void Initialize(IPublisher<EscapeGateOpenedMessage> escapeGateOpenedPublisher)
        {
            _escapeGateOpenedPublisher = escapeGateOpenedPublisher;
            Debug.Log("[EscapeGateNetworkEventHandler] Publisher 설정 완료");
        }

        /// <summary>
        /// 탈출구 오픈 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.ExitOpen)]
        public static void ExitOpen(ExitOpenCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log($"[EscapeGateNetworkEventHandler] 탈출구 오픈 수신: Count={command.count}, Exits=[{string.Join(", ", command.exits.Select(exit => exit.id))}]");

                if (_escapeGateOpenedPublisher != null)
                {
                    var message = new EscapeGateOpenedMessage(command.count, command.exits.Select(exit => exit.id).ToArray());
                    _escapeGateOpenedPublisher.Publish(message);
                    Debug.Log("[EscapeGateNetworkEventHandler] EscapeGateOpenedMessage 발행 완료");
                }
                else
                {
                    Debug.LogError("[EscapeGateNetworkEventHandler] EscapeGateOpenedPublisher가 설정되지 않음");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EscapeGateNetworkEventHandler] 탈출구 오픈 처리 실패: {e.Message}");
            }
        }
    }
}