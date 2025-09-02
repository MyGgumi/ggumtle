package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record CreatePartyResult(
        List<Long> clientMemberIds,
        String partyId
) {
}
