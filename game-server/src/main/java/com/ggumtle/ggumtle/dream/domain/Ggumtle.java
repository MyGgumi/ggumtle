package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.ToString;

import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

@ToString
public class Ggumtle {

    public static final int INIT_LEFT_FEED_COUNT = 30;
    public static final double DIG_UP_RADIUS_SQ = 10000 * 10000;
    public static final double FEED_RADIUS_SQ = 10000 * 10000;

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
        return this.position.getDistanceSquareWith(position) <= DIG_UP_RADIUS_SQ;
    }

    public boolean detectFeed(Position position) {
        return this.position.getDistanceSquareWith(position) <= FEED_RADIUS_SQ;
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
