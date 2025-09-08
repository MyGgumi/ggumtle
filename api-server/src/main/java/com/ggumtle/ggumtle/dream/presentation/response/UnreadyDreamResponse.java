package com.ggumtle.ggumtle.dream.presentation.response;

public record UnreadyDreamResponse(
        Long memberId,
        boolean isReady
) {
}
