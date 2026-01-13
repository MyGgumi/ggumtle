package com.ggumtle.loadtest.scenario.runner

import com.ggumtle.loadtest.config.LoadTestConfig
import com.ggumtle.loadtest.metrics.MetricsCollector
import com.ggumtle.loadtest.metrics.TestReport
import com.ggumtle.loadtest.network.ConnectionPool
import com.ggumtle.loadtest.player.VirtualPlayer
import com.ggumtle.loadtest.scenario.dsl.GamePhaseBuilder
import com.ggumtle.loadtest.scenario.dsl.GroupSetupPhaseBuilder
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
        metrics.markStart(scenario.config.playerCount)

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

                    val client = connectionPool.createClient()
                    val player = VirtualPlayer(playerId, client, metrics)
                    synchronized(players) { players.add(player) }

                    logger.debug { "Created player $playerId" }
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

            // Phase 2: Group Setup
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
            }

            // Phase 3: Game
            val hasRoleScenarios = scenario.monggingScenarios.isNotEmpty() || scenario.mongdungScenario != null

            if (hasRoleScenarios) {
                // Role-based game phase
                logger.info { "Starting role-based game phase (duration: ${scenario.config.duration})" }
                logger.info { "Mongging scenarios: ${scenario.monggingScenarios.keys}, Mongdung: ${scenario.mongdungScenario != null}" }

                val gameJobs = groups.flatMap { group ->
                    // Group players by role (based on server-assigned isMongging)
                    val monggingPlayers = group.players.filter { it.isMongging }
                    val mongdungPlayers = group.players.filter { !it.isMongging }

                    logger.debug { "Group ${group.groupId}: ${monggingPlayers.size} monggings, ${mongdungPlayers.size} mongdungs" }

                    val monggingJobs = monggingPlayers.mapIndexed { monggingIndex, player ->
                        launch(SupervisorJob()) {
                            try {
                                val roleScenario = scenario.monggingScenarios[monggingIndex]
                                withTimeout(scenario.config.duration) {
                                    val builder = GamePhaseBuilder(player)
                                    if (roleScenario != null) {
                                        with(roleScenario) { builder.execute() }
                                    } else {
                                        // Fallback to default game phase
                                        scenario.gamePhase?.block?.invoke(builder)
                                    }
                                }
                            } catch (e: TimeoutCancellationException) {
                                logger.debug { "Mongging ${player.id} completed (timeout)" }
                            } catch (e: CancellationException) {
                                // Normal cancellation
                            } catch (e: Exception) {
                                logger.error(e) { "Game error for mongging ${player.id}" }
                                metrics.recordError(player.id, "game", e)
                            }
                        }
                    }

                    val mongdungJobs = mongdungPlayers.map { player ->
                        launch(SupervisorJob()) {
                            try {
                                withTimeout(scenario.config.duration) {
                                    val builder = GamePhaseBuilder(player)
                                    val roleScenario = scenario.mongdungScenario
                                    if (roleScenario != null) {
                                        with(roleScenario) { builder.execute() }
                                    } else {
                                        // Fallback to default game phase
                                        scenario.gamePhase?.block?.invoke(builder)
                                    }
                                }
                            } catch (e: TimeoutCancellationException) {
                                logger.debug { "Mongdung ${player.id} completed (timeout)" }
                            } catch (e: CancellationException) {
                                // Normal cancellation
                            } catch (e: Exception) {
                                logger.error(e) { "Game error for mongdung ${player.id}" }
                                metrics.recordError(player.id, "game", e)
                            }
                        }
                    }

                    monggingJobs + mongdungJobs
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
                logger.info { "Role-based game phase completed" }

            } else if (scenario.gamePhase != null) {
                // Default game phase (all players run same scenario)
                val phase = scenario.gamePhase
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
            metrics.markEnd()

        } finally {
            connectionPool.closeAll()
        }

        metrics.generateReport(scenario.name)
    }
}
