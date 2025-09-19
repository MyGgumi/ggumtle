package com.ggumtle.ggumtle.dream.presentation.response;

import com.ggumtle.ggumtle.dream.application.result.AcceptPartyInvitationResult;

public record AcceptPartyInvitationResponse(
        Long joinedMemberId,
        String joinedMemberNickname,
        Long monggingClassId,
        Integer monggingLevel
) {
    public static AcceptPartyInvitationResponse from(AcceptPartyInvitationResult result) {
        return new AcceptPartyInvitationResponse(
                result.joinedMemberId(),
                result.joinedMemberNickname(),
                result.monggingClassId(),
                result.monggingLevel()
        );
    }
}
