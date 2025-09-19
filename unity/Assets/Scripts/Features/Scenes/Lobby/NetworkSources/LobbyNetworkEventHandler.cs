using DotNetty.Transport.Channels;
using Features.Scenes.Lobby.Messages;
using MessagePipe;
using Networks.Attributes;
using Networks.Packets;
using Networks.Sessions;
using UnityEngine;
using VContainer;

namespace Features.Scenes.Lobby.NetworkSources
{
    /// <summary>
    /// 로비 관련 네트워크 이벤트를 처리하는 핸들러
    /// CommandHandler로 패킷을 받아서 MessagePipe로 이벤트 발행
    /// </summary>
    public class LobbyNetworkEventHandler
    {
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public LobbyNetworkEventHandler()
        {
            if (_enableDebugLogs)
            {
                Debug.Log("[LobbyNetworkEventHandler] 초기화 완료");
            }
        }

        [CommandHandler(PacketType.VerifyTokenResponse)]
        public static void HandleVerifyTokenResponse(
            VerifyTokenCommand command,
            IChannelHandlerContext ctx
        )
        {
            Debug.Log("[LobbyNetworkEventHandler] 토큰 검증 응답 수신");
            Debug.Log(
                $"[LobbyNetworkEventHandler] SessionId: {command.SessionId}, Success: {command.Success}"
            );

            // MessagePipe로 토큰 검증 완료 이벤트 발행 (LobbySceneManager용)
            var publisher = GlobalMessagePipe.GetPublisher<TokenVerifiedMessage>();
            if (command.Success)
            {
                publisher.Publish(TokenVerifiedMessage.Success(command.SessionId));
                Debug.Log(
                    "[LobbyNetworkEventHandler] 토큰 검증 성공 메시지 발행 → LobbySceneManager"
                );
            }
            else
            {
                publisher.Publish(TokenVerifiedMessage.Failure("토큰 검증 실패"));
                Debug.Log(
                    "[LobbyNetworkEventHandler] 토큰 검증 실패 메시지 발행 → LobbySceneManager"
                );
            }
        }
    }
}
