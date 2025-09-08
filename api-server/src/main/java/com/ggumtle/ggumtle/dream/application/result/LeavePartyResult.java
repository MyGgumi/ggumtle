package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record LeavePartyResult(
        List<Long> participantMembers,
        Long leftMemberId,
        Long newLeaderId
) {
}
