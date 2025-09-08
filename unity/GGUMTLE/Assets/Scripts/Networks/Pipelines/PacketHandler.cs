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
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Debug.Log("Channel Active");
            base.ChannelActive(context);
        }
        
        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Debug.Log("Channel Inactive");
            base.ChannelInactive(context);
        }
    }
}