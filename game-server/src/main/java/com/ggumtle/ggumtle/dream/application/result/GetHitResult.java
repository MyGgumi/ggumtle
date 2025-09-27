package com.ggumtle.ggumtle.dream.application.result;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;

public record GetHitResult(
        Result result,
        int leftHp
) {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        NOT_ALIVE,
        ALIVE, KNOCK_OUT, DEAD
    }
}
