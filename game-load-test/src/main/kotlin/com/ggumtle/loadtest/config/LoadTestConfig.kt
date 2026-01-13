package com.ggumtle.loadtest.config

import kotlinx.serialization.Serializable

/**
 * Load test configuration
 */
@Serializable
data class LoadTestConfig(
    val host: String = "localhost",
    val port: Int = 9000
)

/**
 * Server configuration section
 */
@Serializable
data class ServerConfig(
    val host: String = "localhost",
    val port: Int = 9000
)

/**
 * Test configuration section
 */
@Serializable
data class TestConfig(
    val playerCount: Int = 60,
    val roomIdStart: Long = -1,
    val roomIdEnd: Long = -20,
    val playersPerRoom: Int = 3,
    val durationSeconds: Int = 300,
    val rampUpSeconds: Int = 30,
    val moveIntervalMs: Long = 16
)

/**
 * Auth configuration section
 */
@Serializable
data class AuthConfig(
    val useMockToken: Boolean = true,
    val mockTokenPrefix: String = "mock-player-"
)

/**
 * Full YAML configuration
 */
@Serializable
data class FullConfig(
    val server: ServerConfig = ServerConfig(),
    val test: TestConfig = TestConfig(),
    val auth: AuthConfig = AuthConfig()
) {
    fun toLoadTestConfig(): LoadTestConfig {
        return LoadTestConfig(
            host = server.host,
            port = server.port
        )
    }
}
