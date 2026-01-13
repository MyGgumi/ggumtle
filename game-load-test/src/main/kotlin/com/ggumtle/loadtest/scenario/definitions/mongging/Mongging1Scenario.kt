package com.ggumtle.loadtest.scenario.definitions.mongging

import com.ggumtle.loadtest.scenario.definitions.RoleScenario
import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder
import mu.KotlinLogging
import kotlin.time.Duration.Companion.milliseconds
import kotlin.time.Duration.Companion.seconds

private val logger = KotlinLogging.logger {}

/**
 * Mongging 1 - Box item collection scenario
 */
object Mongging1Scenario : RoleScenario {
    override suspend fun GamePhaseBuilder.execute() {
        logger.info { "Player ${player.id}: Starting Mongging1 scenario (Box item collection)" }

        val box = player.mapData.boxes.firstOrNull()
        if (box == null) {
            logger.warn { "Player ${player.id}: No boxes in map, falling back to movement" }
//            continuousMove(duration = 10.seconds)
            return
        }

        logger.info { "Player ${player.id}: Found box ${box.id} at (${box.x}, ${box.y}, ${box.z})" }

        // Move to box position
        moveToBox(box.id)
        wait(500.milliseconds)

        // Open box
        openBox(box.id)
        wait(1.seconds)

        // Take first item if available
        val boxState = player.getBoxState(box.id)
        if (boxState != null && boxState.items.isNotEmpty()) {
            takeItem(box.id, 0)
            logger.info { "Player ${player.id}: Took item from box ${box.id}" }
        }

        wait(1.seconds)

        // Close box
        closeBox(box.id)
        logger.info { "Player ${player.id}: Completed Mongging1 scenario" }
    }
}
