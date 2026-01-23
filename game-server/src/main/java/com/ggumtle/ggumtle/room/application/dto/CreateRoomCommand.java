package com.ggumtle.ggumtle.room.application.dto;

import lombok.Builder;

import java.util.List;

public record CreateRoomCommand(
        Long roomId,
        List<Player> players
) {
    @Builder
    public record Player(
            long playerId,
            String nickname,
            long monggingClassId,
            int additionalHp,
            int additionalTaskSpeed,
            int additionalHealSpeed
    ) {
    }
}
