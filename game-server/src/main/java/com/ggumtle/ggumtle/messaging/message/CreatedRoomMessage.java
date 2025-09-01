package com.ggumtle.ggumtle.messaging.message;

public record CreatedRoomMessage(
        Long roomId,
        String dreamServerId
) {
}
