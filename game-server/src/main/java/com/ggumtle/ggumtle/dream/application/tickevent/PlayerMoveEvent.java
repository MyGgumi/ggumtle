package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.PLAYER_MOVE)
public record PlayerMoveEvent(
        Channel channel,
        Timestamp timeStamp,
        Command command
) implements TickEvent {
    public record Command(
            int x,
            int y,
            int z,
            int vx,
            int vy,
            int vz
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.PLAYER_MOVE;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
