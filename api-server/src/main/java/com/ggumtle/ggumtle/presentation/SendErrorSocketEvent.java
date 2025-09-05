package com.ggumtle.ggumtle.presentation;

import com.ggumtle.ggumtle.common.SocketType;

import java.util.List;

public record SendErrorSocketEvent(
        SocketType socketType,
        List<Long> memberIds,
        String code,
        String message
) {
}
