package com.ggumtle.ggumtle.dream.application.command;

public record AttackWithItemCommand(
        int vx, int vy, int vz,
        int itemId
) {
}
