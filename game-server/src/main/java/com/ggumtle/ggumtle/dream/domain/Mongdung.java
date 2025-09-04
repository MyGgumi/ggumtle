package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;

public class Mongdung extends Player {
    private static final int BASE_MOVE_SPEED = 100;

    protected int moveSpeed;

    public Mongdung(long id, Position position) {
        super(id, position);

        this.moveSpeed = BASE_MOVE_SPEED;
    }
}
