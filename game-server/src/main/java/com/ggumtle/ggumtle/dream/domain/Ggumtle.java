package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.Getter;
import lombok.ToString;

import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

@Getter
@ToString
public class Ggumtle {

    private final static int INIT_LEFT_FEED_COUNT = 30;

    private final int id;

    private final Position position;

    private AtomicInteger leftFeedCount;

    private AtomicBoolean isDugUp;

    public Ggumtle(int id, Position position) {
        this.id = id;
        this.position = position;
        this.leftFeedCount = new AtomicInteger(INIT_LEFT_FEED_COUNT);
        this.isDugUp = new AtomicBoolean(false);
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
