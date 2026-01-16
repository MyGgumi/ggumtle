package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.STOP_REVIVE)
public record StopReviveEvent(
        Channel channel,
        Long timeStamp
) implements TickEvent {

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.STOP_REVIVE;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
