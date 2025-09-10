package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;

public class FakeGgumtle extends Ggumtle {

    public FakeGgumtle(int id, Position position) {
        super(id, position);
    }

    @Override
    public int tryDigUp() {
        super.tryDigUp();

        return -1;
    }
}
