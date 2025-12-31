package com.ggumtle.loadtest.cli

import com.ggumtle.loadtest.config.ConfigLoader
import com.ggumtle.loadtest.config.EnvLoader
import com.ggumtle.loadtest.scenario.dsl.scenario
import com.ggumtle.loadtest.scenario.runner.ScenarioRunner
import com.github.ajalt.clikt.core.CliktCommand
import com.github.ajalt.clikt.parameters.options.default
import com.github.ajalt.clikt.parameters.options.option
import com.github.ajalt.clikt.parameters.types.int
import kotlinx.coroutines.runBlocking
import mu.KotlinLogging
import kotlin.time.Duration.Companion.seconds

private val logger = KotlinLogging.logger {}

/**
 * Run command to execute load test scenarios
 */
class RunCommand : CliktCommand(
    name = "run",
    help = "Run a load test scenario"
) {
    private val config by option("-c", "--config", help = "Config file path")
        .default("config.yaml")

    private val host by option("-h", "--host", help = "Server host")
        .default(EnvLoader.gameServerHost)

    private val port by option("-p", "--port", help = "Server port")
        .int()
        .default(EnvLoader.gameServerPort)

    private val players by option("-n", "--players", help = "Number of players")
        .int()
        .default(5)

    private val durationSeconds by option("-d", "--duration", help = "Test duration in seconds")
        .int()
        .default(11)

    override fun run() = runBlocking {
        logger.info { "Starting load test..." }
        logger.info { "Host: $host:$port, Players: $players, Duration: ${durationSeconds}s" }

        // Load config
        val fullConfig = ConfigLoader.load(config)
        val loadTestConfig = fullConfig.toLoadTestConfig().copy(
            host = host,
            port = port
        )

        // Create default scenario with dynamic room creation
        val testScenario = scenario("Dynamic Room Load Test") {
            config {
                playerCount = players
                playersPerRoom = 5  // 5 players per group/room
                duration = durationSeconds.seconds
                rampUpDuration = 10.seconds
            }

            // Group-based setup: leader creates room, all players join
            groupSetup {
                authenticate()
                coordinatedRoomSetup()  // Leader creates room, members wait and join
                sendSceneChange()
                waitForGameStart()
            }

            game {
                // Continuous movement for the duration
                continuousMove(duration = (durationSeconds - 10).seconds)
            }

            teardown {
                disconnect()
            }
        }

        // Run scenario
        val runner = ScenarioRunner(testScenario, loadTestConfig)
        val report = runner.run()

        // Print report
        report.print()
    }
}
