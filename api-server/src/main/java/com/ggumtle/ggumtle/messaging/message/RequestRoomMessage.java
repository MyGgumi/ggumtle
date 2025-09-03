package com.ggumtle.ggumtle.messaging.message;

import java.util.List;

public record RequestRoomMessage(
        String requestId,
        List<Long> playerIds
) {
}
