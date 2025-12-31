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
    val groupSetupPhase: GroupSetupPhase?,
    val gamePhase: GamePhase?,
    val teardownPhase: TeardownPhase?
)

/**
 * Scenario configuration
 */
data class ScenarioConfig(
    val playerCount: Int = 10,
    val playersPerRoom: Int = 3,
    val duration: Duration = 1.minutes,
    val rampUpDuration: Duration = 10.seconds,
    val moveIntervalMs: Long = 16
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

/**
 * Group setup phase definition (suspend lambda with group context)
 */
class GroupSetupPhase(
    val block: suspend GroupSetupPhaseContext.() -> Unit
)

// Context interfaces for type safety
interface GroupSetupPhaseContext
interface GamePhaseContext
interface TeardownPhaseContext
