package com.ggumtle.loadtest.scenario.definitions.mongdung

import com.ggumtle.loadtest.scenario.definitions.RoleScenario
import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder
import mu.KotlinLogging
import kotlin.time.Duration.Companion.seconds

private val logger = KotlinLogging.logger {}

/**
 * Mongdung - Attack scenario
 */
object MongdungScenario : RoleScenario {
    override suspend fun GamePhaseBuilder.execute() {
        logger.info { "Player ${player.id}: Starting Mongdung scenario (Attack)" }

        // Move around to find monggings
        continuousMove(duration = 5.seconds)

        // Attack nearby monggings
        repeat(3) { attackRound ->
            logger.debug { "Player ${player.id}: Attack round ${attackRound + 1}" }

            // Hit mongging with velocity
            hitMongging(
                targetId = 1L, // Target first mongging
                vx = 0,
                vy = 0,
                vz = 1
            )
            wait(1.seconds)

            // Use skill
            mongdungSkill(skillType = 1)
            wait(2.seconds)
        }

        // Continue moving
        continuousMove(duration = 5.seconds)

        logger.info { "Player ${player.id}: Completed Mongdung scenario" }
    }
}
