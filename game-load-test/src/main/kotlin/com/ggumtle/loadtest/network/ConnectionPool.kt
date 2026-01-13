package com.ggumtle.loadtest.network

import io.netty.channel.EventLoopGroup
import io.netty.channel.MultiThreadIoEventLoopGroup
import io.netty.channel.nio.NioIoHandler
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import mu.KotlinLogging

private val logger = KotlinLogging.logger {}

/**
 * Manages multiple game client connections
 */
class ConnectionPool(
    private val host: String,
    private val port: Int,
    threadCount: Int = Runtime.getRuntime().availableProcessors() * 2
) {
    private val eventLoopGroup: EventLoopGroup =
        MultiThreadIoEventLoopGroup(threadCount, NioIoHandler.newFactory())
    private val clients = mutableListOf<GameClient>()
    private val mutex = Mutex()

    /**
     * Create and connect a new client
     */
    suspend fun createClient(): GameClient = mutex.withLock {
        val client = GameClient(host, port, eventLoopGroup).connect()
        clients.add(client)
        logger.debug { "Created client #${clients.size}" }
        client
    }

    /**
     * Close all connections and shutdown the event loop group
     */
    suspend fun closeAll() {
        mutex.withLock {
            clients.forEach { client ->
                runCatching { client.close() }
            }
            clients.clear()
        }
        eventLoopGroup.shutdownGracefully().sync()
        logger.info { "All connections closed" }
    }

    /**
     * Number of active connections
     */
    val activeCount: Int
        get() = clients.count { it.isConnected }

    /**
     * Total number of clients
     */
    val totalCount: Int
        get() = clients.size
}
