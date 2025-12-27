package com.ggumtle.loadtest.protocol.body

import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets

/**
 * Sealed interface for all packet body types
 */
sealed interface PacketBody {
    fun toBytes(): ByteArray
}

/**
 * Empty body for packets that don't require data
 */
object EmptyBody : PacketBody {
    override fun toBytes(): ByteArray = ByteArray(0)
}

/**
 * Authentication token body (UTF-8 string)
 */
data class AuthTokenBody(val token: String) : PacketBody {
    override fun toBytes(): ByteArray = token.toByteArray(StandardCharsets.UTF_8)
}

/**
 * Room join command - Long roomId (8 bytes)
 */
data class RoomJoinBody(val roomId: Long) : PacketBody {
    override fun toBytes(): ByteArray =
        ByteBuffer.allocate(Long.SIZE_BYTES).putLong(roomId).array()
}

/**
 * Player movement - 3 ints (x, y, z) = 12 bytes
 */
data class PlayerMoveBody(
    val x: Int,
    val y: Int,
    val z: Int
) : PacketBody {
    override fun toBytes(): ByteArray =
        ByteBuffer.allocate(12)
            .putInt(x)
            .putInt(y)
            .putInt(z)
            .array()
}

/**
 * Hit mongging command - 3 ints + 1 long = 20 bytes
 */
data class HitMonggingBody(
    val vx: Int,
    val vy: Int,
    val vz: Int,
    val targetId: Long
) : PacketBody {
    override fun toBytes(): ByteArray =
        ByteBuffer.allocate(20)
            .putInt(vx)
            .putInt(vy)
            .putInt(vz)
            .putLong(targetId)
            .array()
}

/**
 * Single Int body - for boxId, ggumtleId, exitId, skillType, itemId etc.
 */
data class IntBody(val value: Int) : PacketBody {
    override fun toBytes(): ByteArray =
        ByteBuffer.allocate(Int.SIZE_BYTES).putInt(value).array()
}

/**
 * Single Long body - for targetId etc.
 */
data class LongBody(val value: Long) : PacketBody {
    override fun toBytes(): ByteArray =
        ByteBuffer.allocate(Long.SIZE_BYTES).putLong(value).array()
}

/**
 * Two Ints body - for (boxId, index) or (boxId, itemId)
 */
data class TwoIntsBody(val first: Int, val second: Int) : PacketBody {
    override fun toBytes(): ByteArray =
        ByteBuffer.allocate(8)
            .putInt(first)
            .putInt(second)
            .array()
}

/**
 * Attack with item - 4 ints (x, y, z, itemId) = 16 bytes
 */
data class AttackWithItemBody(
    val x: Int,
    val y: Int,
    val z: Int,
    val itemId: Int
) : PacketBody {
    override fun toBytes(): ByteArray =
        ByteBuffer.allocate(16)
            .putInt(x)
            .putInt(y)
            .putInt(z)
            .putInt(itemId)
            .array()
}
