package com.ggumtle.loadtest.scenario.definitions.mongging

import com.ggumtle.loadtest.scenario.definitions.RoleScenario
import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder
import mu.KotlinLogging
import kotlin.time.Duration.Companion.milliseconds
import kotlin.time.Duration.Companion.seconds

private val logger = KotlinLogging.logger {}

/**
 * Mongging 0 (Leader) - Ggumtle excavation scenario
 */
object Mongging0Scenario : RoleScenario {
    override suspend fun GamePhaseBuilder.execute() {
        logger.info { "Player ${player.id}: Starting Mongging0 scenario (Ggumtle excavation)" }

        val ggumtle = player.mapData.ggumtles.firstOrNull()
        if (ggumtle == null) {
            logger.warn { "Player ${player.id}: No ggumtles in map, falling back to movement" }
//            continuousMove(duration = 10.seconds)
            return
        }

        logger.info { "Player ${player.id}: Found ggumtle ${ggumtle.id} at (${ggumtle.x}, ${ggumtle.y}, ${ggumtle.z})" }

        // Move to ggumtle position
        moveToGgumtle(ggumtle.id)
        wait(500.milliseconds)

        // Dig up ggumtle and wait for response
        val result = digUpGgumtleAndWait(ggumtle.id)
        logger.info { "Player ${player.id}: Dig result = $result (success=${result.isSuccess})" }

        // Hold dig for a while
        wait(10.seconds)

        // Feed ggumtle
        feedGgumtle(ggumtle.id, 5.seconds)

        // Stop digging
//        stopDigging()
        logger.info { "Player ${player.id}: Completed Mongging0 scenario" }
    }
}
