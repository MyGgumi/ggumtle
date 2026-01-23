package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.DIG_UP_GGUMTLE)
public record DigUpEvent(
        Channel channel,
        Timestamp timeStamp,
        Command command
) implements TickEvent {
    public record Command(
            int ggumtleId
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.DIG_UP_GGUMTLE;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
