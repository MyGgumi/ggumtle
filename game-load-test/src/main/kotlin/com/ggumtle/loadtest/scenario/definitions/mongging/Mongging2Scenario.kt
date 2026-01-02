package com.ggumtle.loadtest.scenario.definitions.mongging

import com.ggumtle.loadtest.scenario.definitions.RoleScenario
import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder
import mu.KotlinLogging
import kotlin.time.Duration.Companion.seconds

private val logger = KotlinLogging.logger {}

/**
 * Mongging 2 - Movement test scenario
 */
object Mongging2Scenario : RoleScenario {
    override suspend fun GamePhaseBuilder.execute() {
        logger.info { "Player ${player.id}: Starting Mongging2 scenario (Movement test)" }

        // Continuous random movement
//        continuousMove(duration = 15.seconds)

        logger.info { "Player ${player.id}: Completed Mongging2 scenario" }
    }
}
