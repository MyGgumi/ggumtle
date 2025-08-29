package com.ggumtle.ggumtle.presentation;

import java.util.List;

public record SendErrorSocketEvent(
        List<String> sessionIds,
        String message
) {
}
