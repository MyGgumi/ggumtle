package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record CancelMatchingResult(
        List<Long> participantIds,
        String message
) {
}
