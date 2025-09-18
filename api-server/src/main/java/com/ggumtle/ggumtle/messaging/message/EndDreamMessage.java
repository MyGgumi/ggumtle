package com.ggumtle.ggumtle.messaging.message;

public record EndDreamMessage(
        long roomId,
        boolean isMonggingWin
) {
}
