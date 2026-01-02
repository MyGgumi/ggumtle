package com.ggumtle.loadtest.scenario.dsl

import com.ggumtle.loadtest.player.DigUpResult
import com.ggumtle.loadtest.player.VirtualPlayer
import com.ggumtle.loadtest.protocol.body.PlayerInfoBody
import com.ggumtle.loadtest.scenario.definitions.RoleScenario
import com.ggumtle.loadtest.scenario.model.*
import kotlinx.coroutines.delay
import mu.KotlinLogging
import kotlin.time.Duration
import kotlin.time.Duration.Companion.milliseconds
import kotlin.time.Duration.Companion.seconds

private val logger = KotlinLogging.logger {}

/**
 * DSL marker for scenario building
 */
@DslMarker
annotation class ScenarioDsl

/**
 * Entry point for creating a scenario
 */
fun scenario(name: String, block: ScenarioBuilder.() -> Unit): Scenario {
    return ScenarioBuilder(name).apply(block).build()
}

/**
 * Main scenario builder
 */
@ScenarioDsl
class ScenarioBuilder(private val name: String) {
    private var config = ScenarioConfig()
    private var groupSetupBlock: (suspend GroupSetupPhaseBuilder.() -> Unit)? = null
    private var gameBlock: (suspend GamePhaseBuilder.() -> Unit)? = null
    private var teardownBlock: (suspend TeardownPhaseBuilder.() -> Unit)? = null

    // Role-based scenarios
    private val monggingScenarios: MutableMap<Int, RoleScenario> = mutableMapOf()
    private var mongdungScenario: RoleScenario? = null

    fun config(block: ScenarioConfigBuilder.() -> Unit) {
        config = ScenarioConfigBuilder().apply(block).build()
    }

    fun groupSetup(block: suspend GroupSetupPhaseBuilder.() -> Unit) {
        groupSetupBlock = block
    }

    fun game(block: suspend GamePhaseBuilder.() -> Unit) {
        gameBlock = block
    }

    /**
     * Register a mongging scenario for a specific index (0-3)
     */
    fun mongging(index: Int, scenario: RoleScenario) {
        require(index in 0..3) { "Mongging index must be 0-3, got $index" }
        monggingScenarios[index] = scenario
    }

    /**
     * Register the mongdung scenario
     */
    fun mongdung(scenario: RoleScenario) {
        this.mongdungScenario = scenario
    }

    fun teardown(block: suspend TeardownPhaseBuilder.() -> Unit) {
        teardownBlock = block
    }

    fun build(): Scenario = Scenario(
        name = name,
        config = config,
        groupSetupPhase = groupSetupBlock?.let { block -> GroupSetupPhase { (this as GroupSetupPhaseBuilder).block() } },
        monggingScenarios = monggingScenarios.toMap(),
        mongdungScenario = mongdungScenario,
        gamePhase = gameBlock?.let { block -> GamePhase { (this as GamePhaseBuilder).block() } },
        teardownPhase = teardownBlock?.let { block -> TeardownPhase { (this as TeardownPhaseBuilder).block() } }
    )
}

/**
 * Configuration builder
 */
@ScenarioDsl
class ScenarioConfigBuilder {
    var playerCount: Int = 10
    var playersPerRoom: Int = 3
    var duration: Duration = 60.seconds
    var rampUpDuration: Duration = 10.seconds
    var moveIntervalMs: Long = 16

    fun build() = ScenarioConfig(
        playerCount = playerCount,
        playersPerRoom = playersPerRoom,
        duration = duration,
        rampUpDuration = rampUpDuration,
        moveIntervalMs = moveIntervalMs
    )
}

/**
 * Group-based setup phase builder for coordinated room creation
 */
@ScenarioDsl
class GroupSetupPhaseBuilder(
    val group: PlayerGroup,
    val player: VirtualPlayer,
    private val token: String
) : GroupSetupPhaseContext {

    val isLeader: Boolean = group.isLeader(player)

    suspend fun authenticate() {
        player.authenticate(token)
    }

    /**
     * Leader creates room, members wait and then all join
     * Synchronizes all players before proceeding to ensure Dream is created
     */
    suspend fun coordinatedRoomSetup() {
        if (isLeader) {
            // Leader creates the room
            val playerInfos = group.toPlayerInfoBodies()
            val roomId = player.createRoom(playerInfos)

            if (roomId != null) {
                group.roomId = roomId
            } else {
                throw RuntimeException("Room creation failed for group ${group.groupId}")
            }
        } else {
            // Members wait for room to be created
            var attempts = 0
            while (group.roomId == null && attempts < 100) {
                delay(100)
                attempts++
            }

            if (group.roomId == null) {
                throw RuntimeException("Timeout waiting for room creation in group ${group.groupId}")
            }
        }

        // All players join the room
        val roomId = group.roomId ?: throw RuntimeException("Room ID not set")
        player.joinRoom(roomId)

        // Synchronization: Wait for all players to join before proceeding
        // This ensures Dream is created (triggered when last player joins)
        val joined = group.incrementJoinedCount()
        logger.debug { "Player ${player.id} joined room $roomId (${joined}/${group.size})" }

        // Wait for all players to join
        var waitAttempts = 0
        while (group.joinedCount < group.size && waitAttempts < 100) {
            delay(50)
            waitAttempts++
        }

        if (group.joinedCount < group.size) {
            logger.warn { "Player ${player.id}: Not all players joined (${group.joinedCount}/${group.size})" }
        } else {
            logger.debug { "Player ${player.id}: All players joined, proceeding" }
            // Wait for server to complete Dream creation
            // CreateDreamEvent → DreamService.createDream() is async
            delay(1000)
        }
    }

    suspend fun sendSceneChange() {
        player.sendSceneChange()
    }

    suspend fun waitForGameStart(): Boolean {
        return player.waitForGameStart()
    }

    suspend fun wait(duration: Duration) {
        delay(duration)
    }
}

/**
 * Game phase builder with DSL methods
 */
@ScenarioDsl
class GamePhaseBuilder(val player: VirtualPlayer) : GamePhaseContext {

    // Movement
    suspend fun move(x: Int, y: Int, z: Int, vx: Int = 0, vy: Int = 0, vz: Int = 0) {
        player.sendMove(x, y, z, vx, vy, vz)
    }

    suspend fun randomMove(count: Int = 1, intervalMs: Long = 16) {
        val (baseX, baseY, baseZ) = player.spawnPosition
        repeat(count) { i ->
            val x = baseX + (i % 100)
            val y = baseY
            val z = baseZ + (i / 100)
            // Calculate velocity based on movement direction
            val vx = if (i > 0) 1 else 0
            val vz = if (i > 0 && i % 100 == 0) 1 else 0
            player.sendMove(x, y, z, vx, 0, vz)
            delay(intervalMs)
        }
    }

    suspend fun continuousMove(duration: Duration, intervalMs: Long = 16) {
        val endTime = System.currentTimeMillis() + duration.inWholeMilliseconds
        var step = 0
        val (baseX, baseY, baseZ) = player.spawnPosition

        while (System.currentTimeMillis() < endTime) {
            val x = baseX + (step % 100)
            val y = baseY
            val z = baseZ + (step / 100) % 100
            // Calculate velocity based on movement pattern
            val vx = 1  // Moving in x direction
            val vz = if (step > 0 && step % 100 == 0) 1 else 0
            player.sendMove(x, y, z, vx, 0, vz)
            step++
            delay(intervalMs)
        }
    }

    // Position-based movement (using map data)
    suspend fun moveToPosition(x: Int, y: Int, z: Int) {
        player.sendMove(x, y, z, 0, 0, 0)
    }

    suspend fun moveToGgumtle(ggumtleId: Int) {
        val pos = player.mapData.findGgumtle(ggumtleId)
            ?: throw IllegalArgumentException("Ggumtle $ggumtleId not found in map data")
        player.sendMove(pos.x, pos.y, pos.z, 0, 0, 0)
    }

    suspend fun moveToBox(boxId: Int) {
        val pos = player.mapData.findBox(boxId)
            ?: throw IllegalArgumentException("Box $boxId not found in map data")
        player.sendMove(pos.x, pos.y, pos.z, 0, 0, 0)
    }

    suspend fun moveToHealPack(itemId: Int) {
        val pos = player.mapData.findHealPack(itemId)
            ?: throw IllegalArgumentException("HealPack $itemId not found in map data")
        player.sendMove(pos.x, pos.y, pos.z, 0, 0, 0)
    }

    suspend fun moveToSpeedPack(itemId: Int) {
        val pos = player.mapData.findSpeedPack(itemId)
            ?: throw IllegalArgumentException("SpeedPack $itemId not found in map data")
        player.sendMove(pos.x, pos.y, pos.z, 0, 0, 0)
    }

    // Ggumtle interactions
    suspend fun digUpGgumtle(ggumtleId: Int) {
        player.sendDigUp(ggumtleId)
    }

    /**
     * Send DIG_UP_GGUMTLE and wait for DIG_UP_RECEIVE response
     * @param ggumtleId ID of the ggumtle to dig
     * @param timeoutMs timeout in milliseconds to wait for response
     * @return DigUpResult from server
     */
    suspend fun digUpGgumtleAndWait(ggumtleId: Int, timeoutMs: Long = 5000): DigUpResult {
        player.sendDigUp(ggumtleId)
        return player.waitForDigResponse(timeoutMs)
    }

    suspend fun stopDigging() {
        player.sendStopDigging()
    }

    suspend fun feedGgumtle(ggumtleId: Int, duration: Duration = 3.seconds) {
        player.sendStartFeed(ggumtleId)
        delay(duration)
        player.sendStopFeed()
    }

    suspend fun startFeed(ggumtleId: Int) {
        player.sendStartFeed(ggumtleId)
    }

    suspend fun stopFeed() {
        player.sendStopFeed()
    }

    // Box interactions
    suspend fun openBox(boxId: Int) {
        player.sendShowBox(boxId)
    }

    suspend fun closeBox(boxId: Int) {
        player.sendCloseBox(boxId)
    }

    suspend fun takeItem(boxId: Int, itemIndex: Int) {
        player.sendTakeItem(boxId, itemIndex)
    }

    suspend fun putItem(boxId: Int, itemId: Int) {
        player.sendPutItem(boxId, itemId)
    }

    // Combat
    suspend fun hitMongging(targetId: Long, vx: Int = 0, vy: Int = 0, vz: Int = 0) {
        player.sendHitMongging(targetId, vx, vy, vz)
    }

    suspend fun startRevive(targetId: Long) {
        player.sendStartRevive(targetId)
    }

    suspend fun stopRevive() {
        player.sendStopRevive()
    }

    suspend fun mongdungSkill(skillType: Int) {
        player.sendMongdungSkill(skillType)
    }

    suspend fun attackWithItem(x: Int, y: Int, z: Int, itemId: Int) {
        player.sendAttackWithItem(x, y, z, itemId)
    }

    suspend fun useFieldItem(itemId: Int) {
        player.sendUseFieldItem(itemId)
    }

    suspend fun useDefibrillator() {
        player.sendUseDefibrillator()
    }

    // Escape
    suspend fun escape(exitId: Int) {
        player.sendEscape(exitId)
    }

    // Utility
    suspend fun wait(duration: Duration) {
        delay(duration)
    }

    suspend fun repeat(times: Int, block: suspend GamePhaseBuilder.() -> Unit) {
        kotlin.repeat(times) { block() }
    }

    // Conditional execution based on player type
    suspend fun ifMongging(block: suspend GamePhaseBuilder.() -> Unit) {
        if (player.isMongging) block()
    }

    suspend fun ifMongdung(block: suspend GamePhaseBuilder.() -> Unit) {
        if (!player.isMongging) block()
    }
}

/**
 * Teardown phase builder
 */
@ScenarioDsl
class TeardownPhaseBuilder(val player: VirtualPlayer) : TeardownPhaseContext {

    suspend fun disconnect() {
        player.disconnect()
    }

    suspend fun wait(duration: Duration) {
        delay(duration)
    }
}

