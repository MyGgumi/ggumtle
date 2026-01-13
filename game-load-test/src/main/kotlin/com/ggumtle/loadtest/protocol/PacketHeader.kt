package com.ggumtle.loadtest.protocol

import io.netty.buffer.ByteBuf
import java.nio.ByteBuffer

/**
 * Packet header structure (14 bytes, Big Endian)
 *
 * [0-1]   short - Packet Type
 * [2-5]   int   - Data Length
 * [6-13]  long  - Timestamp
 */
data class PacketHeader(
    val packetType: Short,
    val dataLength: Int,
    val timestamp: Long
) {
    companion object {
        const val HEADER_SIZE = 14 // short(2) + int(4) + long(8)

        fun fromByteBuf(buf: ByteBuf): PacketHeader {
            return PacketHeader(
                packetType = buf.readShort(),
                dataLength = buf.readInt(),
                timestamp = buf.readLong()
            )
        }

        fun fromBytes(bytes: ByteArray): PacketHeader {
            require(bytes.size == HEADER_SIZE) { "Invalid header size: ${bytes.size}" }
            val buffer = ByteBuffer.wrap(bytes)
            return PacketHeader(
                packetType = buffer.short,
                dataLength = buffer.int,
                timestamp = buffer.long
            )
        }
    }

    fun writeTo(buf: ByteBuf) {
        buf.writeShort(packetType.toInt())
        buf.writeInt(dataLength)
        buf.writeLong(timestamp)
    }

    fun toBytes(): ByteArray {
        return ByteBuffer.allocate(HEADER_SIZE)
            .putShort(packetType)
            .putInt(dataLength)
            .putLong(timestamp)
            .array()
    }
}
