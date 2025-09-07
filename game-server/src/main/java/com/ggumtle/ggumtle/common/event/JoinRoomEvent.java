package com.ggumtle.ggumtle.common.event;

import com.ggumtle.ggumtle.session.Session;

public record JoinRoomEvent(
        long roomId,
        Session session
) {
}
