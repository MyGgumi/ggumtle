package com.ggumtle.ggumtle.presentation;

import java.util.List;

public record SendSocketEvent(
        List<Long> memberIds,
        Object data
) {
}
