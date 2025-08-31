package com.ggumtle.ggumtle.room.command;

import com.ggumtle.ggumtle.server.Command;

public record RoomCreateCommand(
        int partyMemberCount
) implements Command {
}
