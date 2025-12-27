package com.ggumtle.loadtest.network

import com.ggumtle.loadtest.protocol.Packet
import com.ggumtle.loadtest.protocol.SendPacketType
import com.ggumtle.loadtest.protocol.body.PacketBody
import io.netty.bootstrap.Bootstrap
import io.netty.channel.Channel
import io.netty.channel.ChannelOption
import io.netty.channel.EventLoopGroup
import io.netty.channel.socket.nio.NioSocketChannel
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import mu.KotlinLogging

private val logger = KotlinLogging.logger {}

/**
 * Coroutine-aware Netty client for game server communication
 */
@OptIn(ExperimentalCoroutinesApi::class)
class GameClient(
    private val host: String,
    private val port: Int,
    private val eventLoopGroup: EventLoopGroup
) {
    private val channelDeferred = CompletableDeferred<Channel>()
    private val _packetFlow = MutableSharedFlow<Packet>(replay = 0, extraBufferCapacity = 64)

    val packetFlow: SharedFlow<Packet> = _packetFlow.asSharedFlow()

    val isConnected: Boolean
        get() = channelDeferred.isCompleted &&
                runCatching { channelDeferred.getCompleted().isActive }.getOrDefault(false)

    /**
     * Connect to the game server
     */
    suspend fun connect(): GameClient {
        val bootstrap = Bootstrap()
            .group(eventLoopGroup)
            .channel(NioSocketChannel::class.java)
            .option(ChannelOption.SO_KEEPALIVE, true)
            .option(ChannelOption.TCP_NODELAY, true)
            .handler(ClientInitializer(_packetFlow))

        val future = bootstrap.connect(host, port).awaitChannel()
        channelDeferred.complete(future)
        logger.info { "Connected to $host:$port" }
        return this
    }

    /**
     * Send a packet with type and body
     */
    suspend fun send(type: SendPacketType, body: PacketBody) {
        val packet = Packet.create(type, body)
        send(packet)
    }

    /**
     * Send an empty packet (no body)
     */
    suspend fun sendEmpty(type: SendPacketType) {
        val packet = Packet.createEmpty(type)
        send(packet)
    }

    /**
     * Send a raw packet
     */
    suspend fun send(packet: Packet) {
        val channel = channelDeferred.await()
        channel.writeAndFlush(packet).awaitComplete()
    }

    /**
     * Close the connection
     */
    suspend fun close() {
        if (channelDeferred.isCompleted) {
            runCatching {
                channelDeferred.getCompleted().close().awaitComplete()
            }
        }
        logger.debug { "Client closed" }
    }
}
