package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.ToString;

import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

@ToString
public class Ggumtle {

    public final static int INIT_LEFT_FEED_COUNT = 30;
    private static final double DIG_UP_RADIUS = 10000;
    private static final double DIG_UP_BOTTOM = 500;
    private static final double DIG_UP_TOP = 1000;

    public final int id;

    public final Position position;

    private AtomicInteger leftFeedCount;

    private AtomicBoolean isDugUp;

    public Ggumtle(int id, Position position) {
        this.id = id;
        this.position = position;
        this.leftFeedCount = new AtomicInteger(INIT_LEFT_FEED_COUNT);
        this.isDugUp = new AtomicBoolean(false);
    }

    public boolean detectDigUp(Position position) {
        int dx = position.x - this.position.x;
        int dz = position.z - this.position.z;
        int distSq = dx * dx + dz * dz;

        return (distSq <= DIG_UP_RADIUS * DIG_UP_RADIUS) && (position.y <= this.position.y + DIG_UP_TOP) && (position.y >= this.position.y - DIG_UP_BOTTOM);
    }

    public boolean isDugUp() {
        return isDugUp.get();
    }

    public int tryDigUp() {
         return this.isDugUp.compareAndSet(false, true) ? 1 : 0;
    }

    public boolean isDone() {
        return leftFeedCount.get() <= 0;
    }

    public int feed() {
        return leftFeedCount.decrementAndGet();
    }
}
