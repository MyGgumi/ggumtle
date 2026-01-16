package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.START_REVIVE)
public record StartReviveEvent(
        Channel channel,
        Long timeStamp,
        Command command
) implements TickEvent {
    public record Command(
            long targetMonggingId
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.START_REVIVE;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
