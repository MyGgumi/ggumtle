package com.ggumtle.ggumtle.room.command;

import com.ggumtle.ggumtle.common.dto.Command;

public record RoomCreateCommand(
        int partyMemberCount
) implements Command {
}
