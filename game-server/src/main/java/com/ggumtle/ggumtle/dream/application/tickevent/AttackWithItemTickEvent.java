package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.ATTACK_WITH_ITEM)
public record AttackWithItemTickEvent(
        Channel channel,
        Timestamp timestamp,
        Command command
) implements TickEvent {
    public record Command(
            int effectX, int effectY, int effectZ,
            int itemId
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.ATTACK_WITH_ITEM;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
