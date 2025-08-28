package com.ggumtle.ggumtle.presentation;

import java.util.List;

public record SendSocketEvent(
        List<String> sessionIds,
        Object data
) {
}
