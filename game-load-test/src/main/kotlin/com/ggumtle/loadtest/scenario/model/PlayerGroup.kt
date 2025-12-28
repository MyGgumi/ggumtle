package com.ggumtle.loadtest.scenario.model

import com.ggumtle.loadtest.player.VirtualPlayer
import com.ggumtle.loadtest.protocol.body.PlayerInfoBody
import mu.KotlinLogging
import java.util.concurrent.atomic.AtomicInteger
import java.util.concurrent.atomic.AtomicReference

private val logger = KotlinLogging.logger {}

/**
 * Represents a group of players that will play together in one room
 */
data class PlayerGroup(
    val groupId: Int,
    val leaderIndex: Int = 0,
    val players: MutableList<VirtualPlayer> = mutableListOf()
) {
    private val _createdRoomId = AtomicReference<Long?>(null)
    private val _joinedCount = AtomicInteger(0)

    val size: Int get() = players.size
    val leader: VirtualPlayer? get() = players.getOrNull(leaderIndex)
    val members: List<VirtualPlayer> get() = players.filterIndexed { i, _ -> i != leaderIndex }

    var roomId: Long?
        get() = _createdRoomId.get()
        set(value) { _createdRoomId.set(value) }

    val joinedCount: Int get() = _joinedCount.get()

    /**
     * Increment joined count and return the new value
     */
    fun incrementJoinedCount(): Int = _joinedCount.incrementAndGet()

    /**
     * Reset joined count to zero
     */
    fun resetJoinedCount() { _joinedCount.set(0) }

    /**
     * Check if a player is the leader
     */
    fun isLeader(player: VirtualPlayer): Boolean = leader?.id == player.id

    /**
     * Generate player info bodies for room creation
     */
    fun toPlayerInfoBodies(): List<PlayerInfoBody> {
        return players.map { player ->
            PlayerInfoBody(
                playerId = player.id.toLong(),
                monggingClassId = 1L,  // Default class for load testing
                additionalHp = 0,
                additionalTaskSpeed = 0,
                additionalHealSpeed = 0
            )
        }
    }

    override fun toString(): String =
        "PlayerGroup(id=$groupId, size=$size, leader=${leader?.id}, roomId=$roomId)"
}
