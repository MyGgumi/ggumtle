package com.ggumtle.loadtest.scenario.model

import kotlin.time.Duration
import kotlin.time.Duration.Companion.minutes
import kotlin.time.Duration.Companion.seconds

/**
 * Compiled scenario ready for execution
 */
data class Scenario(
    val name: String,
    val config: ScenarioConfig,
    val setupPhase: SetupPhase?,
    val gamePhase: GamePhase?,
    val teardownPhase: TeardownPhase?
)

/**
 * Scenario configuration
 */
data class ScenarioConfig(
    val playerCount: Int = 10,
    val roomIdRange: LongRange = -1L..-20L,
    val playersPerRoom: Int = 3,
    val duration: Duration = 1.minutes,
    val rampUpDuration: Duration = 10.seconds,
    val moveIntervalMs: Long = 16
)

/**
 * Setup phase definition (suspend lambda)
 */
class SetupPhase(
    val block: suspend SetupPhaseContext.() -> Unit
)

/**
 * Game phase definition (suspend lambda)
 */
class GamePhase(
    val block: suspend GamePhaseContext.() -> Unit
)

/**
 * Teardown phase definition (suspend lambda)
 */
class TeardownPhase(
    val block: suspend TeardownPhaseContext.() -> Unit
)

// Context interfaces for type safety
interface SetupPhaseContext
interface GamePhaseContext
interface TeardownPhaseContext
