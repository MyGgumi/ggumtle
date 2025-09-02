package com.ggumtle.ggumtle.dream.presentation.response;

public record AcceptPartyInvitationResponse(
        Long joinedMemberId,
        String joinedMemberNickname
) {
}
