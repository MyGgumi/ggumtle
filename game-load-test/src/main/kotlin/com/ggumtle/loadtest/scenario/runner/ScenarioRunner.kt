package com.ggumtle.loadtest.scenario.runner

import com.ggumtle.loadtest.config.LoadTestConfig
import com.ggumtle.loadtest.metrics.MetricsCollector
import com.ggumtle.loadtest.metrics.TestReport
import com.ggumtle.loadtest.network.ConnectionPool
import com.ggumtle.loadtest.player.VirtualPlayer
import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder
import com.ggumtle.loadtest.scenario.dsl.GroupSetupPhaseBuilder
import com.ggumtle.loadtest.scenario.dsl.SetupPhaseBuilder
import com.ggumtle.loadtest.scenario.dsl.TeardownPhaseBuilder
import com.ggumtle.loadtest.scenario.model.PlayerGroup
import com.ggumtle.loadtest.scenario.model.Scenario
import com.ggumtle.loadtest.util.MockTokenGenerator
import kotlinx.coroutines.*
import mu.KotlinLogging

private val logger = KotlinLogging.logger {}

/**
 * Executes a scenario with multiple virtual players
 */
class ScenarioRunner(
    private val scenario: Scenario,
    private val config: LoadTestConfig,
    private val metrics: MetricsCollector = MetricsCollector()
) {
    /**
     * Run the scenario and return a test report
     */
    suspend fun run(): TestReport = coroutineScope {
        logger.info { "Starting scenario: ${scenario.name}" }
        logger.info { "Players: ${scenario.config.playerCount}, Duration: ${scenario.config.duration}" }

        val connectionPool = ConnectionPool(config.host, config.port)
        val players = mutableListOf<VirtualPlayer>()

        try {
            // Phase 1: Create players with ramp-up
            val rampUpDelay = if (scenario.config.playerCount > 1) {
                scenario.config.rampUpDuration.inWholeMilliseconds / scenario.config.playerCount
            } else 0L

            logger.info { "Creating ${scenario.config.playerCount} players with ${rampUpDelay}ms ramp-up delay" }

            val playerJobs = (1..scenario.config.playerCount).map { playerId ->
                async {
                    if (playerId > 1) delay(rampUpDelay)

                    val roomId = assignRoomId(playerId)
                    val client = connectionPool.createClient()
                    val player = VirtualPlayer(playerId, client, roomId, metrics)
                    synchronized(players) { players.add(player) }

                    logger.debug { "Created player $playerId for room $roomId" }
                    player
                }
            }
            val createdPlayers = playerJobs.awaitAll()
            logger.info { "All ${createdPlayers.size} players created" }

            // Organize players into groups
            val groups = mutableListOf<PlayerGroup>()
            val playersPerGroup = scenario.config.playersPerRoom
            createdPlayers.chunked(playersPerGroup).forEachIndexed { groupIndex, groupPlayers ->
                val group = PlayerGroup(
                    groupId = groupIndex + 1,
                    leaderIndex = 0,
                    players = groupPlayers.toMutableList()
                )
                groups.add(group)
                logger.debug { "Created group ${group.groupId} with ${group.size} players (leader: ${group.leader?.id})" }
            }
            logger.info { "Organized ${createdPlayers.size} players into ${groups.size} groups" }

            // Phase 2: Group Setup (if defined) or Regular Setup
            scenario.groupSetupPhase?.let { phase ->
                logger.info { "Starting group setup phase" }

                val groupJobs = groups.map { group ->
                    launch {
                        // Execute setup for each player in the group
                        val playerJobs = group.players.map { player ->
                            async {
                                try {
                                    val token = MockTokenGenerator.generate(player.id)
                                    val builder = GroupSetupPhaseBuilder(group, player, token)
                                    phase.block(builder)
                                } catch (e: Exception) {
                                    logger.error(e) { "Group setup failed for player ${player.id} in group ${group.groupId}" }
                                    metrics.recordError(player.id, "groupSetup", e)
                                }
                            }
                        }
                        playerJobs.awaitAll()
                    }
                }
                groupJobs.joinAll()
                logger.info { "Group setup phase completed" }
            } ?: scenario.setupPhase?.let { phase ->
                logger.info { "Starting setup phase" }
                val setupJobs = createdPlayers.map { player ->
                    launch {
                        try {
                            val token = MockTokenGenerator.generate(player.id)
                            val builder = SetupPhaseBuilder(player, token)
                            phase.block(builder)
                        } catch (e: Exception) {
                            logger.error(e) { "Setup failed for player ${player.id}" }
                            metrics.recordError(player.id, "setup", e)
                        }
                    }
                }
                setupJobs.joinAll()
                logger.info { "Setup phase completed" }
            }

            // Phase 3: Game
            scenario.gamePhase?.let { phase ->
                logger.info { "Starting game phase (duration: ${scenario.config.duration})" }

                val gameJobs = createdPlayers.map { player ->
                    launch(SupervisorJob()) {
                        try {
                            withTimeout(scenario.config.duration) {
                                val builder = GamePhaseBuilder(player)
                                phase.block(builder)
                            }
                        } catch (e: TimeoutCancellationException) {
                            logger.debug { "Player ${player.id} completed (timeout)" }
                        } catch (e: CancellationException) {
                            // Normal cancellation
                        } catch (e: Exception) {
                            logger.error(e) { "Game error for player ${player.id}" }
                            metrics.recordError(player.id, "game", e)
                        }
                    }
                }

                // Progress reporting
                val progressJob = launch {
                    while (isActive) {
                        delay(10_000)
                        logger.info { "Progress: ${metrics.getSummary()}" }
                    }
                }

                gameJobs.joinAll()
                progressJob.cancel()
                logger.info { "Game phase completed" }
            }

            // Phase 4: Teardown
            scenario.teardownPhase?.let { phase ->
                logger.info { "Starting teardown phase" }
                createdPlayers.forEach { player ->
                    try {
                        val builder = TeardownPhaseBuilder(player)
                        phase.block(builder)
                    } catch (e: Exception) {
                        logger.warn { "Teardown error for player ${player.id}: ${e.message}" }
                    }
                }
            } ?: run {
                // Default teardown: disconnect all
                createdPlayers.forEach {
                    runCatching { it.disconnect() }
                }
            }

            logger.info { "Scenario completed" }

        } finally {
            connectionPool.closeAll()
        }

        metrics.generateReport(scenario.name)
    }

    /**
     * Assign a room ID based on player ID
     */
    private fun assignRoomId(playerId: Int): Long {
        val roomRange = scenario.config.roomIdRange
        val roomCount = (roomRange.last - roomRange.first).toInt().coerceAtLeast(1) + 1
        val roomIndex = (playerId - 1) / scenario.config.playersPerRoom % roomCount
        return roomRange.first - roomIndex
    }
}
