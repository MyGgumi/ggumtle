package com.ggumtle.ggumtle.messaging.message;

public record CreatedDreamMessage(
        String roomId,
        String dreamServerId
) {
}
