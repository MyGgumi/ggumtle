package com.ggumtle.ggumtle.presentation;

import java.util.List;

public record SendErrorSocketEvent(
        List<Long> memberIds,
        String code,
        String message
) {
}
