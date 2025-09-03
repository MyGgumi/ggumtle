package com.ggumtle.ggumtle.messaging.message;

public record CreatedDreamMessage(
        Long roomId,
        String requestId,
        String dreamServerId
) {
}
