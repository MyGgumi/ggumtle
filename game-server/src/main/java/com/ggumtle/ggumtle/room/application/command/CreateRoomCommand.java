package com.ggumtle.ggumtle.room.application.command;

import java.util.List;

public record CreateRoomCommand(
        List<PlayerInfoCommand> players
) {
    public record PlayerInfoCommand(
            long playerId,
            long monggingClassId,
            int additionalHp,
            int additionalTaskSpeed,
            int additionalHealSpeed
    ) {
    }
}
