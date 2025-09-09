package com.ggumtle.ggumtle.dream.application.event;

import java.util.List;

public record MatchingCancelledEvent(
        List<Long> memberIds,
        String message
) {
}
