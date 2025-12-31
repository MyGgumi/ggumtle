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

    // Map data from INITIALIZE_MAP
    var mapData: MapData = MapData()
        private set

    // Room creation result
    var createdRoomId: Long? = null
        private set

    // Box states (boxId -> BoxState)
    private val boxStates = mutableMapOf<Int, BoxState>()

    // Currently opened box ID (null if not viewing any box)
    var currentBoxId: Int? = null
        private set

    // Player's inventory - simplified as single item ID for now
    var heldItemId: Int = -1
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

    suspend fun createRoom(players: List<PlayerInfoBody>): Long? {
        logger.info { "Player $id: Creating room with ${players.size} players" }
        _state.value = PlayerState.CREATING_ROOM
        val start = System.nanoTime()

        client.send(SendPacketType.ROOM_CREATE, RoomCreateBody(players))

        try {
            withTimeout(10_000) {
                _state.first { it == PlayerState.ROOM_CREATED || it == PlayerState.ERROR }
            }
        } catch (e: TimeoutCancellationException) {
            logger.error { "Player $id: Room creation timeout" }
            _state.value = PlayerState.ERROR
            return null
        }

        metrics.recordLatency("roomCreate", System.nanoTime() - start)
        return createdRoomId
    }

    suspend fun joinRoom(roomId: Long) {
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
                logger.debug { "Player $id: Joined room" }
            }

            ReceivePacketType.ROOM_CREATE -> {
                val data = packet.data
                if (data != null && data.size >= 12) {
                    val buffer = ByteBuffer.wrap(data)
                    val success = buffer.int == 1
                    val newRoomId = buffer.long

                    if (success) {
                        createdRoomId = newRoomId
                        _state.value = PlayerState.ROOM_CREATED
                        logger.info { "Player $id: Created room $newRoomId" }
                    } else {
                        _state.value = PlayerState.ERROR
                        logger.error { "Player $id: Room creation failed" }
                    }
                } else {
                    _state.value = PlayerState.ERROR
                    logger.error { "Player $id: Invalid room creation response" }
                }
            }

            ReceivePacketType.INITIALIZE_MAP -> {
                parseMapData(packet.data)
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

            ReceivePacketType.SHOW_BOX -> {
                parseShowBox(packet.data)
            }

            ReceivePacketType.CLOSE_BOX -> {
                parseCloseBox(packet.data)
            }

            ReceivePacketType.TAKE_ITEM -> {
                parseTakeItem(packet.data)
            }

            ReceivePacketType.PUT_ITEM -> {
                parsePutItem(packet.data)
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

    private fun parseMapData(data: ByteArray?) {
        if (data == null || data.size < 16) return  // At least 4 counts (4 bytes each)

        try {
            val buffer = ByteBuffer.wrap(data)

            // Parse boxes
            val boxCount = buffer.int
            val boxes = mutableListOf<EntityPosition>()
            repeat(boxCount) {
                if (buffer.remaining() >= 16) {
                    boxes.add(EntityPosition(buffer.int, buffer.int, buffer.int, buffer.int))
                }
            }

            // Parse ggumtles
            val ggumtleCount = buffer.int
            val ggumtles = mutableListOf<EntityPosition>()
            repeat(ggumtleCount) {
                if (buffer.remaining() >= 16) {
                    ggumtles.add(EntityPosition(buffer.int, buffer.int, buffer.int, buffer.int))
                }
            }

            // Parse healPacks
            val healPackCount = buffer.int
            val healPacks = mutableListOf<EntityPosition>()
            repeat(healPackCount) {
                if (buffer.remaining() >= 16) {
                    healPacks.add(EntityPosition(buffer.int, buffer.int, buffer.int, buffer.int))
                }
            }

            // Parse speedPacks
            val speedPackCount = buffer.int
            val speedPacks = mutableListOf<EntityPosition>()
            repeat(speedPackCount) {
                if (buffer.remaining() >= 16) {
                    speedPacks.add(EntityPosition(buffer.int, buffer.int, buffer.int, buffer.int))
                }
            }

            mapData = MapData(boxes, ggumtles, healPacks, speedPacks)
            logger.debug { "Player $id: Map initialized - ${boxes.size} boxes, ${ggumtles.size} ggumtles, ${healPacks.size} healPacks, ${speedPacks.size} speedPacks" }
        } catch (e: Exception) {
            logger.warn(e) { "Player $id: Failed to parse map data" }
        }
    }

    // ===== Box Response Parsing =====

    /**
     * Parse SHOW_BOX (51) response
     * 바이트 구조: success(1) + boxId(4) + itemCount(4) + items(36) = 45 bytes
     */
    private fun parseShowBox(data: ByteArray?) {
        if (data == null || data.size < 45) {
            logger.warn { "Player $id: Invalid SHOW_BOX response size: ${data?.size}" }
            return
        }

        try {
            val buffer = ByteBuffer.wrap(data)
            val success = buffer.get() == 1.toByte()

            if (!success) {
                logger.debug { "Player $id: SHOW_BOX failed" }
                return
            }

            val boxId = buffer.int
            val itemCount = buffer.int  // Should be 9 (BOX_SIZE)

            val items = IntArray(BoxState.BOX_SIZE) { i ->
                if (i < itemCount && buffer.remaining() >= 4) buffer.int else -1
            }

            boxStates[boxId] = BoxState(boxId, items)
            currentBoxId = boxId

            logger.debug { "Player $id: Opened box $boxId with ${items.count { it != -1 }} items" }
            metrics.incrementCounter("boxOpensSuccess")
        } catch (e: Exception) {
            logger.warn(e) { "Player $id: Failed to parse SHOW_BOX response" }
        }
    }

    /**
     * Parse CLOSE_BOX (53) response
     * 바이트 구조: result(4) = 4 bytes
     */
    private fun parseCloseBox(data: ByteArray?) {
        if (data == null || data.size < 4) {
            logger.warn { "Player $id: Invalid CLOSE_BOX response size: ${data?.size}" }
            return
        }

        try {
            val buffer = ByteBuffer.wrap(data)
            val resultCode = buffer.int
            val result = CloseBoxResult.fromValue(resultCode)

            val closedBox = currentBoxId
            if (result == CloseBoxResult.SUCCESS) {
                currentBoxId = null
                logger.debug { "Player $id: Closed box $closedBox" }
            } else {
                logger.debug { "Player $id: CLOSE_BOX failed with result $result" }
            }
        } catch (e: Exception) {
            logger.warn(e) { "Player $id: Failed to parse CLOSE_BOX response" }
        }
    }

    /**
     * Parse TAKE_ITEM (55) response
     * 바이트 구조: result(4) + playerId(8) + boxId(4) + boxSize(4) + items(36) + takenItem(4) = 60 bytes
     *
     * 브로드캐스트 처리:
     * - playerId == serverId: 본인이 아이템 가져감 → heldItemId 업데이트
     * - playerId != serverId: 다른 플레이어가 가져감 → 상자 상태만 업데이트
     */
    private fun parseTakeItem(data: ByteArray?) {
        if (data == null || data.size < 60) {
            logger.warn { "Player $id: Invalid TAKE_ITEM response size: ${data?.size}" }
            return
        }

        try {
            val buffer = ByteBuffer.wrap(data)
            val resultCode = buffer.int
            val result = TakeItemResult.fromValue(resultCode)
            val playerId = buffer.long
            val boxId = buffer.int
            val boxSize = buffer.int  // Should be 9

            val items = IntArray(BoxState.BOX_SIZE) { i ->
                if (i < boxSize && buffer.remaining() >= 4) buffer.int else -1
            }

            val takenItemId = if (buffer.remaining() >= 4) buffer.int else -1

            if (result == TakeItemResult.SUCCESS) {
                // 상자 상태 업데이트 (본인/타인 무관)
                boxStates[boxId] = BoxState(boxId, items)

                if (playerId == serverId) {
                    // 본인이 아이템 가져감
                    heldItemId = takenItemId
                    logger.debug { "Player $id: Took item $takenItemId from box $boxId" }
                    metrics.incrementCounter("itemTakesSuccess")
                } else {
                    // 다른 플레이어가 아이템 가져감 (브로드캐스트)
                    logger.trace { "Player $id: Player $playerId took item from box $boxId" }
                }
            } else {
                logger.debug { "Player $id: TAKE_ITEM failed with result $result" }
                metrics.incrementCounter("itemTakesFailed")
            }
        } catch (e: Exception) {
            logger.warn(e) { "Player $id: Failed to parse TAKE_ITEM response" }
        }
    }

    /**
     * Parse PUT_ITEM (57) response
     * 바이트 구조: result(4) + boxSize(4) + items(36) = 44 bytes
     */
    private fun parsePutItem(data: ByteArray?) {
        if (data == null || data.size < 44) {
            logger.warn { "Player $id: Invalid PUT_ITEM response size: ${data?.size}" }
            return
        }

        try {
            val buffer = ByteBuffer.wrap(data)
            val resultCode = buffer.int
            val result = PutItemResult.fromValue(resultCode)
            val boxSize = buffer.int

            if (result == PutItemResult.SUCCESS && boxSize > 0) {
                val items = IntArray(BoxState.BOX_SIZE) { i ->
                    if (i < boxSize && buffer.remaining() >= 4) buffer.int else -1
                }

                // 현재 열린 상자 상태 업데이트
                currentBoxId?.let { boxId ->
                    boxStates[boxId] = BoxState(boxId, items)
                }

                val putItemId = heldItemId
                heldItemId = -1  // 아이템 소모

                logger.debug { "Player $id: Put item $putItemId to box" }
                metrics.incrementCounter("itemPutsSuccess")
            } else {
                logger.debug { "Player $id: PUT_ITEM failed with result $result" }
                metrics.incrementCounter("itemPutsFailed")
            }
        } catch (e: Exception) {
            logger.warn(e) { "Player $id: Failed to parse PUT_ITEM response" }
        }
    }

    // ===== Box State Queries =====

    /** Get box state by ID */
    fun getBoxState(boxId: Int): BoxState? = boxStates[boxId]

    /** Get current opened box state */
    fun getCurrentBoxState(): BoxState? = currentBoxId?.let { boxStates[it] }

    /** Check if player has an item */
    fun hasItem(): Boolean = heldItemId != -1

    /** Check if viewing a box */
    fun isViewingBox(): Boolean = currentBoxId != null
}
