package com.ggumtle.ggumtle.dream.application.command;

public record AcceptPartyInvitationCommand(
        Long requesterId,
        String invitationId
) {
}
