package com.ggumtle.loadtest.player

import com.ggumtle.loadtest.metrics.MetricsCollector
import com.ggumtle.loadtest.network.GameClient
import com.ggumtle.loadtest.protocol.Packet
import com.ggumtle.loadtest.protocol.ReceivePacketType
import com.ggumtle.loadtest.protocol.SendPacketType
import com.ggumtle.loadtest.protocol.body.*
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.*
import kotlin.coroutines.cancellation.CancellationException
import mu.KotlinLogging
import java.nio.ByteBuffer

private val logger = KotlinLogging.logger {}

/**
 * Simulates a virtual player connecting to the game server
 */
class VirtualPlayer(
    val id: Int,
    private val client: GameClient,
    val roomId: Long,
    private val metrics: MetricsCollector
) {
    private val _state = MutableStateFlow(PlayerState.CONNECTED)
    val state: StateFlow<PlayerState> = _state.asStateFlow()

    // Player info from server
    var serverId: Long = 0L
        private set
    var isMongging: Boolean = true
        private set
    var spawnPosition: Triple<Int, Int, Int> = Triple(0, 0, 0)
        private set

    private val scope = CoroutineScope(Dispatchers.IO + SupervisorJob())

    init {
        scope.launch {
            client.packetFlow.collect { packet ->
                handlePacket(packet)
            }
        }
    }

    // ===== Authentication & Room Join =====

    suspend fun authenticate(token: String) {
        logger.info { "Player $id: Starting authentication" }
        _state.value = PlayerState.AUTHENTICATING
        val start = System.nanoTime()

        client.send(SendPacketType.VERIFY_TOKEN, AuthTokenBody(token))
        logger.debug { "Player $id: VERIFY_TOKEN packet sent" }

        // Wait for authentication response with timeout
        try {
            withTimeout(5000) {
                _state.first { it == PlayerState.AUTHENTICATED || it == PlayerState.ERROR }
            }
        } catch (e: TimeoutCancellationException) {
            logger.error { "Player $id: Authentication timeout - no response received" }
            _state.value = PlayerState.ERROR
        }
        metrics.recordLatency("auth", System.nanoTime() - start)
    }

    suspend fun joinRoom() {
        _state.value = PlayerState.JOINING_ROOM
        val start = System.nanoTime()

        client.send(SendPacketType.ROOM_JOIN, RoomJoinBody(roomId))

        // Wait for room join response
        _state.first { it == PlayerState.IN_ROOM || it == PlayerState.ERROR }
        metrics.recordLatency("joinRoom", System.nanoTime() - start)
    }

    suspend fun sendSceneChange() {
        _state.value = PlayerState.LOADING_SCENE
        client.sendEmpty(SendPacketType.SCENE_CHANGE)
        _state.value = PlayerState.WAITING_FOR_START
    }

    suspend fun waitForGameStart(): Boolean {
        return try {
            withTimeout(30_000) {
                _state.first { it == PlayerState.IN_GAME || it == PlayerState.ERROR }
            }
            _state.value == PlayerState.IN_GAME
        } catch (e: TimeoutCancellationException) {
            logger.warn { "Player $id: Timeout waiting for game start" }
            false
        }
    }

    // ===== Movement =====

    suspend fun sendMove(x: Int, y: Int, z: Int, vx: Int = 0, vy: Int = 0, vz: Int = 0) {
        client.send(SendPacketType.PLAYER_MOVE, PlayerMoveBody(x, y, z, vx, vy, vz))
        metrics.incrementCounter("moves")
    }

    // ===== Ggumtle Interactions =====

    suspend fun sendDigUp(ggumtleId: Int) {
        client.send(SendPacketType.DIG_UP_GGUMTLE, IntBody(ggumtleId))
        metrics.incrementCounter("digUps")
    }

    suspend fun sendStopDigging() {
        client.sendEmpty(SendPacketType.STOP_DIGGING)
    }

    suspend fun sendStartFeed(ggumtleId: Int) {
        client.send(SendPacketType.START_FEED, IntBody(ggumtleId))
        metrics.incrementCounter("feeds")
    }

    suspend fun sendStopFeed() {
        client.sendEmpty(SendPacketType.STOP_FEED)
    }

    // ===== Box Interactions =====

    suspend fun sendShowBox(boxId: Int) {
        client.send(SendPacketType.SHOW_BOX, IntBody(boxId))
        metrics.incrementCounter("boxOpens")
    }

    suspend fun sendCloseBox(boxId: Int) {
        client.send(SendPacketType.CLOSE_BOX, IntBody(boxId))
    }

    suspend fun sendTakeItem(boxId: Int, itemIndex: Int) {
        client.send(SendPacketType.TAKE_ITEM_FROM_BOX, TwoIntsBody(boxId, itemIndex))
        metrics.incrementCounter("itemTakes")
    }

    suspend fun sendPutItem(boxId: Int, itemId: Int) {
        client.send(SendPacketType.PUT_ITEM_TO_BOX, TwoIntsBody(boxId, itemId))
    }

    // ===== Combat Interactions =====

    suspend fun sendHitMongging(targetId: Long, vx: Int = 0, vy: Int = 0, vz: Int = 0) {
        client.send(SendPacketType.HIT_MONGGING, HitMonggingBody(vx, vy, vz, targetId))
        metrics.incrementCounter("hits")
    }

    suspend fun sendStartRevive(targetId: Long) {
        client.send(SendPacketType.START_REVIVE, LongBody(targetId))
    }

    suspend fun sendStopRevive() {
        client.sendEmpty(SendPacketType.STOP_REVIVE)
    }

    suspend fun sendMongdungSkill(skillType: Int) {
        client.send(SendPacketType.MONGDUNG_SKILL, IntBody(skillType))
        metrics.incrementCounter("skills")
    }

    suspend fun sendAttackWithItem(x: Int, y: Int, z: Int, itemId: Int) {
        client.send(SendPacketType.ATTACK_WITH_ITEM, AttackWithItemBody(x, y, z, itemId))
    }

    suspend fun sendUseFieldItem(itemId: Int) {
        client.send(SendPacketType.USE_FIELD_ITEM, IntBody(itemId))
    }

    suspend fun sendUseDefibrillator() {
        client.sendEmpty(SendPacketType.USE_DEFIBRILLATOR)
    }

    // ===== Escape =====

    suspend fun sendEscape(exitId: Int) {
        client.send(SendPacketType.ESCAPE, IntBody(exitId))
        metrics.incrementCounter("escapes")
    }

    // ===== Lifecycle =====

    suspend fun disconnect() {
        scope.cancel()
        client.close()
        _state.value = PlayerState.DISCONNECTED
    }

    // ===== Packet Handling =====

    private fun handlePacket(packet: Packet) {
        val type = packet.receivePacketType ?: return

        when (type) {
            ReceivePacketType.VERIFY_TOKEN -> {
                // 응답 바디 파싱 (9 bytes: 1 byte success + 8 bytes sessionId)
                val data = packet.data
                if (data != null && data.isNotEmpty()) {
                    val success = data[0].toInt() == 1
                    if (success) {
                        val sessionId = if (data.size >= 9) {
                            ByteBuffer.wrap(data, 1, 8).long
                        } else -1L
                        _state.value = PlayerState.AUTHENTICATED
                        logger.info { "Player $id: Authenticated successfully (sessionId=$sessionId)" }
                    } else {
                        _state.value = PlayerState.ERROR
                        logger.error { "Player $id: Authentication failed - server returned failure" }
                    }
                } else {
                    _state.value = PlayerState.AUTHENTICATED
                    logger.debug { "Player $id: Authenticated (no response body)" }
                }
            }

            ReceivePacketType.ROOM_JOIN -> {
                _state.value = PlayerState.IN_ROOM
                logger.debug { "Player $id: Joined room $roomId" }
            }

            ReceivePacketType.INITIALIZE_PLAYER -> {
                parsePlayerInfo(packet.data)
            }

            ReceivePacketType.GAME_START -> {
                _state.value = PlayerState.IN_GAME
                logger.debug { "Player $id: Game started" }
            }

            ReceivePacketType.END -> {
                _state.value = PlayerState.GAME_ENDED
                logger.debug { "Player $id: Game ended" }
            }

            else -> {
                logger.trace { "Player $id: Received ${type.name}" }
            }
        }
    }

    private fun parsePlayerInfo(data: ByteArray?) {
        if (data == null || data.size < 4) return

        try {
            val buffer = ByteBuffer.wrap(data)
            val count = buffer.int

            repeat(count) {
                if (buffer.remaining() < 28) return@repeat  // Minimum player data size

                val playerId = buffer.long
                val isMine = buffer.get() == 1.toByte()
                val playerIsMongging = buffer.get() == 1.toByte()
                buffer.long // classId
                val x = buffer.int
                val y = buffer.int
                val z = buffer.int
                buffer.int // maxHp
                buffer.int // moveSpeed
                buffer.int // healSpeed
                buffer.int // workSpeed
                val nicknameLen = buffer.int
                if (buffer.remaining() >= nicknameLen) {
                    buffer.position(buffer.position() + nicknameLen)
                }

                if (isMine) {
                    serverId = playerId
                    isMongging = playerIsMongging
                    spawnPosition = Triple(x, y, z)
                    logger.debug { "Player $id: Initialized as ${if (isMongging) "Mongging" else "Mongdung"} at ($x, $y, $z)" }
                }
            }
        } catch (e: Exception) {
            logger.warn(e) { "Player $id: Failed to parse player info" }
        }
    }
}
