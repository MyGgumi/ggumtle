package com.ggumtle.loadtest.protocol

import com.ggumtle.loadtest.protocol.body.PacketBody

/**
 * Complete packet with header and optional data
 */
data class Packet(
    val header: PacketHeader,
    val data: ByteArray?
) {
    companion object {
        fun create(type: SendPacketType, body: PacketBody): Packet {
            val data = body.toBytes()
            val header = PacketHeader(
                packetType = type.value,
                dataLength = data.size,
                timestamp = System.currentTimeMillis()
            )
            return Packet(header, if (data.isEmpty()) null else data)
        }

        fun createEmpty(type: SendPacketType): Packet {
            val header = PacketHeader(
                packetType = type.value,
                dataLength = 0,
                timestamp = System.currentTimeMillis()
            )
            return Packet(header, null)
        }
    }

    val sendPacketType: SendPacketType?
        get() = SendPacketType.fromValue(header.packetType)

    val receivePacketType: ReceivePacketType?
        get() = ReceivePacketType.fromValue(header.packetType)

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as Packet

        if (header != other.header) return false
        if (data != null) {
            if (other.data == null) return false
            if (!data.contentEquals(other.data)) return false
        } else if (other.data != null) return false

        return true
    }

    override fun hashCode(): Int {
        var result = header.hashCode()
        result = 31 * result + (data?.contentHashCode() ?: 0)
        return result
    }

    override fun toString(): String {
        return "Packet(type=${header.packetType}, dataLen=${header.dataLength}, timestamp=${header.timestamp})"
    }
}
