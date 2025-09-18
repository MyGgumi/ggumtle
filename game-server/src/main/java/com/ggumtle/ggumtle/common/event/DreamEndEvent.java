package com.ggumtle.ggumtle.common.event;

public record DreamEndEvent(
        long roomId,
        boolean isMonggingWin
) {
}
