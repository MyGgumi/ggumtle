package com.ggumtle.loadtest.scenario.definitions.mongging

import com.ggumtle.loadtest.scenario.definitions.RoleScenario
import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder
import mu.KotlinLogging
import kotlin.time.Duration.Companion.seconds

private val logger = KotlinLogging.logger {}

/**
 * Mongging 3 - Escape scenario
 */
object Mongging3Scenario : RoleScenario {
    override suspend fun GamePhaseBuilder.execute() {
        logger.info { "Player ${player.id}: Starting Mongging3 scenario (Escape)" }

        // Wait for game to progress
        wait(5.seconds)

        // Move around to explore
//        continuousMove(duration = 5.seconds)

        // Try to escape (using exit ID 1 as default)
        logger.info { "Player ${player.id}: Attempting escape at exit 1" }
        escape(exitId = 1)

        // Continue moving after escape attempt
        continuousMove(duration = 5.seconds)

        logger.info { "Player ${player.id}: Completed Mongging3 scenario" }
    }
}
