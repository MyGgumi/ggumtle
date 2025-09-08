package com.ggumtle.ggumtle.dream.application;

import lombok.RequiredArgsConstructor;

import java.util.concurrent.ScheduledFuture;

@RequiredArgsConstructor
public class WorkingGgumtleThread {
    final long playerId;
    final int ggumtleId;
    final ScheduledFuture<?> scheduledFuture;
    final ThreadType threadType;

    public enum ThreadType {
        DIG_UP, FEED
    }
}
