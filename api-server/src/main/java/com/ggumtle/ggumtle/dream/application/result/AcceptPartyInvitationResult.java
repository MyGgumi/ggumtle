package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record AcceptPartyInvitationResult(
        List<Long> memberIds,
        Long joinedMemberId,
        String joinedMemberNickname
) {
}
