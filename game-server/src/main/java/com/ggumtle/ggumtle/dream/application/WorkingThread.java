package com.ggumtle.ggumtle.dream.application;

import lombok.RequiredArgsConstructor;

import java.util.concurrent.ScheduledFuture;

@RequiredArgsConstructor
public class WorkingThread {
    final long playerId;
    final ScheduledFuture<?> scheduledFuture;
    final ThreadType threadType;
    final int ggumtleId;
    final long targetPlayerId;

    public enum ThreadType {
        DIG_UP, FEED, REVIVE
    }

    public WorkingThread(long playerId, ScheduledFuture<?> scheduledFuture, ThreadType threadType, int ggumtleId) {
        this.playerId = playerId;
        this.scheduledFuture = scheduledFuture;
        this.threadType = threadType;
        this.ggumtleId = ggumtleId;
        this.targetPlayerId = -1;
    }

    public WorkingThread(long playerId, ScheduledFuture<?> scheduledFuture, ThreadType threadType, long targetPlayerId) {
        this.playerId = playerId;
        this.scheduledFuture = scheduledFuture;
        this.threadType = threadType;
        this.ggumtleId = -1;
        this.targetPlayerId = targetPlayerId;
    }
}
