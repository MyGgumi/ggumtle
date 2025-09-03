package com.ggumtle.ggumtle.messaging.event;

public record CreatedRoomEvent(
        Long roomId,
        String requestId,
        String dreamServerId
) {
}
