package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.USE_DEFIBRILLATOR)
public record UseDefibrillatorTickEvent(
        Channel channel
) implements TickEvent {

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
