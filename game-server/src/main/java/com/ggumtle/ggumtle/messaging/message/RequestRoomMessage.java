package com.ggumtle.ggumtle.messaging.message;

import java.util.List;

public record RequestRoomMessage(
        String requestId,
        List<Player> players
) {
    public record Player(
            Long id,
            String nickname,
            Long monggingClassId,
            Integer additionalHp,
            Integer additionalTaskSpeed,
            Integer additionalHealSpeed
    ) {
    }
}
