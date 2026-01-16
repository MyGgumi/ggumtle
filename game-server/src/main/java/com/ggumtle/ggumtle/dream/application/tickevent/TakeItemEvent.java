package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.common.annotation.TickEventType;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import io.netty.channel.Channel;

@TickEventType(type = ReceivePacketType.TAKE_ITEM_FROM_BOX)
public record TakeItemEvent(
        Channel channel,
        Long timeStamp,
        Command command
) implements TickEvent {
    public record Command(
            int boxId,
            int index
    ) {
    }

    @Override
    public ReceivePacketType type() {
        return ReceivePacketType.TAKE_ITEM_FROM_BOX;
    }

    @Override
    public void process(Dream dream) {
        dream.on(this);
    }
}
