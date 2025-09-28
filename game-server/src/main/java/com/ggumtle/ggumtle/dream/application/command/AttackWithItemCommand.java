package com.ggumtle.ggumtle.dream.application.command;

public record AttackWithItemCommand(
        int effectX, int effectY, int effectZ,
        int itemId
) {
}
