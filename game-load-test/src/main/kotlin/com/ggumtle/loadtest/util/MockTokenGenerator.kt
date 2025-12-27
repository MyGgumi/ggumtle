package com.ggumtle.loadtest.util

/**
 * Generates mock tokens for testing
 * The game server should be configured to accept these mock tokens in test mode
 */
object MockTokenGenerator {

    /**
     * Generate a mock token for a player
     */
    fun generate(playerId: Int): String {
        return "mock-token-player-$playerId"
    }

    /**
     * Generate a mock token with custom prefix
     */
    fun generate(playerId: Int, prefix: String): String {
        return "$prefix$playerId"
    }
}
