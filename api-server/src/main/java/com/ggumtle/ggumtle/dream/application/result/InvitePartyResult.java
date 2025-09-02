package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record InvitePartyResult(
        List<Long> memberIds,
        String invitationId
) {
}
