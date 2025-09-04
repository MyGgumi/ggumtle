package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;

import java.util.ArrayList;
import java.util.List;

public class Player {
    protected long id;

    protected int speed;

    protected List<Position> positions = new ArrayList<>();

    public Player(long id) {
        this.id = id;
    }
}
