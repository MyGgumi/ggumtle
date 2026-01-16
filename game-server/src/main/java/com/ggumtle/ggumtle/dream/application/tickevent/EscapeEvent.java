package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.ESCAPE)
public record EscapeEvent(
        Channel channel,
        Long timeStamp,
        Command command
) implements TickEvent {
    public record Command(
            int exitId
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.ESCAPE;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
