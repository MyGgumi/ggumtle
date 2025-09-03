package com.ggumtle.ggumtle.room.application.command;

import com.ggumtle.ggumtle.common.dto.Command;

public record JoinRoomCommand(
        Long roomId
) implements Command {
}
