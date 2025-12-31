package com.ggumtle.loadtest.cli

import com.ggumtle.loadtest.config.ConfigLoader
import com.ggumtle.loadtest.config.EnvLoader
import com.ggumtle.loadtest.scenario.dsl.scenario
import com.ggumtle.loadtest.scenario.model.Scenario
import com.ggumtle.loadtest.scenario.runner.ScenarioRunner
import com.github.ajalt.clikt.core.CliktCommand
import com.github.ajalt.clikt.parameters.options.default
import com.github.ajalt.clikt.parameters.options.option
import com.github.ajalt.clikt.parameters.types.int
import kotlinx.coroutines.runBlocking
import mu.KotlinLogging
import kotlin.time.Duration.Companion.milliseconds
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

    private val scenarioName by option("-s", "--scenario", help = "Scenario to run: default, dig-test")
        .default("default")

    override fun run() = runBlocking {
        logger.info { "Starting load test..." }
        logger.info { "Host: $host:$port, Players: $players, Duration: ${durationSeconds}s, Scenario: $scenarioName" }

        // Load config
        val fullConfig = ConfigLoader.load(config)
        val loadTestConfig = fullConfig.toLoadTestConfig().copy(
            host = host,
            port = port
        )

        // Select scenario based on option
        val testScenario = when (scenarioName) {
            "dig-test" -> createDigTestScenario()
            else -> createDefaultScenario()
        }

        // Run scenario
        val runner = ScenarioRunner(testScenario, loadTestConfig)
        val report = runner.run()

        // Print report
        report.print()
    }

    /**
     * Default scenario: dynamic room creation with continuous movement
     */
    private fun createDefaultScenario(): Scenario = scenario("Dynamic Room Load Test") {
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

    /**
     * Ggumtle dig test scenario: verify DIG_UP_RECEIVE response
     */
    private fun createDigTestScenario(): Scenario = scenario("Ggumtle Dig Test") {
        config {
            playerCount = players
            playersPerRoom = 5
            duration = durationSeconds.seconds
            rampUpDuration = 10.seconds
        }

        groupSetup {
            authenticate()
            coordinatedRoomSetup()
            sendSceneChange()
            waitForGameStart()
        }

        game {
            // Check ggumtles from map data
            val ggumtles = player.mapData.ggumtles
            if (ggumtles.isEmpty()) {
                logger.warn { "Player ${player.id}: No ggumtles in map!" }
                return@game
            }

            val targetGgumtle = ggumtles.first()
            logger.info { "Player ${player.id}: Found ggumtle ${targetGgumtle.id} at (${targetGgumtle.x}, ${targetGgumtle.y}, ${targetGgumtle.z})" }

            // 1. Move to ggumtle position
            moveToGgumtle(targetGgumtle.id)
            wait(500.milliseconds)

            // 2. Dig up ggumtle and wait for response
            val result = digUpGgumtleAndWait(targetGgumtle.id)
            logger.info { "Player ${player.id}: Dig result = $result (success=${result.isSuccess})" }

            // 3. Hold dig for a while then stop
            wait(2.seconds)
            stopDigging()
            logger.info { "Player ${player.id}: Stopped digging" }
        }

        teardown {
            disconnect()
        }
    }
}
