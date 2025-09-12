using System;
using System.Collections.Concurrent;
using System.Reflection;
using DotNetty.Transport.Channels;
using Network;
using Networks.Packets;
using UnityEngine;

namespace Networks.Pipelines
{
    public class PacketHandler : SimpleChannelInboundHandler<Packet>
    {
        private readonly CommandFactoryMapper _commandFactoryMapper = CommandFactoryMapper.Instance;
        private readonly CommandDispatcher _commandDispatcher = CommandDispatcher.Instance;
        
        protected override void ChannelRead0(IChannelHandlerContext ctx, Packet packet)
        {
            Debug.Log($"Packet Received: {packet.Header.Type}");
            
            // 모든 패킷 처리를 메인 스레드에서 실행하도록 MainThreadDispatcher로 넘김
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                try
                {
                    // 패킷을 Command로 변환
                    var command = _commandFactoryMapper.CreateCommand(packet.Header.Type, packet.Data);
                    
                    if (command != null)
                    {
                        Debug.Log($"Command created: {command.Type}");
                        
                        // CommandDispatcher를 통해 처리 (CommandHandler 우선, 없으면 NetworkApi)
                        _commandDispatcher.Dispatch(command, ctx);
                    }
                    else
                    {
                        Debug.LogWarning($"Command를 생성할 수 없습니다: {packet.Header.Type}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"패킷 처리 중 오류 발생: {ex.Message}");
                }
            });
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Debug.Log("Channel Active");
            
            // Channel Active 이벤트도 메인 스레드에서 처리
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                Debug.Log("Channel Active - MainThread에서 처리됨");
            });
            
            base.ChannelActive(context);
        }
        
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Debug.Log("Channel Inactive");
            
            // Channel Inactive 이벤트도 메인 스레드에서 처리
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                Debug.Log("Channel Inactive - MainThread에서 처리됨");
            });
            
            base.ChannelInactive(context);
        }
    }
}