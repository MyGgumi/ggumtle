package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record InvitePartyResult(
        Long inviteeId,
        String invitationId,
        String inviteeNickname
) {
}
