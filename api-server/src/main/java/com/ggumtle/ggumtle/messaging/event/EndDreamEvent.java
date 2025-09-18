package com.ggumtle.ggumtle.messaging.event;

public record EndDreamEvent(
        long roomId,
        boolean isMonggingWin
) {
}
