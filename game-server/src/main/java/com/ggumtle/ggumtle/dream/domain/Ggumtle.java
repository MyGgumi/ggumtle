package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.Getter;
import lombok.ToString;

@Getter
@ToString
public class Ggumtle {

    private final static int INIT_LEFT_FEED_COUNT = 30;

    private final int id;

    private final Position position;

    private int leftFeedCount;

    public Ggumtle(int id, Position position) {
        this.id = id;
        this.position = position;
        this.leftFeedCount = INIT_LEFT_FEED_COUNT;
    }
}
