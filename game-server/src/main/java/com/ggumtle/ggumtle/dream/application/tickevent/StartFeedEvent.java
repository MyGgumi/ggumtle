package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.START_FEED)
public record StartFeedEvent(
        Channel channel,
        Long timeStamp,
        Command command
) implements TickEvent {
    public record Command(
            int ggumtleId
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.START_FEED;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
