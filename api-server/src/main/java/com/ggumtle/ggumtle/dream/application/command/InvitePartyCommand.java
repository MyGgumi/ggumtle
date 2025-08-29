package com.ggumtle.ggumtle.dream.application.command;

public record InvitePartyCommand(
        Long requesterId,
        Long inviteeId
) {
}
