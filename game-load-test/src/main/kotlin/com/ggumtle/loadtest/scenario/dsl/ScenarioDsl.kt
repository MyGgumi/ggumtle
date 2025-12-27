package com.ggumtle.loadtest.scenario.dsl

import com.ggumtle.loadtest.player.VirtualPlayer
import com.ggumtle.loadtest.scenario.model.*
import kotlinx.coroutines.delay
import kotlin.time.Duration
import kotlin.time.Duration.Companion.milliseconds
import kotlin.time.Duration.Companion.seconds

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
    private var setupBlock: (suspend SetupPhaseBuilder.() -> Unit)? = null
    private var gameBlock: (suspend GamePhaseBuilder.() -> Unit)? = null
    private var teardownBlock: (suspend TeardownPhaseBuilder.() -> Unit)? = null

    fun config(block: ScenarioConfigBuilder.() -> Unit) {
        config = ScenarioConfigBuilder().apply(block).build()
    }

    fun setup(block: suspend SetupPhaseBuilder.() -> Unit) {
        setupBlock = block
    }

    fun game(block: suspend GamePhaseBuilder.() -> Unit) {
        gameBlock = block
    }

    fun teardown(block: suspend TeardownPhaseBuilder.() -> Unit) {
        teardownBlock = block
    }

    fun build(): Scenario = Scenario(
        name = name,
        config = config,
        setupPhase = setupBlock?.let { SetupPhase { SetupPhaseBuilder(player, token).it() } },
        gamePhase = gameBlock?.let { GamePhase { GamePhaseBuilder(player).it() } },
        teardownPhase = teardownBlock?.let { TeardownPhase { TeardownPhaseBuilder(player).it() } }
    )
}

/**
 * Configuration builder
 */
@ScenarioDsl
class ScenarioConfigBuilder {
    var playerCount: Int = 10
    var roomIdStart: Long = -1L
    var roomIdEnd: Long = -20L
    var playersPerRoom: Int = 3
    var duration: Duration = 60.seconds
    var rampUpDuration: Duration = 10.seconds
    var moveIntervalMs: Long = 16

    fun build() = ScenarioConfig(
        playerCount = playerCount,
        roomIdRange = roomIdStart..roomIdEnd,
        playersPerRoom = playersPerRoom,
        duration = duration,
        rampUpDuration = rampUpDuration,
        moveIntervalMs = moveIntervalMs
    )
}

/**
 * Setup phase builder with DSL methods
 */
@ScenarioDsl
class SetupPhaseBuilder(
    val player: VirtualPlayer,
    private val token: String
) : SetupPhaseContext {

    suspend fun authenticate() {
        player.authenticate(token)
    }

    suspend fun joinRoom() {
        player.joinRoom()
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
    suspend fun move(x: Int, y: Int, z: Int) {
        player.sendMove(x, y, z)
    }

    suspend fun randomMove(count: Int = 1, intervalMs: Long = 16) {
        val (baseX, baseY, baseZ) = player.spawnPosition
        repeat(count) { i ->
            val x = baseX + (i % 100)
            val y = baseY
            val z = baseZ + (i / 100)
            player.sendMove(x, y, z)
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
            player.sendMove(x, y, z)
            step++
            delay(intervalMs)
        }
    }

    // Ggumtle interactions
    suspend fun digUpGgumtle(ggumtleId: Int) {
        player.sendDigUp(ggumtleId)
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

// Extension property for context access
private val SetupPhaseContext.player: VirtualPlayer
    get() = (this as SetupPhaseBuilder).player

private val SetupPhaseContext.token: String
    get() = throw IllegalStateException("Token should be accessed through SetupPhaseBuilder")

private val GamePhaseContext.player: VirtualPlayer
    get() = (this as GamePhaseBuilder).player

private val TeardownPhaseContext.player: VirtualPlayer
    get() = (this as TeardownPhaseBuilder).player
