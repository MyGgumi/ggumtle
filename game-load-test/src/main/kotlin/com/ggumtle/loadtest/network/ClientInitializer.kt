package com.ggumtle.loadtest.network

import com.ggumtle.loadtest.protocol.Packet
import com.ggumtle.loadtest.protocol.codec.PacketDecoder
import com.ggumtle.loadtest.protocol.codec.PacketEncoder
import io.netty.channel.Channel
import io.netty.channel.ChannelInitializer
import kotlinx.coroutines.flow.MutableSharedFlow

/**
 * Initializes the Netty channel pipeline
 */
class ClientInitializer(
    private val packetFlow: MutableSharedFlow<Packet>
) : ChannelInitializer<Channel>() {

    override fun initChannel(ch: Channel) {
        ch.pipeline()
            .addLast("decoder", PacketDecoder())
            .addLast("encoder", PacketEncoder())
            .addLast("handler", ClientHandler(packetFlow))
    }
}
