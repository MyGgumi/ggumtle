package com.ggumtle.loadtest.protocol.codec

import com.ggumtle.loadtest.protocol.Packet
import io.netty.buffer.ByteBuf
import io.netty.channel.ChannelHandlerContext
import io.netty.handler.codec.MessageToByteEncoder
import mu.KotlinLogging

private val logger = KotlinLogging.logger {}

/**
 * Encodes Packet to bytes for network transmission
 */
class PacketEncoder : MessageToByteEncoder<Packet>() {

    override fun encode(ctx: ChannelHandlerContext, packet: Packet, out: ByteBuf) {
        // Write header (14 bytes)
        packet.header.writeTo(out)

        // Write data if present
        packet.data?.let { out.writeBytes(it) }

        logger.trace {
            "Encoded packet: type=${packet.header.packetType}, len=${packet.header.dataLength}"
        }
    }
}
