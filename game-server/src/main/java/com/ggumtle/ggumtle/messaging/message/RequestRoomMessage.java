package com.ggumtle.ggumtle.messaging.message;

import java.util.List;

public record RequestRoomMessage(
        List<Long> playerIds
) {
}
