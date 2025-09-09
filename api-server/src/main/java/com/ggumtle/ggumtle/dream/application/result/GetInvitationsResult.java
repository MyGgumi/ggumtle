package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record GetInvitationsResult(
        List<Invitation> invitations
) {
    public record Invitation(
            String invitationId,
            String inviterNickname
    ){
    }
}
