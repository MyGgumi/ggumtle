package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record UnreadyDreamResult(
        List<Long> participantMemberIds,
        Long memberId,
        boolean isReady
) {
}
