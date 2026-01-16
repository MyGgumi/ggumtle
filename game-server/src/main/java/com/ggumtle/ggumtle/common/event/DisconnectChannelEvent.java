package com.ggumtle.ggumtle.common.event;

import io.netty.channel.Channel;

public record DisconnectChannelEvent(
        Channel channel
) {
}
