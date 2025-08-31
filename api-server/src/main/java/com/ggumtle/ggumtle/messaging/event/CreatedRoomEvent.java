package com.ggumtle.ggumtle.messaging.event;

public record CreatedRoomEvent(
        String roomId,
        String dreamServerId
) {
}
