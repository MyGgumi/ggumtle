package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.STOP_FEED)
public record StopFeedingTickEvent(
        Channel channel,
        Timestamp timestamp
) implements TickEvent {

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
