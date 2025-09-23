package com.ggumtle.ggumtle.messaging.message;

import java.util.List;

public record EndDreamMessage(
        long roomId,
        List<PlayerState> playerStates
) {
    public record PlayerState(
            long id,
            int coin
    ) {
    }
}
