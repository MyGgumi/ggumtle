package com.ggumtle.ggumtle.dream.presentation.response;

import com.ggumtle.ggumtle.dream.application.result.GetInvitationsResult;

import java.util.List;

public record GetInvitationsResponse(
        List<Invitation> invitations
) {
    public static GetInvitationsResponse from(GetInvitationsResult result) {
        return new GetInvitationsResponse(
                result.invitations().stream()
                        .map(invitation -> new Invitation(
                                invitation.invitationId(),
                                invitation.inviterNickname()
                        ))
                        .toList()
        );
    }

    public record Invitation(
            String invitationId,
            String inviterNickname
    ){}
}
