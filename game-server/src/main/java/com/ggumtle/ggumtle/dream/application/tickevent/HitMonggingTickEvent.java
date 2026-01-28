package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.HIT_MONGGING)
public record HitMonggingTickEvent(
        Channel channel,
        Timestamp timestamp,
        Command command
) implements TickEvent {
    public record Command(
            int vx,
            int vy,
            int vz,
            long targetId
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.HIT_MONGGING;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
