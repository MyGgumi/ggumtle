package com.ggumtle.ggumtle.room.application.request;

import com.ggumtle.ggumtle.room.application.dto.CreateRoomCommand;

import java.util.List;

public record CreateRoomRequest(
        List<PlayerInfo> players
) {
    public record PlayerInfo(
            long playerId,
            long monggingClassId,
            int additionalHp,
            int additionalTaskSpeed,
            int additionalHealSpeed
    ) {
    }

    public CreateRoomCommand toCommand() {
        return new CreateRoomCommand(
                null,
                players.stream()
                        .map(p -> CreateRoomCommand.Player.builder()
                                .playerId(p.playerId)
                                .additionalHp(p.additionalHp)
                                .additionalHealSpeed(p.additionalHealSpeed)
                                .additionalTaskSpeed(p.additionalTaskSpeed)
                                .monggingClassId(p.monggingClassId)
                                .build()
                        )
                        .toList()
        );
    }
}
