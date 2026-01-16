package com.ggumtle.ggumtle.dream.application.tickevent;

import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;

public interface TickEvent {
    ReceivePacketType type();

    void process(Dream dream);
}
