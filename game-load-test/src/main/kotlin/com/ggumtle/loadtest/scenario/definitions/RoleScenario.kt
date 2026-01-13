package com.ggumtle.loadtest.scenario.definitions

import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder

/**
 * Role-based scenario interface for defining player-specific behaviors.
 * Each role (Mongging/Mongdung) can have its own scenario implementation.
 */
interface RoleScenario {
    /**
     * Execute the scenario for a player.
     * Called within GamePhaseBuilder context, providing access to all game actions.
     */
    suspend fun GamePhaseBuilder.execute()
}
