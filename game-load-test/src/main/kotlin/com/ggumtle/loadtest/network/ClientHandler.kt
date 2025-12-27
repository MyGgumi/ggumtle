package com.ggumtle.loadtest.network

import com.ggumtle.loadtest.protocol.Packet
import io.netty.channel.ChannelHandlerContext
import io.netty.channel.SimpleChannelInboundHandler
import kotlinx.coroutines.flow.MutableSharedFlow
import mu.KotlinLogging

private val logger = KotlinLogging.logger {}

/**
 * Handles incoming packets and emits them to a SharedFlow
 */
class ClientHandler(
    private val packetFlow: MutableSharedFlow<Packet>
) : SimpleChannelInboundHandler<Packet>() {

    override fun channelActive(ctx: ChannelHandlerContext) {
        logger.debug { "Channel active: ${ctx.channel().remoteAddress()}" }
        super.channelActive(ctx)
    }

    override fun channelInactive(ctx: ChannelHandlerContext) {
        logger.debug { "Channel inactive: ${ctx.channel().remoteAddress()}" }
        super.channelInactive(ctx)
    }

    override fun channelRead0(ctx: ChannelHandlerContext, packet: Packet) {
        logger.trace { "Received packet: ${packet.header.packetType}" }
        packetFlow.tryEmit(packet)
    }

    override fun exceptionCaught(ctx: ChannelHandlerContext, cause: Throwable) {
        logger.error(cause) { "Exception in channel handler" }
        ctx.close()
    }
}
