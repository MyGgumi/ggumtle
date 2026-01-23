package com.ggumtle.ggumtle.messaging.message;

import com.ggumtle.ggumtle.room.application.dto.CreateRoomCommand;

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

    public CreateRoomCommand toCommand() {
        return new CreateRoomCommand(
                null,
                players.stream().map(p -> CreateRoomCommand.Player.builder()
                        .playerId(p.id)
                        .monggingClassId(p.monggingClassId)
                        .additionalTaskSpeed(p.additionalTaskSpeed)
                        .additionalHealSpeed(p.additionalHealSpeed)
                        .additionalHp(p.additionalHp)
                        .build()
                ).toList()
        );
    }
}
