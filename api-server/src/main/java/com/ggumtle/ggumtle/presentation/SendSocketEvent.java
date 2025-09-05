package com.ggumtle.ggumtle.presentation;

import com.ggumtle.ggumtle.common.SocketType;

import java.util.List;

public record SendSocketEvent(
        SocketType socketType,
        List<Long> memberIds,
        Object data
) {
}
