package com.ggumtle.ggumtle.messaging.message;

import java.io.Serializable;
import java.util.List;

public record RequestRoomMessage(
        List<Long> playerIds
) implements Serializable {
}
