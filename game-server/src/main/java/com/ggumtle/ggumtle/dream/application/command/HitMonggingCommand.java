package com.ggumtle.ggumtle.dream.application.command;

public record HitMonggingCommand(
        int vx,
        int vy,
        int vz,
        long targetId
) {
}
