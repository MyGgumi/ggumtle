package com.ggumtle.loadtest.protocol.codec

import com.ggumtle.loadtest.protocol.Packet
import com.ggumtle.loadtest.protocol.PacketHeader
import io.netty.buffer.ByteBuf
import io.netty.channel.ChannelHandlerContext
import io.netty.handler.codec.ByteToMessageDecoder
import mu.KotlinLogging

private val logger = KotlinLogging.logger {}

/**
 * Decodes bytes from network to Packet objects
 */
class PacketDecoder : ByteToMessageDecoder() {

    override fun decode(ctx: ChannelHandlerContext, buf: ByteBuf, out: MutableList<Any>) {
        // Wait for complete header
        if (buf.readableBytes() < PacketHeader.HEADER_SIZE) {
            return
        }

        buf.markReaderIndex()

        // Read header
        val header = PacketHeader.fromByteBuf(buf)

        // Wait for complete body
        if (buf.readableBytes() < header.dataLength) {
            buf.resetReaderIndex()
            return
        }

        // Read data if present
        val data = if (header.dataLength > 0) {
            ByteArray(header.dataLength).also { buf.readBytes(it) }
        } else {
            null
        }

        val packet = Packet(header, data)
        out.add(packet)

        logger.trace {
            "Decoded packet: type=${header.packetType}, len=${header.dataLength}"
        }
    }
}
